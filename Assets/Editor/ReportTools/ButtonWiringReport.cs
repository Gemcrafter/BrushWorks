// Editor script. Drop in Assets/Editor. Menu: Advanced/Reports/Button Wiring Report
// Select a panel prefab or scene object, then run. Writes button wiring to Support/Reports.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TiltBrush
{
    public static class ButtonWiringReport
    {
        [MenuItem("Advanced/Reports/Button Wiring Report")]
        public static void Run()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                Debug.LogWarning("Report Button Wiring failed: select a panel prefab or scene object");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Button Wiring Report");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Target: " + target.name);
            sb.AppendLine();

            DumpButtons(target.transform, "", sb);

            string dir = Path.Combine(Application.dataPath, "..", "Support", "Reports");
            Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, "ButtonWiringReport.txt");
            outPath = Path.GetFullPath(outPath);
            File.WriteAllText(outPath, sb.ToString());
            EditorUtility.RevealInFinder(outPath);
            Debug.LogWarning(sb.ToString());
            Debug.LogWarning("Report Button Wiring has been run, check Support>Reports folder");
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
                        here + " [" + mb.GetType().Name + "." + n + "] = " + val
                        + (mapped != null ? " -> " + mapped : ""));
                }
            }

            for (int i = 0; i < root.childCount; i++)
                DumpButtons(root.GetChild(i), here, sb);
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
