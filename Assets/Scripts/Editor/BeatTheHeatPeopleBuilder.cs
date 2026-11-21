#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Menu: Beat the Heat > Setup People. Extracts and shrinks the Mixamo character textures (Quest-friendly),
/// then builds one shared Animator Controller from the Mixamo animation clips.
/// </summary>
public static class BeatTheHeatPeopleBuilder
{
    public const string CharDir = "Assets/People/Characters";
    public const string AnimDir = "Assets/People/Animations";
    public const string ControllerPath = "Assets/People/Customer.controller";

    [MenuItem("Beat the Heat/Setup People")]
    public static void Setup()
    {
        ExtractAndShrinkTextures();
        BuildController();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("People ready: textures capped at 1024 and Customer.controller built.");
    }

    static void ExtractAndShrinkTextures()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { CharDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            string texDir = CharDir + "/" + name + "_Textures";
            if (!AssetDatabase.IsValidFolder(texDir)) AssetDatabase.CreateFolder(CharDir, name + "_Textures");
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) continue;
            mi.ExtractTextures(texDir);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            foreach (var tg in AssetDatabase.FindAssets("t:Texture2D", new[] { texDir }))
            {
                string tp = AssetDatabase.GUIDToAssetPath(tg);
                var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
                if (ti == null) continue;
                string lower = tp.ToLower();
                if (lower.Contains("normal") || lower.Contains("_nrm")) ti.textureType = TextureImporterType.NormalMap;
                ti.maxTextureSize = 1024;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
        }
    }

    static AnimationClip Clip(string file)
    {
        string path = AnimDir + "/" + file + ".fbx";
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
    }

    static void BuildController()
    {
        if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = ctrl.layers[0].stateMachine;

        var idle = sm.AddState("Idle"); idle.motion = Clip("Idle");
        var walk = sm.AddState("Walk"); walk.motion = Clip("Walking");
        sm.defaultState = idle;

        var toWalk = idle.AddTransition(walk); toWalk.hasExitTime = false; toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = walk.AddTransition(idle); toIdle.hasExitTime = false; toIdle.duration = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // trigger name -> clip file
        var gestures = new (string trigger, string clip)[]
        {
            ("Wave", "Waving"), ("Talk", "Talking"), ("Nod", "HeadNodYes"), ("Shake", "ShakingHeadNo"),
            ("Thank", "Thankful"), ("Point", "PointingForward"), ("Angry", "AngryPoint"),
        };
        foreach (var g in gestures)
        {
            ctrl.AddParameter(g.trigger, AnimatorControllerParameterType.Trigger);
            var st = sm.AddState(g.trigger); st.motion = Clip(g.clip);
            var any = sm.AddAnyStateTransition(st); any.hasExitTime = false; any.duration = 0.15f; any.canTransitionToSelf = false;
            any.AddCondition(AnimatorConditionMode.If, 0, g.trigger);
            var back = st.AddTransition(idle); back.hasExitTime = true; back.exitTime = 0.92f; back.duration = 0.2f;
        }
        EditorUtility.SetDirty(ctrl);
    }
}
#endif
