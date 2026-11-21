#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Menu: Beat the Heat > Build Market Scene.
/// Builds the whole stall with primitives (counter, shelves, cooler, umbrella, bag, six items, three customers,
/// world-space sign) and wires every script. Swap the primitives for Asset Store models afterwards.
/// </summary>
public static class BeatTheHeatSceneBuilder
{
    [MenuItem("Beat the Heat/Build Market Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects()) if (go.name == "Main Camera") Object.DestroyImmediate(go);

        // XR rig: the full Starter Assets rig (camera + left/right controllers with grab, poke and ray interactors)
        const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        const string ActionsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
        GameObject origin = null;
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPath);
        if (rigPrefab != null) origin = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        else
        {
            Debug.LogWarning("Starter Assets rig not found, using the basic XR Origin (no controllers).");
            EditorApplication.ExecuteMenuItem("GameObject/XR/XR Origin (VR)");
            origin = GameObject.Find("XR Origin (VR)");
        }
        if (origin != null)
        {
            origin.transform.position = new Vector3(0, 0, 0.85f); origin.transform.rotation = Quaternion.Euler(0, 180f, 0);
            // Head height is fixed from the headset's start pose (works seated or standing, ignores floor calibration)
            var xro = origin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (xro != null)
            {
                xro.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device;
                xro.CameraYOffset = 1.45f;
                EditorUtility.SetDirty(xro);
            }
            if (origin.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>(true) == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(ActionsPath);
                var iam = origin.AddComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>();
                if (asset != null) iam.actionAssets = new List<UnityEngine.InputSystem.InputActionAsset> { asset };
            }
        }
        EditorApplication.ExecuteMenuItem("GameObject/XR/Interaction Manager");

        var root = new GameObject("Market").transform;

        // Ground and stall
        Prim(PrimitiveType.Plane, "Ground", root, Vector3.zero, new Vector3(8, 1, 8), new Color(0.85f, 0.75f, 0.55f));
        Prim(PrimitiveType.Cube, "Counter", root, new Vector3(0, 0.9f, 0.4f), new Vector3(1.8f, 0.1f, 0.6f), new Color(0.55f, 0.35f, 0.2f));
        Prim(PrimitiveType.Cube, "CounterBase", root, new Vector3(0, 0.45f, 0.4f), new Vector3(1.7f, 0.9f, 0.5f), new Color(0.45f, 0.3f, 0.18f));
        Prim(PrimitiveType.Cube, "ShelfBack", root, new Vector3(0, 1.3f, 1.6f), new Vector3(1.8f, 0.05f, 0.35f), new Color(0.5f, 0.32f, 0.2f));
        Prim(PrimitiveType.Cube, "ShelfBackLow", root, new Vector3(0, 1.05f, 1.6f), new Vector3(1.8f, 0.05f, 0.35f), new Color(0.5f, 0.32f, 0.2f));
        var umbrella = Prim(PrimitiveType.Cylinder, "Umbrella", root, new Vector3(0.9f, 2.3f, 0.5f), new Vector3(0.6f, 0.1f, 0.6f), new Color(0.9f, 0.3f, 0.2f));
        Object.DestroyImmediate(umbrella.GetComponent<Collider>());

        // Real PBR materials from Poly Haven (CC0)
        var plank = PolyMat("brown_planks_03", new Vector2(2, 1));
        var soil = PolyMat("red_laterite_soil_stones", new Vector2(30, 30));
        foreach (var n in new[] { "Counter", "CounterBase", "ShelfBack", "ShelfBackLow" }) Retex(root.Find(n), plank);
        Retex(root.Find("Ground"), soil);

        // Managers
        var mgr = new GameObject("Managers");
        var score = mgr.AddComponent<ScoreManager>();
        var heat = mgr.AddComponent<HeatManager>();
        var fb = mgr.AddComponent<FeedbackManager>();
        var gm = mgr.AddComponent<GameManager>();
        var spawner = mgr.AddComponent<CustomerSpawner>();
        var ui = mgr.AddComponent<UIManager>();
        var tut = mgr.AddComponent<TutorialManager>();
        var flow = mgr.AddComponent<GameFlowTrigger>();
        gm.Spawner = spawner;

        // Items
        var itemDefs = new (ItemType t, Color c, PrimitiveType p, Vector3 pos, Vector3 size)[]
        {
            (ItemType.Apple, Color.red,                     PrimitiveType.Sphere,   new Vector3(-0.7f, 1.38f, 1.6f), Vector3.one * 0.08f),
            (ItemType.Banana, Color.yellow,                  PrimitiveType.Capsule,  new Vector3(-0.4f, 1.38f, 1.6f), new Vector3(0.05f, 0.09f, 0.05f)),
            (ItemType.Lemon,  new Color(1f, 0.6f, 0.1f),     PrimitiveType.Sphere,   new Vector3(-0.1f, 1.38f, 1.6f), Vector3.one * 0.09f),
            (ItemType.Bread,  new Color(0.8f, 0.6f, 0.3f),   PrimitiveType.Cube,     new Vector3(0.2f, 1.38f, 1.6f),  new Vector3(0.18f, 0.07f, 0.08f)),
            (ItemType.Water,  new Color(0.4f, 0.7f, 1f),     PrimitiveType.Cylinder, new Vector3(-0.3f, 1.13f, 1.6f), new Vector3(0.06f, 0.1f, 0.06f)),
            (ItemType.Soda,   new Color(0.8f, 0.1f, 0.1f),   PrimitiveType.Cylinder, new Vector3(0.0f, 1.13f, 1.6f),  new Vector3(0.06f, 0.1f, 0.06f)),
        };
        StallItem mango = null;
        foreach (var d in itemDefs)
        {
            var go = RealOrPrim(d.t, d.p, root, d.pos, d.size, d.c);
            go.AddComponent<Rigidbody>();
            var si = go.AddComponent<StallItem>(); si.Type = d.t;
            if (d.t == ItemType.Lemon) mango = si;
        }

        // Cooler (drinks live here too, lid is a visual cue; items are also on the lower shelf for the slice)
        Prim(PrimitiveType.Cube, "Cooler", root, new Vector3(-0.75f, 1.0f, 0.45f), new Vector3(0.3f, 0.15f, 0.2f), new Color(0.3f, 0.6f, 0.9f));

        // Decorative hamburger buns on the shelf (real scan)
        var buns = LoadReal("hamburger_buns", "hamburger_buns_1k.fbx", "hamburger_buns", 0.22f, false);
        if (buns != null) { buns.transform.SetParent(root); buns.transform.position = new Vector3(0.6f, 1.33f, 1.6f); }

        // Paper bag: kraft-paper body, folded top and a BAG label, with a trigger collider the player can grab
        var bagGo = new GameObject("Bag"); bagGo.transform.SetParent(root); bagGo.transform.position = new Vector3(0.5f, 1.09f, 0.4f);
        var bagCol = bagGo.AddComponent<BoxCollider>(); bagCol.isTrigger = false; bagCol.size = new Vector3(0.24f, 0.27f, 0.15f);
        var kraft = new Color(0.72f, 0.52f, 0.3f); var kraftDark = new Color(0.55f, 0.38f, 0.2f);
        BagPart(bagGo.transform, "BagBody", new Vector3(0, 0, 0), new Vector3(0.24f, 0.27f, 0.15f), kraft);
        BagPart(bagGo.transform, "BagFold", new Vector3(0, 0.145f, 0), new Vector3(0.25f, 0.035f, 0.16f), kraftDark);
        BagPart(bagGo.transform, "BagGussetL", new Vector3(-0.121f, 0, 0), new Vector3(0.004f, 0.25f, 0.08f), kraftDark);
        BagPart(bagGo.transform, "BagGussetR", new Vector3(0.121f, 0, 0), new Vector3(0.004f, 0.25f, 0.08f), kraftDark);
        BagPart(bagGo.transform, "BagStripe", new Vector3(0, -0.02f, 0.077f), new Vector3(0.16f, 0.09f, 0.003f), new Color(0.95f, 0.92f, 0.8f));
        WorldText(bagGo.transform, "BAG", bagGo.transform.position + new Vector3(0, -0.02f, 0.082f), new Vector2(0.15f, 0.08f), new Color(0.35f, 0.18f, 0.05f), 180f);
        var bagRb = bagGo.AddComponent<Rigidbody>();
        var bag = bagGo.AddComponent<Bag>();
        var bagGrab = bagGo.AddComponent<XRGrabInteractable>();
        flow.BagGrab = bagGrab;
        tut.PlayerBag = bag; tut.Lemon = mango;

        // Shelf labels, like price tags in a shop (they face the player when turning to the shelf)
        var tagBg = new Color(0.96f, 0.95f, 0.85f); var tagFg = new Color(0.08f, 0.08f, 0.08f);
        foreach (var d in itemDefs)
        {
            float y = d.pos.y > 1.2f ? 1.25f : 1.0f;
            var tp = new Vector3(d.pos.x, y, 1.42f);
            var panel = Prim(PrimitiveType.Cube, "Label_" + d.t, root, tp, new Vector3(0.2f, 0.05f, 0.004f), tagBg);
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            WorldText(root, d.t.ToString().ToUpper(), tp + new Vector3(0, 0, -0.003f), new Vector2(0.19f, 0.045f), tagFg, 0f);
        }

        // Umbrella rope
        var rope = Prim(PrimitiveType.Capsule, "UmbrellaRope", root, new Vector3(-0.95f, 1.6f, 0.4f), new Vector3(0.04f, 0.2f, 0.04f), Color.gray);
        rope.AddComponent<Rigidbody>();
        rope.AddComponent<XRGrabInteractable>();
        var ur = rope.AddComponent<UmbrellaRope>(); ur.Umbrella = umbrella.transform;

        // Customers: real people (Mixamo), pooled, one per spot. They walk in from the street and out again.
        BeatTheHeatPeopleMaterials.Fix();
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BeatTheHeatPeopleBuilder.ControllerPath);
        string[] roster = { "Remy", "Customer_Ch06", "Customer_Ch07" };
        var spots = new List<Transform>();
        var street = new GameObject("Street").transform; street.SetParent(root);
        for (int i = 0; i < 3; i++)
        {
            var spot = new GameObject("Spot" + i).transform;
            spot.SetParent(root); spot.position = new Vector3(-0.8f + i * 0.8f, 0, -0.4f);
            spots.Add(spot);
            float side = (i % 2 == 0) ? -1f : 1f;
            var entry = new GameObject("Entry" + i).transform; entry.SetParent(street); entry.position = new Vector3(side * 9f, 0, -2.6f);
            var exit = new GameObject("Exit" + i).transform; exit.SetParent(street); exit.position = new Vector3(-side * 9f, 0, -2.6f);

            var cust = new GameObject("Customer" + i);
            cust.transform.SetParent(root); cust.transform.position = spot.position;
            var model = SpawnPerson(roster[i], cust.transform, controller);
            if (model == null)   // fallback if the people assets are not in the project
            {
                var cap = Prim(PrimitiveType.Capsule, "Body", cust.transform, spot.position + Vector3.up, new Vector3(0.5f, 0.8f, 0.5f), Color.HSVToRGB(i / 3f, 0.5f, 0.9f));
                Object.DestroyImmediate(cap.GetComponent<Collider>());
            }
            var anim = cust.AddComponent<PersonAnim>(); anim.Anim = model != null ? model.GetComponentInChildren<Animator>() : null;
            var mover = cust.AddComponent<PersonMover>();
            var c = cust.AddComponent<Customer>();
            c.VoiceId = roster[i] == "Remy" ? "remy" : roster[i] == "Customer_Ch06" ? "amy" : "danny";
            c.Mover = mover; c.Anim = anim; c.SpotPoint = spot; c.EntryPoint = entry; c.ExitPoint = exit;

            var hand = new GameObject("HandSpot").transform; hand.SetParent(cust.transform, false);
            hand.localPosition = new Vector3(0, 1.1f, 0.7f); c.HandSpot = hand;
            var hz = hand.gameObject.AddComponent<SphereCollider>(); hz.isTrigger = true; hz.radius = 0.45f;
            hand.gameObject.AddComponent<HandoverZone>().Owner = c;

            var bubbleRoot = MakeBubble(cust.transform, out var text, out var fill, out var speech);
            var ob = bubbleRoot.AddComponent<OrderBubble>(); ob.OrderText = text; ob.PatienceFill = fill; ob.SpeechText = speech;
            c.Bubble = ob;
            spawner.Pool.Add(c);
            if (i == 0) tut.PracticeCustomer = c;
            cust.SetActive(false);
        }

