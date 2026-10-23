// Editor script. Drop in Assets/Editor. Menu: Advanced/Reports/Missing Asset Reference Report
// Walks prefabs. Reports object-reference fields that are null and have a stored guid that does not resolve.
// Also searches .meta files for each missing guid and shows the path if it still exists in the project.
// Menu: Advanced/Reports/Lookup GUID — paste a guid, shows path or "not in project".
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TiltBrush
{
    public static class MissingAssetReferenceReport
    {
        static readonly Regex k_RefLine = new Regex(@"^\s*([A-Za-z0-9_]+):\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]+)", RegexOptions.Compiled);
        static readonly Regex k_ScriptLine = new Regex(@"^\s*m_Script:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]+)", RegexOptions.Compiled);
        static readonly Regex k_GameObjectName = new Regex(@"^\s*m_Name:\s*(.+)$", RegexOptions.Compiled);
        static readonly Regex k_MetaGuid = new Regex(@"^guid:\s*([0-9a-f]+)", RegexOptions.Compiled);

        static Dictionary<string, string> s_GuidToPath;

        [MenuItem("Advanced/Reports/Missing Asset Reference Report")]
        public static void Run()
        {
            BuildGuidMap();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Missing Asset Reference Report");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int missingCount = 0;
            int prefabCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;
                string full = Path.GetFullPath(path);
                if (!File.Exists(full)) continue;
                prefabCount++;

                string[] lines = File.ReadAllLines(full);
                StringBuilder prefabSb = new StringBuilder();
                bool any = false;

                string currentName = "";
                string currentScriptGuid = "";
                Type currentScriptType = null;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    Match nameMatch = k_GameObjectName.Match(line);
                    if (nameMatch.Success)
                        currentName = nameMatch.Groups[1].Value.Trim();

                    Match scriptMatch = k_ScriptLine.Match(line);
                    if (scriptMatch.Success)
                    {
                        currentScriptGuid = scriptMatch.Groups[2].Value;
                        string scriptPath = AssetDatabase.GUIDToAssetPath(currentScriptGuid);
                        MonoScript ms = string.IsNullOrEmpty(scriptPath) ? null : AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                        currentScriptType = ms != null ? ms.GetClass() : null;
                    }

                    Match refMatch = k_RefLine.Match(line);
                    if (!refMatch.Success) continue;
                    string fieldName = refMatch.Groups[1].Value;
                    string refGuid = refMatch.Groups[3].Value;
                    if (fieldName == "m_Script") continue;

                    string assetPath = AssetDatabase.GUIDToAssetPath(refGuid);
                    if (!string.IsNullOrEmpty(assetPath)) continue;

                    string typeName = currentScriptType != null ? currentScriptType.Name : ("script:" + currentScriptGuid);
                    string expected = "?";
                    if (currentScriptType != null)
                    {
                        Type ft = GetFieldType(currentScriptType, fieldName);
                        if (ft != null) expected = ft.Name;
                    }

                    string found = s_GuidToPath.ContainsKey(refGuid) ? s_GuidToPath[refGuid] : "not in project";

                    prefabSb.AppendLine("  Missing: " + currentName + " / " + typeName + "." + fieldName);
                    prefabSb.AppendLine("    expects " + expected + ", guid " + refGuid);
                    prefabSb.AppendLine("    meta search: " + found);
                    missingCount++;
                    any = true;
                }

                if (any)
                {
                    sb.AppendLine(path);
                    sb.Append(prefabSb.ToString());
                    sb.AppendLine();
                }
            }

            sb.AppendLine("Prefabs scanned: " + prefabCount);
            sb.AppendLine("Missing asset references: " + missingCount);

            string dir = Path.Combine(Application.dataPath, "..", "Support", "Reports");
            Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, "MissingAssetReferenceReport.txt");
            outPath = Path.GetFullPath(outPath);
            File.WriteAllText(outPath, sb.ToString());
            EditorUtility.RevealInFinder(outPath);
            Debug.LogWarning("Report Missing Asset Reference has been run, check Support>Reports folder");
        }

        [MenuItem("Advanced/Reports/Lookup GUID")]
        public static void LookupGuid()
        {
            string guid = EditorInputDialog.Show("Lookup GUID", "Paste guid:", "");
            if (string.IsNullOrEmpty(guid)) return;
            guid = guid.Trim().ToLowerInvariant();

            BuildGuidMap();
            string found = s_GuidToPath.ContainsKey(guid) ? s_GuidToPath[guid] : null;
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("GUID: " + guid);
            if (!string.IsNullOrEmpty(assetPath))
                sb.AppendLine("AssetDatabase: " + assetPath);
            else
                sb.AppendLine("AssetDatabase: not resolved");
            if (found != null)
                sb.AppendLine("Meta search: " + found);
            else
                sb.AppendLine("Meta search: not in project");

            EditorUtility.DisplayDialog("Lookup GUID", sb.ToString(), "OK");
        }

        static void BuildGuidMap()
        {
            if (s_GuidToPath != null) return;
            s_GuidToPath = new Dictionary<string, string>();
            string assetsRoot = Application.dataPath;
            string[] metas = Directory.GetFiles(assetsRoot, "*.meta", SearchOption.AllDirectories);
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
                    if (!s_GuidToPath.ContainsKey(g))
                        s_GuidToPath[g] = rel;
                    break;
                }
            }
        }

        static Type GetFieldType(Type type, string fieldName)
        {
            FieldInfo fi = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) return null;
            return fi.FieldType;
        }
    }

    // Minimal input dialog so Lookup GUID can take a string.
    public class EditorInputDialog : EditorWindow
    {
        string m_Text = "";
        string m_Message = "";
        Action<string> m_OnOk;

        public static string Show(string title, string message, string defaultText)
        {
            string result = null;
            EditorInputDialog w = CreateInstance<EditorInputDialog>();
            w.titleContent = new GUIContent(title);
            w.m_Message = message;
            w.m_Text = defaultText ?? "";
            w.m_OnOk = (s) => { result = s; };
            w.minSize = new Vector2(400, 100);
            w.maxSize = new Vector2(600, 120);
            w.ShowModal();
            return result;
        }

        void OnGUI()
        {
            GUILayout.Label(m_Message);
            m_Text = EditorGUILayout.TextField(m_Text);
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                m_OnOk?.Invoke(m_Text);
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                m_OnOk?.Invoke(null);
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
