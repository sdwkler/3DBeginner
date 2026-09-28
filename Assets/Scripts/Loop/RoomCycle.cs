using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-50)]
public class RoomCycle : MonoBehaviour
{
    public static RoomCycle Instance { get; private set; }

    public JudgmentDoor yesDoor;
    public JudgmentDoor noDoor;
    public Doorway outdoorDoor;
    public AnomalyCatalog catalog;
    public Vector3 indoorReturnCenter = new Vector3(-1.2f, 1.1f, 8.2f);
    public Vector3 indoorReturnSize = new Vector3(4f, 2.6f, 2f);

    enum Pending
    {
        None,
        Correct,
        Wrong
    }

    Pending m_Pending = Pending.None;
    bool m_FinalRound;
    bool m_Won;
    AnomalyRoll m_Roll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
            return;
        if (FindObjectOfType<RoomCycle>() != null)
            return;

        GameObject go = new GameObject("RoomCycle");
        go.AddComponent<RoomCycle>();
    }

    void Awake()
    {
        Instance = this;
        if (catalog == null)
            catalog = GetComponent<AnomalyCatalog>();
        if (catalog == null)
            catalog = gameObject.AddComponent<AnomalyCatalog>();

        BindDoors();
        EnsureIndoorReturn();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        int stage = 0;
        if (LoopProgress.Instance != null)
            LoopProgress.Instance.SetStage(stage);
        BeginRound(stage, false);
    }

    public static event Action<AnomalyRoll> RoundBegan;

    public AnomalyRoll CurrentRoll
    {
        get { return m_Roll; }
    }

    public bool HasAnomaly
    {
        get { return m_Roll.hasAnomaly; }
    }

    public void RecordChoice(JudgmentChoice choice)
    {
        if (m_Won || m_Pending != Pending.None)
            return;

        bool choseYes = choice == JudgmentChoice.Yes;
        m_Pending = choseYes == m_Roll.hasAnomaly ? Pending.Correct : Pending.Wrong;
    }

    public void ArriveIndoor()
    {
        if (m_Won || m_Pending == Pending.None || TeleportMosaic.IsBusy)
            return;

        ApplyPending(false);
    }

    public void CommitOutdoorArrival()
    {
        if (m_Won || m_Pending == Pending.None)
            return;

        ApplyPending(true);
    }

    void ApplyPending(bool startWalker)
    {
        Pending pending = m_Pending;
        m_Pending = Pending.None;
        LoopProgress progress = LoopProgress.Instance;

        int stage;
        if (pending == Pending.Wrong)
        {
            m_FinalRound = false;
            stage = 0;
            if (progress != null)
                progress.SetStage(stage);
            BeginRound(stage, false);
        }
        else if (m_FinalRound)
        {
            Win();
            return;
        }
        else
        {
            stage = progress != null ? progress.stage + 1 : 1;
            if (progress != null)
                progress.SetStage(stage);

            int lamps = progress != null ? progress.LampCount : 7;
            bool finalRound = lamps > 0 && stage >= lamps;
            BeginRound(stage, finalRound);
        }

        if (!startWalker)
            return;

        GhostDirector director = FindObjectOfType<GhostDirector>();
        if (director != null)
            director.NotifyEnteredStartDoor();
    }

    void BeginRound(int stage, bool finalRound)
    {
        if (stage <= 0 && catalog != null)
            catalog.ClearSeen();

        m_FinalRound = finalRound;
        m_Roll = catalog != null ? catalog.Roll(stage, finalRound) : new AnomalyRoll();
        if (stage <= 0)
            m_Roll.hasAnomaly = false;

        if (catalog != null)
            catalog.Apply(m_Roll);

        if (yesDoor != null)
            yesDoor.choice = JudgmentChoice.Yes;
        if (noDoor != null)
            noDoor.choice = JudgmentChoice.No;

        if (RoundBegan != null)
            RoundBegan(m_Roll);
    }

    public void DebugRefresh(string[] ids)
    {
        if (catalog == null)
            return;

        m_Roll = catalog.ForceMany(ids);
        catalog.Apply(m_Roll);
        if (RoundBegan != null)
            RoundBegan(m_Roll);

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player == null || !player.enabled)
            return;

        GhostDirector director = FindObjectOfType<GhostDirector>();
        if (director != null)
            director.NotifyEnteredStartDoor();
    }

    void Win()
    {
        m_Won = true;
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            player.controlsLocked = true;
            player.Halt();
        }

        GameMenu menu = GameMenu.Instance;
        if (menu == null)
            menu = gameObject.AddComponent<GameMenu>();
        menu.ShowVictory();
    }

    void BindDoors()
    {
        if (outdoorDoor == null)
        {
            GameObject outdoorObject = GameObject.Find("StartDoor");
            if (outdoorObject != null)
                outdoorDoor = outdoorObject.GetComponent<Doorway>();
        }

        yesDoor = BindJudgment("Final_door_Yes", JudgmentChoice.Yes, yesDoor);
        noDoor = BindJudgment("Final_door_No", JudgmentChoice.No, noDoor);
        SilenceLeaves(yesDoor);
        SilenceLeaves(noDoor);
        SilenceNamed("StartDoor_out");
    }

    JudgmentDoor BindJudgment(string objectName, JudgmentChoice choice, JudgmentDoor existing)
    {
        if (existing != null)
            return existing;

        GameObject host = GameObject.Find(objectName);
        if (host == null)
            return null;

        FitTrigger(host);
        Doorway doorway = host.GetComponent<Doorway>();
        if (doorway == null)
            doorway = host.AddComponent<Doorway>();
        doorway.place = DoorPlace.Indoor;
        doorway.exitDoor = outdoorDoor;
        doorway.correctPassage = false;

        JudgmentDoor judgment = host.GetComponent<JudgmentDoor>();
        if (judgment == null)
            judgment = host.AddComponent<JudgmentDoor>();
        judgment.choice = choice;
        return judgment;
    }

    void SilenceLeaves(JudgmentDoor judgment)
    {
        if (judgment == null)
            return;

        Doorway[] doorways = judgment.GetComponentsInChildren<Doorway>(true);
        for (int i = 0; i < doorways.Length; i++)
        {
            if (doorways[i].gameObject != judgment.gameObject)
                doorways[i].enabled = false;
        }
    }

    static void SilenceNamed(string objectName)
    {
        GameObject host = GameObject.Find(objectName);
        if (host == null)
            return;

        Doorway doorway = host.GetComponent<Doorway>();
        if (doorway != null)
            doorway.enabled = false;
    }

    static void FitTrigger(GameObject host)
    {
        Renderer[] renderers = host.GetComponentsInChildren<Renderer>(true);
        BoxCollider box = host.GetComponent<BoxCollider>();
        if (box == null)
            box = host.AddComponent<BoxCollider>();
        box.isTrigger = true;

        if (renderers == null || renderers.Length == 0)
        {
            box.center = new Vector3(0f, 0.4f, 0f);
            box.size = new Vector3(2.2f, 2.4f, 1.6f);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        bounds.Expand(new Vector3(1.2f, 0.8f, 1.2f));

        box.center = host.transform.InverseTransformPoint(bounds.center);
        Vector3 scale = host.transform.lossyScale;
        box.size = new Vector3(
            SafeDivide(bounds.size.x, scale.x),
            SafeDivide(bounds.size.y, scale.y),
            SafeDivide(bounds.size.z, scale.z));
    }

    void EnsureIndoorReturn()
    {
        if (FindObjectOfType<IndoorReturn>() != null)
            return;

        GameObject volume = new GameObject("IndoorReturn");
        volume.transform.position = indoorReturnCenter;
        BoxCollider box = volume.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = indoorReturnSize;
        volume.AddComponent<IndoorReturn>();
    }

    static float SafeDivide(float value, float scale)
    {
        if (Mathf.Abs(scale) < 0.0001f)
            return value;
        return value / scale;
    }
}