        // Background market-goers strolling along the street lanes (ambience only)
        string[] crowd = { "Remy", "Customer_Ch07", "Customer_Ch06", "Remy", "Customer_Ch06" };
        for (int i = 0; i < crowd.Length; i++)
        {
            var p = new GameObject("Pedestrian" + i); p.transform.SetParent(street);
            float z = -4.5f - (i % 3) * 1.4f; float dir = (i % 2 == 0) ? 1f : -1f;
            var m = SpawnPerson(crowd[i], p.transform, controller);
            if (m == null) { Object.DestroyImmediate(p); continue; }
            p.transform.position = new Vector3(-6f * dir, 0, z);
            var pa = p.AddComponent<PersonAnim>(); pa.Anim = m.GetComponentInChildren<Animator>();
            var ped = p.AddComponent<Pedestrian>();
            ped.A = new Vector3(-7f, 0, z); ped.B = new Vector3(7f, 0, z); ped.Speed = 0.9f + 0.15f * i;
            p.transform.localScale = Vector3.one * Random.Range(0.96f, 1.04f);
        }
        spawner.Spots = spots.ToArray();

        // HUD (world-space canvas that follows the player's view, top of the screen)
        var canvasGo = new GameObject("StallSign", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(root);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        canvasGo.transform.position = new Vector3(0f, 1.95f, -0.2f);
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(1000, 260);
        canvasGo.transform.localScale = Vector3.one * 0.0009f;
        canvasGo.AddComponent<FollowHud>();
        var hudBg = new GameObject("BG", typeof(Image)); hudBg.transform.SetParent(canvasGo.transform, false);
        hudBg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.6f);
        ((RectTransform)hudBg.transform).sizeDelta = new Vector2(1000, 260);
        ui.DayText = HudLabel(canvasGo.transform, "Day", new Vector2(-375, 75), 240, 38, new Color(1f, 0.9f, 0.6f));
        ui.CoinText = HudLabel(canvasGo.transform, "Coins", new Vector2(-125, 75), 240, 52, new Color(1f, 0.85f, 0.2f));
        ui.TimerText = HudLabel(canvasGo.transform, "Timer", new Vector2(125, 75), 240, 52, Color.white);
        ui.HeatText = HudLabel(canvasGo.transform, "Heat", new Vector2(375, 75), 240, 44, new Color(1f, 0.55f, 0.3f));
        ui.MessageText = HudLabel(canvasGo.transform, "Message", new Vector2(0, -50), 940, 38, Color.white);
        ((RectTransform)ui.MessageText.transform).sizeDelta = new Vector2(940, 120);
        tut.Hint = HudLabel(canvasGo.transform, "TutorialHint", new Vector2(0, -100), 940, 40, new Color(0.6f, 1f, 0.6f));

