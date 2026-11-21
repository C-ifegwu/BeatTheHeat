using System.Collections.Generic;
using UnityEngine;

/// <summary>Paper bag. Items released near it are packed in and travel with it until the sale.</summary>
[RequireComponent(typeof(Collider))]
public class Bag : MonoBehaviour
{
    public static Bag Instance { get; private set; }
    public const float PackRadius = 0.38f;
    public readonly List<StallItem> Items = new List<StallItem>();
    public System.Action OnContentsChanged;

    Vector3 homePos; Quaternion homeRot; float lostTime;
    bool hidden; float respawnAt; bool wasKinematic;
    public float HeldSeconds { get; private set; }
    public bool IsHeld { get { return grab != null && grab.isSelected; } }
    public bool Hidden { get { return hidden; } }
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    void Awake()
    {
        Instance = this; homePos = transform.position; homeRot = transform.rotation;
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    /// <summary>If the bag is dropped on the floor or falls away, it goes back on the counter (never lost).</summary>
    void Update()
    {
        if (hidden)
        {
            if (Time.time >= respawnAt) Respawn();
            return;
        }
        HeldSeconds = IsHeld ? HeldSeconds + Time.deltaTime : 0f;
        bool held = grab != null && grab.isSelected;
        if (!held && (transform.position.y < 0.6f || transform.position.y < -1f)) lostTime += Time.deltaTime; else lostTime = 0f;
        if (lostTime > 2f || transform.position.y < -1f)
        {
            lostTime = 0f;
            var rb = GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            transform.SetPositionAndRotation(homePos, homeRot);
        }
    }

    /// <summary>True when a released item is close enough to be packed.</summary>
    public bool IsNear(Vector3 p)
    {
        if (hidden) return false;
        return (p - (transform.position + Vector3.up * 0.1f)).sqrMagnitude <= PackRadius * PackRadius;
    }

    /// <summary>The sale is made: the bag goes to the customer (vanishes) and a fresh one returns to the counter.</summary>
    public void Consume()
    {
        Empty();
        if (grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; wasKinematic = rb.isKinematic; rb.isKinematic = true; }
        SetVisible(false);
        hidden = true; HeldSeconds = 0f; respawnAt = Time.time + 1.8f;
    }

    /// <summary>Empties the bag and puts it back on the counter (new day, restart, back to menu).</summary>
    public void ResetToHome()
    {
        Empty();
        if (grab != null && grab.isSelected && grab.interactionManager != null)
            grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
        if (!hidden)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null) wasKinematic = rb.isKinematic;
        }
        Respawn();
    }

    void Respawn()
    {
        hidden = false; lostTime = 0f;
        transform.SetPositionAndRotation(homePos, homeRot);
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = wasKinematic; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        SetVisible(true);
    }

    void SetVisible(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = on;
    }

    public void Pack(StallItem item)
    {
        if (item == null || Items.Contains(item)) return;
        Items.Add(item);
        item.PackInto(transform, Items.Count - 1);
        FeedbackManager.Instance?.BagRustle(transform.position);
        OnContentsChanged?.Invoke();
    }

    /// <summary>The sale is done: the packed goods go with the customer.</summary>
    public void Empty()
    {
        foreach (var i in Items) if (i != null) Destroy(i.gameObject);
        Items.Clear(); OnContentsChanged?.Invoke();
    }

    public int CountOf(ItemType t) { int n = 0; foreach (var i in Items) if (i != null && i.Type == t) n++; return n; }
}
