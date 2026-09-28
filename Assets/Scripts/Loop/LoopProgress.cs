using System.Collections.Generic;
using UnityEngine;

public class LoopProgress : MonoBehaviour
{
    public static LoopProgress Instance { get; private set; }

    [Tooltip("当前点亮的灯数。选错会回到 0。")]
    public int stage;

    public int LampCount
    {
        get
        {
            if (m_Lamps == null)
                CollectLamps();
            return m_Lamps != null ? m_Lamps.Length : 0;
        }
    }

    Transform[] m_Lamps;

    void Awake()
    {
        Instance = this;
        CollectLamps();
        if (stage < 0)
            stage = 0;
        Apply();
        EnsureCaption();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterCorrectEntry()
    {
        SetStage(stage + 1);
    }

    public void SetStage(int value)
    {
        int count = LampCount;
        if (value < 0)
            value = 0;
        if (count > 0 && value > count)
            value = count;
        stage = value;
        Apply();
    }

    public void Apply()
    {
        if (m_Lamps == null)
            CollectLamps();

        for (int i = 0; i < m_Lamps.Length; i++)
            SetLamp(m_Lamps[i], i < stage);
    }

    void CollectLamps()
    {
        var lamps = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<Light>(true) != null)
                lamps.Add(child);
        }

        m_Lamps = lamps.ToArray();
    }

    static void SetLamp(Transform lamp, bool on)
    {
        Light[] lights = lamp.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
            lights[i].enabled = on;

        LightFlicker[] flickers = lamp.GetComponentsInChildren<LightFlicker>(true);
        for (int i = 0; i < flickers.Length; i++)
        {
            flickers[i].enabled = on;
            if (on || flickers[i].flickeringRenderer == null)
                continue;

            Renderer renderer = flickers[i].flickeringRenderer;
            Material material = renderer.material;
            if (material != null && material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.black);
        }
    }

    void EnsureCaption()
    {
        if (transform.Find("FloorCaption") != null)
            return;

        Vector3 center = transform.position;
        int count = 0;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<Light>(true) == null)
                continue;
            sum += child.position;
            count++;
        }

        if (count > 0)
            center = sum / count;

        Transform facing = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<Light>(true) == null)
                continue;
            facing = child;
            break;
        }

        Vector3 forward = facing != null ? facing.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();
        center += Vector3.up * 0.28f + forward * 0.06f;

        GameObject root = new GameObject("FloorCaption");
        root.transform.SetParent(transform, true);
        root.transform.position = center;
        root.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        root.transform.localScale = Vector3.one * 0.0032f;
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(340f, 52f);

        GameObject plateObject = new GameObject("Plate", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        plateObject.transform.SetParent(root.transform, false);
        RectTransform plateRect = plateObject.GetComponent<RectTransform>();
        plateRect.anchorMin = Vector2.zero;
        plateRect.anchorMax = Vector2.one;
        plateRect.offsetMin = Vector2.zero;
        plateRect.offsetMax = Vector2.zero;
        UnityEngine.UI.Image plate = plateObject.GetComponent<UnityEngine.UI.Image>();
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        plate.sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        plate.color = new Color(0.12f, 0.1f, 0.08f, 0.82f);
        plate.raycastTarget = false;

        GameObject textObject = new GameObject("Line", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        textObject.transform.SetParent(plateObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        UnityEngine.UI.Text text = textObject.GetComponent<UnityEngine.UI.Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "鬼屋通关层数";
        text.raycastTarget = false;
        root.AddComponent<FloorCaption>();
    }
}

public class FloorCaption : MonoBehaviour
{
    Vector3 m_BasePosition;
    Quaternion m_BaseRotation;

    void Awake()
    {
        m_BasePosition = transform.localPosition;
        m_BaseRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        float time = Time.unscaledTime;
        float shake = Mathf.PerlinNoise(time * 9f, 0.2f) - 0.5f;
        transform.localPosition = m_BasePosition + new Vector3(shake * 0.04f, Mathf.Sin(time * 2.4f) * 0.025f, 0f);
        transform.localRotation = m_BaseRotation * Quaternion.Euler(0f, 0f, shake * 1.6f);
    }
}
