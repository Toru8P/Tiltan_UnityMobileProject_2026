using System.Collections.Generic;
using _Scripts.MainGame.Difficulty;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor
{
    [CustomEditor(typeof(GeneralDifficultyManager))]
    public class GeneralDifficultyManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _difficultyEntriesProp;
        private SerializedProperty _onDifficultyPhaseChangedProp;

        private void OnEnable()
        {
            _difficultyEntriesProp = serializedObject.FindProperty("difficultyEntries");
            _onDifficultyPhaseChangedProp = serializedObject.FindProperty("onDifficultyPhaseChanged");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            EditorGUILayout.Space();

            DrawEntries();
            EditorGUILayout.Space();

            DrawValidation();
            DrawButtons();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_onDifficultyPhaseChangedProp);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                MonoScript script = MonoScript.FromMonoBehaviour((GeneralDifficultyManager)target);
                EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
            }
        }

        private void DrawEntries()
        {
            EditorGUILayout.LabelField("Difficulty Entries", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            for (int i = 0; i < _difficultyEntriesProp.arraySize; i++)
            {
                SerializedProperty element = _difficultyEntriesProp.GetArrayElementAtIndex(i);
                SerializedProperty phaseProp = element.FindPropertyRelative("phase");
                SerializedProperty timeProp = element.FindPropertyRelative("startTime");

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Entry {i}", EditorStyles.boldLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    _difficultyEntriesProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(phaseProp);
                EditorGUILayout.PropertyField(timeProp, new GUIContent("Start Time"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("Add Entry"))
            {
                int index = _difficultyEntriesProp.arraySize;
                _difficultyEntriesProp.InsertArrayElementAtIndex(index);

                SerializedProperty newElement = _difficultyEntriesProp.GetArrayElementAtIndex(index);
                newElement.FindPropertyRelative("phase").enumValueIndex = (int)DifficultyPhase.Easy;
                newElement.FindPropertyRelative("startTime").floatValue = 0f;
            }
        }

        private void DrawValidation()
        {
            if (_difficultyEntriesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No entries defined. GetPhaseForTime will return Easy.", MessageType.Info);
                return;
            }

            HashSet<DifficultyPhase> usedPhases = new();
            bool hasDuplicatePhase = false;
            bool hasNegativeTime = false;
            bool notSorted = false;

            float previousTime = float.MinValue;

            for (int i = 0; i < _difficultyEntriesProp.arraySize; i++)
            {
                SerializedProperty element = _difficultyEntriesProp.GetArrayElementAtIndex(i);

                DifficultyPhase phase = (DifficultyPhase)element.FindPropertyRelative("phase").intValue;
                float startTime = element.FindPropertyRelative("startTime").floatValue;

                if (!usedPhases.Add(phase))
                    hasDuplicatePhase = true;

                if (startTime < 0f)
                    hasNegativeTime = true;

                if (startTime < previousTime)
                    notSorted = true;

                previousTime = startTime;
            }

            if (hasDuplicatePhase)
                EditorGUILayout.HelpBox("Duplicate phases found. Usually each phase should appear once.", MessageType.Warning);

            if (hasNegativeTime)
                EditorGUILayout.HelpBox("Negative start times found. Usually start times should be 0 or greater.", MessageType.Warning);

            if (notSorted)
                EditorGUILayout.HelpBox("Entries are not sorted by Start Time.", MessageType.Warning);
        }

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Sort By Time"))
            {
                serializedObject.ApplyModifiedProperties();

                GeneralDifficultyManager manager = (GeneralDifficultyManager)target;
                Undo.RecordObject(manager, "Sort Difficulty Entries");
                manager.SortEntriesByTime();
                EditorUtility.SetDirty(manager);

                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}