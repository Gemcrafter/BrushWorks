// Editor script. Drop in Assets/Editor. Menu: Advanced/Reports/GUID Map Report
// Writes every guid under Assets to Support/Reports/GuidMapReport.txt as "guid path".
// One line per .meta. Searchable offline.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TiltBrush
{
    public static class GuidMapReport
    {
        static readonly Regex k_MetaGuid = new Regex(@"^guid:\s*([0-9a-f]+)", RegexOptions.Compiled);

        [MenuItem("Advanced/Reports/GUID Map Report")]
        public static void Run()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GUID Map Report");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Format: guid path");
            sb.AppendLine();

            string assetsRoot = Application.dataPath;
            string[] metas = Directory.GetFiles(assetsRoot, "*.meta", SearchOption.AllDirectories);
            int count = 0;

            foreach (string meta in metas)
            {
                string[] lines = File.ReadAllLines(meta);
                for (int i = 0; i < lines.Length && i < 10; i++)
                {
                    Match m = k_MetaGuid.Match(lines[i]);
                    if (!m.Success) continue;
                    string g = m.Groups[1].Value;
                    string assetPath = meta.Substring(0, meta.Length - 5);
                    string rel = "Assets" + assetPath.Substring(assetsRoot.Length).Replace('\\', '/');
                    sb.AppendLine(g + " " + rel);
                    count++;
                    break;
                }
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + count);

            string dir = Path.Combine(Application.dataPath, "..", "Support", "Reports");
            Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, "GuidMapReport.txt");
            outPath = Path.GetFullPath(outPath);
            File.WriteAllText(outPath, sb.ToString());
            EditorUtility.RevealInFinder(outPath);
            Debug.LogWarning("Report GUID Map has been run, check Support>Reports folder");
        }
    }
}
#endif
