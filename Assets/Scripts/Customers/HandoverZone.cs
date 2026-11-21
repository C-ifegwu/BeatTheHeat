using UnityEngine;

/// <summary>Legacy marker at a customer's hand. The handover itself is decided in Customer.Update
/// (the bag must be carried up to the customer's body), so this does nothing on its own.</summary>
[RequireComponent(typeof(Collider))]
public class HandoverZone : MonoBehaviour
{
    public Customer Owner;
    void Reset() { GetComponent<Collider>().isTrigger = true; }
}
