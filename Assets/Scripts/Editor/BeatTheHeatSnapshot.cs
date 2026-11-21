using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Beat the Heat > Snapshot Stall: renders test PNGs + a diagnostics report to /Snapshots.</summary>
public static class BeatTheHeatSnapshot
{
    [MenuItem("Beat the Heat/Snapshot Stall")]
    public static void Snap()
    {
        string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Snapshots");
        Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        foreach (var c in Object.FindObjectsByType<Customer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Report(c.transform, sb);
        foreach (var p in Object.FindObjectsByType<Pedestrian>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Report(p.transform, sb);
        File.WriteAllText(Path.Combine(dir, "report.txt"), sb.ToString());

        Shoot(dir, "front", new Vector3(0f, 1.6f, 1.6f), Quaternion.Euler(8f, 180f, 0f), 70f);
        Shoot(dir, "wide", new Vector3(0f, 3.2f, 5.5f), Quaternion.Euler(18f, 180f, 0f), 65f);
        Shoot(dir, "customer", new Vector3(-0.6f, 1.5f, 1.5f), Quaternion.Euler(3f, 180f, 0f), 40f);
        Shoot(dir, "street", new Vector3(0f, 1.7f, -1.0f), Quaternion.Euler(5f, 180f, 0f), 75f);
        Debug.Log("Snapshots written to " + dir);
    }

    static void Report(Transform t, StringBuilder sb)
    {
        sb.AppendLine($"== {t.name} active={t.gameObject.activeInHierarchy} pos={t.position} scale={t.lossyScale}");
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            sb.AppendLine($"  R {r.name} enabled={r.enabled} size={r.bounds.size}");
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) { sb.AppendLine("    MAT null"); continue; }
                string col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-";
                string tex = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null ? m.GetTexture("_BaseMap").name : "none";
                sb.AppendLine($"    MAT {m.name} shader={m.shader.name} col={col} map={tex} surf={(m.HasProperty("_Surface") ? m.GetFloat("_Surface") : -1)}");
            }
        }
    }

    static void Shoot(string dir, string name, Vector3 pos, Quaternion rot, float fov)
    {
        var go = new GameObject("SnapCam");
        var cam = go.AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(pos, rot);
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 120f;
        cam.clearFlags = CameraClearFlags.Skybox;
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
    }
}
