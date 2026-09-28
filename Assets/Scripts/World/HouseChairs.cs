using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class HouseChairs : MonoBehaviour
{
    public GameObject chairPrefab;

    class Seat
    {
        public Transform transform;
        public Vector3 position;
        public Quaternion rotation;
        public bool hunter;
        public bool floater;
        public Rigidbody body;
        public bool bodyWasOurs;
        public ChairStalker stalker;
        public Collider addedBox;
    }

    readonly List<Seat> m_Seats = new List<Seat>();
    Transform m_Desk;
    Transform m_Flow;
    bool m_Collected;

    void Awake()
    {
        Collect();
    }

    public void Apply(AnomalyRoll roll)
    {
        Collect();
        Restore();
        if (roll.Contains(GhostDirector.ChairFloatId))
            FloatAll();
        else if (roll.Contains(GhostDirector.ChairHuntId))
            Hunt();
        else if (roll.Contains(GhostDirector.ChairPileId))
            ShowDesk();
    }

    void Collect()
    {
        if (m_Collected)
            return;

        m_Collected = true;
        m_Desk = FindExact("Chair_desk");
        m_Flow = FindExact("Chair_flow");
        HideGroups();

        GameObject group = GameObject.Find("Chairs");
        if (group != null)
            TakeChairs(group.transform, true, false);
        if (m_Flow != null)
            TakeChairs(m_Flow, false, true);
    }

    void TakeChairs(Transform root, bool hunter, bool floater)
    {
        Transform[] chairs = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < chairs.Length; i++)
        {
            Transform chair = chairs[i];
            if (chair == null || chair == root || chair.GetComponent<Renderer>() == null)
                continue;

            if (chair.parent != root)
                continue;

            Seat seat = new Seat();
            seat.transform = chair;
            seat.position = chair.position;
            seat.rotation = chair.rotation;
            seat.hunter = hunter;
            seat.floater = floater;
            m_Seats.Add(seat);
        }
    }

    static Transform FindExact(string objectName)
    {
        Transform[] all = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform node = all[i];
            if (node == null || !node.gameObject.scene.IsValid())
                continue;
            if (string.Equals(node.name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return node;
        }

        return null;
    }

    void HideGroups()
    {
        if (m_Desk != null)
            m_Desk.gameObject.SetActive(false);
        if (m_Flow != null)
            m_Flow.gameObject.SetActive(false);
    }

    void Restore()
    {
        for (int i = 0; i < m_Seats.Count; i++)
        {
            Seat seat = m_Seats[i];
            if (seat.transform == null)
                continue;

            if (seat.stalker != null)
                DestroyImmediate(seat.stalker);
            if (seat.bodyWasOurs && seat.body != null)
                DestroyImmediate(seat.body);
            if (seat.addedBox != null)
                DestroyImmediate(seat.addedBox);

            seat.stalker = null;
            seat.body = null;
            seat.addedBox = null;
            seat.bodyWasOurs = false;
            seat.transform.SetPositionAndRotation(seat.position, seat.rotation);
        }

        HideGroups();
    }

    void ShowDesk()
    {
        if (m_Desk == null)
            return;

        m_Desk.gameObject.SetActive(true);
        Renderer[] shown = m_Desk.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < shown.Length; i++)
        {
            if (shown[i] != null)
                shown[i].enabled = true;
        }
    }

    void Hunt()
    {
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        for (int i = 0; i < m_Seats.Count; i++)
        {
            Seat seat = m_Seats[i];
            if (!seat.hunter || seat.transform == null)
                continue;

            GameObject chair = seat.transform.gameObject;
            chair.isStatic = false;
            Renderer shown = chair.GetComponent<Renderer>();
            if (shown != null)
                shown.enabled = true;
            seat.addedBox = EnsureSolid(chair, seat);
            seat.body = EnsureBody(chair, seat, true, 7f);
            if (player != null)
                Face(seat.transform, player.transform.position);

            seat.stalker = chair.AddComponent<ChairStalker>();
            seat.stalker.Arm(seat.body);
        }
    }

    void FloatAll()
    {
        if (m_Flow != null)
            m_Flow.gameObject.SetActive(true);

        for (int i = 0; i < m_Seats.Count; i++)
        {
            Seat seat = m_Seats[i];
            if (!seat.floater || seat.transform == null)
                continue;

            GameObject chair = seat.transform.gameObject;
            chair.isStatic = false;
            seat.addedBox = EnsureSolid(chair, seat);
            Rigidbody body = EnsureBody(chair, seat, false, 0.12f);
            body.drag = 0.15f;
            body.angularDrag = 0.2f;
            body.constraints = RigidbodyConstraints.None;
            seat.transform.SetPositionAndRotation(seat.position, seat.rotation);
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    static void Face(Transform chair, Vector3 target)
    {
        Vector3 flat = target - chair.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f)
            return;
        chair.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    static Collider EnsureSolid(GameObject chair, Seat seat)
    {
        Collider[] colliders = chair.GetComponents<Collider>();
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
                colliders[i].isTrigger = false;
            }

            return null;
        }

        BoxCollider box = chair.AddComponent<BoxCollider>();
        Renderer renderer = chair.GetComponent<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            box.center = chair.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = chair.transform.lossyScale;
            box.size = new Vector3(
                SafeDivide(bounds.size.x, scale.x),
                SafeDivide(bounds.size.y, scale.y),
                SafeDivide(bounds.size.z, scale.z));
        }

        if (seat != null)
            seat.addedBox = box;
        return box;
    }

    static float SafeDivide(float value, float scale)
    {
        float abs = Mathf.Abs(scale);
        return abs < 0.0001f ? value : value / abs;
    }

    static Rigidbody EnsureBody(GameObject chair, Seat seat, bool gravity, float mass)
    {
        Rigidbody body = chair.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = chair.AddComponent<Rigidbody>();
            seat.bodyWasOurs = true;
        }

        seat.body = body;
        body.mass = mass;
        body.useGravity = gravity;
        body.isKinematic = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = gravity ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.None;
        return body;
    }
}

