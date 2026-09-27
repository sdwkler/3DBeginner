using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoopHouseBuilder
{
    const string ScenePath = "Assets/Scenes/GameScene.unity";
    const string WallPath = "Assets/Prefabs/Environment/Corridors/Wall_Straight_01.prefab";
    const string WardrobePath = "Assets/Prefabs/Environment/Bedroom/Wardrobe.prefab";
    const string BedPath = "Assets/Prefabs/Environment/Bedroom/Bed.prefab";
    const string LightPath = "Assets/Prefabs/Environment/Corridors/Light.prefab";
    const string DoorPath = "Assets/Prefabs/Environment/Corridors/Door.prefab";
    const string ChairPath = "Assets/Prefabs/Environment/Dinigroom/Chair_1.prefab";
    const string ClockPath = "Assets/Prefabs/Environment/Dinigroom/Clock.prefab";
    const string MirrorPath = "Assets/Prefabs/Environment/Bathroom/Mirror.prefab";
    const string FloorMaterialPath = "Assets/Materials/Corridor/Floor.mat";
    const string WallMaterialPath = "Assets/Materials/Corridor/Wall_Corridor.mat";

    const float RepeatedLength = 16f;
    const float IntroStart = 0f;
    const float MiddleStart = 16f;
    const float OutroStart = 46f;
    const float OutroEnd = 62f;

    static void RebuildFromMenu()
    {
        EditorUtility.DisplayDialog(
            "Rebuild Loop House",
            "这个菜单会用平面走廊盖掉三维房子，已经停用。请打开 Assets/Scenes/GameScene。",
            "确定");
    }

    static void TryBuild(bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        Scene previous = EditorSceneManager.GetActiveScene();
        Scene gameScene = EditorSceneManager.GetSceneByPath(ScenePath);
        bool openedHere = false;

        if (!gameScene.IsValid() || !gameScene.isLoaded)
        {
            gameScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedHere = true;
        }

        if (!force && HasLoopLayout(gameScene))
        {
            if (openedHere)
                EditorSceneManager.CloseScene(gameScene, true);
            return;
        }

        EditorSceneManager.SetActiveScene(gameScene);

        if (force)
        {
            Transform existing = FindRoot(gameScene, "LoopLayout");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
        }

        Transform level = FindRoot(gameScene, "Level");
        if (level != null)
            level.gameObject.SetActive(false);

        BuildLayout();
        PlaceActors(gameScene);

        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        if (previous.IsValid() && previous.isLoaded && previous != gameScene)
            EditorSceneManager.SetActiveScene(previous);

        if (openedHere)
            EditorSceneManager.CloseScene(gameScene, true);
    }

    static bool HasLoopLayout(Scene scene)
    {
        return FindRoot(scene, "LoopLayout") != null;
    }

    static Transform FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
                return root.transform;
        }

        return null;
    }

    static void BuildLayout()
    {
        GameObject layout = new GameObject("LoopLayout");
        Transform root = layout.transform;

        CreateFloorAndCeiling(root);
        PlacePlainStretch(root, IntroStart);
        PlacePlainStretch(root, OutroStart);
        PlaceMiddleCorridor(root);
        PlaceWardrobeRoom(root);
        PlaceConnectedRooms(root);
        PlaceAnomalyRoom(root);
        CreateLoopTrigger(root);
    }

    static void CreateFloorAndCeiling(Transform root)
    {
        Material floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
        Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root, false);
        floor.transform.position = new Vector3(2f, -0.1f, 31f);
        floor.transform.localScale = new Vector3(24f, 0.2f, 70f);
        AssignMaterial(floor, floorMaterial);

        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetParent(root, false);
        ceiling.transform.position = new Vector3(2f, 2.55f, 31f);
        ceiling.transform.localScale = new Vector3(24f, 0.1f, 70f);
        AssignMaterial(ceiling, wallMaterial);
    }

    static void PlacePlainStretch(Transform root, float zStart)
    {
        PlaceWallRunZ(root, -1.5f, zStart, Mathf.RoundToInt(RepeatedLength), null);
        PlaceWallRunZ(root, 1.5f, zStart, Mathf.RoundToInt(RepeatedLength), null);
        PlaceProp(LightPath, root, new Vector3(-1.2f, 1.8f, zStart + 4f), Quaternion.identity);
        PlaceProp(LightPath, root, new Vector3(-1.2f, 1.8f, zStart + 12f), Quaternion.identity);
    }

    static void PlaceMiddleCorridor(Transform root)
    {
        PlaceWallRunZ(root, -1.5f, MiddleStart, 30, z => z >= 28f && z < 30f);
        PlaceWallRunZ(root, 1.5f, MiddleStart, 30, z =>
            (z >= 18f && z < 20f) || (z >= 24f && z < 26f));

        PlaceWallX(root, 1.5f, IntroStart, 3);
        PlaceWallX(root, 1.5f, OutroEnd, 3);
    }

    static void PlaceWardrobeRoom(Transform root)
    {
        float x1 = 6.5f;
        float z0 = 17f;
        float z1 = 23f;

        PlaceWallX(root, x1, z0, 5);
        PlaceWallX(root, x1, z1, 5);
        PlaceWallRunZ(root, x1, z0, 6, null);
        PlaceProp(DoorPath, root, new Vector3(1.7f, 0f, 19f), Quaternion.Euler(0f, -90f, 0f));
        PlaceProp(WardrobePath, root, new Vector3(4.2f, 1.05f, 21.2f), Quaternion.Euler(0f, 180f, 0f));
        PlaceProp(BedPath, root, new Vector3(5.2f, 0f, 18.4f), Quaternion.Euler(0f, 90f, 0f));
        PlaceProp(LightPath, root, new Vector3(4f, 1.8f, 20f), Quaternion.identity);
    }

    static void PlaceConnectedRooms(Transform root)
    {
        PlaceWallX(root, 11.5f, 17f, 5);
        PlaceWallX(root, 11.5f, 23f, 2);
        PlaceWallX(root, 8f, 23f, 2);
        PlaceWallRunZ(root, 6.5f, 17f, 6, z => z >= 19f && z < 21f);
        PlaceWallRunZ(root, 11.5f, 17f, 6, null);
        PlaceWallRunZ(root, 10f, 23f, 3, null);
        PlaceWallRunZ(root, 8f, 24f, 2, null);
        PlaceWallX(root, 10f, 26f, 9);

        PlaceProp(DoorPath, root, new Vector3(6.7f, 0f, 20f), Quaternion.Euler(0f, -90f, 0f));
        PlaceProp(DoorPath, root, new Vector3(1.7f, 0f, 25f), Quaternion.Euler(0f, -90f, 0f));
        PlaceProp(WardrobePath, root, new Vector3(9.4f, 1.05f, 18.6f), Quaternion.Euler(0f, 90f, 0f));
        PlaceProp(BedPath, root, new Vector3(10.2f, 0f, 21.4f), Quaternion.Euler(0f, -90f, 0f));
        PlaceProp(LightPath, root, new Vector3(8.5f, 1.8f, 20f), Quaternion.identity);
    }

    static void PlaceAnomalyRoom(Transform root)
    {
        PlaceWallX(root, -1.5f, 26f, 5);
        PlaceWallX(root, -1.5f, 33f, 5);
        PlaceWallRunZ(root, -6.5f, 26f, 7, null);

        PlaceProp(DoorPath, root, new Vector3(-1.7f, 0f, 29f), Quaternion.Euler(0f, 90f, 0f));
        PlaceProp(ChairPath, root, new Vector3(-3.2f, 0f, 28.2f), Quaternion.Euler(0f, 20f, 0f));
        PlaceProp(ChairPath, root, new Vector3(-4.4f, 0f, 30.4f), Quaternion.Euler(0f, 160f, 0f));
        PlaceProp(ClockPath, root, new Vector3(-5.6f, 0f, 31.5f), Quaternion.Euler(0f, 90f, 0f));
        PlaceProp(MirrorPath, root, new Vector3(-6.1f, 1.2f, 29.2f), Quaternion.Euler(0f, 90f, 0f));
        PlaceProp(LightPath, root, new Vector3(-3.6f, 1.8f, 29.5f), Quaternion.identity);
    }

    static void CreateLoopTrigger(Transform root)
    {
        GameObject fromAnchor = new GameObject("LoopFromAnchor");
        fromAnchor.transform.SetParent(root, false);
        fromAnchor.transform.position = new Vector3(0f, 0f, OutroStart);

        GameObject toAnchor = new GameObject("LoopToAnchor");
        toAnchor.transform.SetParent(root, false);
        toAnchor.transform.position = new Vector3(0f, 0f, IntroStart);

        GameObject trigger = new GameObject("LoopReturnTrigger");
        trigger.transform.SetParent(root, false);
        trigger.transform.position = new Vector3(0f, 1f, OutroStart + 8f);

        BoxCollider collider = trigger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(2.4f, 2f, 0.4f);

        LoopReturn loopReturn = trigger.AddComponent<LoopReturn>();
        loopReturn.fromAnchor = fromAnchor.transform;
        loopReturn.toAnchor = toAnchor.transform;
    }

    static void PlaceActors(Scene scene)
    {
        Transform player = FindRoot(scene, "JohnLemon");
        if (player != null)
        {
            player.SetPositionAndRotation(new Vector3(0f, 0.05f, 2f), Quaternion.identity);
        }

        Transform gargoyle = FindRoot(scene, "Gargoyle");
        if (gargoyle != null)
        {
            gargoyle.SetPositionAndRotation(new Vector3(6.5f, 0f, 20f), Quaternion.Euler(0f, -90f, 0f));
            Observer observer = gargoyle.GetComponentInChildren<Observer>();
            if (observer != null && player != null)
            {
                observer.player = player;
                observer.gameEnding = Object.FindObjectOfType<GameEnding>();
            }
        }
    }

    static void PlaceWallRunZ(Transform parent, float x, float zStart, int count, System.Func<float, bool> skip)
    {
        Quaternion rotation = Quaternion.Euler(0f, -90f, 0f);
        for (int i = 0; i < count; i++)
        {
            float z = zStart + i;
            if (skip != null && skip(z))
                continue;

            PlaceProp(WallPath, parent, new Vector3(x, 0f, z), rotation);
        }
    }

    static void PlaceWallX(Transform parent, float xEnd, float z, int count)
    {
        for (int i = 0; i < count; i++)
            PlaceProp(WallPath, parent, new Vector3(xEnd - i, 0f, z), Quaternion.identity);
    }

    static void PlaceProp(string path, Transform parent, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(position, rotation);
    }

    static void AssignMaterial(GameObject target, Material material)
    {
        if (material == null)
            return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }
}