        var thermo = Prim(PrimitiveType.Cube, "Thermometer", root, new Vector3(1.0f, 1.55f, 0.3f), new Vector3(0.08f, 0.5f, 0.05f), Color.white);
        var fill2 = Prim(PrimitiveType.Cube, "ThermometerFill", thermo.transform, Vector3.zero, new Vector3(0.7f, 0.01f, 1.1f), Color.red);
        fill2.transform.localPosition = new Vector3(0, -0.5f, 0);
        ui.ThermometerFill = fill2.transform; ui.ThermometerRenderer = fill2.GetComponent<Renderer>();

        // Optional dressing from the SimplePoly City pack, if it is in the project
        // Backdrop buildings and trees, fitted by their real bounds so they never block the stall or the street
        DressFit("Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Fruits  Shop.prefab", root, -9f, -15f, 7f, 0f);
        DressFit("Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Bakery.prefab", root, 0f, -15f, 7f, 0f);
        DressFit("Assets/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building_Fast Food.prefab", root, 9f, -15f, 7f, 0f);
        DressFit("Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Fir Tree.prefab", root, -5f, -9.5f, 4.5f, 0f);
        DressFit("Assets/SimplePoly City - Low Poly Assets/Prefab/Natures/Natures_Fir Tree.prefab", root, 5.5f, -10f, 4.0f, 40f);
        Dress("Assets/RPG Pack/Prefabs/Coin.prefab", root, new Vector3(0.75f, 0.97f, 0.4f), 0f, 0.5f);

