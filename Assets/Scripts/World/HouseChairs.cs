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
        public Rigidbody body;
        public bool bodyWasOurs;
        public ChairStalker stalker;
        public Collider addedBox;
    }

    readonly List<Seat> m_Seats = new List<Seat>();
    GameObject m_Pile;
    bool m_Collected;

    public void Apply(AnomalyRoll roll)
    {
        Collect();
        Restore();
        if (roll.Contains(GhostDirector.ChairFloatId))
            FloatAll();
        else if (roll.Contains(GhostDirector.ChairHuntId))
            Hunt();
        else if (roll.Contains(GhostDirector.ChairPileId))
            Pile();
    }

    void Collect()
    {
        if (m_Collected)
            return;

        m_Collected = true;
        GameObject group = GameObject.Find("Chairs");
        Transform[] all = FindObjectsOfType<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform chair = all[i];
            if (chair == null || chair.GetComponent<Renderer>() == null)
                continue;
            if (chair.name.IndexOf("Chair", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (chair.GetComponentInParent<HouseChairs>() != null)
                continue;

            Seat seat = new Seat();
            seat.transform = chair;
            seat.position = chair.position;
            seat.rotation = chair.rotation;
            seat.hunter = group != null && chair.parent == group.transform;
            m_Seats.Add(seat);
        }
    }

    void Restore()
    {
        if (m_Pile != null)
        {
            Destroy(m_Pile);
            m_Pile = null;
        }

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
    }

    void Pile()
    {
        GameObject source = chairPrefab;
        Transform template = Template();
        if (source == null && template != null)
            source = template.gameObject;
        if (source == null)
            return;

        Vector3 near = AverageHunters();
        Transform table = NearestTable(near);
        if (table == null)
            return;

        Bounds bounds = TableBounds(table);
        m_Pile = new GameObject("ChairPile");
        Vector3 scale = template != null ? template.lossyScale : Vector3.one;
        const int columns = 4;
        const int count = 12;
        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            float u = column / (float)(columns - 1);
            float v = (row % 3) / 2f;
            float x = Mathf.Lerp(bounds.min.x, bounds.max.x, Mathf.Lerp(0.16f, 0.84f, u));
            float z = Mathf.Lerp(bounds.min.z, bounds.max.z, Mathf.Lerp(0.2f, 0.8f, v));
            float y = bounds.max.y + 0.02f + (row >= 2 ? 0.5f : 0f);
            Quaternion rotation = Quaternion.Euler(0f, (i * 47f) % 360f, 0f);
            GameObject copy = Instantiate(source, new Vector3(x, y, z), rotation, m_Pile.transform);
            copy.name = "TableChair";
            Transform[] nodes = copy.GetComponentsInChildren<Transform>(true);
            for (int n = 0; n < nodes.Length; n++)
                nodes[n].gameObject.isStatic = false;
            copy.transform.localScale = scale;
            Renderer[] shown = copy.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < shown.Length; r++)
            {
                if (shown[r] != null)
                    shown[r].enabled = true;
            }
            EnsureSolid(copy, null);
            Rigidbody body = copy.GetComponent<Rigidbody>();
            if (body == null)
                body = copy.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
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
        for (int i = 0; i < m_Seats.Count; i++)
        {
            Seat seat = m_Seats[i];
            if (seat.transform == null)
                continue;

            GameObject chair = seat.transform.gameObject;
            chair.isStatic = false;
            seat.addedBox = EnsureSolid(chair, seat);
            Rigidbody body = EnsureBody(chair, seat, false, 0.12f);
            body.drag = 0.15f;
            body.angularDrag = 0.2f;
            body.constraints = RigidbodyConstraints.None;
            Vector3 position = seat.position;
            position.y += Random.Range(0.45f, 2.5f);
            seat.transform.position = position;
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

    Transform Template()
    {
        for (int i = 0; i < m_Seats.Count; i++)
        {
            if (m_Seats[i].hunter && m_Seats[i].transform != null)
                return m_Seats[i].transform;
        }

        for (int i = 0; i < m_Seats.Count; i++)
        {
            if (m_Seats[i].transform != null)
                return m_Seats[i].transform;
        }

        return null;
    }

    Vector3 AverageHunters()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < m_Seats.Count; i++)
        {
            if (!m_Seats[i].hunter || m_Seats[i].transform == null)
                continue;
            sum += m_Seats[i].position;
            count++;
        }

        return count > 0 ? sum / count : transform.position;
    }

    static Transform NearestTable(Vector3 near)
    {
        Transform[] nodes = FindObjectsOfType<Transform>();
        Transform best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < nodes.Length; i++)
        {
            Transform node = nodes[i];
            if (node == null || !IsDiningTable(node))
                continue;

            Vector3 point = node.position;
            Collider body = node.GetComponent<Collider>();
            if (body != null)
                point = body.bounds.center;
            float distance = Flat(point, near);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = node;
            }
        }

        return best;
    }

    static bool IsDiningTable(Transform node)
    {
        string name = node.name;
        if (name.IndexOf("Bedside", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (name.IndexOf("Chair", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        return name.IndexOf("Dinning", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Dining_Table", System.StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("DiningTable", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static Bounds TableBounds(Transform table)
    {
        Collider body = table.GetComponent<Collider>();
        if (body != null)
            return body.bounds;

        Renderer renderer = table.GetComponent<Renderer>();
        if (renderer != null)
        {
            MeshFilter filter = table.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name.IndexOf("Combined Mesh") < 0)
                return TransformBounds(table, filter.sharedMesh.bounds);
            return renderer.bounds;
        }

        return new Bounds(table.position, new Vector3(2f, 0.7f, 1f));
    }

    static Bounds TransformBounds(Transform space, Bounds local)
    {
        Vector3 center = space.TransformPoint(local.center);
        Vector3 extent = local.extents;
        Vector3 axisX = space.TransformVector(extent.x, 0f, 0f);
        Vector3 axisY = space.TransformVector(0f, extent.y, 0f);
        Vector3 axisZ = space.TransformVector(0f, 0f, extent.z);
        extent.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extent.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extent.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);
        return new Bounds(center, extent * 2f);
    }

    static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
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

        Collider body = GetComponent<Collider>();
        Bounds bounds = body != null
            ? body.bounds
            : new Bounds(transform.position + Vector3.up * 0.55f, new Vector3(0.5f, 1.1f, 0.45f));
        Vector3[] points =
        {
            bounds.center,
            bounds.center + new Vector3(bounds.extents.x * 0.6f, 0f, 0f),
            bounds.center - new Vector3(bounds.extents.x * 0.6f, 0f, 0f),
            bounds.center + new Vector3(0f, bounds.extents.y * 0.55f, 0f),
            bounds.center + new Vector3(0f, 0f, bounds.extents.z * 0.6f)
        };
        for (int i = 0; i < points.Length; i++)
        {
            if (PointInView(camera, points[i]))
                return true;
        }

        return false;
    }

    bool PointInView(Camera camera, Vector3 point)
    {
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
            if (hits[i].collider.GetComponentInParent<PlayerMovement>() != null)
                continue;
            return hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform);
        }

        return true;
    }
}
