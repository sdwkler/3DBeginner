using UnityEngine;

public class IndoorReturn : MonoBehaviour
{
    void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        TryArrive(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryArrive(other);
    }

    static void TryArrive(Collider other)
    {
        if (other == null || !other.CompareTag("Player"))
            return;
        if (RoomCycle.Instance == null)
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.enabled || player.controlsLocked)
            return;

        RoomCycle.Instance.ArriveIndoor();
    }
}