        // Directional light as the "sun"
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun != null) sun.transform.rotation = Quaternion.Euler(50, -30, 0);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Market.unity");
        Debug.Log("Beat the Heat scene built. Add the scene to Build Settings, switch to Android, enable OpenXR (Oculus Touch profile), IL2CPP, ARM64.");
    }

    [MenuItem("Beat the Heat/Apply Quest Build Settings")]
    public static void QuestSettings()
    {
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.productName = "Beat the Heat: Market Seller";
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Market.unity", true) };
        Debug.Log("Quest settings applied (IL2CPP, ARM64, API 29+). Switch platform to Android and enable OpenXR in Project Settings > XR Plug-in Management.");
    }

    static readonly Dictionary<ItemType, (string folder, string fbx, float size)> Real = new Dictionary<ItemType, (string, string, float)>
    {
        { ItemType.Apple, ("food_apple_01", "food_apple_01_1k.fbx", 0.09f) },   
        { ItemType.Banana, ("bananas", "bananas_1k.fbx", 0.22f) },
        { ItemType.Lemon,  ("lemon", "lemon_1k.fbx", 0.10f) },                   
        { ItemType.Bread,  ("croissant", "croissant_1k.fbx", 0.15f) },
    };

    static GameObject RealOrPrim(ItemType t, PrimitiveType p, Transform parent, Vector3 pos, Vector3 size, Color c)
    {
        if (Real.TryGetValue(t, out var r))
        {
            var go = LoadReal(r.folder, r.fbx, r.folder, r.size, t == ItemType.Banana);
            if (go != null) { go.name = t.ToString(); go.transform.SetParent(parent); go.transform.position = pos; return go; }
        }
        return Prim(p, t.ToString(), parent, pos, size, c);
    }

    /// <summary>Loads a Poly Haven FBX, applies its PBR material, scales it to targetSize and adds a box collider.</summary>
    static GameObject LoadReal(string folder, string fbx, string matName, float targetSize, bool keepLargestOnly)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyHaven/" + folder + "/" + fbx);
        if (prefab == null) return null;
        var wrapper = new GameObject(folder);
        var model = (GameObject)Object.Instantiate(prefab, wrapper.transform);
        var mat = PolyMat(matName, Vector2.one);
        var rends = model.GetComponentsInChildren<Renderer>();
        if (keepLargestOnly && rends.Length > 1)
        {
            Renderer best = rends[0];
            foreach (var r in rends) if (r.bounds.size.sqrMagnitude > best.bounds.size.sqrMagnitude) best = r;
            foreach (var r in rends) if (r != best) Object.DestroyImmediate(r.gameObject);
            rends = model.GetComponentsInChildren<Renderer>();
        }
        foreach (var r in rends) r.sharedMaterial = mat;
        var b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);
        float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (max > 0.0001f) model.transform.localScale *= targetSize / max;
        b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);
        var col = wrapper.AddComponent<BoxCollider>();
        col.center = wrapper.transform.InverseTransformPoint(b.center); col.size = b.size;
        return wrapper;
    }

    /// <summary>Creates (once) a URP Lit material from a Poly Haven texture set: diffuse plus normal map.</summary>
    static Material PolyMat(string name, Vector2 tiling)
    {
        string matPath = "Assets/PolyHaven/Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) { existing.SetTextureScale("_BaseMap", tiling); return existing; }
        if (!AssetDatabase.IsValidFolder("Assets/PolyHaven/Materials")) AssetDatabase.CreateFolder("Assets/PolyHaven", "Materials");
        string dir = "Assets/PolyHaven/" + name + "/textures/";
        var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + name + "_diff_1k.jpg");
        string nPath = dir + name + "_nor_gl_1k.exr";
        var ti = AssetImporter.GetAtPath(nPath) as TextureImporter;
        if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
        var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(nPath);
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (diff != null) { m.SetTexture("_BaseMap", diff); m.SetTexture("_MainTex", diff); }
        if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
        m.SetFloat("_Smoothness", 0.35f);
        m.SetTextureScale("_BaseMap", tiling);
        AssetDatabase.CreateAsset(m, matPath);
        return m;
    }

    static void Retex(Transform t, Material m)
    {
        if (t == null) return;
        var r = t.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = m;
    }

    static GameObject SpawnPerson(string name, Transform parent, RuntimeAnimatorController controller)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/People/Characters/" + name + ".fbx");
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
        if (name == "Remy") go.transform.localScale = Vector3.one * 0.5f;   // Remy FBX is exported ~2x the size of the others
        var a = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
        a.runtimeAnimatorController = controller; a.applyRootMotion = false;
        return go;
    }

    /// <summary>Instantiates a prefab, scales it to targetHeight, centres it on (x, z) and sits it on the ground.</summary>
    static void DressFit(string path, Transform parent, float x, float z, float targetHeight, float yaw)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one;
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.size.y > 0.001f) go.transform.localScale = Vector3.one * (targetHeight / b.size.y);
        b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        go.transform.position += new Vector3(x - b.center.x, -b.min.y, z - b.center.z);
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
    }

    static void Dress(string path, Transform parent, Vector3 pos, float yaw, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent); go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * scale;
    }

    static void BagPart(Transform parent, string name, Vector3 localPos, Vector3 size, Color c)
    {
        var go = Prim(PrimitiveType.Cube, name, parent, parent.position, size, c);
        go.transform.localPosition = localPos;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    /// <summary>Flat 3D text (readable from -z at rotY 0, from +z at rotY 180) that auto-sizes to its box.</summary>
    static void WorldText(Transform parent, string text, Vector3 pos, Vector2 box, Color color, float rotY)
    {
        var go = new GameObject("Text_" + text);
        go.transform.SetParent(parent); go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0) * (parent.rotation);
        var t = go.AddComponent<TextMeshPro>();
        t.text = text; t.color = color; t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.enableWordWrapping = false; t.enableAutoSizing = true; t.fontSizeMin = 0.5f; t.fontSizeMax = 12f;
        ((RectTransform)go.transform).sizeDelta = box;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        var mat = new Material(sh) { color = color };
        r.sharedMaterial = mat;
        return go;
    }

    static TMP_Text HudLabel(Transform parent, string name, Vector2 pos, float width, float size, Color color)
    {
        var t = Label(parent, name, pos, size);
        ((RectTransform)t.transform).sizeDelta = new Vector2(width, 100);
        t.color = color; t.fontStyle = FontStyles.Bold; t.text = "";
        return t;
    }

    static TMP_Text Label(Transform parent, string name, Vector2 pos, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).anchoredPosition = pos;
        ((RectTransform)go.transform).sizeDelta = new Vector2(600, 150);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.text = name; t.color = Color.white;
        return t;
    }

    static GameObject MakeBubble(Transform parent, out TMP_Text text, out Image fill, out TMP_Text speech)
    {
        var canvasGo = new GameObject("Bubble", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(parent, false);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(520, 400);
        canvasGo.transform.localPosition = new Vector3(0, 2.2f, 0);
        canvasGo.transform.localScale = Vector3.one * 0.0013f;
        var bg = new GameObject("BG", typeof(Image)); bg.transform.SetParent(canvasGo.transform, false);
        bg.GetComponent<Image>().color = new Color(1, 1, 1, 0.85f);
        ((RectTransform)bg.transform).sizeDelta = new Vector2(520, 400);
        speech = Label(canvasGo.transform, "Speech", new Vector2(0, 110), 34); speech.color = new Color(0.1f, 0.1f, 0.1f);
        ((RectTransform)speech.transform).sizeDelta = new Vector2(480, 150); speech.enableWordWrapping = true; speech.fontStyle = FontStyles.Italic;
        text = Label(canvasGo.transform, "Order", new Vector2(0, -50), 40); text.color = Color.black;
        ((RectTransform)text.transform).sizeDelta = new Vector2(480, 170);
        var bar = new GameObject("Patience", typeof(Image)); bar.transform.SetParent(canvasGo.transform, false);
        fill = bar.GetComponent<Image>();
        ((RectTransform)bar.transform).anchoredPosition = new Vector2(0, -165);
        ((RectTransform)bar.transform).sizeDelta = new Vector2(440, 28);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
        return canvasGo;
    }
}
#endif
