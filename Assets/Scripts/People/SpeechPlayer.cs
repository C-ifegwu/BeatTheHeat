using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Plays real recorded (neural text-to-speech) speech. Sentences are assembled at runtime from short
/// clips in Resources/Voice/&lt;voice&gt;/ so any combination of items sounds natural.
/// </summary>
public static class SpeechPlayer
{
    const float Gap = 0.10f;   // seconds of silence between fragments
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static string Slug(string t)
    {
        return Regex.Replace(t.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
    }

    static AudioClip Load(string voice, string text)
    {
        string key = voice + "/" + Slug(text);
        if (!cache.TryGetValue(key, out var c)) { c = Resources.Load<AudioClip>("Voice/" + key); if (c != null && c.loadState != AudioDataLoadState.Loaded) c.LoadAudioData(); cache[key] = c; }
        return c;
    }

    /// <summary>Joins the fragments into one clip. Returns null if no fragment could be found.</summary>
    public static AudioClip Build(string voice, IList<string> segments)
    {
        var clips = new List<AudioClip>();
        foreach (var s in segments)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            var c = Load(voice, s);
            if (c != null) clips.Add(c);
        }
        if (clips.Count == 0) return null;
        int freq = clips[0].frequency, gap = (int)(Gap * freq), total = 0;
        foreach (var c in clips) total += c.samples + gap;
        var data = new float[total];
        int pos = 0;
        foreach (var c in clips)
        {
            var d = new float[c.samples * c.channels];
            c.GetData(d, 0);
            for (int i = 0; i < c.samples; i++) data[pos + i] = d[i * c.channels];
            pos += c.samples + gap;
        }
        var result = AudioClip.Create("Speech", total, 1, freq, false);
        result.SetData(data, 0);
        return result;
    }

    /// <summary>Speaks the segments from the given source. Returns false if no recorded speech is available.</summary>
    public static bool Speak(AudioSource src, string voice, IList<string> segments, float pitch)
    {
        if (src == null) return false;
        var clip = Build(voice, segments);
        if (clip == null) return false;
        src.Stop();
        src.pitch = pitch;
        src.spatialBlend = 1f; src.minDistance = 1.5f; src.maxDistance = 12f; src.volume = 1f;
        src.clip = clip;
        src.Play();
        return true;
    }
}
