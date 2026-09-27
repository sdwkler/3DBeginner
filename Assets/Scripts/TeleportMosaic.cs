using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TeleportMosaic : MonoBehaviour
{
    public static TeleportMosaic Instance { get; private set; }
    public static bool IsBusy { get; private set; }

    public float blurInDuration = 0.45f;
    public float blurOutDuration = 0.55f;
    public float peakPixelSize = 56f;

    Material m_Material;
    GameObject m_Canvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        BuildOverlay();
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (m_Material != null)
            Destroy(m_Material);
    }

    public static TeleportMosaic Ensure()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("TeleportMosaic");
        return go.AddComponent<TeleportMosaic>();
    }

    public IEnumerator Play(System.Action atPeak)
    {
        IsBusy = true;
        SetVisible(true);
        yield return Fade(1f, peakPixelSize, blurInDuration);
        if (atPeak != null)
            atPeak();
        yield return Fade(peakPixelSize, 1f, blurOutDuration);
        SetVisible(false);
        IsBusy = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
            m_Material.SetFloat("_Pixel", Mathf.Lerp(from, to, t));
            yield return null;
        }

        m_Material.SetFloat("_Pixel", to);
    }

    void SetVisible(bool visible)
    {
        if (m_Canvas != null)
            m_Canvas.SetActive(visible);
        if (visible && m_Material != null)
            m_Material.SetFloat("_Pixel", 1f);
    }

    void BuildOverlay()
    {
        Shader shader = Shader.Find("Hidden/ScreenMosaic");
        if (shader == null)
        {
            Debug.LogError("找不到 Hidden/ScreenMosaic。");
            return;
        }

        m_Material = new Material(shader);
        m_Material.SetFloat("_Pixel", 1f);

        m_Canvas = new GameObject("MosaicCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        m_Canvas.transform.SetParent(transform, false);
        Canvas canvas = m_Canvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = m_Canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageObject = new GameObject("Mosaic", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(m_Canvas.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        image.sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        image.material = m_Material;
        image.raycastTarget = false;
        image.color = Color.white;
    }
}
