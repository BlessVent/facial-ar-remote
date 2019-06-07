﻿using UnityEditor;
using UnityEngine;

namespace Unity.Labs.FacialRemote
{
    [CustomEditor(typeof(BlendShapeMappings))]
    public class BlendShapeMappingsEditor : Editor
    {
        SerializedProperty m_StreamSettings;
        SerializedProperty m_LocationIdentifiers;
        SerializedProperty m_BlendShapeNames;

        void OnEnable()
        {
            m_StreamSettings = serializedObject.FindProperty("m_StreamSettings");
            m_LocationIdentifiers = serializedObject.FindProperty("m_LocationIdentifiers");
            m_BlendShapeNames = serializedObject.FindProperty("m_BlendShapeNames");
        }

        public override void OnInspectorGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_StreamSettings);
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Location Identifiers");
                EditorGUILayout.LabelField("Blend Shape Names");
                EditorGUILayout.EndHorizontal();
                for (var i = 0; i < m_LocationIdentifiers.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(m_LocationIdentifiers.GetArrayElementAtIndex(i), new GUIContent());
                    GUI.enabled = true;
                    if (m_BlendShapeNames.arraySize > i)
                        EditorGUILayout.PropertyField(m_BlendShapeNames.GetArrayElementAtIndex(i), new GUIContent());
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                if (check.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
