using UnityEngine;
using UnityEngine.XR;

/// <summary>Sound and haptic feedback in one place. Missing clips are skipped, so it works with no audio assets.</summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }
    public AudioClip CoinClink, Buzz, Rustle, Creak, Murmur;
    public AudioSource Source;

    void Awake() { Instance = this; if (Source == null) Source = gameObject.AddComponent<AudioSource>(); Source.spatialBlend = 1f;
        if (CoinClink == null) CoinClink = ProceduralAudio.Coin();
        if (Buzz == null) Buzz = ProceduralAudio.Buzz();
        if (Rustle == null) Rustle = ProceduralAudio.Rustle();
        if (Creak == null) Creak = ProceduralAudio.Creak();
    }

    void Play(AudioClip c, Vector3 at) { if (c != null) AudioSource.PlayClipAtPoint(c, at); }

    static void Haptic(XRNode hand, float amp, float dur)
    {
        var d = InputDevices.GetDeviceAtXRNode(hand);
        if (d.isValid) d.SendHapticImpulse(0, amp, dur);
    }

    static void BothHands(float amp, float dur) { Haptic(XRNode.LeftHand, amp, dur); Haptic(XRNode.RightHand, amp, dur); }

    public void Grab(Transform interactor)
    {
        bool left = interactor != null && interactor.name.ToLower().Contains("left");
        Haptic(left ? XRNode.LeftHand : XRNode.RightHand, 0.2f, 0.05f);
    }

    public void BagRustle(Vector3 at) { Play(Rustle, at); }
    public void RopeCreak(Vector3 at) { Play(Creak, at); Haptic(XRNode.LeftHand, 0.3f, 0.15f); }

    public void Sale(Vector3 at, bool perfect) { Play(CoinClink, at); BothHands(perfect ? 0.7f : 0.4f, 0.15f); }

    public void CustomerSpeaks(Vector3 at) { if (Murmur != null) Play(Murmur, at); }   // no fake babble: customers only speak real recorded lines

    public void Unhappy(Vector3 at)
    {
        Play(Buzz, at);
        BothHands(0.5f, 0.08f);
        Invoke(nameof(SecondBuzz), 0.15f);   // short double buzz
    }
    void SecondBuzz() { BothHands(0.5f, 0.08f); }
}
