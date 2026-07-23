using System.IO;
using _Scripts.MainGame.SaveLoad;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor
{
    [CustomEditor(typeof(SaveLoadManager))]
    public class SaveLoadManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SaveLoadManager manager = (SaveLoadManager)target;
            string path = manager.CurrentWorldPath;
            bool exists = File.Exists(path);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Save Data", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Slot", manager.ActiveSlot.ToString());
            EditorGUILayout.LabelField("Status", exists ? "World file exists" : "No world file for this slot");
            EditorGUILayout.SelectableLabel(path, EditorStyles.miniLabel, GUILayout.Height(28));

            using (new EditorGUI.DisabledScope(!exists))
            {
                if (GUILayout.Button("Delete Current Slot's World Save"))
                {
                    if (EditorUtility.DisplayDialog("Delete Save",
                            "Delete the world save for the active slot?\n\n" + path, "Delete", "Cancel"))
                    {
                        manager.Delete();
                        Debug.Log("[SaveLoad] Deleted world save for active slot.");
                    }
                }
            }

            if (GUILayout.Button("Open Save Folder"))
            {
                string folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Reveals the file if it exists, otherwise opens the containing folder.
                EditorUtility.RevealInFinder(exists ? path : folder);
            }
        }
    }
}
