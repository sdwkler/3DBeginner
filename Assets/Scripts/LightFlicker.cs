using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LightFlicker : MonoBehaviour
{
    public enum FlickerMode
    {
        Random,
        AnimationCurve
    }

    public Light flickeringLight;
    public Renderer flickeringRenderer;
    public FlickerMode flickerMode;
    public float lightIntensityMin = 1.25f;
    public float lightIntensityMax = 2.25f;
    public float flickerDuration = 0.075f;
    public AnimationCurve intensityCurve;

    Material m_FlickeringMaterial;
    Color m_EmissionColor;
    float m_Timer;
    float m_FlickerLightIntensity;

    static readonly int k_EmissionColorID = Shader.PropertyToID(k_EmissiveColorName);

    const string k_EmissiveColorName = "_EmissionColor";
    const string k_EmissionName = "_Emission";
    const float k_LightIntensityToEmission = 2f / 3f;

    void Start()
    {
        // 如果面板未手动拖入 Light，尝试自动获取当前物体上的 Light 组件
        if (flickeringLight == null)
        {
            flickeringLight = GetComponent<Light>();
        }

        // 仅在绑定了 Renderer 时才初始化发光材质，防止纯氛围灯报错
        if (flickeringRenderer != null)
        {
            m_FlickeringMaterial = flickeringRenderer.material;
            if (m_FlickeringMaterial != null)
            {
                m_FlickeringMaterial.EnableKeyword(k_EmissionName);
                m_EmissionColor = m_FlickeringMaterial.GetColor(k_EmissionColorID);
            }
        }

        // 初始化默认亮度，避免运行第一帧在计时器触发前灯光突然变暗为 0
        if (flickerMode == FlickerMode.Random)
        {
            ChangeRandomFlickerLightIntensity();
        }
        else if (flickerMode == FlickerMode.AnimationCurve)
        {
            ChangeAnimatedFlickerLightIntensity();
        }
    }

    void Update()
    {
        m_Timer += Time.deltaTime;

        if (flickerMode == FlickerMode.Random)
        {
            if (m_Timer >= flickerDuration)
            {
                ChangeRandomFlickerLightIntensity();
            }
        }
        else if (flickerMode == FlickerMode.AnimationCurve)
        {
            ChangeAnimatedFlickerLightIntensity();
        }

        // 判空保护：确保灯光组件存在时才修改亮度
        if (flickeringLight != null)
        {
            flickeringLight.intensity = m_FlickerLightIntensity;
        }

        // 判空保护：确保发光材质存在时才修改自发光颜色（修复第 59 行报错）
        if (m_FlickeringMaterial != null)
        {
            m_FlickeringMaterial.SetColor(k_EmissionColorID, m_EmissionColor * m_FlickerLightIntensity * k_LightIntensityToEmission);
        }
    }

    void ChangeRandomFlickerLightIntensity()
    {
        m_FlickerLightIntensity = Random.Range(lightIntensityMin, lightIntensityMax);
        m_Timer = 0f;
    }

    void ChangeAnimatedFlickerLightIntensity()
    {
        // 判空保护：防止切换到 AnimationCurve 模式但未配置曲线关键帧时数组越界报错
        if (intensityCurve == null || intensityCurve.length == 0)
            return;

        m_FlickerLightIntensity = intensityCurve.Evaluate(m_Timer);

        if (m_Timer >= intensityCurve[intensityCurve.length - 1].time)
            m_Timer = intensityCurve[0].time;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LightFlicker))]
public class LightFlickerEditor : Editor
{
    SerializedProperty m_ScriptProp;
    SerializedProperty m_FlickeringLightProp;
    SerializedProperty m_FlickeringRendererProp;
    SerializedProperty m_FlickerModeProp;
    SerializedProperty m_LightIntensityMinProp;
    SerializedProperty m_LightIntensityMaxProp;
    SerializedProperty m_FlickerDurationProp;
    SerializedProperty m_IntensityCurveProp;

    void OnEnable()
    {
        m_ScriptProp = serializedObject.FindProperty("m_Script");
        m_FlickeringLightProp = serializedObject.FindProperty("flickeringLight");
        m_FlickeringRendererProp = serializedObject.FindProperty("flickeringRenderer");
        m_FlickerModeProp = serializedObject.FindProperty("flickerMode");
        m_LightIntensityMinProp = serializedObject.FindProperty("lightIntensityMin");
        m_LightIntensityMaxProp = serializedObject.FindProperty("lightIntensityMax");
        m_FlickerDurationProp = serializedObject.FindProperty("flickerDuration");
        m_IntensityCurveProp = serializedObject.FindProperty("intensityCurve");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(m_ScriptProp);
        GUI.enabled = true;

        EditorGUILayout.PropertyField(m_FlickeringLightProp);
        EditorGUILayout.PropertyField(m_FlickeringRendererProp);
        EditorGUILayout.PropertyField(m_FlickerModeProp);

        if (m_FlickerModeProp.enumValueIndex == 0)
        {
            EditorGUILayout.PropertyField(m_LightIntensityMinProp);
            EditorGUILayout.PropertyField(m_LightIntensityMaxProp);
            EditorGUILayout.PropertyField(m_FlickerDurationProp);
        }
        else if (m_FlickerModeProp.enumValueIndex == 1)
        {
            EditorGUILayout.PropertyField(m_IntensityCurveProp);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif