using UnityEngine;
using UnityEngine.UI;

public class GargoyleGuide : MonoBehaviour
{
    static readonly Color Yellow = new Color(1f, 0.82f, 0.18f);
    static readonly Color Red = new Color(0.85f, 0.05f, 0.04f);

    Light m_Lamp;
    Transform m_Head;
    Text m_YesText;
    Text m_NoText;
    bool m_YesOnRight;
    bool m_NoOnRight;
    bool m_Catching;
    float m_CatchLock;

    public void Apply(AnomalyRoll roll)
    {
        EnsureBuilt();
        bool garbled = roll.Contains(GhostDirector.GargoyleGarbledId);
        m_Catching = roll.Contains(GhostDirector.GargoyleCatchId);
        m_YesText.text = Line(garbled ? "※＃&＠" : "有异常", m_YesOnRight);
        m_NoText.text = Line(garbled ? "□▓?＃" : "无异常", m_NoOnRight);
        PaintLamp();
    }

    void LateUpdate()
    {
        PaintLamp();
    }

    void PaintLamp()
    {
        if (m_Lamp != null)
            m_Lamp.color = m_Catching ? Red : Yellow;

        if (m_Cones == null)
            CacheCones();

        Color tint = m_Catching ? new Color(1f, 0f, 0f, 0.67f) : new Color(1f, 0.62f, 0.08f, 0.67f);
        Color glow = m_Catching ? new Color(1.4f, 0.04f, 0f) : new Color(1.7f, 0.85f, 0.12f);
        for (int i = 0; i < m_Cones.Length; i++)
        {
            Material cone = m_Cones[i];
            if (cone == null)
                continue;
            cone.SetColor("_Color", tint);
            cone.SetColor("_EmissionColor", glow);
            cone.EnableKeyword("_EMISSION");
        }
    }

    Material[] m_Cones;

    void CacheCones()
    {
        var found = new System.Collections.Generic.List<Material>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] shared = renderers[i].sharedMaterials;
            Material[] instanced = null;
            for (int j = 0; j < shared.Length; j++)
            {
                if (shared[j] == null || shared[j].name.IndexOf("Light_Cone", System.StringComparison.Ordinal) < 0)
                    continue;
                if (instanced == null)
                    instanced = renderers[i].materials;
                if (j < instanced.Length && instanced[j] != null)
                    found.Add(instanced[j]);
            }
        }

        m_Cones = found.ToArray();
    }

    static string Line(string body, bool onRight)
    {
        return onRight ? "←  " + body : body + "  →";
    }

    void Update()
    {
        if (!m_Catching || Time.time < m_CatchLock || TeleportMosaic.IsBusy)
            return;

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player == null || !player.enabled || player.controlsLocked)
            return;
        if (!InGrabLane(player.transform.position))
            return;

        Doorway exit = Outdoor();
        if (exit == null)
            return;

        m_CatchLock = Time.time + 2.5f;
        Vector3 position = exit.transform.TransformPoint(exit.spawnLocalOffset);
        float yaw = exit.exitYaw;
        player.controlsLocked = true;
        player.Halt();
        TeleportMosaic mosaic = TeleportMosaic.Ensure();
        StartCoroutine(SendOut(player, mosaic, position, yaw));
    }

    System.Collections.IEnumerator SendOut(PlayerMovement player, TeleportMosaic mosaic, Vector3 position, float yaw)
    {
        yield return mosaic.Play(() =>
        {
            player.PlaceFacing(position, yaw);
            player.Halt();
        });
        player.controlsLocked = false;
    }

    bool InGrabLane(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return local.z > 0.35f && local.z < 2.6f
            && Mathf.Abs(local.x) < 0.7f
            && local.y > -0.2f && local.y < 2.2f;
    }

    void EnsureBuilt()
    {
        if (m_YesText != null)
            return;

        m_Head = FindHead();
        m_Lamp = GetComponentInChildren<Light>(true);
        if (m_Lamp == null)
        {
            GameObject lampObject = new GameObject("Flashlight");
            lampObject.transform.SetParent(transform, false);
            Vector3 headLocal = transform.InverseTransformPoint(m_Head.position);
            lampObject.transform.localPosition = headLocal + new Vector3(0.15f, -0.15f, 0.25f);
            lampObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            m_Lamp = lampObject.AddComponent<Light>();
            m_Lamp.type = LightType.Spot;
            m_Lamp.range = 9f;
            m_Lamp.spotAngle = 48f;
            m_Lamp.intensity = 2.6f;
        }

        PaintLamp();
        Vector3 head = transform.InverseTransformPoint(m_Head.position);
        m_YesOnRight = OnRight("Final_door_Yes");
        m_NoOnRight = OnRight("Final_door_No");
        m_YesText = MakeLabel("有异常", m_YesOnRight, head.y);
        m_NoText = MakeLabel("无异常", m_NoOnRight, head.y);
    }

    bool OnRight(string doorName)
    {
        Vector3 flat = DoorPoint(doorName) - transform.position;
        flat.y = 0f;
        return Vector3.Dot(flat, transform.right) >= 0f;
    }

    Text MakeLabel(string line, bool onRight, float headY)
    {
        float side = onRight ? 0.62f : -0.62f;
        GameObject root = new GameObject(line);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(side, headY + 0.38f, 0.22f);
        root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        root.transform.localScale = Vector3.one * 0.0032f;
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240f, 64f);

        GameObject plateObject = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        plateObject.transform.SetParent(root.transform, false);
        RectTransform plateRect = plateObject.GetComponent<RectTransform>();
        plateRect.anchorMin = Vector2.zero;
        plateRect.anchorMax = Vector2.one;
        plateRect.offsetMin = Vector2.zero;
        plateRect.offsetMax = Vector2.zero;
        Image plate = plateObject.GetComponent<Image>();
        plate.sprite = White();
        plate.color = new Color(0.93f, 0.9f, 0.82f, 0.92f);
        plate.raycastTarget = false;

        GameObject textObject = new GameObject("Line", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(plateObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 4f);
        textRect.offsetMax = new Vector2(-6f, -4f);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.12f, 0.1f, 0.08f);
        text.text = Line(line, onRight);
        text.raycastTarget = false;
        return text;
    }

    Transform FindHead()
    {
        Transform[] nodes = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].name.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return nodes[i];
        }

        GameObject head = new GameObject("HeadAnchor");
        head.transform.SetParent(transform, false);
        head.transform.localPosition = new Vector3(0f, 1.55f, 0.15f);
        return head.transform;
    }

    static Vector3 DoorPoint(string objectName)
    {
        GameObject door = GameObject.Find(objectName);
        if (door == null)
            return Vector3.zero;

        Renderer[] renderers = door.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return door.transform.position;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    static Doorway Outdoor()
    {
        GameObject door = GameObject.Find("StartDoor");
        return door != null ? door.GetComponent<Doorway>() : null;
    }

    static Sprite White()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }
}
