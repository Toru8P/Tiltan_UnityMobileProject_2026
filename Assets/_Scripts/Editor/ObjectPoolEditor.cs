// Assets/_Scripts/Editor/ObjectPoolEditor.cs

using System.Collections.Generic;
using System.Reflection;
using _Scripts.MainGame.Pool;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor
{
    [CustomEditor(typeof(ObjectPool))]
    public class ObjectPoolEditor : UnityEditor.Editor
    {
        private ObjectPool _pool;
        private FieldInfo _poolsField;
        // store foldout states per prefab instance id
        private static Dictionary<int, bool> s_foldouts = new Dictionary<int, bool>();

        void OnEnable()
        {
            _pool = (ObjectPool)target;
            _poolsField = typeof(ObjectPool).GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public override void OnInspectorGUI()
        {
            // Draw the default inspector first (so you keep other fields visible)
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pools (prefab -> instances)", EditorStyles.boldLabel);

            if (_poolsField == null)
            {
                EditorGUILayout.HelpBox("Could not find private field '_pools' via reflection.", MessageType.Error);
                return;
            }

            var pools = _poolsField.GetValue(_pool) as Dictionary<GameObject, Queue<GameObject>>;
            if (pools == null || pools.Count == 0)
            {
                EditorGUILayout.LabelField("No pooled entries found.");
                return;
            }

            // Iterate keys (prefabs)
            foreach (var kv in pools)
            {
                GameObject prefab = kv.Key;
                Queue<GameObject> queue = kv.Value;

                int keyId = prefab ? prefab.GetInstanceID() : 0;
                bool state = s_foldouts.ContainsKey(keyId) ? s_foldouts[keyId] : false;

                string header = prefab != null ? $"{prefab.name} ({queue?.Count ?? 0})" : $"<NULL PREFAB> ({queue?.Count ?? 0})";
                state = EditorGUILayout.Foldout(state, header, true);
                s_foldouts[keyId] = state;

                if (state)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.BeginHorizontal();
                    if (prefab != null)
                    {
                        if (GUILayout.Button("Ping Prefab", GUILayout.Width(90))) EditorGUIUtility.PingObject(prefab);
                        if (GUILayout.Button("Select Prefab", GUILayout.Width(100))) Selection.activeObject = prefab;
                    }
                    else
                    {
                        GUILayout.Label("Prefab is null", GUILayout.MinWidth(100));
                    }

                    if (GUILayout.Button("Remove null entries", GUILayout.Width(160)))
                    {
                        RemoveNullEntriesFor(prefab, pools);
                        EditorUtility.SetDirty(_pool);
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("Instances in pool:", EditorStyles.miniBoldLabel);

                    if (queue == null || queue.Count == 0)
                    {
                        EditorGUILayout.LabelField("  (empty)");
                    }
                    else
                    {
                        int i = 0;
                        foreach (var instance in queue)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField($"[{i}]", instance, typeof(GameObject), false);
                            if (instance)
                            {
                                if (GUILayout.Button("Ping", GUILayout.Width(46))) EditorGUIUtility.PingObject(instance);
                                if (GUILayout.Button("Select", GUILayout.Width(52))) Selection.activeObject = instance;
                            }
                            else
                            {
                                GUILayout.Label("null", GUILayout.Width(100));
                            }
                            EditorGUILayout.EndHorizontal();
                            i++;
                        }
                    }

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }
            }
        }

        private void RemoveNullEntriesFor(GameObject prefab, Dictionary<GameObject, Queue<GameObject>> pools)
        {
            if (!pools.ContainsKey(prefab)) return;
            var q = pools[prefab];
            var newQ = new Queue<GameObject>();
            foreach (var go in q)
                if (go != null) newQ.Enqueue(go);

            pools[prefab] = newQ;
        }
    }
}