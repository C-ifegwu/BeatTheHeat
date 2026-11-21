using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// A real person: walks in from the street, greets you, says their order out loud (speech bubble + order list),
/// waits with a patience bar, reacts to what you hand over, then walks away. Pooled (three at once).
/// </summary>
public class Customer : MonoBehaviour
{
    public CustomerState State { get; private set; } = CustomerState.Left;
    public List<ItemType> Order { get; private set; } = new List<ItemType>();
    public float Patience01 { get; private set; } = 1f;
    public float PatienceSeconds = 30f;
    public Mood Mood { get; private set; }
    public Transform HandSpot;          // where the bag is handed over
    public OrderBubble Bubble;
    public Transform SpotPoint, EntryPoint, ExitPoint;
    public PersonMover Mover;
    public PersonAnim Anim;
    public string VoiceId = "amy";      // which recorded voice this person uses (remy / amy / danny)
    public AudioSource Voice;           // optional: assign a voice AudioSource, clips come from FeedbackManager if present

    public Action<Customer> OnLeft, OnServed;
    bool sighed;
    float hintUntil;
    static readonly List<Customer> Active = new List<Customer>();
    void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    const float HandoverRadius = 0.55f;
    Vector3 BodyPoint { get { return transform.position + Vector3.up * 1.1f + transform.forward * 0.15f; } }
    bool BagTouches(Bag bag) { return (bag.transform.position - BodyPoint).sqrMagnitude < HandoverRadius * HandoverRadius; }
    bool IsClosestTo(Bag bag)
    {
        float mine = (bag.transform.position - BodyPoint).sqrMagnitude;
        foreach (var c in Active)
            if (c != null && c != this && c.State == CustomerState.Waiting && (bag.transform.position - c.BodyPoint).sqrMagnitude < mine) return false;
        return true;
    }

    static float MoodPatience(Mood m)
    {
        switch (m) { case Mood.Hurried: return 0.7f; case Mood.Chatty: return 1.3f; case Mood.Grumpy: return 0.85f; default: return 1f; }
    }

    public void Arrive(List<ItemType> order, float patienceSeconds)
    {
        Order = order; Mood = (Mood)Random.Range(0, 4);
        PatienceSeconds = patienceSeconds * MoodPatience(Mood);
        Patience01 = 1f; sighed = false;
        State = CustomerState.Arriving;
        gameObject.SetActive(true);
        if (Bubble != null) Bubble.Hide();

        if (Mover != null && EntryPoint != null && SpotPoint != null)
        {
            Vector3 spot = SpotPoint.position, entry = EntryPoint.position;
            transform.position = new Vector3(entry.x, 0f, entry.z);
            transform.rotation = Quaternion.LookRotation(new Vector3(spot.x - entry.x, 0f, 0.01f));
            Mover.MoveAlong(new[] { new Vector3(spot.x, 0f, entry.z), new Vector3(spot.x, 0f, spot.z) }, Vector3.forward, ArrivedAtSpot);
        }
        else
        {
            if (SpotPoint != null) transform.position = SpotPoint.position;
            ArrivedAtSpot();
        }
    }

    void ArrivedAtSpot()
    {
        if (State != CustomerState.Arriving) return;
        State = CustomerState.Waiting;
        float heat = HeatManager.Instance != null ? HeatManager.Instance.DrinkBias : 0f;
        string line = CustomerDialogue.Request(Order, Mood, heat);
        if (Anim != null) Anim.Gesture(Mood == Mood.Polite || Mood == Mood.Chatty ? "Wave" : "Talk");
        if (Bubble != null) Bubble.Show(Order, line);
        Speak();
    }

    void Speak()
    {
        if (Voice == null)
        {
            var existing = gameObject.GetComponents<AudioSource>();
            Voice = existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>();
        }
        if (Voice == null) { FeedbackManager.Instance?.CustomerSpeaks(transform.position); return; }
        float pitch = Mood == Mood.Hurried ? 1.05f : Mood == Mood.Grumpy ? 0.94f : 1f;
        if (!SpeechPlayer.Speak(Voice, VoiceId, CustomerDialogue.Segments, pitch))
            FeedbackManager.Instance?.CustomerSpeaks(transform.position);   // fallback: synthetic babble
    }

