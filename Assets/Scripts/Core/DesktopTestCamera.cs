using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Editor-only convenience: adds a camera at the player's eye position so the Game view shows
/// the stall when no headset is tracking. Never included in a Quest build.
/// </summary>
public static class DesktopTestCamera
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (head.isValid) return; // a real headset is tracking
        var go = new GameObject("DesktopTestCamera");
        var cam = go.AddComponent<Camera>();
        cam.depth = 100;
        cam.fieldOfView = 70;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 80f;
        go.transform.position = new Vector3(0f, 1.6f, 1.6f);
        go.transform.rotation = Quaternion.Euler(8f, 180f, 0f);
    }
#endif
}
