using System;
using UnityEngine;

[Serializable]
public class NormalClass
{
    public string id;
    public GameObject[] shownWhenActive;
}

[Serializable]
public class AnomalyClass
{
    [Tooltip("异常自己的名字。")]
    public string id;

    [Tooltip("这个异常替换哪一个正常类。一个正常类可以对应多个异常。")]
    public string normalId;

    [Tooltip("这些异常不能和当前异常同时出现。")]
    public string[] exclusiveAnomalyIds;

    [Tooltip("这个异常要换掉的正常类。没写就只改行为，不隐藏角色。")]
    public string[] replacesNormalIds;

    [Tooltip("1 初级，2 中级，3 高级。")]
    public int tier = 1;

    [Tooltip("占用本层预算。")]
    public int cost = 1;

    public GameObject[] shownWhenActive;
}

public struct AnomalyRoll
{
    public bool hasAnomaly;
    public string normalId;
    public string anomalyId;
    public string[] anomalyIds;

    public bool Contains(string id)
    {
        if (!hasAnomaly || string.IsNullOrEmpty(id))
            return false;

        if (anomalyIds != null)
        {
            for (int i = 0; i < anomalyIds.Length; i++)
            {
                if (anomalyIds[i] == id)
                    return true;
            }
        }

        return anomalyId == id;
    }
}

public class AnomalyCatalog : MonoBehaviour
{
    [Tooltip("关掉时每一轮都是正常，用来先走通流程。")]
    public bool enableAnomalies;

    public NormalClass[] normals = new NormalClass[0];
    public AnomalyClass[] anomalies = new AnomalyClass[0];

    readonly System.Collections.Generic.List<string> m_Seen = new System.Collections.Generic.List<string>();

    public AnomalyRoll Roll(int stage, bool finalFloor)
    {
        AnomalyRoll roll = EmptyRoll();
        if (stage <= 0 || !enableAnomalies || anomalies == null || anomalies.Length == 0)
            return roll;

        if (!finalFloor && UnityEngine.Random.value < NormalChance(stage))
            return roll;

        int budget = BudgetFor(stage, finalFloor);
        var picked = new System.Collections.Generic.List<AnomalyClass>();
        for (int guard = 0; guard < 6 && budget > 0; guard++)
        {
            var pool = Candidates(budget, picked, NeedFresh(picked, stage), stage);
            if (pool.Count == 0)
                pool = Candidates(budget, picked, false, stage);
            if (pool.Count == 0)
                break;

            AnomalyClass choice = Weighted(pool, stage, finalFloor);
            picked.Add(choice);
            budget -= Mathf.Max(1, choice.cost);
        }

        EnsureFresh(picked, stage);
        return Pack(picked);
    }

    public AnomalyRoll Force(string anomalyId)
    {
        return ForceMany(new[] { anomalyId });
    }

    public AnomalyRoll ForceMany(string[] anomalyIds)
    {
        var picked = new System.Collections.Generic.List<AnomalyClass>();
        if (anomalyIds != null)
        {
            for (int i = 0; i < anomalyIds.Length; i++)
            {
                AnomalyClass item = FindAnomaly(anomalyIds[i]);
                if (item != null)
                    picked.Add(item);
            }
        }

        return Pack(picked);
    }

    AnomalyRoll EmptyRoll()
    {
        AnomalyRoll roll = new AnomalyRoll();
        roll.anomalyIds = new string[0];
        if (normals != null && normals.Length > 0)
            roll.normalId = normals[0].id;
        return roll;
    }

    static int BudgetFor(int stage, bool finalFloor)
    {
        if (finalFloor)
            return 9;
        if (stage <= 1)
            return 2;
        if (stage == 2)
            return 3;
        if (stage == 3)
            return 4;
        if (stage == 4)
            return 5;
        if (stage == 5)
            return 6;
        return 7;
    }

    static float NormalChance(int stage)
    {
        if (stage <= 1)
            return 0.40f;
        if (stage == 2)
            return 0.30f;
        if (stage == 3)
            return 0.20f;
        return 0.10f;
    }

    static float TierWeight(int tier, int stage, bool finalFloor)
    {
        float along = Mathf.Clamp01((stage - 1) / 6f);
        if (tier <= 1)
            return Mathf.Lerp(5f, 1.6f, along);
        if (tier == 2)
            return stage < 2 ? 0.7f : Mathf.Lerp(1.3f, 3.2f, along);
        if (stage < 4)
            return 0f;

        float high = Mathf.Clamp01((stage - 4) / 3f);
        if (finalFloor)
            high = 1f;
        return Mathf.Lerp(0.45f, 3.6f, high);
    }

    bool NeedFresh(System.Collections.Generic.List<AnomalyClass> picked, int stage)
    {
        if (HasFresh(picked) || !AnyUnseen(stage))
            return false;
        return true;
    }

    bool AnyUnseen(int stage)
    {
        for (int i = 0; i < anomalies.Length; i++)
        {
            AnomalyClass item = anomalies[i];
            if (item != null && Allowed(item, stage) && !m_Seen.Contains(item.id))
                return true;
        }

        return false;
    }

    bool HasFresh(System.Collections.Generic.List<AnomalyClass> picked)
    {
        for (int i = 0; i < picked.Count; i++)
        {
            if (picked[i] != null && !m_Seen.Contains(picked[i].id))
                return true;
        }

        return false;
    }

