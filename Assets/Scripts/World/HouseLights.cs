using System.Collections.Generic;
using UnityEngine;

public class HouseLights : MonoBehaviour
{
    public GameObject showerPrefab;
    public GameObject monsterLampPrefab;

    const float ApproachDistance = 3.3f;

    class Fixture
    {
        public Light light;
        public Transform root;
        public Color color;
        public float intensity;
        public bool wasEnabled;
        public LightFlicker flickerScript;
        public bool flickerWasEnabled;
        public Renderer[] renderers;
        public bool[] rendererWasEnabled;
        public float interval;
        public float timer;
        public bool blink;
        public float offTime;
        public float onTime;
        public float phase;
        public bool varyFlicker;
        public bool extinguish;
        public bool bursting;
        public bool dead;
        public float burstLeft;
        public bool tinted;
        public bool normalWhenNear;
        public Color anomalyColor;
        public GameObject replacement;
        public Light replacementLight;
        public bool monster;
    }

    readonly List<Fixture> m_Eligible = new List<Fixture>();
    bool m_Collected;

    public void Apply(AnomalyRoll roll)
    {
        Collect();
        Restore();
        if (!roll.hasAnomaly)
            return;

        var pool = new List<Fixture>();
        for (int i = 0; i < m_Eligible.Count; i++)
        {
            if (!m_Eligible[i].monster)
                pool.Add(m_Eligible[i]);
        }
        if (roll.Contains(GhostDirector.LampShowerId))
            ReplaceSome(pool, true);
        if (roll.Contains(GhostDirector.LampMonsterId))
            ReplaceSome(pool, false);

        if (roll.Contains(GhostDirector.FlickerDieId))
            ArmExtinguish(pool);
        else if (roll.Contains(GhostDirector.FlickerAllId))
            ArmBlink(pool, true);
        else if (roll.Contains(GhostDirector.FlickerSomeId))
            ArmBlink(pool, false);

        if (roll.Contains(GhostDirector.ColorNearId))
            ArmColor(pool, true);
        else if (roll.Contains(GhostDirector.ColorSomeId))
            ArmColor(pool, false);

        if (roll.Contains(GhostDirector.MonsterRedId))
            TintMonsters();
    }

    void Update()
    {
        Transform player = Player();
        for (int i = 0; i < m_Eligible.Count; i++)
            Tick(m_Eligible[i], player);
    }

    void Tick(Fixture fixture, Transform player)
    {
        if (fixture.light == null || fixture.dead)
            return;

        bool near = player != null && FlatDistance(player.position, fixture.root.position) <= ApproachDistance;
        if (fixture.blink)
            ApplyBlink(fixture);

        if (fixture.tinted)
        {
            Color color = fixture.anomalyColor;
            if (fixture.normalWhenNear && near)
                color = fixture.color;
            fixture.light.color = color;
            if (fixture.light.enabled)
                fixture.light.intensity = fixture.intensity;
            if (fixture.replacementLight != null)
                fixture.replacementLight.color = color;
        }

        if (fixture.extinguish && !fixture.bursting && near)
        {
            fixture.bursting = true;
            fixture.burstLeft = Random.Range(1f, 2f);
            fixture.interval = 0.045f;
            fixture.varyFlicker = true;
        }

        if (fixture.bursting)
        {
            fixture.burstLeft -= Time.deltaTime;
            if (fixture.burstLeft <= 0f)
            {
                fixture.dead = true;
                fixture.bursting = false;
                fixture.varyFlicker = false;
                fixture.light.enabled = false;
                if (fixture.replacementLight != null)
                    fixture.replacementLight.enabled = false;
                return;
            }
        }

        if (!fixture.varyFlicker || fixture.interval <= 0f)
            return;

        fixture.timer += Time.deltaTime;
        if (fixture.timer < fixture.interval)
            return;

        fixture.timer = 0f;
        float span = fixture.bursting ? 0.15f : 0.55f;
        float value = Random.Range(fixture.intensity * (1f - span), fixture.intensity * (1f + span * 0.35f));
        fixture.light.intensity = value;
        if (fixture.replacementLight != null)
            fixture.replacementLight.intensity = value;
    }

    void Collect()
    {
        if (m_Collected)
            return;

        m_Collected = true;
        Light[] lights = FindObjectsOfType<Light>();
        var seen = new HashSet<Transform>();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || light.type == LightType.Directional)
                continue;
            if (light.GetComponentInParent<LoopProgress>() != null)
                continue;
            if (light.GetComponentInParent<GargoyleGuide>() != null)
                continue;

