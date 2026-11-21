using UnityEngine;

/// <summary>
/// Generates all game audio in code (no audio assets needed): coin clink, buzz, bag rustle, rope creak,
/// synthetic "voices" for customers, and a looping market ambience.
/// </summary>
public static class ProceduralAudio
{
    const int SR = 22050;

    static AudioClip Make(string name, float[] d)
    {
        var c = AudioClip.Create(name, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    public static AudioClip Coin()
    {
        int n = (int)(SR * 0.45f); var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float a = Mathf.Exp(-t * 9f);
            float b = t > 0.07f ? Mathf.Exp(-(t - 0.07f) * 8f) : 0f;
            d[i] = 0.35f * (Mathf.Sin(2 * Mathf.PI * 2400f * t) * a + Mathf.Sin(2 * Mathf.PI * 3200f * t) * b);
        }
        return Make("Coin", d);
    }

    public static AudioClip Buzz()
    {
        int n = (int)(SR * 0.22f); var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float sq = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 110f * t));
            d[i] = 0.25f * sq * Mathf.Clamp01(1f - t / 0.22f);
        }
        return Make("Buzz", d);
    }

    public static AudioClip Rustle()
    {
        var rnd = new System.Random(7);
        int n = (int)(SR * 0.5f); var d = new float[n]; float lp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float w = (float)(rnd.NextDouble() * 2 - 1);
            lp += (w - lp) * 0.55f;
            float env = Mathf.Clamp01(t * 30f) * Mathf.Exp(-t * 6f) * (0.6f + 0.4f * Mathf.Sin(t * 70f));
            d[i] = 0.3f * (w - lp) * env;
        }
        return Make("Rustle", d);
    }

    public static AudioClip Creak()
    {
        int n = (int)(SR * 0.6f); var d = new float[n]; float ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float f = 140f + 60f * Mathf.Sin(t * 18f) + 40f * t;
            ph += f / SR;
            float saw = 2f * (ph - Mathf.Floor(ph)) - 1f;
            d[i] = 0.18f * saw * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.6f));
        }
        return Make("Creak", d);
    }

    /// <summary>Speech-like babble: glottal pulse through two formant resonators, shaped into syllables.</summary>
    public static AudioClip Voice(float f0, int syllables, int seed)
    {
        var rnd = new System.Random(seed);
        float syl = 0.17f;
        int n = (int)(SR * syl * syllables) + SR / 8;
        var d = new float[n];
        float ph = 0;
        float y1a = 0, y2a = 0, y1b = 0, y2b = 0;
        for (int s = 0; s < syllables; s++)
        {
            float pitch = f0 * (0.9f + 0.25f * (float)rnd.NextDouble());
            float f1 = 380f + 450f * (float)rnd.NextDouble();
            float f2 = 900f + 1300f * (float)rnd.NextDouble();
            Reson(f1, 90f, out float a1, out float b1a, out float c1);
            Reson(f2, 130f, out float a2, out float b2a, out float c2);
            int start = (int)(s * syl * SR), len = (int)(syl * SR * 0.85f);
            for (int i = 0; i < len && start + i < n; i++)
            {
                float t = i / (float)len;
                float env = Mathf.Pow(Mathf.Sin(Mathf.PI * t), 1.4f);
                ph += pitch / SR; float g = ph - Mathf.Floor(ph);
                float glottal = (g < 0.4f ? g / 0.4f : 1f - (g - 0.4f) / 0.6f) * 2f - 1f;
                float ya = a1 * glottal + b1a * y1a + c1 * y2a; y2a = y1a; y1a = ya;
                float yb = a2 * glottal + b2a * y1b + c2 * y2b; y2b = y1b; y1b = yb;
                d[start + i] += (ya + 0.6f * yb) * env;
            }
        }
        float peak = 0.0001f; foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
        for (int i = 0; i < n; i++) d[i] = d[i] / peak * 0.5f;
        return Make("Voice", d);
    }

    static void Reson(float f, float bw, out float a, out float b, out float c)
    {
        c = -Mathf.Exp(-2f * Mathf.PI * bw / SR);
        b = 2f * Mathf.Exp(-Mathf.PI * bw / SR) * Mathf.Cos(2f * Mathf.PI * f / SR);
        a = 1f - b - c;
    }

    /// <summary>Ten seconds of a soft market breeze with a few distant birds. No voices at all, so an empty stall is quiet of speech. Loops seamlessly.</summary>
    public static AudioClip Ambience()
    {
        var rnd = new System.Random(11);
        int n = SR * 10; var d = new float[n]; float lp = 0, lp2 = 0, lp3 = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float w = (float)(rnd.NextDouble() * 2 - 1);
            lp += (w - lp) * 0.05f; lp2 += (lp - lp2) * 0.15f; lp3 += (w - lp3) * 0.4f;
            float swell = 0.65f + 0.2f * Mathf.Sin(2f * Mathf.PI * t / 10f * 2f) + 0.15f * Mathf.Sin(2f * Mathf.PI * t / 10f * 5f + 1f);
            d[i] = (lp2 * 2.2f + lp3 * 0.12f) * swell * 0.55f;
        }
        for (int k = 0; k < 3; k++)           // a few short distant bird chirps
        {
            int start = (int)((1.2f + k * 3.1f + (float)rnd.NextDouble()) * SR);
            float f0 = 2900f + 700f * (float)rnd.NextDouble();
            for (int c = 0; c < 3; c++)
            {
                int s0 = start + c * (int)(0.16f * SR), len = (int)(0.11f * SR);
                for (int i = 0; i < len && s0 + i < n; i++)
                {
                    float u = i / (float)len, env = Mathf.Sin(Mathf.PI * u);
                    float f = f0 + 900f * u;
                    d[s0 + i] += Mathf.Sin(2f * Mathf.PI * f * (i / (float)SR)) * env * 0.05f;
                }
            }
        }
        float peak = 0.0001f; foreach (var v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
        for (int i = 0; i < n; i++) d[i] = d[i] / peak * 0.6f;
        int fade = SR / 8;
        for (int i = 0; i < fade; i++) { float a2 = i / (float)fade; d[i] *= a2; d[n - 1 - i] *= a2; }
        return Make("Ambience", d);
    }
}
