using UnityEngine;

[DefaultExecutionOrder(10000)]
public class PlayerMovement : MonoBehaviour
{
    public float turnSpeed = 20f;

    [Header("View pivot")]
    public Vector3 headOffset = new Vector3(0f, 1.2f, 0f);
    public float orbitRadius = 0.15f;
    public float fieldOfView = 78f;
    public float nearClipPlane = 0.05f;

    [Header("Look limits")]
    public float minPitch = -35f;
    public float maxPitch = 60f;
    public float lookSensitivity = 3f;
    public float runMultiplier = 2f;
    public bool controlsLocked;

    Animator m_Animator;
    Rigidbody m_Rigidbody;
    Camera m_Camera;
    Transform[] m_HeadBones;
    Vector3 m_Movement;
    Quaternion m_Rotation = Quaternion.identity;
    float m_LookYaw;
    float m_LookPitch;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Camera = Camera.main;
        m_LookYaw = transform.eulerAngles.y;
        m_Rotation = transform.rotation;
        SetCursorLocked(true);
    }

    public void SuppressHead(float amount)
    {
        if (m_HeadBones == null || m_HeadBones.Length == 0)
            m_HeadBones = CollectHeadBones(transform);

        Vector3 hidden = Vector3.one * 0.001f;
        float t = Mathf.Clamp01(amount);
        for (int i = 0; i < m_HeadBones.Length; i++)
        {
            if (m_HeadBones[i] != null)
                m_HeadBones[i].localScale = Vector3.Lerp(Vector3.one, hidden, t);
        }
    }

    public void TeleportKeepingLook(Vector3 position, Quaternion rotation, float yawDelta)
    {
        m_LookYaw += yawDelta;
        m_Rotation = rotation;

        if (m_Rigidbody != null)
        {
            Vector3 velocity = m_Rigidbody.velocity;
            Vector3 angularVelocity = m_Rigidbody.angularVelocity;
            m_Rigidbody.position = position;
            m_Rigidbody.rotation = rotation;
            m_Rigidbody.velocity = velocity;
            m_Rigidbody.angularVelocity = angularVelocity;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    void OnDisable()
    {
        SuppressHead(0f);
        SetCursorLocked(false);
    }

    public void Halt()
    {
        m_Movement = Vector3.zero;
        if (m_Animator != null)
        {
            m_Animator.SetBool("IsWalking", false);
            m_Animator.speed = 1f;
        }
        if (m_Rigidbody != null)
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void PlaceFacing(Vector3 position, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        m_LookYaw = yaw;
        m_LookPitch = 0f;
        m_Rotation = rotation;
        m_Movement = Vector3.zero;
        if (m_Rigidbody != null)
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.position = position;
            m_Rigidbody.rotation = rotation;
        }

        transform.SetPositionAndRotation(position, rotation);
    }

    void Update()
    {
        if (controlsLocked)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLocked(false);
        else if (Input.GetMouseButtonDown(0))
            SetCursorLocked(true);

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        m_LookYaw += Input.GetAxis("Mouse X") * lookSensitivity;
        m_LookPitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        m_LookPitch = Mathf.Clamp(m_LookPitch, minPitch, maxPitch);
    }

    void FixedUpdate()
    {
        if (controlsLocked)
        {
            m_Movement = Vector3.zero;
            if (m_Animator != null)
            {
                m_Animator.SetBool("IsWalking", false);
                m_Animator.speed = 1f;
            }
            m_Rotation = transform.rotation;
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Quaternion planarLook = Quaternion.Euler(0f, m_LookYaw, 0f);
        Vector3 planarForward = planarLook * Vector3.forward;
        Vector3 planarRight = planarLook * Vector3.right;

        m_Movement = planarRight * horizontal + planarForward * vertical;
        m_Movement.y = 0f;

        bool hasHorizontalInput = !Mathf.Approximately(horizontal, 0f);
        bool hasVerticalInput = !Mathf.Approximately(vertical, 0f);
        bool isWalking = hasHorizontalInput || hasVerticalInput;

        if (m_Movement.sqrMagnitude > 0f)
            m_Movement.Normalize();

        bool running = isWalking && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        m_Animator.speed = running ? Mathf.Max(1f, runMultiplier) : 1f;
        m_Animator.SetBool("IsWalking", isWalking);

        if (isWalking)
        {
            Vector3 desiredForward = Vector3.RotateTowards(
                transform.forward,
                planarForward,
                turnSpeed * Time.deltaTime,
                0f
            );
            m_Rotation = Quaternion.LookRotation(desiredForward);
        }
        else
        {
            m_Rotation = transform.rotation;
        }
    }

    Vector3 m_ShoveDir;
    float m_ShoveRemain;
    float m_ShoveSpan;
    float m_ShoveHeight;
    float m_ShoveGroundY;
    float m_ShoveLock;

    public void Shove(Vector3 direction)
    {
        if (Time.time < m_ShoveLock)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        m_ShoveDir = direction.normalized;
        m_ShoveSpan = 1.7f;
        m_ShoveRemain = m_ShoveSpan;
        m_ShoveHeight = 0.85f;
        m_ShoveGroundY = m_Rigidbody != null ? m_Rigidbody.position.y : transform.position.y;
        m_ShoveLock = Time.time + 0.4f;
    }

    void OnAnimatorMove()
    {
        Vector3 position = m_Rigidbody.position + m_Movement * m_Animator.deltaPosition.magnitude;
        if (m_ShoveRemain > 0f)
        {
            float step = Mathf.Min(5.5f * Time.deltaTime, m_ShoveRemain);
            m_ShoveRemain -= step;
            float along = 1f - (m_ShoveRemain / m_ShoveSpan);
            position += m_ShoveDir * step;
            position.y = m_ShoveGroundY + Mathf.Sin(along * Mathf.PI) * m_ShoveHeight;
        }

        m_Rigidbody.MovePosition(position);
        m_Rigidbody.MoveRotation(m_Rotation);
    }

    void LateUpdate()
    {
        SuppressHead(1f);

        if (m_Camera == null)
            return;

        Vector3 root = m_Rigidbody != null ? m_Rigidbody.position : transform.position;
        Quaternion yawOnly = Quaternion.Euler(0f, m_LookYaw, 0f);
        Quaternion look = Quaternion.Euler(m_LookPitch, m_LookYaw, 0f);
        Vector3 head = root + yawOnly * headOffset;
        Vector3 cameraPosition = head + yawOnly * Vector3.forward * orbitRadius;
        m_Camera.fieldOfView = fieldOfView;
        m_Camera.nearClipPlane = nearClipPlane;
        m_Camera.transform.SetPositionAndRotation(cameraPosition, look);
    }

    static Transform[] CollectHeadBones(Transform root)
    {
        string[] names = { "Head", "Neck", "LeftEye", "RightEye", "Mouth" };
        var found = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < names.Length; i++)
        {
            Transform bone = FindNamedChild(root, names[i]);
            if (bone != null)
                found.Add(bone);
        }

        return found.ToArray();
    }

    static Transform FindNamedChild(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
