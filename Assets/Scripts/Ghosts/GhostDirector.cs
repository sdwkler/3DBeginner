using UnityEngine;

[DefaultExecutionOrder(-80)]
public class GhostDirector : MonoBehaviour
{
    public const string WalkingId = "walking";
    public const string SleepingId = "sleeping";
    public const string WashingId = "washing";
    public const string StareId = "stare";
    public const string WalkerRepeatId = "walker-repeat";
    public const string WalkerPlayerId = "walker-player";
    public const string BatherPlayerId = "bather-player";
    public const string FlickerSomeId = "flicker-some";
    public const string FlickerAllId = "flicker-all";
    public const string FlickerDieId = "flicker-die";
    public const string ColorSomeId = "color-some";
    public const string ColorNearId = "color-near";
    public const string LampShowerId = "lamp-shower";
    public const string LampMonsterId = "lamp-monster";
    public const string MonsterRedId = "monster-red";
    public const string WalkerBackId = "walker-back";
    public const string WalkerTailId = "walker-tail";
    public const string GargoyleGarbledId = "gargoyle-garbled";
    public const string GargoyleCatchId = "gargoyle-catch";
    public const string ChairPileId = "chair-pile";
    public const string ChairHuntId = "chair-hunt";
    public const string ChairFloatId = "chair-float";

    public bool allowAnomalies;
    public RuntimeAnimatorController walkController;
    public RuntimeAnimatorController showerController;
    public GameObject playerPrefab;
    public GameObject playerModel;
    public GameObject showerPrefab;
    public GameObject monsterLampPrefab;
    public GameObject chairPrefab;
    public Transform startDoor;

    GhostWalker m_Walker;
    GhostSleeper m_Sleeper;
    GhostBather m_Bather;
    GhostBather m_PlayerBather;
    GameObject m_PlayerWalker;
    bool m_GateOpen;

    HouseLights m_Lights;
    GargoyleGuide m_Gargoyle;
    HouseChairs m_Chairs;

    void Awake()
    {
        m_Lights = GetComponent<HouseLights>();
        if (m_Lights == null)
            m_Lights = gameObject.AddComponent<HouseLights>();
        m_Lights.showerPrefab = showerPrefab;
        m_Lights.monsterLampPrefab = monsterLampPrefab;
        BindGargoyle();
        m_Chairs = GetComponent<HouseChairs>();
        if (m_Chairs == null)
            m_Chairs = gameObject.AddComponent<HouseChairs>();
        m_Chairs.chairPrefab = chairPrefab;
        PrepareCatalog();
        BindGhosts();
        RoomCycle.RoundBegan += OnRound;
    }

    void OnDestroy()
    {
        RoomCycle.RoundBegan -= OnRound;
    }

    void Start()
    {
        PrepareCatalog();
        if (RoomCycle.Instance != null)
            OnRound(RoomCycle.Instance.CurrentRoll);
    }

    public void NotifyEnteredStartDoor()
    {
        m_GateOpen = true;
        if (m_Walker != null && m_Walker.Visible)
            m_Walker.BeginMarch();
        if (m_PlayerWalker != null && m_PlayerWalker.activeSelf)
        {
            GhostWalker walker = m_PlayerWalker.GetComponent<GhostWalker>();
            if (walker != null)
                walker.BeginMarch();
        }
    }

    void PrepareCatalog()
    {
        RoomCycle cycle = FindObjectOfType<RoomCycle>();
        if (cycle == null)
            return;

        AnomalyCatalog catalog = cycle.catalog != null
            ? cycle.catalog
            : cycle.GetComponent<AnomalyCatalog>();
        if (catalog == null)
            catalog = cycle.gameObject.AddComponent<AnomalyCatalog>();

        cycle.catalog = catalog;
        catalog.enableAnomalies = allowAnomalies;
        catalog.normals = new NormalClass[]
        {
            new NormalClass { id = WalkingId },
            new NormalClass { id = SleepingId },
            new NormalClass { id = WashingId }
        };
        catalog.anomalies = BuildAnomalies();
    }

    static AnomalyClass Entry(string id, int tier, string normalId, string[] exclusive, string[] replaces)
    {
        return new AnomalyClass
        {
            id = id,
            tier = tier,
            cost = tier,
            normalId = normalId,
            exclusiveAnomalyIds = exclusive,
            replacesNormalIds = replaces
        };
    }

