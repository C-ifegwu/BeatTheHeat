#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Converts Standard-shader materials from imported Asset Store packs to URP Lit.</summary>
public static class BeatTheHeatMaterialFixer
{
    [MenuItem("Beat the Heat/Upgrade Imported Materials to URP")]
    public static void Upgrade()
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) { Debug.LogError("URP Lit shader not found."); return; }
        string[] folders = { "Assets/SimplePoly City - Low Poly Assets", "Assets/RPG Pack", "Assets/WoodInterior" };
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", folders))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == urp) continue;
            Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
            Color col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
            m.shader = urp;
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", col);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(m); n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Upgraded " + n + " materials to URP Lit.");
    }
}
#endif
