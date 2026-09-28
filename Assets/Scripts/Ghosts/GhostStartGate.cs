using UnityEngine;

public class GhostStartGate : MonoBehaviour
{
    public GhostDirector director;

    void FixedUpdate()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || !box.enabled)
            return;

        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            box.transform.rotation,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
            TryOpen(hits[i]);
    }

    void OnTriggerEnter(Collider other)
    {
        TryOpen(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryOpen(other);
    }

    void TryOpen(Collider other)
    {
        if (director == null || other == null || !other.CompareTag("Player"))
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.enabled || player.controlsLocked)
            return;

        director.NotifyEnteredStartDoor();
    }
}
