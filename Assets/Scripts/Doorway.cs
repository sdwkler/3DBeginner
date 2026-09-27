using System.Collections;
using UnityEngine;

public enum DoorPlace
{
    Indoor = 0,
    Outdoor = 1
}

public enum DoorRole
{
    Entry = 0,
    Exit = 1
}

public class Doorway : MonoBehaviour
{
    [Header("这扇门")]
    public DoorPlace place = DoorPlace.Indoor;

    [Tooltip("留给以后的门功能。现在是否传送只看 Indoor / Outdoor。")]
    public DoorRole role = DoorRole.Entry;

    [Tooltip("只在 Indoor 上设置。勾选表示这扇室内门是正确路线，传送后阶层加 1。")]
    public bool correctPassage;

    [Tooltip("Indoor 碰到后要送到的 Outdoor。Outdoor 留空。")]
    public Doorway exitDoor;

    [Tooltip("在 Outdoor 上设置玩家落地后的朝向，单位是角度。")]
    public float exitYaw;

    [Tooltip("Outdoor 整体坐标下，玩家落点的局部偏移。")]
    public Vector3 spawnLocalOffset = new Vector3(0f, 0f, 1.35f);

    public Vector3 triggerCenter = new Vector3(0f, 0.4f, 0f);
    public Vector3 triggerSize = new Vector3(1.8f, 2.2f, 1.4f);

    BoxCollider m_Trigger;
    float m_IgnoreUntil;

    void Awake()
    {
        if (place == DoorPlace.Indoor)
            EnsureTrigger();
    }

    void FixedUpdate()
    {
        if (m_Trigger == null || !m_Trigger.enabled)
            return;

        Vector3 center = m_Trigger.transform.TransformPoint(m_Trigger.center);
        Vector3 halfExtents = Vector3.Scale(m_Trigger.size, m_Trigger.transform.lossyScale) * 0.5f;
        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            m_Trigger.transform.rotation,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
        for (int i = 0; i < hits.Length; i++)
            TryTeleport(hits[i]);
    }

    void OnTriggerEnter(Collider other)
    {
        TryTeleport(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryTeleport(other);
    }

    void TryTeleport(Collider other)
    {
        if (place != DoorPlace.Indoor)
            return;
        if (Time.time < m_IgnoreUntil || TeleportMosaic.IsBusy)
            return;
        if (exitDoor == null || exitDoor.place != DoorPlace.Outdoor)
            return;
        if (other == null || !other.CompareTag("Player"))
            return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || !player.enabled || player.controlsLocked)
            return;

        m_IgnoreUntil = Time.time + 2f;
        StartCoroutine(Teleport(player));
    }

    IEnumerator Teleport(PlayerMovement player)
    {
        player.controlsLocked = true;
        player.Halt();

        bool correct = correctPassage;
        Doorway destination = exitDoor;
        Vector3 position = destination.transform.TransformPoint(destination.spawnLocalOffset);
        float yaw = destination.exitYaw;

        TeleportMosaic mosaic = TeleportMosaic.Ensure();
        yield return mosaic.Play(() =>
        {
            player.PlaceFacing(position, yaw);
            player.Halt();
            if (correct && LoopProgress.Instance != null)
                LoopProgress.Instance.RegisterCorrectEntry();
        });

        player.controlsLocked = false;
        m_IgnoreUntil = Time.time + 0.75f;
    }

    void EnsureTrigger()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            BoxCollider existing = colliders[i] as BoxCollider;
            if (existing != null && existing.isTrigger)
            {
                m_Trigger = existing;
                return;
            }
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = triggerCenter;
        box.size = triggerSize;
        m_Trigger = box;
    }
}
