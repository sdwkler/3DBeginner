using UnityEngine;
using UnityEngine.UI;

public class GhostSpeechBubble : MonoBehaviour
{
    Text m_Text;
    Transform m_Anchor;
    float m_Height = 2.15f;

    public static GhostSpeechBubble Create(string line)
    {
        GameObject root = new GameObject("SpeechBubble");
        GhostSpeechBubble bubble = root.AddComponent<GhostSpeechBubble>();
        bubble.Build(line);
        return bubble;
    }

    public void Attach(Transform anchor, float height)
    {
        m_Anchor = anchor;
        m_Height = height;
    }

    public void SetLine(string line)
    {
        if (m_Text != null)
            m_Text.text = line;
    }

    public void SetShown(bool shown)
    {
        if (gameObject.activeSelf != shown)
            gameObject.SetActive(shown);
    }

    void LateUpdate()
    {
        if (m_Anchor == null)
            return;

        transform.position = m_Anchor.position + Vector3.up * m_Height;
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 forward = transform.position - camera.transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    void Build(string line)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 70f);
        transform.localScale = Vector3.one * 0.012f;

        GameObject plateObject = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        plateObject.transform.SetParent(transform, false);
        RectTransform plateRect = plateObject.GetComponent<RectTransform>();
        plateRect.anchorMin = Vector2.zero;
        plateRect.anchorMax = Vector2.one;
        plateRect.offsetMin = Vector2.zero;
        plateRect.offsetMax = Vector2.zero;
        Image plate = plateObject.GetComponent<Image>();
        plate.sprite = WhiteSprite();
        plate.color = new Color(0.93f, 0.9f, 0.82f, 0.92f);
        plate.raycastTarget = false;

        GameObject textObject = new GameObject("Line", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(plateObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        m_Text = textObject.GetComponent<Text>();
        m_Text.font = LoadFont();
        m_Text.fontSize = 28;
        m_Text.alignment = TextAnchor.MiddleCenter;
        m_Text.color = new Color(0.15f, 0.08f, 0.08f, 1f);
        m_Text.horizontalOverflow = HorizontalWrapMode.Wrap;
        m_Text.verticalOverflow = VerticalWrapMode.Overflow;
        m_Text.raycastTarget = false;
        m_Text.text = line;
    }

    static Font LoadFont()
    {
        string[] candidates = { "KaiTi", "STKaiti", "SimHei", "Microsoft YaHei", "SimSun" };
        try
        {
            Font font = Font.CreateDynamicFontFromOSFont(candidates, 32);
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
}