    static string[] Except(string self, params string[] group)
    {
        var list = new System.Collections.Generic.List<string>();
        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] != self)
                list.Add(group[i]);
        }

        return list.ToArray();
    }

    AnomalyClass[] BuildAnomalies()
    {
        string[] flicker = { FlickerSomeId, FlickerAllId, FlickerDieId };
        string[] color = { ColorSomeId, ColorNearId };
        string[] lamps = { LampShowerId, LampMonsterId };
        string[] gargoyles = { GargoyleGarbledId, GargoyleCatchId };
        string[] chairs = { ChairPileId, ChairHuntId, ChairFloatId };
        string[] walkers = { WalkerRepeatId, WalkerPlayerId, WalkerBackId, WalkerTailId };
        string[] tail = Except(WalkerTailId, WalkerRepeatId, WalkerPlayerId, WalkerBackId, WalkerTailId, StareId);
        string[] stare = { WalkerTailId };

        return new AnomalyClass[]
        {
            Entry(StareId, 1, WalkingId, stare, new string[0]),
            Entry(WalkerRepeatId, 1, WalkingId, Except(WalkerRepeatId, walkers), new string[0]),
            Entry(WalkerPlayerId, 1, WalkingId, Except(WalkerPlayerId, walkers), new[] { WalkingId }),
            Entry(WalkerBackId, 2, WalkingId, Except(WalkerBackId, walkers), new string[0]),
            Entry(WalkerTailId, 3, WalkingId, tail, new string[0]),
            Entry(BatherPlayerId, 1, WashingId, new string[0], new[] { WashingId }),
            Entry(FlickerSomeId, 1, string.Empty, Except(FlickerSomeId, flicker), new string[0]),
            Entry(FlickerAllId, 2, string.Empty, Except(FlickerAllId, flicker), new string[0]),
            Entry(FlickerDieId, 3, string.Empty, Except(FlickerDieId, flicker), new string[0]),
            Entry(ColorSomeId, 1, string.Empty, Except(ColorSomeId, color), new string[0]),
            Entry(ColorNearId, 2, string.Empty, Except(ColorNearId, color), new string[0]),
            Entry(LampShowerId, 1, string.Empty, Except(LampShowerId, lamps), new string[0]),
            Entry(LampMonsterId, 2, string.Empty, Except(LampMonsterId, lamps), new string[0]),
            Entry(MonsterRedId, 1, string.Empty, new string[0], new string[0]),
            Entry(GargoyleGarbledId, 1, string.Empty, Except(GargoyleGarbledId, gargoyles), new string[0]),
            Entry(GargoyleCatchId, 2, string.Empty, Except(GargoyleCatchId, gargoyles), new string[0]),
            Entry(ChairPileId, 1, string.Empty, Except(ChairPileId, chairs), new string[0]),
            Entry(ChairHuntId, 2, string.Empty, Except(ChairHuntId, chairs), new string[0]),
            Entry(ChairFloatId, 2, string.Empty, Except(ChairFloatId, chairs), new string[0])
        };
    }

    void BindGhosts()
    {
        m_Walker = PrepareWalker(GameObject.Find("Ghost_walking"));
        m_Sleeper = PrepareSleeper(GameObject.Find("Ghost_sleeping"));
        m_Bather = PrepareBather(GameObject.Find("Ghost_washing"), "这回忆的漩涡", false);
        EnsureGate();
    }

    GhostWalker PrepareWalker(GameObject ghost)
    {
        if (ghost == null)
            return null;

        Animator animator = ghost.GetComponent<Animator>();
        if (animator != null && walkController != null)
        {
            animator.runtimeAnimatorController = walkController;
            animator.applyRootMotion = false;
            animator.speed = 0f;
        }

        GhostWalker walker = ghost.GetComponent<GhostWalker>();
        if (walker == null)
            walker = ghost.AddComponent<GhostWalker>();
        walker.Bind(animator, startDoor);
        return walker;
    }

    GhostSleeper PrepareSleeper(GameObject ghost)
    {
        if (ghost == null)
            return null;

        Animator animator = ghost.GetComponent<Animator>();
        if (animator != null)
            animator.speed = 0f;

        GhostSleeper sleeper = ghost.GetComponent<GhostSleeper>();
        if (sleeper == null)
            sleeper = ghost.AddComponent<GhostSleeper>();
        return sleeper;
    }

    GhostBather PrepareBather(GameObject ghost, string line, bool canFly)
    {
        if (ghost == null)
            return null;

        Animator animator = ghost.GetComponent<Animator>();
        if (animator != null && showerController != null)
        {
            animator.runtimeAnimatorController = showerController;
            animator.applyRootMotion = false;
        }

        GhostBather bather = ghost.GetComponent<GhostBather>();
        if (bather == null)
            bather = ghost.AddComponent<GhostBather>();
        bather.canFly = canFly;
        bather.Bind(GhostSpeechBubble.Create(line), line);
        return bather;
    }

    void OnRound(AnomalyRoll roll)
    {
        m_GateOpen = false;
        bool stare = roll.Contains(StareId);
        bool walkerPlayer = roll.Contains(WalkerPlayerId);
        bool batherPlayer = roll.Contains(BatherPlayerId);
        GhostWalker.DriftKind drift = GhostWalker.DriftKind.None;
        if (roll.Contains(WalkerTailId))
            drift = GhostWalker.DriftKind.Tail;
        else if (roll.Contains(WalkerBackId))
            drift = GhostWalker.DriftKind.Back;

        if (m_Walker != null)
        {
            m_Walker.StareActive = stare;
            m_Walker.RepeatAfterDoor = roll.Contains(WalkerRepeatId);
            m_Walker.drift = drift;
            m_Walker.ResetHome();
            m_Walker.SetGhostVisible(!walkerPlayer);
            if (!walkerPlayer && drift != GhostWalker.DriftKind.None)
                m_Walker.BeginMarch();
        }

        if (m_Sleeper != null)
        {
            m_Sleeper.StareActive = stare;
            m_Sleeper.ResetHome();
        }

        if (m_Bather != null)
        {
            m_Bather.StareActive = stare && !batherPlayer;
            m_Bather.canFly = false;
            m_Bather.SetLine("这回忆的漩涡");
            m_Bather.ResetHome();
            m_Bather.SetGhostVisible(!batherPlayer);
        }

        ShowPlayerWalker(walkerPlayer, stare);
        ShowPlayerBather(batherPlayer, stare);
        if (m_Lights != null)
            m_Lights.Apply(roll);
        if (m_Gargoyle != null)
            m_Gargoyle.Apply(roll);
        if (m_Chairs != null)
            m_Chairs.Apply(roll);
    }

    void BindGargoyle()
    {
        GameObject gargoyle = GameObject.Find("Gargoyle");
        if (gargoyle == null)
            return;

        m_Gargoyle = gargoyle.GetComponent<GargoyleGuide>();
        if (m_Gargoyle == null)
            m_Gargoyle = gargoyle.AddComponent<GargoyleGuide>();
    }

    void ShowPlayerWalker(bool show, bool stare)
    {
        if (m_PlayerWalker == null && show)
            m_PlayerWalker = CreatePlayerStand("PlayerWalker", m_Walker != null ? m_Walker.transform : null, true);

        if (m_PlayerWalker == null)
            return;

        m_PlayerWalker.SetActive(show);
        if (!show)
            return;

        GhostWalker walker = m_PlayerWalker.GetComponent<GhostWalker>();
        walker.StareActive = stare;
        walker.RepeatAfterDoor = false;
        walker.ResetHome();
        if (m_GateOpen)
            walker.BeginMarch();
    }

    void ShowPlayerBather(bool show, bool stare)
    {
        if (m_PlayerBather == null && show && m_Bather != null)
        {
            GameObject stand = CreateBareModel("PlayerBather", m_Bather.transform);
            if (stand == null)
                return;

            m_PlayerBather = stand.AddComponent<GhostBather>();
            m_PlayerBather.canFly = true;
            m_PlayerBather.Bind(GhostSpeechBubble.Create("中国猫会飞"), "中国猫会飞");
        }

        if (m_PlayerBather == null)
            return;

        m_PlayerBather.gameObject.SetActive(show);
        if (!show)
            return;

        m_PlayerBather.StareActive = stare;
        m_PlayerBather.canFly = true;
        m_PlayerBather.SetLine("中国猫会飞");
        m_PlayerBather.ResetHome();
    }

    GameObject CreatePlayerStand(string name, Transform pose, bool walking)
    {
        if (playerPrefab == null || pose == null)
            return null;

        GameObject stand = Spawn(playerPrefab, pose);
        if (stand == null)
            return null;

        stand.name = name;
        stand.tag = "Untagged";
        StripGameplay(stand);

        Animator animator = stand.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetBool("IsWalking", walking);
            animator.speed = walking ? 1f : 0f;
        }

        if (walking)
        {
            GhostWalker walker = stand.AddComponent<GhostWalker>();
            walker.Bind(animator, startDoor);
        }

        return stand;
    }

    GameObject CreateBareModel(string name, Transform pose)
    {
        GameObject source = playerModel != null ? playerModel : playerPrefab;
        if (source == null || pose == null)
            return null;

        GameObject stand = Spawn(source, pose);
        if (stand == null)
            return null;

        stand.name = name;
        stand.tag = "Untagged";
        StripGameplay(stand);
        Animator[] animators = stand.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].runtimeAnimatorController = null;
            animators[i].enabled = false;
            Destroy(animators[i]);
        }

        return stand;
    }

    GameObject Spawn(GameObject source, Transform pose)
    {
        Object clone = Instantiate((Object)source, pose.position, pose.rotation, pose.parent);
        GameObject stand = clone as GameObject;
        if (stand == null)
        {
            Component piece = clone as Component;
            stand = piece != null ? piece.gameObject : null;
        }

        if (stand == null && clone != null)
            Destroy(clone);
        return stand;
    }

    static void StripGameplay(GameObject stand)
    {
        MonoBehaviour[] behaviours = stand.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is PlayerMovement || behaviours[i] is GhostActor)
                Destroy(behaviours[i]);
        }

        Rigidbody body = stand.GetComponent<Rigidbody>();
        if (body != null)
            Destroy(body);

        Collider[] colliders = stand.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    void EnsureGate()
    {
        Transform door = startDoor;
        if (door == null)
        {
            GameObject found = GameObject.Find("StartDoor");
            if (found != null)
                door = found.transform;
            startDoor = door;
        }

        if (door == null || door.GetComponent<GhostStartGate>() != null)
            return;

        GhostStartGate gate = door.gameObject.AddComponent<GhostStartGate>();
        gate.director = this;
        BoxCollider box = door.GetComponent<BoxCollider>();
        if (box == null)
            box = door.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 0.4f, 0f);
        box.size = new Vector3(2.4f, 2.4f, 1.8f);
    }
}
