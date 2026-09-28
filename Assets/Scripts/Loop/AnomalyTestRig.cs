using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class AnomalyTestRig : MonoBehaviour
{
    struct Choice
    {
        public string id;
        public string label;
        public bool on;
        public Text text;
    }

    Choice[] m_Choices;
    Text m_Status;
    bool m_Adjusting;
    bool m_CursorWasLocked;
    PlayerMovement m_Player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;
        if (FindObjectOfType<AnomalyTestRig>() != null)
            return;

        GameObject go = new GameObject("AnomalyTest");
        go.AddComponent<AnomalyTestRig>();
    }

    void Awake()
    {
        m_Player = FindObjectOfType<PlayerMovement>();
        BuildChoices();
        BuildPanel();
        RoomCycle.RoundBegan += OnRound;
    }

    void OnDestroy()
    {
        RoomCycle.RoundBegan -= OnRound;
    }

    void Update()
    {
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        if (alt != m_Adjusting)
            SetAdjusting(alt);

        if (!m_Adjusting)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (m_Player != null && m_Player.enabled)
            m_Player.controlsLocked = true;
    }

    void OnRound(AnomalyRoll roll)
    {
        if (!m_Adjusting)
            CopyRoll(roll);
        RefreshStatus(roll);
    }

    void SetAdjusting(bool open)
    {
        if (open == m_Adjusting)
            return;

        m_Adjusting = open;
        if (m_Player == null)
            m_Player = FindObjectOfType<PlayerMovement>();

        if (open)
        {
            m_CursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            if (RoomCycle.Instance != null)
                CopyRoll(RoomCycle.Instance.CurrentRoll);
            PaintChoices();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (m_Player != null && m_Player.enabled)
                m_Player.controlsLocked = true;
            if (RoomCycle.Instance != null)
                RefreshStatus(RoomCycle.Instance.CurrentRoll);
            return;
        }

        if (RoomCycle.Instance != null)
            RefreshStatus(RoomCycle.Instance.CurrentRoll);

        if (m_Player != null && m_Player.enabled)
            m_Player.controlsLocked = false;
        if (m_CursorWasLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Confirm()
    {
        var ids = new List<string>();
        for (int i = 0; i < m_Choices.Length; i++)
        {
            if (m_Choices[i].on)
                ids.Add(m_Choices[i].id);
        }

        if (RoomCycle.Instance != null)
        {
            RoomCycle.Instance.DebugRefresh(ids.ToArray());
            RefreshStatus(RoomCycle.Instance.CurrentRoll);
        }
    }

    void CopyRoll(AnomalyRoll roll)
    {
        for (int i = 0; i < m_Choices.Length; i++)
            m_Choices[i].on = roll.Contains(m_Choices[i].id);
        PaintChoices();
    }

    void PaintChoices()
    {
        for (int i = 0; i < m_Choices.Length; i++)
        {
            if (m_Choices[i].text == null)
                continue;
            m_Choices[i].text.text = (m_Choices[i].on ? "■ " : "□ ") + m_Choices[i].label;
            m_Choices[i].text.color = m_Choices[i].on ? Color.white : new Color(0.78f, 0.78f, 0.78f, 0.85f);
        }
    }

    void RefreshStatus(AnomalyRoll roll)
    {
        if (m_Status == null)
            return;

        int stage = LoopProgress.Instance != null ? LoopProgress.Instance.stage : 0;
        string body = "第 " + stage + " 层\n";
        if (!roll.hasAnomaly)
            body += "异常：无";
        else
        {
            body += "异常：有";
            if (roll.anomalyIds != null)
            {
                for (int i = 0; i < roll.anomalyIds.Length; i++)
                    body += "\n· " + LabelOf(roll.anomalyIds[i]);
            }
        }

        body += m_Adjusting ? "\n\n勾选后按确认，只刷新这一层" : "\n\n按住 Alt 解放鼠标";
        m_Status.text = body;
    }

    string LabelOf(string id)
    {
        for (int i = 0; i < m_Choices.Length; i++)
        {
            if (m_Choices[i].id == id)
                return m_Choices[i].label;
        }

        return id;
    }

    void Toggle(int index)
    {
        m_Choices[index].on = !m_Choices[index].on;
        PaintChoices();
    }

    void BuildChoices()
    {
        m_Choices = new[]
        {
            Line(GhostDirector.StareId, "初级 靠近就转向"),
            Line(GhostDirector.WalkerRepeatId, "初级 走路消失后再走一遍"),
            Line(GhostDirector.WalkerPlayerId, "初级 玩家模型走路"),
            Line(GhostDirector.BatherPlayerId, "初级 玩家模型洗澡"),
            Line(GhostDirector.FlickerSomeId, "初级 部分灯灭1秒再亮"),
            Line(GhostDirector.FlickerAllId, "中级 所有灯一起灭2秒再亮"),
            Line(GhostDirector.FlickerDieId, "高级 靠近后闪一下再灭"),
            Line(GhostDirector.ColorSomeId, "初级 部分灯变绿"),
            Line(GhostDirector.ColorNearId, "中级 灯变绿，靠近恢复"),
            Line(GhostDirector.LampShowerId, "初级 部分灯换成花洒"),
            Line(GhostDirector.LampMonsterId, "中级 部分灯换成怪物灯"),
            Line(GhostDirector.MonsterRedId, "初级 怪物灯变绿"),
            Line(GhostDirector.WalkerBackId, "中级 走路的倒着飘来"),
            Line(GhostDirector.WalkerTailId, "高级 走路的贴在身后"),
            Line(GhostDirector.GargoyleGarbledId, "初级 石像鬼指示乱码"),
            Line(GhostDirector.GargoyleCatchId, "中级 石像鬼红灯抓人"),
            Line(GhostDirector.ChairPileId, "初级 桌上多出很多椅子"),
            Line(GhostDirector.ChairHuntId, "中级 视野外的椅子才会移动"),
            Line(GhostDirector.ChairFloatId, "中级 椅子失重漂浮")
        };
    }

    static Choice Line(string id, string label)
    {
        Choice choice = new Choice();
        choice.id = id;
        choice.label = label;
        return choice;
    }

    void BuildPanel()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetParent(transform, false);
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject canvasObject = new GameObject("AnomalyDebug", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Box", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-18f, -18f);
        panelRect.sizeDelta = new Vector2(430f, 820f);
        Image plate = panel.GetComponent<Image>();
        plate.sprite = White();
        plate.color = new Color(0f, 0f, 0f, 0.62f);
        plate.raycastTarget = true;

        m_Status = CreateText(panel.transform, font, 22, TextAnchor.UpperLeft);
        RectTransform statusRect = m_Status.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0f, -12f);
        statusRect.sizeDelta = new Vector2(-24f, 150f);
        m_Status.text = "第 0 层\n异常：无\n\n按住 Alt 解放鼠标";

        for (int i = 0; i < m_Choices.Length; i++)
        {
            int index = i;
            Button button = CreateButton(panel.transform, font);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -168f - i * 28f);
            rect.sizeDelta = new Vector2(-20f, 26f);
            m_Choices[i].text = button.GetComponentInChildren<Text>();
            button.onClick.AddListener(() => Toggle(index));
        }

        Button confirm = CreateButton(panel.transform, font);
        RectTransform confirmRect = confirm.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.pivot = new Vector2(0.5f, 0f);
        confirmRect.anchoredPosition = new Vector2(0f, 12f);
        confirmRect.sizeDelta = new Vector2(180f, 40f);
        Text confirmText = confirm.GetComponentInChildren<Text>();
        confirmText.text = "确认";
        confirmText.alignment = TextAnchor.MiddleCenter;
        confirmText.color = Color.white;
        confirm.onClick.AddListener(Confirm);
        PaintChoices();
    }

    static Text CreateText(Transform parent, Font font, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject("Status", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Button CreateButton(Transform parent, Font font)
    {
        GameObject go = new GameObject("Choice", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = White();
        image.color = new Color(1f, 1f, 1f, 0.08f);
        Button button = go.GetComponent<Button>();
        Text text = CreateText(go.transform, font, 20, TextAnchor.MiddleLeft);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);
        text.raycastTarget = false;
        return button;
    }

    static Sprite White()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }
}
