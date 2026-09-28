using UnityEngine;

public class LoopReturn : MonoBehaviour
{
    public Transform fromAnchor;
    public Transform toAnchor;

    float m_Cooldown;

    void Update()
    {
        if (m_Cooldown > 0f)
            m_Cooldown -= Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (m_Cooldown > 0f || fromAnchor == null || toAnchor == null)
            return;

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null)
            return;

        Vector3 shift = toAnchor.position - fromAnchor.position;
        if (shift.sqrMagnitude < 0.01f)
            return;

        Vector3 worldPosition = movement.transform.position + shift;
        Quaternion worldRotation = movement.transform.rotation;
        movement.TeleportKeepingLook(worldPosition, worldRotation, 0f);
        m_Cooldown = 1f;
    }
}
