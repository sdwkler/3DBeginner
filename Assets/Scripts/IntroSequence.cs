using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    public PlayerMovement playerMovement;
    public Camera viewCamera;

    [Header("Overlook")]
    public float behindDistance = 3.4f;
    public float overlookHeight = 4.2f;
    public float lookAtHeight = 1f;
    public float overlookFieldOfView = 46f;

    [Header("Handoff")]
    public float cameraBlendDuration = 2.4f;
    public float menuFadeDuration = 0.45f;
    public float forcedWalkDistance = 2f;
    public float forcedWalkSpeed = 1.2f;

    enum Phase
    {
        Title,
        Playing,
        Done
    }

    Phase m_Phase = Phase.Title;
    CanvasGroup m_MenuGroup;
    RectTransform m_TitleRoot;
    Text m_Title;
    Text m_TitleGhost;
    Vector3 m_OverlookPosition;
    Quaternion m_OverlookRotation;
    float m_Blend;
    float m_Walked;
    bool m_WalkDone;
    bool m_ApplyRootMotion;
    Vector3 m_WalkForward;
    Rigidbody m_Rigidbody;
    Animator m_Animator;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "SampleScene")
            return;

        if (FindFirst<IntroSequence>() != null)
            return;

        GameObject go = new GameObject("IntroSequence");
        go.AddComponent<IntroSequence>();
    }

    void Awake()
    {
        if (playerMovement == null)
            playerMovement = FindFirst<PlayerMovement>();

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (playerMovement != null)
            playerMovement.enabled = false;

        Behaviour brain = viewCamera != null
            ? viewCamera.GetComponent("CinemachineBrain") as Behaviour
            : null;
        if (brain != null)
            brain.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BuildMenu();
    }

    void Start()
    {
        if (playerMovement == null || viewCamera == null)
        {
            Debug.LogError("IntroSequence 缺少玩家或主摄像机。");
            return;
        }

        Transform player = playerMovement.transform;
        m_Rigidbody = playerMovement.GetComponent<Rigidbody>();
        m_Animator = playerMovement.GetComponent<Animator>();
        if (m_Animator != null)
            m_ApplyRootMotion = m_Animator.applyRootMotion;

        Vector3 forward = Flatten(player.forward);
        Vector3 origin = m_Rigidbody != null ? m_Rigidbody.position : player.position;
        m_OverlookPosition = origin - forward * behindDistance + Vector3.up * overlookHeight;
        Vector3 lookTarget = origin + Vector3.up * lookAtHeight;
        m_OverlookRotation = Quaternion.LookRotation(lookTarget - m_OverlookPosition, Vector3.up);
        ApplyCamera(0f);
    }

    void Update()
    {
        if (m_Phase == Phase.Title)
            FlickerTitle();

        if (m_Phase != Phase.Playing || m_MenuGroup == null)
            return;

        m_MenuGroup.alpha = Mathf.MoveTowards(
            m_MenuGroup.alpha, 0f, Time.deltaTime / Mathf.Max(0.01f, menuFadeDuration));
    }

    void FixedUpdate()
    {
        if (m_Phase != Phase.Playing || m_WalkDone || m_Rigidbody == null)
            return;

        float step = forcedWalkSpeed * Time.fixedDeltaTime;
        m_Rigidbody.MovePosition(m_Rigidbody.position + m_WalkForward * step);
        m_Walked += step;
        if (m_Walked < forcedWalkDistance)
            return;

        m_WalkDone = true;
        if (m_Animator != null)
            m_Animator.SetBool("IsWalking", false);
    }

    void LateUpdate()
    {
        if (viewCamera == null || m_Phase == Phase.Done)
            return;

        if (m_Phase == Phase.Title)
        {
            ApplyCamera(0f);
            return;
        }

        m_Blend = Mathf.MoveTowards(m_Blend, 1f, Time.deltaTime / Mathf.Max(0.01f, cameraBlendDuration));
        float shaped = Mathf.SmoothStep(0f, 1f, m_Blend);
        ApplyCamera(shaped);
        playerMovement.SuppressHead(shaped);

        if (m_Blend >= 1f && m_WalkDone)
            HandOff();
    }

    public void Begin()
    {
        if (m_Phase != Phase.Title || playerMovement == null)
            return;

        m_Phase = Phase.Playing;
        if (m_MenuGroup != null)
        {
            m_MenuGroup.interactable = false;
            m_MenuGroup.blocksRaycasts = false;
        }

        Transform player = playerMovement.transform;
        m_WalkForward = Flatten(player.forward);
        playerMovement.SuppressHead(0f);

        if (m_Animator != null)
        {
            m_Animator.applyRootMotion = false;
            m_Animator.SetBool("IsWalking", true);
        }
    }

    void HandOff()
    {
        m_Phase = Phase.Done;
        playerMovement.SuppressHead(1f);

        if (m_Animator != null)
        {
            m_Animator.SetBool("IsWalking", false);
            m_Animator.applyRootMotion = m_ApplyRootMotion;
        }

        if (m_MenuGroup != null)
            m_MenuGroup.gameObject.SetActive(false);

        playerMovement.enabled = true;
    }

    void ApplyCamera(float blend)
    {
        Vector3 fpsPosition;
        Quaternion fpsRotation;
        GetFirstPersonPose(out fpsPosition, out fpsRotation);

        viewCamera.fieldOfView = Mathf.Lerp(overlookFieldOfView, playerMovement.fieldOfView, blend);
        viewCamera.nearClipPlane = Mathf.Lerp(0.3f, playerMovement.nearClipPlane, blend);
        viewCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(m_OverlookPosition, fpsPosition, blend),
            Quaternion.Slerp(m_OverlookRotation, fpsRotation, blend));
    }

    void GetFirstPersonPose(out Vector3 position, out Quaternion rotation)
    {
        Transform player = playerMovement.transform;
        float yaw = player.eulerAngles.y;
        Quaternion yawOnly = Quaternion.Euler(0f, yaw, 0f);
        Vector3 root = m_Rigidbody != null ? m_Rigidbody.position : player.position;
        Vector3 head = root + yawOnly * playerMovement.headOffset;
        position = head + yawOnly * Vector3.forward * playerMovement.orbitRadius;
        rotation = yawOnly;
    }

    void FlickerTitle()
    {
        if (m_TitleRoot == null)
            return;

        float time = Time.unscaledTime;
        float shake = Mathf.PerlinNoise(time * 9f, 0.2f) - 0.5f;
        m_TitleRoot.anchoredPosition = new Vector2(shake * 10f, 220f + Mathf.Sin(time * 2.4f) * 6f);
        m_TitleRoot.localRotation = Quaternion.Euler(0f, 0f, shake * 1.6f);

        float pulse = 0.78f + Mathf.Abs(Mathf.Sin(time * 7.5f)) * 0.22f;
        if (Random.value < 0.04f)
            pulse *= 0.55f;

        Color ink = new Color(0.62f, 0.04f, 0.05f, pulse);
        m_Title.color = ink;
        if (m_TitleGhost != null)
        {
            m_TitleGhost.color = new Color(0.25f, 0f, 0f, pulse * 0.55f);
            m_TitleGhost.rectTransform.anchoredPosition = new Vector2(7f + shake * 8f, -6f);
        }
    }

    void BuildMenu()
    {
        EnsureEventSystem();

        Font font = LoadHorrorFont();
        GameObject canvasObject = new GameObject(
            "IntroCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        m_MenuGroup = canvasObject.AddComponent<CanvasGroup>();

        Image veil = CreateImage("Veil", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        veil.color = new Color(0f, 0f, 0f, 0.38f);
        veil.raycastTarget = true;

        m_TitleRoot = CreateRect("TitleRoot", canvasObject.transform);
        m_TitleRoot.anchorMin = new Vector2(0.5f, 0.5f);
        m_TitleRoot.anchorMax = new Vector2(0.5f, 0.5f);
        m_TitleRoot.pivot = new Vector2(0.5f, 0.5f);
        m_TitleRoot.sizeDelta = new Vector2(1200f, 220f);
        m_TitleRoot.anchoredPosition = new Vector2(0f, 220f);

        m_TitleGhost = CreateText("TitleGhost", m_TitleRoot, font, 118, FontStyle.Bold, TextAnchor.MiddleCenter);
        m_TitleGhost.text = "七闯鬼屋";
        m_TitleGhost.color = new Color(0.2f, 0f, 0f, 0.45f);
        m_TitleGhost.rectTransform.anchoredPosition = new Vector2(8f, -8f);
        m_TitleGhost.raycastTarget = false;

        m_Title = CreateText("Title", m_TitleRoot, font, 118, FontStyle.Bold, TextAnchor.MiddleCenter);
        m_Title.text = "七闯鬼屋";
        m_Title.color = new Color(0.62f, 0.04f, 0.05f, 1f);
        m_Title.raycastTarget = false;
        Shadow shadow = m_Title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(5f, -5f);

        GameObject buttonObject = new GameObject(
            "StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(280f, 78f);
        buttonRect.anchoredPosition = Vector2.zero;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = WhiteSprite();
        buttonImage.color = new Color(0.28f, 0.02f, 0.03f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.28f, 0.02f, 0.03f, 0.94f);
        colors.highlightedColor = new Color(0.55f, 0.06f, 0.07f, 1f);
        colors.pressedColor = new Color(0.12f, 0f, 0f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(Begin);

        Text label = CreateText("Label", buttonRect, font, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
        label.text = "开始";
        label.color = new Color(0.93f, 0.86f, 0.78f, 1f);
        label.raycastTarget = false;
    }

    static void EnsureEventSystem()
    {
        if (FindFirst<EventSystem>() != null)
            return;

        GameObject system = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        system.transform.SetParent(null);
    }

    static Font LoadHorrorFont()
    {
        string[] candidates = { "KaiTi", "STKaiti", "SimHei", "Microsoft YaHei", "SimSun" };
        try
        {
            Font font = Font.CreateDynamicFontFromOSFont(candidates, 120);
            if (font != null)
                return font;
        }
        catch (System.Exception)
        {
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Image CreateImage(
        string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = go.GetComponent<Image>();
        image.sprite = WhiteSprite();
        return image;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Text CreateText(string name, Transform parent, Font font, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        return text;
    }

    static Sprite WhiteSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }

    static T FindFirst<T>() where T : Object
    {
        T[] results = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] is Component component)
            {
                GameObject go = component.gameObject;
                if (!go.scene.IsValid() || go.hideFlags != HideFlags.None)
                    continue;
            }

            return results[i];
        }

        return null;
    }
}
