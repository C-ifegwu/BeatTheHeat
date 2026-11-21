using System;
using UnityEngine;

/// <summary>State machine: Menu, Tutorial, Playing, Results (+ pause). Each day lasts 2 minutes.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public event Action<GameState> OnStateChanged;
    public event Action<int, float> OnDayTick;      // day index, seconds left
    public event Action<bool> OnPauseChanged;

    public GameState State { get; private set; } = GameState.Menu;
    public int DayIndex { get; private set; }
    public float DayLength = 120f;
    public bool SkipTutorial;
    public bool IsPaused { get; private set; }

    public CustomerSpawner Spawner;
    float elapsed;

    public DayConfig Config => DayConfig.All[Mathf.Clamp(DayIndex, 0, DayConfig.All.Length - 1)];
    public int LastStars { get; private set; }
    public int BestCoins { get; private set; }
    public int RunCoins { get; private set; }
    public int RunStars { get; private set; }
    public bool HasNextDay => DayIndex + 1 < DayConfig.All.Length;
    public bool TutorialDone => PlayerPrefs.GetInt("BTH_TutDone", 0) == 1;
    public float SecondsLeft => Mathf.Max(0f, DayLength - elapsed);

    void Awake()
    {
        Instance = this;
        BestCoins = PlayerPrefs.GetInt("BTH_Best", 0);
    }

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameSettings.ApplyAll();
        var chime = new GameObject("StartChime").AddComponent<AudioSource>();   // audible "sound works" cue at launch
        chime.spatialBlend = 0f; chime.volume = 1f; chime.clip = ProceduralAudio.Coin(); chime.Play();
        SetState(GameState.Menu);     // the main menu waits for the player (Play button, or grab the bag)
    }

    public void StartGame()
    {
        DayIndex = 0; RunCoins = 0; RunStars = 0;
        if (SkipTutorial || TutorialDone) BeginDay(); else SetState(GameState.Tutorial);
    }

    public void FinishTutorial()
    {
        PlayerPrefs.SetInt("BTH_TutDone", 1); PlayerPrefs.Save();
        BeginDay();
    }

    /// <summary>Leaves the tutorial early (pause menu) and starts the first day.</summary>
    public void SkipTutorialNow()
    {
        if (State != GameState.Tutorial) return;
        SetPaused(false);
        FinishTutorial();
    }

    public void BeginDay()
    {
        elapsed = 0f;
        SetPausedInternal(false);
        if (Bag.Instance != null) Bag.Instance.ResetToHome();
        ScoreManager.Instance.ResetCoins();
        HeatManager.Instance.BeginDay(Config.MaxHeat, DayLength);
        Spawner.BeginDay(Config);
        SetState(GameState.Playing);
    }

    public void RestartDay()
    {
        if (State != GameState.Playing && State != GameState.Results) return;
        if (State == GameState.Results) { RunCoins -= ScoreManager.Instance.Coins; RunStars -= LastStars; }
        BeginDay();
    }

    void Update()
    {
#if UNITY_EDITOR
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            if (State == GameState.Menu) { SkipTutorial = true; StartGame(); }
            else if (State == GameState.Results) NextDayOrRestart();
        }
#endif
        if (State != GameState.Playing || IsPaused) return;
        elapsed += Time.deltaTime;
        HeatManager.Instance.Tick(elapsed);
        OnDayTick?.Invoke(DayIndex, Mathf.Max(0f, DayLength - elapsed));
        if (elapsed >= DayLength) EndDay();
    }

    void EndDay()
    {
        Spawner.Stop();
        var sc = ScoreManager.Instance;
        LastStars = sc.Stars(Config.TargetCoins);
        RunCoins += sc.Coins; RunStars += LastStars;
        if (sc.Coins > BestCoins) BestCoins = sc.Coins;
        PlayerPrefs.SetInt("BTH_Best", BestCoins);
        PlayerPrefs.SetInt("BTH_TotalServed", PlayerPrefs.GetInt("BTH_TotalServed", 0) + sc.Perfect);
        PlayerPrefs.SetInt("BTH_BestStreak", Mathf.Max(PlayerPrefs.GetInt("BTH_BestStreak", 0), sc.BestStreak));
        PlayerPrefs.SetInt("BTH_DaysPlayed", PlayerPrefs.GetInt("BTH_DaysPlayed", 0) + 1);
        PlayerPrefs.Save();
        if (Bag.Instance != null) Bag.Instance.ResetToHome();
        SetState(GameState.Results);
    }

    /// <summary>After the results screen: the next day, or (after the last day) a fresh run from day 1.</summary>
    public void NextDayOrRestart()
    {
        if (HasNextDay) { DayIndex++; BeginDay(); }
        else { DayIndex = 0; RunCoins = 0; RunStars = 0; BeginDay(); }
    }

    public void ToMenu()
    {
        SetPausedInternal(false);
        Spawner.Stop();
        if (Bag.Instance != null) Bag.Instance.ResetToHome();
        DayIndex = 0; RunCoins = 0; RunStars = 0;
        SetState(GameState.Menu);
    }

    public void SetPaused(bool p)
    {
        if (p && State != GameState.Playing && State != GameState.Tutorial) return;
        if (IsPaused == p) return;
        SetPausedInternal(p);
        OnPauseChanged?.Invoke(p);
    }

    void SetPausedInternal(bool p)
    {
        IsPaused = p;
        Time.timeScale = p ? 0f : 1f;
        AudioListener.pause = p;
        if (!p) OnPauseChanged?.Invoke(false);
    }

    void SetState(GameState s) { State = s; OnStateChanged?.Invoke(s); }
}
