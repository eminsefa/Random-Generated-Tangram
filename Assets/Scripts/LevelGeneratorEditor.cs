#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var generator = (LevelGenerator)target;
        if(GUILayout.Button("Set Level"))
        {
            generator.SetLevel();
        }
    }
}
#endif