    System.Collections.Generic.List<AnomalyClass> Candidates(int budget, System.Collections.Generic.List<AnomalyClass> picked, bool unseenOnly, int stage)
    {
        var pool = new System.Collections.Generic.List<AnomalyClass>();
        for (int i = 0; i < anomalies.Length; i++)
        {
            AnomalyClass item = anomalies[i];
            if (!Allowed(item, stage))
                continue;
            if (Mathf.Max(1, item.cost) > budget)
                continue;
            if (unseenOnly && m_Seen.Contains(item.id))
                continue;
            if (Conflicts(item, picked))
                continue;
            pool.Add(item);
        }

        return pool;
    }

    static bool Allowed(AnomalyClass item, int stage)
    {
        if (item == null || string.IsNullOrEmpty(item.id))
            return false;
        if (item.tier >= 3 && stage < 4)
            return false;
        return true;
    }

    bool Conflicts(AnomalyClass item, System.Collections.Generic.List<AnomalyClass> picked)
    {
        for (int i = 0; i < picked.Count; i++)
        {
            if (picked[i] == null)
                continue;
            if (picked[i].id == item.id)
                return true;
            if (Contains(item.exclusiveAnomalyIds, picked[i].id) || Contains(picked[i].exclusiveAnomalyIds, item.id))
                return true;
        }

        return false;
    }

    static AnomalyClass Weighted(System.Collections.Generic.List<AnomalyClass> pool, int stage, bool finalFloor)
    {
        float total = 0f;
        for (int i = 0; i < pool.Count; i++)
            total += TierWeight(pool[i].tier, stage, finalFloor);

        float mark = UnityEngine.Random.value * total;
        for (int i = 0; i < pool.Count; i++)
        {
            mark -= TierWeight(pool[i].tier, stage, finalFloor);
            if (mark <= 0f)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }

    void EnsureFresh(System.Collections.Generic.List<AnomalyClass> picked, int stage)
    {
        if (HasFresh(picked) || !AnyUnseen(stage))
            return;

        AnomalyClass fresh = null;
        for (int i = 0; i < anomalies.Length; i++)
        {
            AnomalyClass item = anomalies[i];
            if (!Allowed(item, stage) || m_Seen.Contains(item.id))
                continue;
            if (fresh == null || item.tier < fresh.tier)
                fresh = item;
        }

        if (fresh == null)
            return;

        for (int i = picked.Count - 1; i >= 0; i--)
        {
            if (Conflicts(fresh, Single(picked[i])))
                picked.RemoveAt(i);
        }

        picked.Add(fresh);
    }

    static System.Collections.Generic.List<AnomalyClass> Single(AnomalyClass item)
    {
        var list = new System.Collections.Generic.List<AnomalyClass>();
        list.Add(item);
        return list;
    }

    AnomalyRoll Pack(System.Collections.Generic.List<AnomalyClass> picked)
    {
        AnomalyRoll roll = EmptyRoll();
        if (picked.Count == 0)
            return roll;

        roll.hasAnomaly = true;
        roll.anomalyIds = new string[picked.Count];
        AnomalyClass lead = picked[0];
        for (int i = 0; i < picked.Count; i++)
        {
            roll.anomalyIds[i] = picked[i].id;
            if (!m_Seen.Contains(picked[i].id))
                m_Seen.Add(picked[i].id);
            if (picked[i].tier > lead.tier)
                lead = picked[i];
        }

        roll.anomalyId = lead.id;
        if (!string.IsNullOrEmpty(lead.normalId))
            roll.normalId = lead.normalId;
        return roll;
    }

    public void ClearSeen()
    {
        m_Seen.Clear();
    }

    public bool Replaces(string anomalyId, string normalId)
    {
        AnomalyClass anomaly = FindAnomaly(anomalyId);
        if (anomaly == null || anomaly.replacesNormalIds == null)
            return false;

        for (int i = 0; i < anomaly.replacesNormalIds.Length; i++)
        {
            if (anomaly.replacesNormalIds[i] == normalId)
                return true;
        }

        return false;
    }

    public AnomalyClass FindAnomaly(string anomalyId)
    {
        if (anomalies == null || string.IsNullOrEmpty(anomalyId))
            return null;

        for (int i = 0; i < anomalies.Length; i++)
        {
            if (anomalies[i] != null && anomalies[i].id == anomalyId)
                return anomalies[i];
        }

        return null;
    }

    public void Apply(AnomalyRoll roll)
    {
        AnomalyClass active = null;
        if (normals != null && roll.hasAnomaly && roll.anomalyIds != null)
        {
            for (int n = 0; n < normals.Length; n++)
            {
                bool replaced = false;
                for (int a = 0; a < roll.anomalyIds.Length; a++)
                {
                    active = FindAnomaly(roll.anomalyIds[a]);
                    if (active != null && Contains(active.replacesNormalIds, normals[n].id))
                        replaced = true;
                }

                SetActive(normals[n].shownWhenActive, !replaced);
            }
        }
        else if (normals != null)
        {
            for (int i = 0; i < normals.Length; i++)
                SetActive(normals[i].shownWhenActive, true);
        }

        if (anomalies == null)
            return;

        for (int i = 0; i < anomalies.Length; i++)
            SetActive(anomalies[i].shownWhenActive, roll.Contains(anomalies[i].id));
    }

    static bool Contains(string[] values, string id)
    {
        if (values == null || string.IsNullOrEmpty(id))
            return false;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == id)
                return true;
        }

        return false;
    }

    static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
