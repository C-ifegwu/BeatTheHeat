using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Speech bubble with item labels and a patience bar above a customer. Faces the player.</summary>
public class OrderBubble : MonoBehaviour
{
    public TMP_Text OrderText;
    public TMP_Text SpeechText;
    public Image PatienceFill;
    public Transform Head;

    static readonly Dictionary<ItemType, string> Emoji = new Dictionary<ItemType, string>
    {
        { ItemType.Apple, "Apple" }, { ItemType.Banana, "Banana" }, { ItemType.Lemon, "Lemon" },
        { ItemType.Bread, "Bread" },   { ItemType.Water, "Water" },   { ItemType.Soda, "Soda" },
    };

    void Awake()
    {
        // compact, readable bubble: narrower so neighbours do not overlap, a bit higher so it clears the head and HUD
        ((RectTransform)transform).sizeDelta = new Vector2(380, 320);
        transform.localScale = Vector3.one * 0.0017f;
        var lp = transform.localPosition; lp.y = 2.3f; transform.localPosition = lp;
        var bg = transform.Find("BG") as RectTransform; if (bg != null) bg.sizeDelta = new Vector2(380, 320);
        if (SpeechText != null)
        {
            var r = (RectTransform)SpeechText.transform; r.anchoredPosition = new Vector2(0, 105); r.sizeDelta = new Vector2(350, 90);
            SpeechText.enableAutoSizing = true; SpeechText.fontSizeMin = 16; SpeechText.fontSizeMax = 26;
        }
        if (OrderText != null)
        {
            var r = (RectTransform)OrderText.transform; r.anchoredPosition = new Vector2(0, -15); r.sizeDelta = new Vector2(350, 150);
            OrderText.enableAutoSizing = true; OrderText.fontSizeMin = 20; OrderText.fontSizeMax = 46;
        }
        if (PatienceFill != null) { var r = PatienceFill.rectTransform; r.anchoredPosition = new Vector2(0, -120); r.sizeDelta = new Vector2(320, 24); }
    }

    public void Show(List<ItemType> order, string speech = null)
    {
        gameObject.SetActive(true);
        if (SpeechText != null) SpeechText.text = speech ?? "";
        var parts = new List<string>();
        foreach (var o in order) parts.Add(Emoji[o]);
        if (OrderText != null) OrderText.text = string.Join("\n", parts);
        SetPatience(1f);
    }

    /// <summary>Shows a spoken line and hides the order list and patience bar (used for thanks / complaints).</summary>
    public void Say(string line)
    {
        gameObject.SetActive(true);
        if (SpeechText != null) SpeechText.text = line;
        if (OrderText != null) OrderText.text = "";
        if (PatienceFill != null) PatienceFill.gameObject.SetActive(false);
    }

    public void SetPatience(float p)
    {
        if (PatienceFill == null) return;
        if (!PatienceFill.gameObject.activeSelf) PatienceFill.gameObject.SetActive(true);
        p = Mathf.Clamp01(p);
        PatienceFill.fillAmount = p;
        PatienceFill.color = Color.Lerp(Color.red, Color.green, p);
    }

    /// <summary>Shows only what is still missing from the bag.</summary>
    public void ShowRemaining(List<ItemType> order, List<StallItem> inBag)
    {
        var need = new List<ItemType>(order);
        if (inBag != null) foreach (var i in inBag) if (i != null) need.Remove(i.Type);
        if (OrderText == null) return;
        if (need.Count == 0) { OrderText.text = "All packed!\nHand over the bag"; return; }
        var parts = new List<string>();
        foreach (var o in need) parts.Add(Emoji[o]);
        OrderText.text = string.Join("\n", parts);
    }

    public void Hide() { gameObject.SetActive(false); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
