using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// All the menus, built at run time from primitives and 3D text so they work in VR with the rig's ray/poke interactors:
/// main menu, how to play, settings, credits, pause and the end-of-day results. Point at a button and press the
/// trigger or grip. Pause with the left Menu button (or Y), or point at the PAUSE sign on the stall.
/// </summary>
public class GameMenus : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { new GameObject("GameMenus").AddComponent<GameMenus>(); }

    enum Scr { None, Main, HowTo, Settings, Credits, Pause, Results }

    class Btn
    {
        public Transform T, LabelT; public Renderer R; public Color Col;
        public System.Action OnClick; public bool Hover; public XRNode Hand = XRNode.RightHand; public bool TrigPrev; public float LastClick;
    }
    class Panel
    {
        public Scr Id; public Transform Root; public float W, H;
        public List<Btn> Btns = new List<Btn>();
        public Dictionary<string, TextMeshPro> Texts = new Dictionary<string, TextMeshPro>();
    }

    static readonly Color Navy = new Color(0.07f, 0.09f, 0.18f), Gold = new Color(1f, 0.75f, 0.15f), Cream = new Color(1f, 0.95f, 0.8f);
    static readonly Color Green = new Color(0.15f, 0.65f, 0.3f), Blue = new Color(0.15f, 0.45f, 0.8f), Red = new Color(0.8f, 0.22f, 0.2f), Orange = new Color(0.95f, 0.5f, 0.1f), Purple = new Color(0.5f, 0.3f, 0.75f);

    readonly Dictionary<Scr, Panel> panels = new Dictionary<Scr, Panel>();
    Scr active = Scr.None, settingsBack = Scr.Main;
    bool following; float snapUntil;
    Shader unlit;
    Transform pauseSign; Btn pauseBtn;
    AudioSource musicSrc, sfxSrc; AudioClip musicClip, clickClip;
    bool menuPrev;
    readonly List<(Transform t, Vector3 axis, float speed)> spinners = new List<(Transform, Vector3, float)>();
    readonly List<(Transform t, float baseY, float amp, float phase)> bobbers = new List<(Transform, float, float, float)>();
    Transform[] stars;

    // ------------------------------------------------------------------ setup
    void Start()
    {
        unlit = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
        musicClip = MakeMusic(); clickClip = MakeClick();
        musicSrc = gameObject.AddComponent<AudioSource>(); musicSrc.clip = musicClip; musicSrc.loop = true; musicSrc.volume = 0.3f; musicSrc.spatialBlend = 0f; musicSrc.ignoreListenerPause = true;
        sfxSrc = gameObject.AddComponent<AudioSource>(); sfxSrc.spatialBlend = 0f; sfxSrc.volume = 0.8f; sfxSrc.ignoreListenerPause = true;

        BuildMain(); BuildHowTo(); BuildSettings(); BuildCredits(); BuildPause(); BuildResults(); BuildPauseSign();

        var gm = GameManager.Instance;
        if (gm == null) { enabled = false; return; }
        gm.OnStateChanged += OnState;
        gm.OnPauseChanged += OnPause;
        OnState(gm.State);
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null) { gm.OnStateChanged -= OnState; gm.OnPauseChanged -= OnPause; }
    }

    Material Mat(Color c)
    {
        var m = new Material(unlit);
        m.color = c;
        return m;
    }

    GameObject Slab(Transform parent, string name, PrimitiveType type, Vector3 lp, Vector3 size, Color c, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name;
        go.transform.SetParent(parent, false); go.transform.localPosition = lp; go.transform.localScale = size;
        if (!keepCollider) Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = Mat(c);
        return go;
    }

    TextMeshPro Txt(Transform parent, string s, Vector2 pos, Vector2 box, float lineH, Color c, bool wrap = false, bool bold = true, TextAlignmentOptions al = TextAlignmentOptions.Center, float z = -0.03f)
    {
        var go = new GameObject("T_" + (s.Length > 10 ? s.Substring(0, 10) : s));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, z); go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        var t = go.AddComponent<TextMeshPro>();
        t.text = s; t.color = c; t.alignment = al; t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.enableAutoSizing = true; t.fontSizeMin = 0.3f; t.fontSizeMax = lineH * 9f;
        t.enableWordWrapping = wrap;
        t.rectTransform.sizeDelta = box;
        return t;
    }

    Panel NewPanel(Scr id, float w, float h, Color bg, string title, float titleLine = 0.1f)
    {
        var p = new Panel { Id = id, W = w, H = h };
        var root = new GameObject("Menu_" + id).transform; root.SetParent(transform, false);
        p.Root = root;
        Slab(root, "Frame", PrimitiveType.Cube, new Vector3(0, 0, 0.012f), new Vector3(w + 0.06f, h + 0.06f, 0.02f), Gold);
        Slab(root, "Bg", PrimitiveType.Cube, Vector3.zero, new Vector3(w, h, 0.02f), bg);
        if (!string.IsNullOrEmpty(title)) Txt(root, title, new Vector2(0, h / 2 - 0.13f), new Vector2(w - 0.1f, titleLine + 0.04f), titleLine, Gold);
        root.gameObject.SetActive(false);
        panels[id] = p;
        return p;
    }

    Btn AddBtn(Transform root, Panel p, string label, Vector2 pos, Vector2 size, Color col, System.Action click, float lineH = 0.065f)
    {
        var b = new Btn { Col = col, OnClick = click };
        var cube = Slab(root, "Btn_" + label, PrimitiveType.Cube, new Vector3(pos.x, pos.y, -0.03f), new Vector3(size.x, size.y, 0.04f), col, true);
        b.T = cube.transform; b.R = cube.GetComponent<Renderer>();
        var tl = Txt(root, label, pos, new Vector2(size.x - 0.04f, size.y - 0.02f), lineH, Color.white, false, true, TextAlignmentOptions.Center, -0.06f);
        b.LabelT = tl.transform;
        var si = cube.AddComponent<XRSimpleInteractable>();
        si.hoverEntered.AddListener(a => { b.Hover = true; b.Hand = HandOf(a.interactorObject.transform); Tint(b, true); });
        si.hoverExited.AddListener(a => { b.Hover = false; Tint(b, false); });
        si.selectEntered.AddListener(a => Click(b));
        if (p != null) p.Btns.Add(b);
        return b;
    }

    void Tint(Btn b, bool on)
    {
        if (b.R == null) return;
        b.R.sharedMaterial.color = on ? Color.Lerp(b.Col, Color.white, 0.35f) : b.Col;
        b.T.localScale = new Vector3(b.T.localScale.x, b.T.localScale.y, on ? 0.055f : 0.04f);
    }

    static XRNode HandOf(Transform t)
    {
        for (var x = t; x != null; x = x.parent)
        {
            string n = x.name.ToLowerInvariant();
            if (n.Contains("left")) return XRNode.LeftHand;
            if (n.Contains("right")) return XRNode.RightHand;
        }
        return XRNode.RightHand;
    }

    void Click(Btn b)
    {
        if (Time.unscaledTime - b.LastClick < 0.4f) return;
        b.LastClick = Time.unscaledTime;
        if (sfxSrc != null) sfxSrc.PlayOneShot(clickClip);
        b.OnClick?.Invoke();
    }

    static void Move(Btn b, float y)
    {
        var p = b.T.localPosition; b.T.localPosition = new Vector3(p.x, y, p.z);
        var l = b.LabelT.localPosition; b.LabelT.localPosition = new Vector3(l.x, y, l.z);
    }

    static void SetBtnVisible(Btn b, bool v) { b.T.gameObject.SetActive(v); b.LabelT.gameObject.SetActive(v); if (!v) b.Hover = false; }

    // ------------------------------------------------------------------ panels
    void BuildMain()
    {
        var p = NewPanel(Scr.Main, 1.1f, 1.4f, Navy, null);
        var r = p.Root;
        Txt(r, "BEAT THE HEAT", new Vector2(0, 0.53f), new Vector2(1.0f, 0.2f), 0.15f, new Color(1f, 0.65f, 0.15f));
        Txt(r, "MARKET SELLER", new Vector2(0, 0.40f), new Vector2(0.9f, 0.1f), 0.08f, Cream);
        Txt(r, "Serve the market. Beat the heat.", new Vector2(0, 0.31f), new Vector2(0.9f, 0.07f), 0.045f, new Color(0.45f, 0.9f, 0.85f), false, false);
        AddBtn(r, p, "PLAY", new Vector2(0, 0.15f), new Vector2(0.8f, 0.15f), Green, () => { GameManager.Instance.SkipTutorial = false; GameManager.Instance.StartGame(); }, 0.09f);
        AddBtn(r, p, "HOW TO PLAY", new Vector2(0, -0.03f), new Vector2(0.8f, 0.12f), Blue, () => Show(Scr.HowTo));
        AddBtn(r, p, "SETTINGS", new Vector2(0, -0.17f), new Vector2(0.8f, 0.12f), Purple, () => { settingsBack = Scr.Main; Show(Scr.Settings); });
        AddBtn(r, p, "CREDITS", new Vector2(0, -0.31f), new Vector2(0.8f, 0.12f), Orange, () => Show(Scr.Credits));
        AddBtn(r, p, "QUIT", new Vector2(0, -0.45f), new Vector2(0.8f, 0.12f), Red, () => Application.Quit());
        p.Texts["stats"] = Txt(r, "", new Vector2(0, -0.585f), new Vector2(1.0f, 0.07f), 0.045f, Cream, false, false);
        Txt(r, "Point at a button, press the trigger. Or grab the paper bag to start.", new Vector2(0, -0.65f), new Vector2(1.02f, 0.05f), 0.035f, new Color(0.7f, 0.75f, 0.9f), false, false);

        // sun with rotating rays, spinning coins and bobbing fruit around the board
        float top = p.H / 2;
        var sun = new GameObject("Sun").transform; sun.SetParent(r, false); sun.localPosition = new Vector3(0, top + 0.22f, 0.02f);
        Slab(sun, "Disc", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.2f, 0.2f, 0.02f), new Color(1f, 0.85f, 0.2f));
        var rays = new GameObject("Rays").transform; rays.SetParent(sun, false);
        for (int i = 0; i < 12; i++)
        {
            var ray = Slab(rays, "Ray", PrimitiveType.Cube, Vector3.zero, new Vector3(0.03f, 0.08f, 0.01f), new Color(1f, 0.7f, 0.1f));
            ray.transform.localRotation = Quaternion.Euler(0, 0, i * 30f);
            ray.transform.localPosition = ray.transform.localRotation * new Vector3(0, 0.17f, 0);
        }
        spinners.Add((rays, Vector3.forward, 25f));
        for (int s = -1; s <= 1; s += 2)
        {
            var spin = new GameObject("CoinSpin").transform; spin.SetParent(r, false); spin.localPosition = new Vector3(s * (p.W / 2 + 0.17f), 0.15f, -0.02f);
            var coin = Slab(spin, "Coin", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.012f, 0.22f), new Color(1f, 0.8f, 0.15f));
            coin.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Slab(spin, "CoinInner", PrimitiveType.Cylinder, new Vector3(0, 0, -0.01f), new Vector3(0.15f, 0.012f, 0.15f), new Color(1f, 0.92f, 0.45f)).transform.localRotation = Quaternion.Euler(90, 0, 0);
            spinners.Add((spin, Vector3.up, 110f * -s));
            var fruit = Slab(r, s < 0 ? "Apple" : "Lemon", PrimitiveType.Sphere, new Vector3(s * (p.W / 2 + 0.17f), -0.4f, -0.02f), s < 0 ? new Vector3(0.14f, 0.14f, 0.05f) : new Vector3(0.17f, 0.11f, 0.05f), s < 0 ? new Color(0.9f, 0.15f, 0.15f) : new Color(1f, 0.9f, 0.2f));
            bobbers.Add((fruit.transform, -0.4f, 0.03f, s));
            if (s < 0) { var leaf = Slab(fruit.transform, "Leaf", PrimitiveType.Cube, new Vector3(0.15f, 0.6f, 0), new Vector3(0.2f, 0.25f, 0.5f), new Color(0.2f, 0.7f, 0.25f)); leaf.transform.localRotation = Quaternion.Euler(0, 0, 30); }
        }
    }

    void BuildHowTo()
    {
        var p = NewPanel(Scr.HowTo, 1.4f, 1.3f, Navy, "HOW TO PLAY", 0.1f);
        string body =
            "1.  A customer walks up and says their order out loud. Their speech bubble lists what they want.\n" +
            "2.  Grab fruit and drinks from the shelves behind you (grip button). The shelf refills by itself.\n" +
            "3.  Let go of the item over the paper bag to pack it.\n" +
            "4.  Pick up the bag and bring it right up to the customer to hand it over.\n" +
            "5.  Correct orders served fast earn tips. Perfect orders in a row build a streak bonus.\n" +
            "6.  When it gets hot, customers lose patience faster. Pull the umbrella rope for shade.\n" +
            "7.  Reach the coin goal each day for stars. Pause any time: left Menu button, or the PAUSE sign.";
        Txt(p.Root, body, new Vector2(0, 0.06f), new Vector2(1.28f, 0.95f), 0.06f, Color.white, true, false, TextAlignmentOptions.TopLeft);
        AddBtn(p.Root, p, "BACK", new Vector2(0, -0.54f), new Vector2(0.5f, 0.12f), Blue, () => Show(Scr.Main));
    }

    void BuildSettings()
    {
        var p = NewPanel(Scr.Settings, 1.2f, 1.25f, Navy, "SETTINGS", 0.1f);
        var r = p.Root;
        Row(p, "Volume", 0.3f, "vol", () => { GameSettings.Volume -= 0.1f; RefreshSettings(); }, () => { GameSettings.Volume += 0.1f; RefreshSettings(); });
        Row(p, "View height", 0.08f, "height", () => { GameSettings.Height -= 0.05f; RefreshSettings(); }, () => { GameSettings.Height += 0.05f; RefreshSettings(); });
        Txt(r, "Raise or lower your view: seated or standing", new Vector2(0, -0.02f), new Vector2(1.1f, 0.05f), 0.035f, new Color(0.7f, 0.75f, 0.9f), false, false);
        Row(p, "Difficulty", -0.2f, "diff", () => { GameSettings.Difficulty -= 1; RefreshSettings(); }, () => { GameSettings.Difficulty += 1; RefreshSettings(); });
        Txt(r, "Harder = customers lose patience faster", new Vector2(0, -0.30f), new Vector2(1.1f, 0.05f), 0.035f, new Color(0.7f, 0.75f, 0.9f), false, false);
        AddBtn(r, p, "BACK", new Vector2(0, -0.5f), new Vector2(0.5f, 0.12f), Blue, () => Show(settingsBack));
    }

    void Row(Panel p, string label, float y, string key, System.Action minus, System.Action plus)
    {
        Txt(p.Root, label, new Vector2(-0.36f, y), new Vector2(0.45f, 0.1f), 0.07f, Cream, false, true, TextAlignmentOptions.Left);
        AddBtn(p.Root, p, "-", new Vector2(0.1f, y), new Vector2(0.13f, 0.11f), Orange, minus, 0.1f);
        p.Texts[key] = Txt(p.Root, "", new Vector2(0.28f, y), new Vector2(0.24f, 0.1f), 0.065f, Color.white);
        AddBtn(p.Root, p, "+", new Vector2(0.46f, y), new Vector2(0.13f, 0.11f), Green, plus, 0.1f);
    }

    void RefreshSettings()
    {
        var p = panels[Scr.Settings];
        p.Texts["vol"].text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
        p.Texts["height"].text = Mathf.RoundToInt((GameSettings.Height - 1.45f) * 100f) + " cm";
        p.Texts["diff"].text = GameSettings.DifficultyName;
    }

    void BuildCredits()
    {
        var p = NewPanel(Scr.Credits, 1.3f, 1.2f, Navy, "CREDITS", 0.1f);
        string body =
            "BEAT THE HEAT: MARKET SELLER\n" +
            "A VR market-stall game for Meta Quest 2\n\n" +
            "Created by Chibueze\n" +
            "ALU: AR/VR Development with Unity II\n\n" +
            "Built with Unity 6, URP, XR Interaction Toolkit and OpenXR\n" +
            "Fruit, bread and ground materials: Poly Haven (CC0)\n" +
            "City buildings: SimplePoly City. Characters and animations: Mixamo\n" +
            "Customer voices: Piper neural text-to-speech";
        Txt(p.Root, body, new Vector2(0, 0.0f), new Vector2(1.2f, 0.85f), 0.06f, Color.white, true, false);
        AddBtn(p.Root, p, "BACK", new Vector2(0, -0.49f), new Vector2(0.5f, 0.12f), Blue, () => Show(Scr.Main));
    }

    Btn pResume, pRestart, pSkip, pSettings, pMenu;
    void BuildPause()
    {
        var p = NewPanel(Scr.Pause, 0.9f, 1.15f, Navy, "PAUSED", 0.12f);
        p.Texts["info"] = Txt(p.Root, "", new Vector2(0, 0.33f), new Vector2(0.8f, 0.08f), 0.055f, Cream, false, false);
        pResume = AddBtn(p.Root, p, "RESUME", Vector2.zero, new Vector2(0.7f, 0.12f), Green, () => GameManager.Instance.SetPaused(false), 0.075f);
        pRestart = AddBtn(p.Root, p, "RESTART DAY", Vector2.zero, new Vector2(0.7f, 0.12f), Orange, () => { GameManager.Instance.RestartDay(); });
        pSkip = AddBtn(p.Root, p, "SKIP TUTORIAL", Vector2.zero, new Vector2(0.7f, 0.12f), Purple, () => GameManager.Instance.SkipTutorialNow());
        pSettings = AddBtn(p.Root, p, "SETTINGS", Vector2.zero, new Vector2(0.7f, 0.12f), Blue, () => { settingsBack = Scr.Pause; Show(Scr.Settings); });
        pMenu = AddBtn(p.Root, p, "MAIN MENU", Vector2.zero, new Vector2(0.7f, 0.12f), Red, () => GameManager.Instance.ToMenu());
    }

    void LayoutPause()
    {
        var gm = GameManager.Instance; bool tut = gm.State == GameState.Tutorial;
        SetBtnVisible(pRestart, !tut); SetBtnVisible(pSkip, tut);
        var order = new List<Btn> { pResume };
        order.Add(tut ? pSkip : pRestart); order.Add(pSettings); order.Add(pMenu);
        float y = 0.17f;
        foreach (var b in order) { Move(b, y); y -= 0.16f; }
        var sc = ScoreManager.Instance;
        panels[Scr.Pause].Texts["info"].text = tut ? "Tutorial" : "Day " + (gm.DayIndex + 1) + ": " + gm.Config.Name + "   " + sc.Coins + " / " + gm.Config.TargetCoins + " coins";
    }

    Btn rNext, rReplay, rMenu;
    void BuildResults()
    {
        var p = NewPanel(Scr.Results, 1.2f, 1.5f, Navy, null);
        var r = p.Root;
        p.Texts["title"] = Txt(r, "", new Vector2(0, 0.62f), new Vector2(1.1f, 0.14f), 0.11f, Gold);
        p.Texts["sub"] = Txt(r, "", new Vector2(0, 0.5f), new Vector2(1.1f, 0.08f), 0.055f, Cream, false, false);
        stars = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var s = Slab(r, "Star" + i, PrimitiveType.Cube, new Vector3((i - 1) * 0.28f, 0.34f, -0.02f), new Vector3(0.14f, 0.14f, 0.02f), Color.gray);
            s.transform.localRotation = Quaternion.Euler(0, 0, 45);
            var s2 = Slab(s.transform, "Spike", PrimitiveType.Cube, Vector3.zero, new Vector3(1f, 1f, 1f), Color.gray);
            s2.transform.localRotation = Quaternion.Euler(0, 0, 45);
            s2.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            stars[i] = s.transform;
        }
        p.Texts["stats"] = Txt(r, "", new Vector2(0, 0.03f), new Vector2(1.1f, 0.42f), 0.065f, Color.white, true, false);
        rNext = AddBtn(r, p, "NEXT DAY", new Vector2(0, -0.35f), new Vector2(0.8f, 0.13f), Green, () => GameManager.Instance.NextDayOrRestart(), 0.08f);
        rReplay = AddBtn(r, p, "REPLAY DAY", new Vector2(0, -0.5f), new Vector2(0.8f, 0.12f), Orange, () => GameManager.Instance.RestartDay());
        rMenu = AddBtn(r, p, "MAIN MENU", new Vector2(0, -0.64f), new Vector2(0.8f, 0.12f), Red, () => GameManager.Instance.ToMenu());
    }

    static string Rank(int stars)
    {
        if (stars >= 9) return "Heat Beater!";
        if (stars >= 6) return "Market Pro";
        if (stars >= 3) return "Street Seller";
        return "Market Rookie";
    }

    void FillResults()
    {
        var gm = GameManager.Instance; var sc = ScoreManager.Instance; var p = panels[Scr.Results];
        bool last = !gm.HasNextDay;
        bool reached = sc.Coins >= gm.Config.TargetCoins;
        p.Texts["title"].text = last ? "ALL DAYS COMPLETE!" : "DAY " + (gm.DayIndex + 1) + (reached ? " COMPLETE!" : " OVER");
        p.Texts["sub"].text = last ? "Rank: " + Rank(gm.RunStars) + "   (" + gm.RunStars + " / 9 stars, " + gm.RunCoins + " coins)"
                                   : gm.Config.Name + (reached ? ": goal reached" : ": goal missed, try again!");
        for (int i = 0; i < 3; i++)
            foreach (var rend in stars[i].GetComponentsInChildren<Renderer>()) rend.sharedMaterial.color = i < gm.LastStars ? new Color(1f, 0.85f, 0.15f) : new Color(0.3f, 0.3f, 0.35f);
        p.Texts["stats"].text =
            "Coins: " + sc.Coins + " / goal " + gm.Config.TargetCoins + "\n" +
            "Perfect orders: " + sc.Perfect + "   Orders served: " + sc.Served + "\n" +
            "Walk-outs: " + sc.WalkOuts + "   Best streak: x" + sc.BestStreak + "\n" +
            "Best day ever: " + gm.BestCoins + " coins";
        ((TextMeshPro)rNext.LabelT.GetComponent<TextMeshPro>()).text = last ? "PLAY AGAIN" : "NEXT DAY";
    }

    void BuildPauseSign()
    {
        pauseSign = new GameObject("PauseSign").transform; pauseSign.SetParent(transform, false);
        pauseSign.position = new Vector3(1.0f, 1.22f, 0.3f); pauseSign.rotation = Quaternion.LookRotation(Vector3.back);
        pauseBtn = AddBtn(pauseSign, null, "PAUSE", Vector2.zero, new Vector2(0.24f, 0.1f), new Color(0.9f, 0.55f, 0.1f), () => GameManager.Instance.SetPaused(true), 0.06f);
        pauseSign.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ state
    void OnState(GameState s)
    {
        bool play = s == GameState.Playing || s == GameState.Tutorial;
        pauseSign.gameObject.SetActive(play && !GameManager.Instance.IsPaused);
        if (s == GameState.Menu)
        {
            RefreshSettings(); RefreshStats();
            Show(Scr.Main); snapUntil = Time.unscaledTime + 3f;
            PlayMusic(true);
        }
        else if (s == GameState.Results) { FillResults(); Show(Scr.Results); snapUntil = Time.unscaledTime + 0.5f; PlayMusic(true); }
        else { Show(Scr.None); PlayMusic(false); }
    }

    void OnPause(bool paused)
    {
        var gm = GameManager.Instance;
        bool play = gm.State == GameState.Playing || gm.State == GameState.Tutorial;
        pauseSign.gameObject.SetActive(play && !paused);
        if (paused) { LayoutPause(); RefreshSettings(); Show(Scr.Pause); }
        else if (active == Scr.Pause || active == Scr.Settings) Show(Scr.None);
    }

    void RefreshStats()
    {
        panels[Scr.Main].Texts["stats"].text = "Best day: " + PlayerPrefs.GetInt("BTH_Best", 0) + " coins   Perfect orders: " + PlayerPrefs.GetInt("BTH_TotalServed", 0) + "   Best streak: x" + PlayerPrefs.GetInt("BTH_BestStreak", 0);
    }

    void Show(Scr s)
    {
        foreach (var kv in panels)
        {
            kv.Value.Root.gameObject.SetActive(kv.Key == s);
            foreach (var b in kv.Value.Btns) { b.Hover = false; Tint(b, false); }
        }
        active = s; following = false;
        if (s == Scr.Settings) RefreshSettings();
        if (s == Scr.Pause) LayoutPause();
        if (s != Scr.None) Place(panels[s], true);
    }

    void PlayMusic(bool on)
    {
        if (musicSrc == null) return;
        if (on && !musicSrc.isPlaying) musicSrc.Play();
        else if (!on && musicSrc.isPlaying) musicSrc.Stop();
    }

    // ------------------------------------------------------------------ placement and input
    static Transform Head()
    {
        var cam = Camera.main;
        if (cam == null) { float best = float.MinValue; foreach (var k in Camera.allCameras) if (k.enabled && k.depth > best) { best = k.depth; cam = k; } }
        return cam != null ? cam.transform : null;
    }

    float DistFor(Scr s) { return s == Scr.Main ? 1.35f : s == Scr.Results ? 1.3f : 1.1f; }

    Vector3 TargetFor(Transform h, float dist)
    {
        Vector3 f = h.forward; f.y = 0f;
        f = f.sqrMagnitude < 0.01f ? Vector3.back : f.normalized;
        return new Vector3(h.position.x + f.x * dist, h.position.y - 0.05f, h.position.z + f.z * dist);
    }

    void Place(Panel p, bool snap)
    {
        var h = Head(); if (h == null) return;
        var tgt = TargetFor(h, DistFor(p.Id));
        if (snap) p.Root.position = tgt; else p.Root.position = Vector3.Lerp(p.Root.position, tgt, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
        Vector3 dir = p.Root.position - h.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) p.Root.rotation = Quaternion.LookRotation(dir);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        foreach (var s in spinners) if (s.t != null) s.t.Rotate(s.axis, s.speed * Time.unscaledDeltaTime, Space.Self);
        foreach (var b in bobbers) if (b.t != null) { var lp = b.t.localPosition; lp.y = b.baseY + b.amp * Mathf.Sin(t * 2f + b.phase); b.t.localPosition = lp; }

        // pause: left Menu button (or Y), Escape in the editor
        bool menuNow = false;
        var d = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (d.isValid)
        {
            bool v;
            if (d.TryGetFeatureValue(CommonUsages.menuButton, out v) && v) menuNow = true;
            if (d.TryGetFeatureValue(CommonUsages.secondaryButton, out v) && v) menuNow = true;
        }
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.isPressed) menuNow = true;
        if (menuNow && !menuPrev)
        {
            var gm = GameManager.Instance;
            if (gm.IsPaused) gm.SetPaused(false); else gm.SetPaused(true);
        }
        menuPrev = menuNow;

        // active panel: keep in view (lazy follow), and trigger presses on the hovered button
        if (active != Scr.None && panels.TryGetValue(active, out var p))
        {
            var h = Head();
            if (h != null)
            {
                if (t < snapUntil) Place(p, true);
                else
                {
                    Vector3 to = p.Root.position - h.position; to.y = 0f;
                    Vector3 hf = h.forward; hf.y = 0f;
                    float ang = hf.sqrMagnitude > 0.01f ? Vector3.Angle(hf, to) : 0f;
                    if (!following && (ang > 50f || Mathf.Abs(p.Root.position.y - (h.position.y - 0.05f)) > 0.4f)) following = true;
                    if (following) { Place(p, false); if (ang < 8f) following = false; }
                }
            }
            PollTriggers(p.Btns);
        }
        else if (pauseSign != null && pauseSign.gameObject.activeSelf) PollTriggers(new List<Btn> { pauseBtn });
    }

    void PollTriggers(List<Btn> btns)
    {
        foreach (var b in btns)
        {
            if (!b.T.gameObject.activeInHierarchy) continue;
            bool down = false;
            if (b.Hover)
            {
                var dev = InputDevices.GetDeviceAtXRNode(b.Hand);
                if (dev.isValid) dev.TryGetFeatureValue(CommonUsages.triggerButton, out down);
            }
            if (down && !b.TrigPrev) Click(b);
            b.TrigPrev = down;
        }
    }

    // ------------------------------------------------------------------ generated audio
    static AudioClip MakeClick()
    {
        int sr = 22050, n = (int)(sr * 0.09f); var d = new float[n];
        for (int i = 0; i < n; i++) { float t = i / (float)sr; d[i] = Mathf.Sin(2 * Mathf.PI * (1100f + 600f * t) * t) * Mathf.Exp(-t * 45f) * 0.5f; }
        var c = AudioClip.Create("Click", n, 1, sr, false); c.SetData(d, 0); return c;
    }

    static AudioClip MakeMusic()
    {
        int sr = 22050; float step = 0.29f; int notes = 32;
        int n = (int)(sr * step * notes); var d = new float[n];
        float[] scale = { 261.63f, 293.66f, 329.63f, 392f, 440f, 523.25f };
        int[] mel = { 0, 2, 4, 5, 4, 2, 3, 1, 0, 2, 4, 2, 5, 4, 2, 0, 3, 4, 5, 4, 3, 2, 1, 2, 4, 2, 0, 2, 4, 5, 4, 0 };
        float[] bass = { 130.81f, 98f, 110f, 98f };
        for (int k = 0; k < notes; k++)
        {
            int s0 = (int)(k * step * sr), len = (int)(step * 1.7f * sr);
            float f = scale[mel[k]];
            for (int i = 0; i < len && s0 + i < n; i++)
            {
                float t = i / (float)sr, env = Mathf.Exp(-t * 4.5f) * Mathf.Min(1f, t * 150f);
                d[s0 + i] += (Mathf.Sin(2 * Mathf.PI * f * t) * 0.55f + Mathf.Sin(4 * Mathf.PI * f * t) * 0.15f) * env * 0.3f;
            }
            if (k % 4 == 0)
            {
                float bf = bass[(k / 8) % 4]; int blen = (int)(step * 3.6f * sr);
                for (int i = 0; i < blen && s0 + i < n; i++)
                {
                    float t = i / (float)sr, env = Mathf.Exp(-t * 2.2f) * Mathf.Min(1f, t * 60f);
                    d[s0 + i] += Mathf.Sin(2 * Mathf.PI * bf * t) * env * 0.28f;
                }
            }
        }
        int fade = sr / 8;
        for (int i = 0; i < fade; i++) { float a = i / (float)fade; d[n - 1 - i] *= a; d[i] *= a; }
        var c = AudioClip.Create("MenuMusic", n, 1, sr, false); c.SetData(d, 0); return c;
    }
}

/// <summary>Floating "+25" style text above a customer when a sale is made (or "-5" when they walk out).</summary>
public class CoinPopup : MonoBehaviour
{
    float age; TextMeshPro text; Vector3 start; Color col;

    public static void Spawn(Vector3 pos, string s, Color c)
    {
        var go = new GameObject("Popup");
        go.transform.position = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.text = s; t.color = c; t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold; t.fontSize = 1.6f;
        t.enableWordWrapping = false; t.rectTransform.sizeDelta = new Vector2(1f, 0.4f);
        var p = go.AddComponent<CoinPopup>(); p.text = t; p.start = pos; p.col = c;
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.position = start + Vector3.up * (age * 0.35f);
        var cam = Camera.main;
        if (cam != null) { Vector3 d = transform.position - cam.transform.position; if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d); }
        float a = Mathf.Clamp01(1.6f - age);
        text.color = new Color(col.r, col.g, col.b, a);
        if (age > 1.6f) Destroy(gameObject);
    }
}
