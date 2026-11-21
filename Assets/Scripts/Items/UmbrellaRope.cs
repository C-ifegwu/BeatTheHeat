using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Grab and pull the rope down to open the umbrella for shade. Uses an XR grab interactable.</summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class UmbrellaRope : MonoBehaviour
{
    public float PullDistance = 0.12f;
    public Transform Umbrella;
    Vector3 restLocal;
    XRGrabInteractable grab;

    void Awake()
    {
        Style();
        grab = GetComponent<XRGrabInteractable>();
        restLocal = transform.localPosition;
        var rb = GetComponent<Rigidbody>(); rb.isKinematic = true;
        grab.trackPosition = true; grab.trackRotation = false; grab.throwOnDetach = false;
    }

    /// <summary>A red pull handle on a thin cord hanging from the umbrella (it used to be a floating grey capsule beside the player).</summary>
    void Style()
    {
        transform.position = new Vector3(0.82f, 1.62f, 0.55f);
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(0.05f, 0.07f, 0.05f);
        var r = GetComponent<Renderer>();
        if (r != null) { r.material.color = new Color(0.85f, 0.15f, 0.12f); }
        var cc = GetComponent<CapsuleCollider>();
        if (cc != null) { cc.radius = 3f; cc.height = 4f; }    // generous grab area (about 15 cm)
        var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cord.name = "Cord";
        Destroy(cord.GetComponent<Collider>());
        cord.transform.SetParent(transform, false);
        cord.transform.localPosition = new Vector3(0f, 5.43f, 0f);
        cord.transform.localScale = new Vector3(0.24f, 4.43f, 0.24f);
        if (r != null) { var cr = cord.GetComponent<Renderer>(); cr.sharedMaterial = new Material(r.sharedMaterial) { color = new Color(0.45f, 0.32f, 0.2f) }; }
    }

    void Start() { grab.selectExited.AddListener(OnReleased); HeatManager.Instance.OnShadeChanged += OnShade; }
    void OnDestroy() { if (grab != null) grab.selectExited.RemoveListener(OnReleased); if (HeatManager.Instance != null) HeatManager.Instance.OnShadeChanged -= OnShade; }

    void Update()
    {
        // Lock sideways movement, only allow a vertical pull.
        var p = transform.localPosition;
        p.x = restLocal.x; p.z = restLocal.z;
        p.y = Mathf.Clamp(p.y, restLocal.y - PullDistance * 1.5f, restLocal.y);
        transform.localPosition = p;

        if (grab.isSelected && restLocal.y - p.y >= PullDistance)
        {
            if (HeatManager.Instance.TryOpenUmbrella()) FeedbackManager.Instance?.RopeCreak(transform.position);
        }
    }

    void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _) { transform.localPosition = restLocal; }

    void OnShade(bool on)
    {
        if (Umbrella != null) Umbrella.localScale = on ? new Vector3(1.6f, 0.1f, 1.6f) : new Vector3(0.6f, 0.1f, 0.6f);
    }
}
