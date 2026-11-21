using TMPro;
using UnityEngine;

/// <summary>About 60 seconds: grab a lemon, bag it, hand it over, pull the umbrella rope.</summary>
public class TutorialManager : MonoBehaviour
{
    public Bag PlayerBag;
    public StallItem Lemon;
    public Customer PracticeCustomer;
    public TMP_Text Hint;
    int step = -1;

    void Start()
    {
        GameManager.Instance.OnStateChanged += s =>
        {
            if (s != GameState.Tutorial) step = -1;
            if (s == GameState.Tutorial) Begin();
            else if (Hint != null) Hint.text = s == GameState.Playing && GameManager.Instance.DayIndex == 0 ? "Grab an item  >  drop it in the bag  >  bring the bag to the customer" : "";
        };
    }

    void Begin()
    {
        step = 0;
        PracticeCustomer.Arrive(new System.Collections.Generic.List<ItemType> { ItemType.Lemon }, 9999f);
        PracticeCustomer.OnServed += _ => { if (step == 2) Advance(); };
        PlayerBag.OnContentsChanged += OnBag;
        HeatManager.Instance.OnShadeChanged += on => { if (on && step == 3) Finish(); };
        Show();
    }

    void OnBag() { if (step == 1 && PlayerBag.CountOf(ItemType.Lemon) > 0) Advance(); }

    void Update()
    {
        if (step == 0 && Lemon != null && Lemon.isSelected) Advance();
    }

    void Advance() { step++; Show(); }

    void Show()
    {
        if (Hint == null) return;
        Hint.text = step switch
        {
            0 => "1. Grab the lemon",
            1 => "2. Put it in the bag",
            2 => "3. Hand the bag to the customer",
            3 => "4. Pull the umbrella rope for shade",
            _ => ""
        };
    }

    void Finish()
    {
        step = -1;
        PlayerBag.OnContentsChanged -= OnBag;
        GameManager.Instance.FinishTutorial();
    }
}
