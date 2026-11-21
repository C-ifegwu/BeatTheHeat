using UnityEngine;

/// <summary>Keeps the stall HUD softly in front of the player at the top of their view, always facing them.</summary>
public class FollowHud : MonoBehaviour
{
    public float Distance = 1.2f, Height = 0.55f, Smooth = 6f;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) { var all = Camera.allCameras; float best = float.MinValue; foreach (var k in all) if (k.depth > best && k.enabled) { best = k.depth; cam = k; } }
        if (cam == null) return;
        Transform c = cam.transform;
        Vector3 fwd = c.forward; fwd.y = 0f;
        fwd = fwd.sqrMagnitude < 0.01f ? c.forward : fwd.normalized;
        Vector3 target = c.position + fwd * Distance + Vector3.up * Height;
        transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-Smooth * Time.deltaTime));
        transform.rotation = Quaternion.LookRotation(transform.position - c.position);
    }
}
