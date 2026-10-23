// Editor script. Drop in Assets/Editor. Menu: Advanced/Reports/Panel Wiring Full Report
// Writes a single file with panel map entries and every button on every prefab.
#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TiltBrush
{
    public static class PanelWiringFullReport
    {
        [MenuItem("Advanced/Reports/Panel Wiring Full Report")]
        public static void Run()
        {
            PanelManager pm = Object.FindObjectOfType<PanelManager>();
            if (pm == null)
            {
                Debug.LogWarning("Report Panel Wiring failed: no PanelManager in scene");
                return;
            }

            var so = new SerializedObject(pm);
            var map = so.FindProperty("m_PanelMap");
            if (map == null || !map.isArray)
            {
                Debug.LogWarning("Report Panel Wiring failed: m_PanelMap not found");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Panel Wiring Full Report");
            sb.AppendLine("Generated: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Panel map entries: " + map.arraySize);
            sb.AppendLine();
            sb.AppendLine("Note: Some panels have explicit enum values that do not match their array index.");
            sb.AppendLine("The array index is only used for the Init uniqueness check.");
            sb.AppendLine("The prefab type is what buttons look up. They do not have to match.");
            sb.AppendLine("Expected mismatches are labeled. Unexpected mismatches are flagged as MISMATCH.");
            sb.AppendLine();

            for (int i = 0; i < map.arraySize; ++i)
            {
                var entry = map.GetArrayElementAtIndex(i);
                var prefabProp = entry.FindPropertyRelative("m_PanelPrefab");
                GameObject prefab = prefabProp != null ? prefabProp.objectReferenceValue as GameObject : null;

                BasePanel.PanelType indexType = (BasePanel.PanelType)i;
                string prefabName = prefab != null ? prefab.name : "(null)";
                string prefabType = "(no panel)";
                int prefabTypeVal = -1;
                if (prefab != null)
                {
                    BasePanel panel = prefab.GetComponent<BasePanel>();
                    if (panel != null)
                    {
                        prefabType = panel.Type.ToString();
                        prefabTypeVal = (int)panel.Type;
                    }
                }

                sb.AppendLine("=== [" + i + "] " + prefabName + " ===");
                sb.AppendLine("  indexType: " + indexType + " (" + (int)indexType + ")");
                sb.AppendLine("  prefabType: " + prefabType + " (" + prefabTypeVal + ")");
                if (prefab != null && prefabTypeVal != (int)indexType)
                {
                    if (IsExpectedMismatch(i, prefabTypeVal))
                    {
                        sb.AppendLine("  expected: explicit enum value or shared type");
                    }
                    else
                    {
                        sb.AppendLine("  MISMATCH: prefab type does not match index");
                    }
                }

                if (prefab != null)
                {
                    DumpButtons(prefab.transform, "", sb);
                }
                sb.AppendLine();
            }

            string dir = Path.Combine(Application.dataPath, "..", "Support", "Reports");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "PanelWiringFullReport.txt");
            path = Path.GetFullPath(path);
            File.WriteAllText(path, sb.ToString());
            EditorUtility.RevealInFinder(path);
            Debug.LogWarning("Report Panel Wiring has been run, check Support>Reports folder");
        }

        static bool IsExpectedMismatch(int index, int prefabType)
        {
            // Explicit enum values that do not match array index
            if (index == 35 && prefabType == 5200) return true;  // WebcamPanel
            if (index == 36 && prefabType == 6000) return true;  // Scripts
            if (index == 37 && prefabType == 8000) return true;  // SnapSettings
            if (index == 38 && prefabType == 11600) return true; // QuillLibrary
            if (index == 39 && prefabType == 12000) return true; // TransformPanel
            if (index == 40 && prefabType == 14100) return true; // MirrorControls
            if (index == 41 && prefabType == 14101) return true; // MultiMirrorSettings
            if (index == 42 && prefabType == 14200) return true; // BrushCuration
            if (index == 43 && prefabType == 14300) return true; // AdvancedLaunch
            if (index == 44 && prefabType == 15000) return true; // LayersPanel
            if (index == 46 && prefabType == 20300) return true; // WhatsNewPanel
            if (index == 47 && prefabType == 20301) return true; // BlocksPromoPanel
            if (index == 48 && prefabType == 20302) return true; // AdminPanelViewOnly
            // Shared types (mobile variants and aliases)
            if (index == 13 && prefabType == 2) return true;     // BrushesPanel_Mobile shares Brush
            if (index == 26 && prefabType == 4) return true;     // AdminPanel shares AdminPanelMobile
            if (index == 45 && prefabType == 7) return true;     // ExperimentalPanel shares Experimental
            return false;
        }

        static void DumpButtons(Transform root, string path, StringBuilder sb)
        {
            string here = string.IsNullOrEmpty(path) ? root.name : path + "/" + root.name;

            foreach (var mb in root.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                var so = new SerializedObject(mb);
                var iter = so.GetIterator();
                bool enter = true;
                while (iter.Next(enter))
                {
                    enter = false;
                    if (iter.propertyType != SerializedPropertyType.Enum &&
                        iter.propertyType != SerializedPropertyType.Integer)
                    {
                        continue;
                    }

                    string n = iter.name;
                    if (n != "m_Command" && n != "m_Type" && n != "m_PanelType" &&
                        n != "m_CommandParam" && n != "m_CommandParam2")
                    {
                        continue;
                    }

                    int val = iter.intValue;
                    string mapped = MapValue(n, val);
                    sb.AppendLine(
                        "  " + here + " [" + mb.GetType().Name + "." + n + "] = " + val
                        + (mapped != null ? " -> " + mapped : ""));
                }
            }

            for (int i = 0; i < root.childCount; ++i)
            {
                DumpButtons(root.GetChild(i), here, sb);
            }
        }

        static string MapValue(string field, int val)
        {
            if (field == "m_Command")
            {
                if (System.Enum.IsDefined(typeof(SketchControlsScript.GlobalCommands), val))
                {
                    return ((SketchControlsScript.GlobalCommands)val).ToString();
                }
                return "(not in GlobalCommands)";
            }

            if (field == "m_Type" || field == "m_PanelType")
            {
                if (System.Enum.IsDefined(typeof(BasePanel.PanelType), val))
                {
                    return ((BasePanel.PanelType)val).ToString();
                }
                return "(not in PanelType)";
            }

            return null;
        }
    }
}
#endif
