using UnityEditor;

[CustomEditor(typeof(AnomalyTestRig))]
public class AnomalyTestRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("调试在游戏画面右上角。按住 Alt 解放鼠标后勾选，再按确认，当前这一层会直接刷新，不会重开场景。", MessageType.Info);
    }
}
