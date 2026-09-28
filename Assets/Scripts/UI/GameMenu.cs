using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    public static GameMenu Instance { get; private set; }

    CanvasGroup m_Dialog;
    CanvasGroup m_Victory;
    PlayerMovement m_Player;
    bool m_DialogOpen;
    bool m_VictoryOpen;
    float m_VictoryTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;
        if (FindObjectOfType<GameMenu>() != null)
            return;

        GameObject go = new GameObject("GameMenu");
        go.AddComponent<GameMenu>();
    }

    void Awake()
    {
        Instance = this;
        m_Player = FindObjectOfType<PlayerMovement>();
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (m_VictoryOpen)
        {
            m_VictoryTimer += Time.unscaledDeltaTime;
            if (m_VictoryTimer >= 3.2f)
                ReturnToStart();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (m_DialogOpen)
            CloseQuit();
        else if (m_Player != null && m_Player.enabled)
            OpenQuit();
    }

    public void ShowVictory()
    {
        m_VictoryOpen = true;
        m_VictoryTimer = 0f;
        m_DialogOpen = false;
        if (m_Dialog != null)
            m_Dialog.gameObject.SetActive(false);
        if (m_Victory != null)
            m_Victory.gameObject.SetActive(true);
        UnlockCursor();
    }

    public void OpenQuit()
    {
        if (m_VictoryOpen)
            return;

        m_DialogOpen = true;
        if (m_Dialog != null)
            m_Dialog.gameObject.SetActive(true);
        if (m_Player != null && m_Player.enabled)
            m_Player.controlsLocked = true;
        UnlockCursor();
    }

    public void CloseQuit()
    {
        m_DialogOpen = false;
        if (m_Dialog != null)
            m_Dialog.gameObject.SetActive(false);
        if (m_Player != null && m_Player.enabled)
        {
            m_Player.controlsLocked = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ConfirmQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ReturnToStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Build()
    {
        EnsureEventSystem();
        Font font = LoadFont();

        GameObject canvasObject = new GameObject(
            "GameMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Button quit = CreateButton(canvasObject.transform, font, "退出", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(168f, 64f), new Vector2(0f, 1f));
        quit.onClick.AddListener(OpenQuit);

        m_Dialog = CreatePanel(canvasObject.transform, font, "是否退出游戏？", "退出", "留下", ConfirmQuit, CloseQuit);
        m_Dialog.gameObject.SetActive(false);

        m_Victory = CreatePanel(canvasObject.transform, font, "闯关成功", "回到开始", null, ReturnToStart, null);
        Text victoryText = m_Victory.GetComponentInChildren<Text>();
        if (victoryText != null)
            victoryText.fontSize = 92;
        m_Victory.gameObject.SetActive(false);
    }

    CanvasGroup CreatePanel(
        Transform parent, Font font, string message, string confirmLabel, string cancelLabel,
        UnityEngine.Events.UnityAction onConfirm, UnityEngine.Events.UnityAction onCancel)
    {
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image veil = panel.GetComponent<Image>();
        veil.sprite = WhiteSprite();
        veil.color = new Color(0f, 0f, 0f, 0.72f);

        Text text = CreateText("Message", panel.transform, font, 64, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.text = message;
        text.color = new Color(0.86f, 0.78f, 0.7f, 1f);
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(900f, 140f);
        text.rectTransform.anchoredPosition = new Vector2(0f, 40f);

        Button confirm = CreateButton(panel.transform, font, confirmLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20f, -90f), new Vector2(220f, 70f), new Vector2(1f, 0.5f));
        confirm.onClick.AddListener(onConfirm);

        if (onCancel != null)
        {
            Button cancel = CreateButton(panel.transform, font, cancelLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(20f, -90f), new Vector2(220f, 70f), new Vector2(0f, 0.5f));
            cancel.onClick.AddListener(onCancel);
        }
        else
        {
            RectTransform confirmRect = confirm.GetComponent<RectTransform>();
            confirmRect.pivot = new Vector2(0.5f, 0.5f);
            confirmRect.anchoredPosition = new Vector2(0f, -90f);
        }

        return panel.GetComponent<CanvasGroup>();
    }

    static Button CreateButton(
        Transform parent, Font font, string label, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
    {
        GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = new Color(0.28f, 0.02f, 0.03f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.55f, 0.06f, 0.07f, 1f);
        colors.pressedColor = new Color(0.12f, 0f, 0f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Label", rect, font, 32, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.text = label;
        text.color = new Color(0.93f, 0.86f, 0.78f, 1f);
        text.raycastTarget = false;
        return button;
    }

    static Text CreateText(string name, Transform parent, Font font, int size, FontStyle style, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
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

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static Font LoadFont()
    {
        string[] candidates = { "KaiTi", "STKaiti", "SimHei", "Microsoft YaHei", "SimSun" };
        try
        {
            Font font = Font.CreateDynamicFontFromOSFont(candidates, 80);
            if (font != null)
                return font;
        }
        catch (System.Exception)
        {
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Sprite WhiteSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }

    static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
