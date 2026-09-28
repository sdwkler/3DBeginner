using UnityEngine;

public class GhostActor : MonoBehaviour
{
    public float bobHeight = 0.12f;
    public float bobSpeed = 1.7f;
    public float stareDistance = 3.4f;
    public float returnSpeed = 70f;

    protected Vector3 m_Home;
    protected Quaternion m_HomeRotation;
    Renderer[] m_Renderers;
    float m_Phase;
    Transform m_Player;
    bool m_Visible = true;
    bool m_StareHeld;
    bool m_Returning;
    Quaternion m_ResumeRotation;

    public bool StareActive { get; set; }
    public bool Visible
    {
        get { return m_Visible; }
    }

    protected virtual void Awake()
    {
        m_Home = transform.position;
        m_HomeRotation = transform.rotation;
        m_Phase = Random.Range(0f, 6f);
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Transform[] nodes = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < nodes.Length; i++)
            nodes[i].gameObject.isStatic = false;

        m_Renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void SetGhostVisible(bool visible)
    {
        m_Visible = visible;
        if (m_Renderers == null)
            return;

        for (int i = 0; i < m_Renderers.Length; i++)
        {
            if (m_Renderers[i] != null)
                m_Renderers[i].enabled = visible;
        }
    }

    public virtual void ResetHome()
    {
        m_StareHeld = false;
        m_Returning = false;
        transform.SetPositionAndRotation(m_Home, m_HomeRotation);
    }

    protected bool HoldForStare()
    {
        if (StareActive && PlayerInRange())
        {
            if (!m_StareHeld)
            {
                m_ResumeRotation = ActivityFacing();
                m_StareHeld = true;
                m_Returning = false;
                OnActivityPaused();
            }

            SnapTowardPlayer();
            return true;
        }

        if (m_StareHeld)
        {
            m_StareHeld = false;
            m_Returning = true;
        }

        if (!m_Returning)
            return false;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            m_ResumeRotation,
            returnSpeed * Time.deltaTime);
        if (Quaternion.Angle(transform.rotation, m_ResumeRotation) > 0.5f)
            return true;

        transform.rotation = m_ResumeRotation;
        m_Returning = false;
        OnActivityResumed();
        return false;
    }

    protected virtual Quaternion ActivityFacing()
    {
        return m_HomeRotation;
    }

    protected virtual void OnActivityPaused()
    {
    }

    protected virtual void OnActivityResumed()
    {
    }

    bool PlayerInRange()
    {
        Transform player = Player();
        if (player == null)
            return false;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= stareDistance * stareDistance && offset.sqrMagnitude > 0.001f;
    }

    void SnapTowardPlayer()
    {
        Transform player = Player();
        if (player == null)
            return;

        Vector3 offset = player.position - transform.position;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
    }

    protected void BobUpright()
    {
        Vector3 position = transform.position;
        position.y = m_Home.y + Mathf.Sin((Time.time + m_Phase) * bobSpeed) * bobHeight;
        transform.position = position;
    }

    protected float BobOffset()
    {
        return Mathf.Sin((Time.time + m_Phase) * bobSpeed) * bobHeight;
    }

    protected Transform Player()
    {
        if (m_Player == null)
        {
            PlayerMovement movement = FindObjectOfType<PlayerMovement>();
            if (movement != null)
                m_Player = movement.transform;
        }

        return m_Player;
    }
}
