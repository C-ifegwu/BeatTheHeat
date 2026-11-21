using System.Diagnostics;
using System.IO;
using UnityEditor;

/// <summary>Dev helper: runs the adb arguments found in adb_cmd.txt (project root) and writes adb_out.txt.</summary>
public static class AdbRunner
{
    [MenuItem("Beat the Heat/Run ADB Command %#j")]
    public static void Run()
    {
        string root = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
        string cmdFile = Path.Combine(root, "adb_cmd.txt"), outFile = Path.Combine(root, "adb_out.txt");
        if (!File.Exists(cmdFile)) { File.WriteAllText(outFile, "no adb_cmd.txt"); return; }
        string adb = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe");
        var sb = new System.Text.StringBuilder();
        foreach (var line in File.ReadAllLines(cmdFile))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            sb.AppendLine("$ adb " + line);
            var psi = new ProcessStartInfo(adb, line) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            using (var p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd(), e = p.StandardError.ReadToEnd();
                p.WaitForExit(30000);
                sb.AppendLine(o); if (e.Length > 0) sb.AppendLine("ERR: " + e);
            }
        }
        File.WriteAllText(outFile, sb.ToString());
        UnityEngine.Debug.Log("adb done: " + outFile);
    }
}
