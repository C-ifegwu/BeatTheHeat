using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Gives the Mixamo characters real URP/Lit materials with their extracted textures
/// (the embedded FBX materials come in untextured). Hair/eyelashes use alpha-clip, not blending (Quest friendly).
/// </summary>
public static class BeatTheHeatPeopleMaterials
{
    const string Root = "Assets/People/";

    [MenuItem("Beat the Heat/Fix People Materials")]
    public static void Fix()
    {
        if (!AssetDatabase.IsValidFolder(Root + "Materials")) AssetDatabase.CreateFolder("Assets/People", "Materials");
        foreach (var name in new[] { "Remy", "Customer_Ch06", "Customer_Ch07" }) FixOne(name);
        AssetDatabase.SaveAssets();
        Debug.Log("People materials fixed.");
    }

    static void FixOne(string character)
    {
        string fbx = Root + "Characters/" + character + ".fbx";
        var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (importer == null) return;
        string texDir = Root + "Characters/" + character + "_Textures";
        var texPaths = new List<string>(Directory.Exists(texDir)
            ? System.Array.ConvertAll(Directory.GetFiles(texDir, "*.png"), p => p.Replace('\\', '/')) : new string[0]);

        bool changed = false;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbx))
        {
            var src = obj as Material;
            if (src == null) continue;
            string matPath = Root + "Materials/" + character + "_" + src.name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            string key = Key(src.name);
            var diffuse = Find(texPaths, key, "diffuse");
            var normal = Find(texPaths, key, "normal");
            var gloss = Find(texPaths, key, "gloss");
            if (diffuse != null) { mat.SetTexture("_BaseMap", diffuse); mat.SetColor("_BaseColor", Color.white); }
            else mat.SetColor("_BaseColor", key.Contains("eyelash") ? new Color(0.05f, 0.04f, 0.04f) : new Color(0.8f, 0.8f, 0.8f));
            if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
            mat.SetFloat("_Smoothness", gloss != null ? 0.35f : 0.25f);
            mat.SetFloat("_Metallic", 0f);

            bool cutout = key.Contains("hair") || key.Contains("eyelash");
            mat.SetFloat("_Surface", 0f);
            mat.SetFloat("_AlphaClip", cutout ? 1f : 0f);
            mat.SetFloat("_Cutoff", 0.5f);
            if (cutout) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = cutout ? 2450 : 2000;
            mat.SetFloat("_Cull", cutout ? 0f : 2f);
            EditorUtility.SetDirty(mat);

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), mat);
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }

    static string Key(string matName)
    {
        string k = matName.ToLowerInvariant();
        if (k.EndsWith("mat")) k = k.Substring(0, k.Length - 3);
        int us = k.IndexOf('_');
        if (k.StartsWith("ch0") && us > 0) k = k.Substring(us + 1);   // ch06_body -> body
        return k;
    }

    static Texture2D Find(List<string> paths, string key, string kind)
    {
        string path = null;
        bool isCh = false;
        foreach (var p in paths)
        {
            string f = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            if (f.StartsWith("ch0")) isCh = true;
            if (!f.Contains(kind)) continue;
            if (f.Contains("_" + key + "_")) { path = p; break; }
        }
        if (path == null && isCh)
        {
            // Ch06/Ch07 textures are numbered: 1001 = body, 1002 = second material (hair / second body part)
            string num = (key == "body" || key == "ch06") ? "1001" : (key == "body1" || key == "hair") ? "1002" : null;
            if (num != null)
                foreach (var p in paths)
                {
                    string f = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                    if (f.Contains(num) && f.Contains(kind)) { path = p; break; }
                }
        }
        return path == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