    float checkTimer;
    void Update()
    {
        if (State != CustomerState.Waiting) return;
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = 0.15f;
            var bag = Bag.Instance;
            if (bag != null && !bag.Hidden)
            {
                if (Bubble != null && Time.time >= hintUntil) Bubble.ShowRemaining(Order, bag.Items);
                // handover: the player must pick the bag up and bring it to the customer's body
                if (bag.IsHeld && bag.HeldSeconds > 0.3f && bag.Items.Count > 0 && Time.time >= hintUntil && BagTouches(bag) && IsClosestTo(bag))
                {
                    if (!OrderComplete(bag) && !HasWrongItem(bag))
                    {
                        // everything so far is right, but not everything is there yet: keep waiting
                        hintUntil = Time.time + 2.5f;
                        if (Bubble != null) Bubble.Say("Not everything yet!");
                        if (Anim != null) Anim.Gesture("Talk");
                    }
                    else { Receive(bag); return; }
                }
            }
        }
        float mult = HeatManager.Instance != null ? HeatManager.Instance.PatienceDrainMultiplier : 1f;
        Patience01 -= (Time.deltaTime * mult) / PatienceSeconds;
        if (Bubble != null) Bubble.SetPatience(Patience01);
        if (!sighed && Patience01 < 0.35f) { sighed = true; if (Anim != null) Anim.Gesture("Angry"); }
        if (Patience01 <= 0f) WalkAway();
    }

    bool OrderComplete(Bag bag)
    {
        var need = new List<ItemType>(Order);
        foreach (var i in bag.Items) if (i != null) need.Remove(i.Type);
        return need.Count == 0;
    }

    bool HasWrongItem(Bag bag)
    {
        var need = new List<ItemType>(Order);
        foreach (var i in bag.Items) if (i != null && !need.Remove(i.Type)) return true;
        return false;
    }

    /// <summary>Checks a bag against the order and pays out. Returns true if the order was fully correct.</summary>
    public bool Receive(Bag bag)
    {
        if (State != CustomerState.Waiting) return false;
        var need = new List<ItemType>(Order);
        int correct = 0;
        foreach (var item in bag.Items)
        {
            if (item != null && need.Remove(item.Type)) correct++;
        }
        bool exact = need.Count == 0 && bag.Items.Count == Order.Count;
        int pts = ScoreManager.Instance.ScoreSale(correct, exact, Patience01);
        CoinPopup.Spawn(transform.position + Vector3.up * 2.0f, (pts >= 0 ? "+" : "") + pts, exact ? new Color(1f, 0.85f, 0.2f) : Color.white);
        FeedbackManager.Instance?.Sale(transform.position, exact);
        State = CustomerState.Served;
        bag.Consume();
        OnServed?.Invoke(this);
        if (Anim != null) Anim.Gesture(exact ? "Thank" : "Shake");
        Leave(CustomerDialogue.Thanks(Mood, exact, Patience01));
        return exact;
    }

    void WalkAway()
    {
        State = CustomerState.Left;
        ScoreManager.Instance.PenaliseWalkOut();
        CoinPopup.Spawn(transform.position + Vector3.up * 2.0f, "-" + ScoreManager.WalkOutPenalty, new Color(1f, 0.3f, 0.25f));
        FeedbackManager.Instance?.Unhappy(transform.position);
        OnLeft?.Invoke(this);
        if (Anim != null) Anim.Gesture("Angry");
        Leave(CustomerDialogue.WalkOut(Mood));
    }

    void Leave(string line)
    {
        Speak();
        StartCoroutine(LeaveRoutine(line));
    }

    IEnumerator LeaveRoutine(string line)
    {
        if (Bubble != null) Bubble.Say(line);
        yield return new WaitForSeconds(1.8f);
        if (Bubble != null) Bubble.Hide();
        if (State == CustomerState.Waiting) State = CustomerState.Left;
        if (Mover != null && ExitPoint != null)
        {
            Vector3 p = transform.position, e = ExitPoint.position;
            Mover.MoveAlong(new[] { new Vector3(p.x, 0f, e.z), new Vector3(e.x, 0f, e.z) }, Vector3.zero, Deactivate);
        }
        else Deactivate();
    }

    void Deactivate()
    {
        if (Mover != null) Mover.Stop();
        gameObject.SetActive(false);   // returned to the pool
    }

    void OnDisable() { Active.Remove(this); StopAllCoroutines(); }
}
