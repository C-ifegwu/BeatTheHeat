using System;
using UnityEngine;

/// <summary>Heat rises 0..100 over a day. Above 60 patience drains 50% faster unless the umbrella gives shade.</summary>
public class HeatManager : MonoBehaviour
{
    public static HeatManager Instance { get; private set; }
    public event Action<float> OnHeatChanged;
    public event Action<bool> OnShadeChanged;

    public float Heat { get; private set; }
    public bool InShade { get; private set; }
    public float ShadeSeconds = 15f, ShadeCooldown = 20f;
    public float HotThreshold = 60f, HotMultiplier = 1.5f;

    float maxHeat = 40f, dayLength = 120f, shadeLeft, cooldownLeft;

    public float CooldownLeft => cooldownLeft;
    public bool ShadeReady => cooldownLeft <= 0f && !InShade;

    void Awake() { Instance = this; }

    public void BeginDay(float maxHeatForDay, float lengthSeconds)
    {
        maxHeat = maxHeatForDay; dayLength = lengthSeconds;
        Heat = 0f; shadeLeft = 0f; cooldownLeft = 0f; SetShade(false);
        OnHeatChanged?.Invoke(Heat);
    }

    public void Tick(float elapsed)
    {
        Heat = Mathf.Lerp(0f, maxHeat, Mathf.Clamp01(elapsed / dayLength));
        OnHeatChanged?.Invoke(Heat);
    }

    void Update()
    {
        if (InShade) { shadeLeft -= Time.deltaTime; if (shadeLeft <= 0f) SetShade(false); }
        else if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;
    }

    public bool TryOpenUmbrella()
    {
        if (!ShadeReady) return false;
        shadeLeft = ShadeSeconds; cooldownLeft = ShadeCooldown; SetShade(true);
        return true;
    }

    void SetShade(bool on) { InShade = on; OnShadeChanged?.Invoke(on); }

    /// <summary>Multiplier applied to customer patience loss.</summary>
    public float PatienceDrainMultiplier => (!InShade && Heat > HotThreshold) ? HotMultiplier : 1f;
    /// <summary>Drinks are more popular when it is hot.</summary>
    public float DrinkBias => Mathf.Clamp01(Heat / 100f);
}
