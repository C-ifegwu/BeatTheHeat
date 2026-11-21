using UnityEngine;

/// <summary>Player options, saved between sessions: volume, view height and difficulty.</summary>
public static class GameSettings
{
    public static float Volume
    {
        get { return PlayerPrefs.GetFloat("BTH_Vol", 1f); }
        set { value = Mathf.Clamp01(value); PlayerPrefs.SetFloat("BTH_Vol", value); AudioListener.volume = value; }
    }

    public static float Height
    {
        get { return PlayerPrefs.GetFloat("BTH_Height", 1.45f); }
        set { value = Mathf.Clamp(value, 0.9f, 1.9f); PlayerPrefs.SetFloat("BTH_Height", value); ApplyHeight(); }
    }

    /// <summary>0 = Easy, 1 = Normal, 2 = Hard.</summary>
    public static int Difficulty
    {
        get { return PlayerPrefs.GetInt("BTH_Diff", 1); }
        set { PlayerPrefs.SetInt("BTH_Diff", Mathf.Clamp(value, 0, 2)); }
    }

    public static string DifficultyName { get { int d = Difficulty; return d == 0 ? "Easy" : d == 2 ? "Hard" : "Normal"; } }
    public static float PatienceMult { get { int d = Difficulty; return d == 0 ? 1.35f : d == 2 ? 0.8f : 1f; } }

    public static void ApplyHeight()
    {
        var xro = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xro != null) xro.CameraYOffset = Height;
    }

    public static void ApplyAll() { AudioListener.volume = Volume; ApplyHeight(); }
}
