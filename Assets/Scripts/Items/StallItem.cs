using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>A grabbable stall item. Restocks the shelf when taken, packs into the bag when released over it.</summary>
[RequireComponent(typeof(Rigidbody))]
public class StallItem : XRGrabInteractable
{
    public ItemType Type;
    Vector3 homePos; Quaternion homeRot;
    bool restocked, ignoredBag;
    float groundTime;
    Transform bagFollow; Vector3 bagOffset, pivotToCenter;
    public bool InBag { get; set; }

    protected override void Awake()
    {
        base.Awake();
        homePos = transform.position; homeRot = transform.rotation;
    }

    protected override void OnSelectEntered(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        FeedbackManager.Instance?.Grab(args.interactorObject.transform);
        if (InBag) return;
        if (!restocked) Restock();
    }

    protected override void OnSelectExited(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        var bag = Bag.Instance;
        if (bag != null && !InBag && bag.IsNear(transform.position)) bag.Pack(this);
    }

    /// <summary>A fresh copy appears on the shelf as soon as one is taken.</summary>
    void Restock()
    {
        restocked = true;
        var copy = Instantiate(this, homePos, homeRot);
        copy.restocked = false; copy.ignoredBag = false; copy.InBag = false; copy.groundTime = 0f; copy.bagFollow = null;
        var rb = copy.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
    }

    /// <summary>Called by the bag: the item stops being a physics object and rides along with the bag.</summary>
    public void PackInto(Transform bag, int index)
    {
        InBag = true;
        var rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        enabled = true;
        bagFollow = bag;
        transform.rotation = Quaternion.identity;
        // Fit the item into the bag mouth: shrink big items, and centre the visible mesh (not the pivot) on its slot.
        var rs = GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float big = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (big > 0.15f) transform.localScale *= 0.15f / big;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            pivotToCenter = b.center - transform.position;
        }
        bagOffset = new Vector3(((index % 3) - 1) * 0.05f, 0.12f + (index / 3) * 0.06f, ((index % 2) - 0.5f) * 0.03f) - pivotToCenter;
    }

    /// <summary>Falls or is dropped off the counter: back to the shelf if it was never restocked, otherwise removed.</summary>
    public void ReturnHome()
    {
        if (restocked) { Destroy(gameObject); return; }
        var rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(homePos, homeRot);
        InBag = false;
    }

    /// <summary>Items never collide with the bag (they would bounce off it); they are packed by proximity instead.</summary>
    void IgnoreBag()
    {
        var bag = Bag.Instance;
        if (bag == null) return;
        ignoredBag = true;
        var mine = GetComponentsInChildren<Collider>(true);
        foreach (var bc in bag.GetComponentsInChildren<Collider>(true))
            foreach (var m in mine) if (m != null && bc != null) Physics.IgnoreCollision(m, bc, true);
    }

    void Update()
    {
        if (!ignoredBag) IgnoreBag();
        if (bagFollow != null) { transform.position = bagFollow.position + bagOffset; return; }
        if (transform.position.y < -1f) { ReturnHome(); return; }
        // dropped or released over the bag mouth: pack it (also covers a missed release event)
        if (restocked && !isSelected && !InBag && Bag.Instance != null && Bag.Instance.IsNear(transform.position)) { Bag.Instance.Pack(this); return; }
        if (!isSelected && transform.position.y < 0.5f)
        {
            groundTime += Time.deltaTime;
            if (groundTime > 3f) { groundTime = 0f; ReturnHome(); }
        }
        else groundTime = 0f;
    }
}
