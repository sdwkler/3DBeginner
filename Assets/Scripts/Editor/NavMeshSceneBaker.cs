using System.IO;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NavMeshSceneBaker
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string RequestPath = "Temp/bake-navmesh.request";

    [MenuItem("Tools/Bake Sample Scene NavMesh")]
    public static void BakeFromMenu()
    {
        BeginBake();
    }

    [InitializeOnLoadMethod]
    static void BakeWhenRequested()
    {
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        EditorApplication.delayCall += BeginBake;
    }

    static void BeginBake()
    {
        SceneSetup();
        NavMeshSurface[] surfaces = Object.FindObjectsOfType<NavMeshSurface>();
        if (surfaces.Length == 0)
        {
            Debug.LogError("SampleScene NavMesh bake found no NavMeshSurface.");
            return;
        }

        NavMeshAssetManager.instance.StartBakingSurfaces(surfaces);
        EditorApplication.update += WaitForBake;
    }

    static void SceneSetup()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path == ScenePath)
            return;

        if (scene.isDirty)
            EditorSceneManager.SaveOpenScenes();

        EditorSceneManager.OpenScene(ScenePath);
    }

    static void WaitForBake()
    {
        NavMeshSurface[] surfaces = Object.FindObjectsOfType<NavMeshSurface>();
        for (int i = 0; i < surfaces.Length; i++)
        {
            if (NavMeshAssetManager.instance.IsSurfaceBaking(surfaces[i]))
                return;
        }

        EditorApplication.update -= WaitForBake;
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("SampleScene NavMesh bake complete. Surfaces: " + surfaces.Length);
    }
}
