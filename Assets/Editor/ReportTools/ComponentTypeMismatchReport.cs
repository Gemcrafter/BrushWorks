// Editor script. Drop in Assets/Editor. Menu: Advanced/Reports/Component Type Mismatch Report
// Walks prefabs. Reports object-reference fields where the stored type does not match the field type,
// and fields where the reference is broken (non-zero id, null object).
// Each line includes the GameObject hierarchy path so it can be found in the Inspector.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TiltBrush
{
    public static class ComponentTypeMismatchReport
    {
        [MenuItem("Advanced/Reports/Component Type Mismatch Report")]
        public static void Run()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Component Type Mismatch Report");
            sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int mismatchCount = 0;
            int brokenCount = 0;
            int missingScriptCount = 0;
            int prefabCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                prefabCount++;

                StringBuilder prefabSb = new StringBuilder();
                bool any = false;

                foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                {
                    string goPath = GetPath(t, prefab.transform);
                    Component[] comps = t.GetComponents<Component>();
                    for (int i = 0; i < comps.Length; i++)
                    {
                        Component c = comps[i];
                        if (c == null)
                        {
                            prefabSb.AppendLine("  Missing script: " + goPath + " (component index " + i + ")");
                            missingScriptCount++;
                            any = true;
                            continue;
                        }
                        MonoBehaviour mb = c as MonoBehaviour;
                        if (mb == null) continue;

                        SerializedObject so = new SerializedObject(mb);
                        SerializedProperty prop = so.GetIterator();
                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;

                            UnityEngine.Object obj = prop.objectReferenceValue;
                            UnityEngine.EntityId entityId = prop.objectReferenceEntityIdValue;

                            if (obj == null && entityId != UnityEngine.EntityId.None)
                            {
                                prefabSb.AppendLine("  Broken reference: " + goPath + " / " + mb.GetType().Name + "." + prop.propertyPath + " (id " + entityId + ")");
                                brokenCount++;
                                any = true;
                                continue;
                            }
                            if (obj == null) continue;

                            Type fieldType = GetFieldType(mb.GetType(), prop.propertyPath);
                            if (fieldType == null) continue;
                            if (fieldType.IsAssignableFrom(obj.GetType())) continue;

                            prefabSb.AppendLine("  Type mismatch: " + goPath + " / " + mb.GetType().Name + "." + prop.propertyPath);
                            prefabSb.AppendLine("    expects " + fieldType.Name + ", stored " + obj.GetType().Name);
                            mismatchCount++;
                            any = true;
                        }
                    }
                }

                if (any)
                {
                    sb.AppendLine(path);
                    sb.Append(prefabSb.ToString());
                    sb.AppendLine();
                }
            }

            sb.AppendLine("Prefabs scanned: " + prefabCount);
            sb.AppendLine("Type mismatches: " + mismatchCount);
            sb.AppendLine("Broken references: " + brokenCount);
            sb.AppendLine("Missing scripts: " + missingScriptCount);

            string dir = Path.Combine(Application.dataPath, "..", "Support", "Reports");
            Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, "ComponentTypeMismatchReport.txt");
            outPath = Path.GetFullPath(outPath);
            File.WriteAllText(outPath, sb.ToString());
            EditorUtility.RevealInFinder(outPath);
            Debug.LogWarning("Report Component Type Mismatch has been run, check Support>Reports folder");
        }

        static Type GetFieldType(Type type, string propertyPath)
        {
            string[] parts = propertyPath.Split('.');
            Type current = type;
            for (int i = 0; i < parts.Length; i++)
            {
                string name = parts[i];
                if (name == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
                {
                    if (current.IsArray)
                        current = current.GetElementType();
                    else if (current.IsGenericType)
                        current = current.GetGenericArguments()[0];
                    i++;
                    continue;
                }
                FieldInfo fi = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi == null) return null;
                current = fi.FieldType;
            }
            return current;
        }

        static string GetPath(Transform t, Transform root)
        {
            if (t == root) return t.name;
            return GetPath(t.parent, root) + "/" + t.name;
        }
    }
}
#endif
