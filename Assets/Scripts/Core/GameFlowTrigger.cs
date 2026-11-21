using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Fallback: grabbing the bag on the main menu starts the game (the Play button is the normal way).</summary>
public class GameFlowTrigger : MonoBehaviour
{
    public XRGrabInteractable BagGrab;

    void OnEnable() { if (BagGrab != null) BagGrab.selectEntered.AddListener(OnGrab); }
    void OnDisable() { if (BagGrab != null) BagGrab.selectEntered.RemoveListener(OnGrab); }

    void OnGrab(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _)
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Menu) gm.StartGame();
    }
}
