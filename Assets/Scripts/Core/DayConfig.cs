using UnityEngine;

[System.Serializable]
public struct DayConfig
{
    public string Name;
    public int MaxWaiting, MinItems, MaxItems, TargetCoins;
    public float MaxHeat, PatienceSeconds, SpawnGap, DrinkBias;

    public static readonly DayConfig[] All =
    {
        new DayConfig { Name = "Sunrise",        MaxWaiting = 3, MinItems = 1, MaxItems = 2, TargetCoins = 100, MaxHeat = 40,  PatienceSeconds = 45, SpawnGap = 1.2f, DrinkBias = 0.1f },
        new DayConfig { Name = "Noon",           MaxWaiting = 3, MinItems = 2, MaxItems = 3, TargetCoins = 200, MaxHeat = 80,  PatienceSeconds = 35, SpawnGap = 1.0f, DrinkBias = 0.4f },
        new DayConfig { Name = "Afternoon Rush", MaxWaiting = 3, MinItems = 3, MaxItems = 3, TargetCoins = 300, MaxHeat = 100, PatienceSeconds = 28, SpawnGap = 0.8f, DrinkBias = 0.7f },
    };
}
