using UnityEngine;

public class GhostBather : GhostActor
{
    public float spinSpeed = 240f;
    public float flySpeed = 0.65f;
    public float flyHeight = 4f;
    public bool canFly;

    GhostSpeechBubble m_Bubble;
    float m_Lift;
    bool m_Flying;

    protected override void Awake()
    {
        base.Awake();
        bobHeight = 0.07f;
    }

    protected override Quaternion ActivityFacing()
    {
        return transform.rotation;
    }

    public void Bind(GhostSpeechBubble bubble, string line)
    {
        m_Bubble = bubble;
        if (m_Bubble != null)
        {
            m_Bubble.SetLine(line);
            m_Bubble.Attach(transform, 2.15f);
        }
    }

    public void SetLine(string line)
    {
        if (m_Bubble != null)
            m_Bubble.SetLine(line);
    }

    void Update()
    {
        if (!Visible)
        {
            if (m_Bubble != null)
                m_Bubble.SetShown(false);
            return;
        }

        if (m_Bubble != null)
            m_Bubble.SetShown(true);

        if (HoldForStare())
        {
            Vector3 held = transform.position;
            held.y = m_Home.y + m_Lift;
            transform.position = held;
            return;
        }

        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (canFly && m_Flying)
            m_Lift = Mathf.MoveTowards(m_Lift, flyHeight, flySpeed * Time.deltaTime);

        Vector3 position = transform.position;
        position.y = m_Home.y + m_Lift + BobOffset();
        transform.position = position;
    }

    void LateUpdate()
    {
        if (!canFly || !Visible || m_Flying)
            return;

        Transform player = Player();
        if (player == null)
            return;

        Vector3 offset = player.position - m_Home;
        offset.y = 0f;
        if (offset.sqrMagnitude <= stareDistance * stareDistance)
            m_Flying = true;
    }

    void OnDisable()
    {
        if (m_Bubble != null)
            m_Bubble.SetShown(false);
    }

    public override void ResetHome()
    {
        m_Lift = 0f;
        m_Flying = false;
        base.ResetHome();
        if (m_Bubble != null)
            m_Bubble.SetShown(Visible);
    }
}
