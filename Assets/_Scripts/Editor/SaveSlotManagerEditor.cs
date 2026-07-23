using System.Collections.Generic;
using System.IO;
using _Scripts.CharacterCreation;
using _Scripts.MainGame.SaveLoad;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor
{
    [CustomEditor(typeof(SaveSlotManager))]
    public class SaveSlotManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SaveSlotManager manager = (SaveSlotManager)target;
            string dir = SaveSlotManager.SaveDirectory;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Save Slots", EditorStyles.boldLabel);

            List<int> indexes = CollectSlotIndexes(manager, dir);
            EditorGUILayout.LabelField("Accessible slots", indexes.Count.ToString());

            if (indexes.Count == 0)
            {
                EditorGUILayout.HelpBox("No save slots found in:\n" + dir, MessageType.Info);
            }
            else
            {
                // Metadata by index, for showing name/date next to each slot.
                Dictionary<int, SaveSlotData> metaByIndex = new Dictionary<int, SaveSlotData>();
                foreach (SaveSlotData d in manager.LoadAllSlots())
                    metaByIndex[d.slotIndex] = d;

                int toDelete = -1;

                foreach (int idx in indexes)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                    metaByIndex.TryGetValue(idx, out SaveSlotData data);
                    bool hasWorld = File.Exists(SaveLoadManager.WorldPath(idx));

                    string title = data != null
                        ? $"[{idx}]  {data.characterName}"
                        : $"[{idx}]  (no metadata)";
                    string detail = data != null ? data.FormattedDate() : "";
                    detail += hasWorld ? "   • world" : "   • no world";

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Delete", GUILayout.Width(70), GUILayout.Height(32)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Slot",
                                $"Delete save slot {idx}?\n\nRemoves metadata, thumbnail and world save.",
                                "Delete", "Cancel"))
                        {
                            toDelete = idx;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (toDelete >= 0)
                {
                    manager.DeleteSlot(toDelete);
                    Debug.Log($"[SaveSlot] Deleted slot {toDelete}.");
                    Repaint();
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh")) Repaint();
            using (new EditorGUI.DisabledScope(!Directory.Exists(dir)))
            {
                if (GUILayout.Button("Open Save Folder"))
                    EditorUtility.RevealInFinder(dir);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Union of slot indexes from metadata files (slot_N.json) and world files (slot_N_world.json).
        private static List<int> CollectSlotIndexes(SaveSlotManager manager, string dir)
        {
            SortedSet<int> set = new SortedSet<int>();

            foreach (SaveSlotData d in manager.LoadAllSlots())
                set.Add(d.slotIndex);

            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir, "slot_*_world.json"))
                {
                    string name = Path.GetFileNameWithoutExtension(file); // e.g. "slot_2_world"
                    string mid = name.Substring("slot_".Length);          // "2_world"
                    int underscore = mid.IndexOf('_');
                    if (underscore > 0 && int.TryParse(mid.Substring(0, underscore), out int idx))
                        set.Add(idx);
                }
            }

            return new List<int>(set);
        }
    }
}
