using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Natural-language requests and reactions, varied by mood and by how hot the day is.</summary>
public static class CustomerDialogue
{
    static readonly Dictionary<ItemType, string[]> Names = new Dictionary<ItemType, string[]>
    {
        { ItemType.Apple, new[] { "apple", "apples" } },
        { ItemType.Banana, new[] { "banana", "bananas" } },
        { ItemType.Lemon,  new[] { "lemon", "lemons" } },
        { ItemType.Bread,  new[] { "loaf of bread", "loaves of bread" } },
        { ItemType.Water,  new[] { "cold water", "cold waters" } },
        { ItemType.Soda,   new[] { "soda", "sodas" } },
    };

    static string Article(string n) { return "aeiou".IndexOf(n[0]) >= 0 ? "an " : "a "; }

    static readonly string[] Numbers = { "zero", "one", "two", "three", "four", "five" };

    public static string ListItems(List<ItemType> order)
    {
        var counts = new SortedDictionary<ItemType, int>();
        foreach (var o in order) counts[o] = counts.TryGetValue(o, out var c) ? c + 1 : 1;
        var parts = new List<string>();
        foreach (var kv in counts)
        {
            string n = Names[kv.Key][kv.Value > 1 ? 1 : 0];
            parts.Add(kv.Value > 1 ? Numbers[Mathf.Min(kv.Value, 5)] + " " + n : Article(n) + n);
        }
        if (parts.Count == 1) return parts[0];
        var sb = new StringBuilder();
        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0) sb.Append(i == parts.Count - 1 ? " and " : ", ");
            sb.Append(parts[i]);
        }
        return sb.ToString();
    }

    /// <summary>Fragments of the line most recently produced, matched to the recorded speech clips.</summary>
    public static readonly List<string> Segments = new List<string>();

    static List<string> ItemFragments(List<ItemType> order)
    {
        var counts = new SortedDictionary<ItemType, int>();
        foreach (var o in order) counts[o] = counts.TryGetValue(o, out var c) ? c + 1 : 1;
        var frags = new List<string>();
        int i = 0;
        foreach (var kv in counts)
        {
            string n = Names[kv.Key][kv.Value > 1 ? 1 : 0];
            if (i > 0 && i == counts.Count - 1) frags.Add("and");
            frags.Add(kv.Value > 1 ? Numbers[Mathf.Min(kv.Value, 5)] + " " + n : Article(n) + n);
            i++;
        }
        return frags;
    }

    static string Pick(string[] a) { return a[Random.Range(0, a.Length)]; }

    static readonly (string before, string after)[] Hurried = { ("Quick,", "I'm in a rush."), ("I'm late, just", "please!"), ("", "and fast, please!") };
    static readonly (string before, string after)[] Chatty = { ("Hi there! Could I get", ""), ("What a day!", "for me, please."), ("Hello hello! I'll take", "") };
    static readonly (string before, string after)[] Grumpy = { ("", "Don't keep me waiting."), ("Give me", "Hurry up."), ("I want", "Now.") };
    static readonly (string before, string after)[] Polite = { ("Good day! May I have", "please?"), ("Hello! I'd like", "please."), ("Good morning!", "please.") };

    public static string Request(List<ItemType> order, Mood mood, float heat01)
    {
        string list = ListItems(order);
        bool drink = order.Contains(ItemType.Water) || order.Contains(ItemType.Soda);
        Segments.Clear();
        string prefix = "";
        if (heat01 > 0.6f && drink)
        {
            string pf = Pick(new[] { "I'm melting out here!", "It is scorching!", "Phew, so hot!" });
            prefix = pf + " "; Segments.Add(pf);
        }
        var set = mood == Mood.Hurried ? Hurried : mood == Mood.Chatty ? Chatty : mood == Mood.Grumpy ? Grumpy : Polite;
        var t = set[Random.Range(0, set.Length)];
        if (t.before != "") Segments.Add(t.before);
        Segments.AddRange(ItemFragments(order));
        if (t.after != "") Segments.Add(t.after.StartsWith("please") ? "please." : t.after);
        string text = (t.before == "" ? "" : t.before + " ") + list + (t.after == "" ? "." : " " + t.after);
        if (mood == Mood.Chatty && t.after == "") text = t.before + " " + list + (t.before.EndsWith("get") ? "?" : ".");
        if (mood == Mood.Hurried && t.before == "Quick,") text = "Quick, " + list + "! " + t.after;
        if (mood == Mood.Hurried && t.before == "I'm late, just") text = "I'm late, just " + list + " please!";
        if (mood == Mood.Hurried && t.before == "") text = list + ", and fast, please!";
        if (mood == Mood.Chatty && t.before == "What a day!") text = "What a day! " + list + " for me, please.";
        if (mood == Mood.Grumpy) text = t.before == "" ? list + ". Don't keep me waiting." : t.before + " " + list + ". " + t.after;
        if (mood == Mood.Polite) text = t.before + " " + list + (t.after == "please?" ? ", please?" : ", please.");
        return prefix + text;
    }

    static string Say(string line) { Segments.Clear(); Segments.Add(line); return line; }

    public static string Thanks(Mood mood, bool perfect, float tipPatience)
    {
        if (!perfect) return Say(Pick(new[] { "Hmm, that isn't quite my order...", "This isn't what I asked for.", "Not what I wanted, but fine." }));
        switch (mood)
        {
            case Mood.Grumpy: return Say(Pick(new[] { "Finally. Fine.", "About time. Thanks." }));
            case Mood.Hurried: return Say(Pick(new[] { "Perfect, thanks! Gotta run!", "Great, bye!" }));
            case Mood.Chatty: return Say(Pick(new[] { "Wonderful, thank you so much!", "You're the best! Have a lovely day!" }));
            default: return Say(Pick(new[] { "Perfect, thank you!", "Thank you, that's lovely!" }));
        }
    }

    public static string WalkOut(Mood mood)
    {
        return Say(mood == Mood.Grumpy ? Pick(new[] { "Unbelievable! I'm leaving!", "Too slow! Forget it!" })
                                   : Pick(new[] { "Sorry, I can't wait any longer.", "Too slow, I'll shop elsewhere!" }));
    }
}
