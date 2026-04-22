using Code.MainGame;
using UnityEditor;
using UnityEngine;

namespace Code.Editor
{
    [CustomEditor(typeof(Planet))]
    public class PlanetEditor : UnityEditor.Editor {

        Planet planet;
        UnityEditor.Editor shapeEditor;
        UnityEditor.Editor colourEditor;

        public override void OnInspectorGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                base.OnInspectorGUI();
                if (check.changed)
                {
                    planet.GeneratePlanet();
                }
            }

            if (GUILayout.Button("Generate Planet"))
            {
                planet.GeneratePlanet();
            }

            DrawSettingsEditor(planet.shapeSettings, planet.OnShapeSettingsUpdated, ref planet.shapeSettingsFoldout, ref shapeEditor);
            DrawSettingsEditor(planet.colourSettings, planet.OnColourSettingsUpdated, ref planet.colourSettingsFoldout, ref colourEditor);
        }

        void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated, ref bool foldout, ref UnityEditor.Editor editor)
        {
            if (settings)
            {
                foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
                using EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope();
                if (foldout)
                {
                    CreateCachedEditor(settings, null, ref editor);
                    editor.OnInspectorGUI();

                    if (check.changed)
                    {
                        if (onSettingsUpdated != null)
                        {
                            onSettingsUpdated();
                        }
                    }
                }
            }
        }

        private void OnEnable()
        {
            planet = (Planet)target;
        }
    }
}