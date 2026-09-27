using System.Collections.Generic;
using UnityEngine;

public class LoopProgress : MonoBehaviour
{
    public static LoopProgress Instance { get; private set; }

    [Tooltip("0 表示还没闯过。每成功一次加 1，之后不会低于 1。")]
    public int stage;

    Transform[] m_Lamps;

    void Awake()
    {
        Instance = this;
        CollectLamps();
        if (stage < 0)
            stage = 0;
        Apply();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterCorrectEntry()
    {
        if (m_Lamps == null || m_Lamps.Length == 0 || stage >= m_Lamps.Length)
            return;

        stage += 1;
        if (stage < 1)
            stage = 1;

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
}
