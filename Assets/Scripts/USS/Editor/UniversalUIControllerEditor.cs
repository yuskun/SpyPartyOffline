using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(UniversalUIController))]
public class UniversalUIControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 先畫預設的 Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Static Debug Info", EditorStyles.boldLabel);

        // 透過反射取得 _cursorControllers
        var field = typeof(UniversalUIController).GetField("_cursorControllers",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (field != null)
        {
            var set = field.GetValue(null) as HashSet<UniversalUIController>;
            if (set != null)
            {
                EditorGUILayout.IntField("Cursor Controllers Count", set.Count);

                foreach (var ctrl in set)
                {
                    if (ctrl != null)
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(ctrl.gameObject.name, ctrl, typeof(UniversalUIController), true);
                        EditorGUI.EndDisabledGroup();
                    }
                }
            }
        }

        // _lastHotkeyFrame
        var hotkeyField = typeof(UniversalUIController).GetField("_lastHotkeyFrame",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (hotkeyField != null)
        {
            int val = (int)hotkeyField.GetValue(null);
            EditorGUILayout.IntField("Last Hotkey Frame", val);
        }

        // Play Mode 時持續刷新
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
