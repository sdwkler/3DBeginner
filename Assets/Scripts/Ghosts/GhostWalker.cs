using UnityEngine;
using UnityEngine.AI;

public class GhostWalker : GhostActor
{
    public float walkSpeed = 1.35f;
    public Transform door;

    public enum DriftKind
    {
        None,
        Back,
        Tail
    }

    public DriftKind drift;

    NavMeshAgent m_Agent;
    Animator m_Animator;
    Vector3 m_ExitPoint;
    bool m_Marching;
    bool m_Gone;
    bool m_Repeat;
    bool m_Drifting;
    int m_Round;
    float m_BaseOffset;

    public bool RepeatAfterDoor
    {
        get { return m_Repeat; }
        set { m_Repeat = value; }
    }

    protected override void Awake()
    {
        base.Awake();
        m_Animator = GetComponent<Animator>();
        m_Agent = GetComponent<NavMeshAgent>();
        if (m_Agent == null)
            m_Agent = gameObject.AddComponent<NavMeshAgent>();

        m_Agent.speed = walkSpeed;
        m_Agent.angularSpeed = 280f;
        m_Agent.acceleration = 12f;
        m_Agent.radius = 0.28f;
        m_Agent.height = 1.5f;
        m_Agent.baseOffset = 0.05f;
        m_BaseOffset = m_Agent.baseOffset;
        m_Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        m_Agent.updateRotation = true;
        m_Agent.autoBraking = true;
        Hold();
    }

    public void Bind(Animator animator, Transform exitDoor)
    {
        if (animator != null)
            m_Animator = animator;
        door = exitDoor;
        m_ExitPoint = ExitPoint();
    }

    public override void ResetHome()
    {
        base.ResetHome();
        m_Round++;
        m_Marching = false;
        m_Drifting = false;
        m_Gone = false;
        m_Agent.updateRotation = true;
        m_Agent.enabled = true;
        Vector3 home = Nearest(m_Home);
        if (m_Agent.isOnNavMesh)
            m_Agent.Warp(home);
        else
            transform.SetPositionAndRotation(home, m_HomeRotation);

        Hold();
        if (m_Animator != null)
            m_Animator.speed = 0f;
    }

    public void BeginMarch()
    {
        if (m_Marching || !Visible)
            return;

        if (drift != DriftKind.None)
        {
            m_Drifting = true;
            m_Gone = false;
            Hold();
            if (m_Animator != null)
                m_Animator.speed = 0f;
            return;
        }

        m_Gone = false;
        Vector3 start = Nearest(m_Home);
        m_Agent.enabled = true;
        if (!m_Agent.isOnNavMesh)
            m_Agent.Warp(start);

        m_ExitPoint = ExitPoint();
        if (!m_Agent.isOnNavMesh)
        {
            m_Agent.enabled = false;
            return;
        }

        m_Agent.SetDestination(Nearest(m_ExitPoint));
        m_Agent.isStopped = false;
        m_Agent.updateRotation = true;
        m_Marching = true;
        if (m_Animator != null)
            m_Animator.speed = 1f;
    }

    void Update()
    {
        if (!Visible)
            return;

        if (HoldForStare())
            return;

        if (m_Drifting)
        {
            if (drift == DriftKind.Tail)
                StickBehind();
            else
                DriftBackward();
            return;
        }

        m_Agent.baseOffset = m_BaseOffset + BobOffset();
        if (!m_Marching)
        {
            BobUpright();
            return;
        }

        m_Agent.updateRotation = true;

        if (!m_Agent.enabled || !m_Agent.isOnNavMesh || m_Agent.pathPending)
            return;

        float remaining = m_Agent.remainingDistance;
        if (float.IsInfinity(remaining) || remaining > 0.55f)
            return;

        ArriveAtDoor();
    }

    void DriftBackward()
    {
        Transform player = Player();
        if (player == null)
        {
            BobUpright();
            return;
        }

        Vector3 toward = player.position - transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > 1.8f * 1.8f)
            transform.position += toward.normalized * 1.35f * Time.deltaTime;

        if (toward.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(-toward.normalized, Vector3.up);

        Vector3 position = transform.position;
        position.y = m_Home.y + 0.28f + BobOffset();
        transform.position = position;
    }

    void StickBehind()
    {
        Transform player = Player();
        if (player == null)
            return;

        Vector3 behind = -player.forward;
        behind.y = 0f;
        if (behind.sqrMagnitude < 0.001f)
            behind = Vector3.back;
        behind.Normalize();

        Vector3 position = player.position + behind * 1.2f;
        position.y = player.position.y + 0.2f + BobOffset() * 0.25f;
        transform.position = position;
        transform.rotation = Quaternion.LookRotation(-behind, Vector3.up);
    }

    void ArriveAtDoor()
    {
        m_Marching = false;
        Hold();
        SetGhostVisible(false);
        if (!m_Repeat || m_Gone)
            return;

        m_Gone = true;
        int round = m_Round;
        StartCoroutine(Respawn(round));
    }

    System.Collections.IEnumerator Respawn(int round)
    {
        yield return new WaitForSeconds(0.45f);
        if (round != m_Round || !m_Repeat)
            yield break;

        m_Gone = false;
        SetGhostVisible(true);
        m_Agent.enabled = true;
        Vector3 home = Nearest(m_Home);
        if (m_Agent.isOnNavMesh)
            m_Agent.Warp(home);
        else
            transform.position = home;

        BeginMarch();
    }

    protected override Quaternion ActivityFacing()
    {
        if (!m_Marching || m_Agent == null || !m_Agent.enabled || !m_Agent.isOnNavMesh)
            return m_HomeRotation;

        Vector3 forward = m_Agent.desiredVelocity;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.04f)
        {
            forward = m_Agent.steeringTarget - transform.position;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.04f)
            return transform.rotation;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    protected override void OnActivityPaused()
    {
        if (m_Agent != null && m_Agent.enabled && m_Agent.isOnNavMesh)
        {
            m_Agent.isStopped = true;
            m_Agent.updateRotation = false;
            m_Agent.velocity = Vector3.zero;
        }

        if (m_Animator != null)
            m_Animator.speed = 0f;
    }

    protected override void OnActivityResumed()
    {
        if (!m_Marching || m_Agent == null || !m_Agent.enabled)
            return;

        m_Agent.isStopped = false;
        m_Agent.updateRotation = true;
        if (m_Animator != null)
            m_Animator.speed = 1f;
    }

    void Hold()
    {
        if (m_Agent == null)
            return;

        if (m_Agent.enabled && m_Agent.isOnNavMesh)
        {
            m_Agent.ResetPath();
            m_Agent.velocity = Vector3.zero;
        }

        m_Agent.enabled = false;
    }

    Vector3 ExitPoint()
    {
        if (door == null)
            return m_Home + transform.forward * 4f;

        return door.position + Vector3.forward * 1.8f;
    }

    static Vector3 Nearest(Vector3 point)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(point, out hit, 3f, NavMesh.AllAreas))
            return hit.position;
        return point;
    }
}
