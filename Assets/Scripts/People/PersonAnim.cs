using UnityEngine;

/// <summary>Thin wrapper around the humanoid Animator (controller built by "Beat the Heat/Setup People").</summary>
public class PersonAnim : MonoBehaviour
{
    public Animator Anim;
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
        if (Anim != null) Anim.applyRootMotion = false;
    }

    public void SetSpeed(float s)
    {
        if (Anim != null && Anim.runtimeAnimatorController != null) Anim.SetFloat(SpeedHash, s, 0.12f, Time.deltaTime);
    }

    /// <summary>Triggers: Wave, Talk, Nod, Shake, Thank, Point, Angry.</summary>
    public void Gesture(string trigger)
    {
        if (Anim != null && Anim.runtimeAnimatorController != null) { Anim.ResetTrigger(trigger); Anim.SetTrigger(trigger); }
    }

    public void Randomise(float phase) { if (Anim != null) Anim.Play(0, 0, phase); }
}
