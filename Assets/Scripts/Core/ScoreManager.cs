using System;
using UnityEngine;

/// <summary>Owns coins and the day's statistics. Raises events so the UI never has to be called directly (Observer).</summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public event Action<int> OnCoinsChanged;
    public event Action OnStatsChanged;
    public int Coins { get; private set; }
    public int Served { get; private set; }
    public int Perfect { get; private set; }
    public int WalkOuts { get; private set; }
    public int Streak { get; private set; }
    public int BestStreak { get; private set; }

    public const int PerItem = 5, FullOrderBonus = 10, MaxTip = 10, WalkOutPenalty = 5, MaxStreakBonus = 10;

    void Awake() { Instance = this; }

    public void ResetCoins()
    {
        Coins = 0; Served = 0; Perfect = 0; WalkOuts = 0; Streak = 0; BestStreak = 0;
        OnCoinsChanged?.Invoke(Coins); OnStatsChanged?.Invoke();
    }

    public void Add(int amount)
    {
        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    /// <summary>Scores a hand-over. Wrong orders are still paid per correct item but get no bonus, tip or streak.</summary>
    public int ScoreSale(int correctItems, bool fullyCorrect, float patience01)
    {
        int total = correctItems * PerItem;
        Served++;
        if (fullyCorrect)
        {
            Perfect++; Streak++; BestStreak = Mathf.Max(BestStreak, Streak);
            total += FullOrderBonus + Mathf.RoundToInt(MaxTip * Mathf.Clamp01(patience01));
            total += Mathf.Min((Streak - 1) * 2, MaxStreakBonus);   // streak bonus: perfect orders in a row pay extra
        }
        else Streak = 0;
        Add(total);
        OnStatsChanged?.Invoke();
        return total;
    }

    public void PenaliseWalkOut() { WalkOuts++; Streak = 0; Add(-WalkOutPenalty); OnStatsChanged?.Invoke(); }

    public int Stars(int target)
    {
        if (Coins >= target * 1.5f) return 3;
        if (Coins >= target * 1.25f) return 2;
        return Coins >= target ? 1 : 0;
    }
}