            Transform root = FixtureRoot(light.transform);
            if (root == null || !seen.Add(root))
                continue;
            if (IsCandle(root) || OnJudgmentApproach(root.position))
                continue;

            Fixture fixture = new Fixture();
            fixture.monster = IsMonsterLamp(root);
            fixture.light = light;
            fixture.root = root;
            fixture.color = light.color;
            fixture.intensity = light.intensity;
            fixture.wasEnabled = light.enabled;
            fixture.flickerScript = root.GetComponentInChildren<LightFlicker>(true);
            fixture.flickerWasEnabled = fixture.flickerScript != null && fixture.flickerScript.enabled;
            fixture.renderers = root.GetComponentsInChildren<Renderer>(true);
            fixture.rendererWasEnabled = new bool[fixture.renderers.Length];
            for (int r = 0; r < fixture.renderers.Length; r++)
                fixture.rendererWasEnabled[r] = fixture.renderers[r] != null && fixture.renderers[r].enabled;
            m_Eligible.Add(fixture);
        }
    }

    void Restore()
    {
        for (int i = 0; i < m_Eligible.Count; i++)
        {
            Fixture fixture = m_Eligible[i];
            if (fixture.replacement != null)
            {
                Destroy(fixture.replacement);
                fixture.replacement = null;
                fixture.replacementLight = null;
            }

            if (fixture.light != null)
            {
                fixture.light.enabled = fixture.wasEnabled;
                fixture.light.color = fixture.color;
                fixture.light.intensity = fixture.intensity;
            }

            if (fixture.renderers != null)
            {
                for (int r = 0; r < fixture.renderers.Length; r++)
                {
                    if (fixture.renderers[r] != null)
                        fixture.renderers[r].enabled = fixture.rendererWasEnabled[r];
                }
            }

            if (fixture.flickerScript != null)
                fixture.flickerScript.enabled = fixture.flickerWasEnabled;

            fixture.interval = 0f;
            fixture.timer = 0f;
            fixture.blink = false;
            fixture.offTime = 0f;
            fixture.onTime = 0f;
            fixture.phase = 0f;
            fixture.varyFlicker = false;
            fixture.extinguish = false;
            fixture.bursting = false;
            fixture.dead = false;
            fixture.tinted = false;
            fixture.normalWhenNear = false;
        }
    }

    void TintMonsters()
    {
        for (int i = 0; i < m_Eligible.Count; i++)
        {
            Fixture fixture = m_Eligible[i];
            if (!fixture.monster || fixture.light == null)
                continue;

            fixture.tinted = true;
            fixture.normalWhenNear = false;
            fixture.anomalyColor = GreenLike(fixture.color);
            fixture.light.color = fixture.anomalyColor;
            fixture.light.intensity = fixture.intensity;
        }
    }

    static Color GreenLike(Color original)
    {
        float glow = Mathf.Max(original.r, Mathf.Max(original.g, original.b));
        if (glow < 0.05f)
            glow = 1f;
        return new Color(0.22f * glow, glow, 0.28f * glow, original.a);
    }

    static bool IsCandle(Transform root)
    {
        return NameHas(root, "Candle");
    }

    static bool IsMonsterLamp(Transform root)
    {
        return NameHas(root, "Monster");
    }

    static bool NameHas(Transform root, string token)
    {
        Transform node = root;
        while (node != null)
        {
            if (node.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            node = node.parent;
        }

        return false;
    }

    void ArmBlink(List<Fixture> pool, bool all)
    {
        List<Fixture> chosen = all ? pool : Some(pool);
        float span = all ? 2f : 1f;
        for (int i = 0; i < chosen.Count; i++)
        {
            Fixture fixture = chosen[i];
            fixture.blink = true;
            fixture.offTime = span;
            fixture.onTime = span;
            fixture.phase = all ? 0f : i * span;
            fixture.varyFlicker = false;
            if (fixture.light != null)
                fixture.light.intensity = fixture.intensity;
            HoldScript(fixture);
        }
    }

    static void ApplyBlink(Fixture fixture)
    {
        if (fixture.light == null)
            return;

        float period = Mathf.Max(0.05f, fixture.offTime + fixture.onTime);
        float phase = Mathf.Repeat(Time.time + fixture.phase, period);
        bool lit = phase >= fixture.offTime && fixture.wasEnabled;
        fixture.light.enabled = lit;
        if (lit)
            fixture.light.intensity = fixture.intensity;
        if (fixture.replacementLight != null)
            fixture.replacementLight.enabled = lit;
    }

    void ArmExtinguish(List<Fixture> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].extinguish = true;
            HoldScript(pool[i]);
        }
    }

    void ArmColor(List<Fixture> pool, bool allEerie)
    {
        List<Fixture> chosen = allEerie ? pool : Some(pool);
        for (int i = 0; i < chosen.Count; i++)
        {
            Fixture fixture = chosen[i];
            if (fixture.light == null)
                continue;

            fixture.tinted = true;
            fixture.normalWhenNear = allEerie;
            fixture.anomalyColor = GreenLike(fixture.color);
            fixture.light.color = fixture.anomalyColor;
            fixture.light.intensity = fixture.intensity;
            HoldScript(fixture);
        }
    }

    void ReplaceSome(List<Fixture> pool, bool shower)
    {
        GameObject prefab = shower ? showerPrefab : monsterLampPrefab;
        if (prefab == null)
            return;

        List<Fixture> chosen = Some(pool);
        for (int i = 0; i < chosen.Count; i++)
        {
            Fixture fixture = chosen[i];
            pool.Remove(fixture);
            HideFixture(fixture);
            GameObject copy = Instantiate(prefab, fixture.root);
            copy.transform.localPosition = shower ? new Vector3(0f, -0.15f, 0.05f) : Vector3.zero;
            copy.transform.localRotation = shower ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            copy.transform.localScale = shower ? Vector3.one * 0.45f : Vector3.one * 0.85f;
            Collider[] colliders = copy.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++)
                colliders[c].enabled = false;
            fixture.replacement = copy;

            if (!shower)
            {
                Light lamp = copy.GetComponentInChildren<Light>();
                if (lamp == null)
                    lamp = copy.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.range = 6.5f;
                lamp.intensity = 1.7f;
                lamp.color = new Color(0.55f, 0.75f, 0.45f);
                fixture.replacementLight = lamp;
                fixture.varyFlicker = true;
                fixture.interval = Random.Range(0.08f, 0.34f);
            }
        }
    }

    static void HideFixture(Fixture fixture)
    {
        if (fixture.light != null)
            fixture.light.enabled = false;
        if (fixture.flickerScript != null)
            fixture.flickerScript.enabled = false;
        if (fixture.renderers == null)
            return;

        for (int i = 0; i < fixture.renderers.Length; i++)
        {
            if (fixture.renderers[i] != null)
                fixture.renderers[i].enabled = false;
        }
    }

    static void HoldScript(Fixture fixture)
    {
        if (fixture.flickerScript != null)
            fixture.flickerScript.enabled = false;
    }

    static List<Fixture> Some(List<Fixture> pool)
    {
        var copy = new List<Fixture>(pool);
        for (int i = copy.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            Fixture item = copy[i];
            copy[i] = copy[swap];
            copy[swap] = item;
        }

        int count = Mathf.Max(1, Mathf.CeilToInt(copy.Count * 0.4f));
        if (count > copy.Count)
            count = copy.Count;
        if (count < copy.Count)
            copy.RemoveRange(count, copy.Count - count);
        return copy;
    }

    static Transform FixtureRoot(Transform light)
    {
        Transform root = light;
        if (root.parent != null && root.parent.name != "Lights")
            root = root.parent;
        return root;
    }

    static bool OnJudgmentApproach(Vector3 point)
    {
        Vector3 origin = new Vector3(-1.2f, 0f, 6f);
        return Along(point, origin, DoorPoint("Final_door_Yes"))
            || Along(point, origin, DoorPoint("Final_door_No"));
    }

    static bool Along(Vector3 point, Vector3 origin, Vector3 door)
    {
        if (door.sqrMagnitude < 0.01f && origin.sqrMagnitude < 0.01f)
            return false;

        Vector3 a = origin;
        Vector3 b = door;
        a.y = 0f;
        b.y = 0f;
        Vector3 flat = point;
        flat.y = 0f;
        Vector3 span = b - a;
        float length = span.sqrMagnitude;
        if (length < 1f)
            return false;

        float t = Vector3.Dot(flat - a, span) / length;
        if (t < 0.45f || t > 1.08f)
            return false;

        Vector3 closest = a + span * Mathf.Clamp01(t);
        return (flat - closest).sqrMagnitude < 3.8f * 3.8f;
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

    static Transform Player()
    {
        PlayerMovement movement = FindObjectOfType<PlayerMovement>();
        return movement != null ? movement.transform : null;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
