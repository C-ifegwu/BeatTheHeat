using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quest quirk: when the headset is taken off and put back on, Android can leave the game's audio stream flagged as
/// "muted by app ops" for as long as that stream lives. A stream created after the app has settled in the foreground
/// is fine, so a few seconds after every start/resume the output stream is rebuilt and looping sounds are restarted.
/// </summary>
public class AudioRecovery : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var g = new GameObject("AudioRecovery");
        DontDestroyOnLoad(g);
        g.AddComponent<AudioRecovery>();
        // soft market breeze, always on (no voices), owned here so it survives scene loads
        var amb = g.AddComponent<AudioSource>();
        amb.clip = ProceduralAudio.Ambience();
        amb.loop = true; amb.spatialBlend = 0f; amb.volume = 0.5f; amb.playOnAwake = false;
        amb.ignoreListenerPause = false;
        amb.Play();
    }

    void Start() { Schedule(); }
    void OnApplicationPause(bool paused) { if (!paused) Schedule(); }
    void OnApplicationFocus(bool focus) { if (focus) Schedule(); }

    void Schedule() { StopAllCoroutines(); StartCoroutine(Run()); }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(6f);
        var loops = new List<KeyValuePair<AudioSource, float>>();
        foreach (var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (s != null && s.isPlaying && s.loop) loops.Add(new KeyValuePair<AudioSource, float>(s, s.time));
        AudioListener.pause = false;
        AudioSettings.Reset(AudioSettings.GetConfiguration());
        yield return null; yield return null;
        foreach (var kv in loops)
            if (kv.Key != null && !kv.Key.isPlaying) { kv.Key.time = kv.Value; kv.Key.Play(); }
    }
}
