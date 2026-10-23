// Editor script. Drop in Assets/Editor. Menu: Advanced/Reports/Panel Map Report
// Must have a scene with PanelManager open. Writes the panel map to Support/Reports.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TiltBrush
{
    public static class PanelMapReport
    {
        [MenuItem("Advanced/Reports/Panel Map Report")]
        public static void Run()
        {
            var pm = UnityEngine.Object.FindObjectOfType<PanelManager>();
            if (pm == null)
            {
                Debug.LogWarning("Report Panel Map failed: no PanelManager in scene");
                return;
            }

            var so = new SerializedObject(pm);
            var map = so.FindProperty("m_PanelMap");
            if (map == null || !map.isArray)
            {
                Debug.LogWarning("Report Panel Map failed: m_PanelMap not found");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Panel Map Report");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Entries: " + map.arraySize);
            sb.AppendLine();

            for (int i = 0; i < map.arraySize; i++)
            {
                var entry = map.GetArrayElementAtIndex(i);
                var prefabProp = entry.FindPropertyRelative("m_PanelPrefab");
                GameObject prefab = prefabProp != null ? prefabProp.objectReferenceValue as GameObject : null;
                string prefabName = prefab != null ? prefab.name : "(null)";
                string typeName = "?";
                if (prefab != null)
                {
                    BasePanel panel = prefab.GetComponent<BasePanel>();
                    if (panel != null)
                    {
                        typeName = panel.Type.ToString() + " (" + (int)panel.Type + ")";
                    }
                }
                sb.AppendLine(i + ": " + typeName + " -> " + prefabName);
            }

            string dir = Path.Combine(Application.dataPath, "..", "Support", "Reports");
            Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, "PanelMapReport.txt");
            outPath = Path.GetFullPath(outPath);
            File.WriteAllText(outPath, sb.ToString());
            EditorUtility.RevealInFinder(outPath);
            Debug.LogWarning(sb.ToString());
            Debug.LogWarning("Report Panel Map has been run, check Support>Reports folder");
        }
    }
}
#endif