public class ChairStalker : MonoBehaviour
{
    Rigidbody m_Body;
    NavMeshPath m_Path;
    float m_NextPath;
    float m_StunUntil;
    float m_HitLock;
    bool m_Turned;

    public void Arm(Rigidbody body)
    {
        m_Body = body;
        m_Path = new NavMeshPath();
    }

    void FixedUpdate()
    {
        if (m_Body == null)
            return;

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null && player.enabled && !m_Turned)
        {
            Vector3 face = player.transform.position - transform.position;
            face.y = 0f;
            if (face.sqrMagnitude > 0.01f)
                m_Body.MoveRotation(Quaternion.LookRotation(face.normalized, Vector3.up));
            m_Turned = true;
        }

        if (player == null || !player.enabled || Time.time < m_StunUntil || Watched())
        {
            Hold();
            return;
        }

        Chase(player.transform.position);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (m_Body == null || Time.time < m_HitLock || Time.time < m_StunUntil)
            return;

        PlayerMovement player = collision.collider.GetComponentInParent<PlayerMovement>();
        if (player == null)
            return;

        Vector3 flat = m_Body.velocity;
        flat.y = 0f;
        if (flat.sqrMagnitude < 1.2f)
            return;

        player.Shove(player.transform.position - transform.position);
        m_StunUntil = Time.time + 1f;
        m_HitLock = Time.time + 1f;
        Hold();
    }

    void Hold()
    {
        Vector3 velocity = m_Body.velocity;
        velocity.x = 0f;
        velocity.z = 0f;
        m_Body.velocity = velocity;
    }

    void Chase(Vector3 target)
    {
        Vector3 next = target;
        if (Time.time >= m_NextPath)
        {
            m_NextPath = Time.time + 0.3f;
            NavMeshHit from;
            NavMeshHit to;
            if (NavMesh.SamplePosition(transform.position, out from, 2.5f, NavMesh.AllAreas)
                && NavMesh.SamplePosition(target, out to, 2.5f, NavMesh.AllAreas))
                NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, m_Path);
        }

        if (m_Path != null && m_Path.corners != null && m_Path.corners.Length > 1)
            next = m_Path.corners[1];

        Vector3 flat = next - transform.position;
        flat.y = 0f;
        Vector3 velocity = flat.sqrMagnitude > 0.04f ? flat.normalized * 4.6f : Vector3.zero;
        velocity.y = m_Body.velocity.y;
        m_Body.velocity = velocity;
        if (flat.sqrMagnitude > 0.05f)
            m_Body.MoveRotation(Quaternion.LookRotation(flat.normalized, Vector3.up));
    }

    bool Watched()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Vector3 point = transform.position + Vector3.up * 0.55f;
        Collider own = GetComponent<Collider>();
        if (own != null)
        {
            Vector3 size = own.bounds.size;
            if (size.x < 2.2f && size.y < 2.2f && size.z < 2.2f)
                point = own.bounds.center;
        }

        Vector3 view = camera.WorldToViewportPoint(point);
        if (view.z <= 0.05f || view.x < 0f || view.x > 1f || view.y < 0f || view.y > 1f)
            return false;

        Vector3 origin = camera.transform.position;
        Vector3 delta = point - origin;
        float distance = delta.magnitude;
        if (distance < 0.2f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(origin, delta / distance, Mathf.Max(0f, distance - 0.05f), ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i].collider;
            if (hit == null || hit.GetComponentInParent<PlayerMovement>() != null)
                continue;
            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        return true;
    }
}
