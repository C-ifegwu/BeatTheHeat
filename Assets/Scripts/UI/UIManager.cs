using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Stall sign (HUD): coins, timer, heat, goal progress bar and streak. Listens to events, never polled (Observer).</summary>
public class UIManager : MonoBehaviour
{
    public TMP_Text CoinText, TimerText, DayText, HeatText, MessageText;
    public Transform ThermometerFill;     // scaled on Y by heat
    public Renderer ThermometerRenderer;

    Canvas hud;
    RectTransform barFill;
    const float BarWidth = 900f;

    void Start()
    {
        hud = CoinText != null ? CoinText.GetComponentInParent<Canvas>() : null;
        LayoutHud();
        BuildBar();
        ScoreManager.Instance.OnCoinsChanged += c => { CoinText.text = "Coins " + c; UpdateBar(); UpdateMessage(); };
        ScoreManager.Instance.OnStatsChanged += UpdateMessage;
        HeatManager.Instance.OnHeatChanged += OnHeat;
        GameManager.Instance.OnDayTick += (d, t) =>
        {
            TimerText.text = Mathf.CeilToInt(t) + "s";
            TimerText.color = t <= 15f ? new Color(1f, 0.35f, 0.3f) : Color.white;
        };
        GameManager.Instance.OnStateChanged += OnState;
        OnState(GameManager.Instance.State);
    }

    /// <summary>Lower, slightly smaller HUD so it no longer sits on top of the customers' order bubbles; message and hint get their own rows.</summary>
    void LayoutHud()
    {
        if (hud == null) return;
        var follow = hud.GetComponent<FollowHud>();
        if (follow != null) { follow.Distance = 1.1f; follow.Height = -0.34f; }
        hud.transform.localScale = Vector3.one * 0.0008f;
        if (MessageText != null)
        {
            var r = (RectTransform)MessageText.transform; r.anchoredPosition = new Vector2(0, -32); r.sizeDelta = new Vector2(960, 50);
            MessageText.enableAutoSizing = true; MessageText.fontSizeMin = 20; MessageText.fontSizeMax = 36; MessageText.enableWordWrapping = false;
        }
        var tut = Object.FindFirstObjectByType<TutorialManager>();
        if (tut != null && tut.Hint != null)
        {
            var r = (RectTransform)tut.Hint.transform; r.anchoredPosition = new Vector2(0, -88); r.sizeDelta = new Vector2(960, 50);
            tut.Hint.enableAutoSizing = true; tut.Hint.fontSizeMin = 20; tut.Hint.fontSizeMax = 32; tut.Hint.enableWordWrapping = false;
        }
    }

    void BuildBar()
    {
        if (hud == null) return;
        var bg = new GameObject("TargetBar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        bg.SetParent(hud.transform, false); bg.anchoredPosition = new Vector2(0, 16); bg.sizeDelta = new Vector2(BarWidth, 14);
        bg.GetComponent<Image>().color = new Color(1, 1, 1, 0.18f);
        barFill = new GameObject("TargetFill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        barFill.SetParent(bg, false); barFill.pivot = new Vector2(0, 0.5f); barFill.anchorMin = barFill.anchorMax = new Vector2(0, 0.5f);
        barFill.anchoredPosition = Vector2.zero; barFill.sizeDelta = new Vector2(0, 14);
        barFill.GetComponent<Image>().color = new Color(1f, 0.8f, 0.2f);
        foreach (float f in new[] { 1f / 1.5f, 1.25f / 1.5f })    // goal and 2-star markers
        {
            var tick = new GameObject("Tick", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            tick.SetParent(bg, false); tick.anchorMin = tick.anchorMax = new Vector2(0, 0.5f);
            tick.anchoredPosition = new Vector2(BarWidth * f, 0); tick.sizeDelta = new Vector2(4, 24);
            tick.GetComponent<Image>().color = Color.white;
        }
    }

    void UpdateBar()
    {
        if (barFill == null) return;
        var gm = GameManager.Instance;
        float f = Mathf.Clamp01(ScoreManager.Instance.Coins / (gm.Config.TargetCoins * 1.5f));
        barFill.sizeDelta = new Vector2(BarWidth * f, 14);
        barFill.GetComponent<Image>().color = ScoreManager.Instance.Coins >= gm.Config.TargetCoins ? new Color(0.4f, 0.9f, 0.4f) : new Color(1f, 0.8f, 0.2f);
    }

    void UpdateMessage()
    {
        var gm = GameManager.Instance;
        if (gm.State != GameState.Playing) return;
        var sc = ScoreManager.Instance;
        MessageText.text = "Goal " + gm.Config.TargetCoins + " coins" + (sc.Streak >= 2 ? "     Streak x" + sc.Streak + "  (+" + Mathf.Min((sc.Streak - 1) * 2, ScoreManager.MaxStreakBonus) + " bonus)" : "");
    }

    void OnHeat(float h)
    {
        HeatText.text = "Heat " + Mathf.RoundToInt(h);
        HeatText.color = Color.Lerp(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.25f, 0.2f), h / 100f);
        if (ThermometerFill != null)
        {
            var s = ThermometerFill.localScale; s.y = Mathf.Max(0.01f, h / 100f); ThermometerFill.localScale = s;
        }
        if (ThermometerRenderer != null) ThermometerRenderer.material.color = Color.Lerp(Color.yellow, Color.red, h / 100f);
    }

    void OnState(GameState s)
    {
        var gm = GameManager.Instance;
        if (hud != null) hud.enabled = s == GameState.Playing || s == GameState.Tutorial;   // menus replace the HUD
        DayText.text = "Day " + (gm.DayIndex + 1) + ": " + gm.Config.Name;
        switch (s)
        {
            case GameState.Menu: MessageText.text = ""; break;
            case GameState.Tutorial: MessageText.text = ""; break;
            case GameState.Playing: UpdateBar(); UpdateMessage(); MessageText.text = "Goal " + gm.Config.TargetCoins + " coins"; break;
            case GameState.Results: MessageText.text = "Day over!"; break;
        }
    }
}
