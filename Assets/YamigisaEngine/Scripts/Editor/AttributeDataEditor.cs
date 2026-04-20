#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Yamigisa.EditorTools
{
    [CustomEditor(typeof(AttributeData))]
    public class AttributeDataEditor : UnityEditor.Editor
    {
        private const string EditorPrefsFolderKey = "Yamigisa.AttributeType.TargetFolder";
        private const string EnumFileName = "AttributeType.cs";
        private const string EnumName = "AttributeType";
        private const string EnumNamespace = "Yamigisa";

        private static readonly string[] DefaultEntries = new[]
        {
            "Health",
            "Hunger",
            "Thirst"
        };

        private string newAttributeTypeName = "";
        private DefaultAsset targetFolderAsset;

        private void OnEnable()
        {
            string savedFolderPath = EditorPrefs.GetString(EditorPrefsFolderKey, string.Empty);
            if (!string.IsNullOrEmpty(savedFolderPath))
            {
                targetFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(savedFolderPath);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Attribute Type Enum Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "If folder is empty: rewrite existing AttributeType.cs found in project.\n" +
                "If folder is assigned: move/create AttributeType.cs in that folder, delete duplicates outside it, then rewrite it.",
                MessageType.Info
            );

            DrawFolderSelector();

            EditorGUILayout.Space(6);
            newAttributeTypeName = EditorGUILayout.TextField("New Attribute Type", newAttributeTypeName);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newAttributeTypeName)))
            {
                if (GUILayout.Button("Add To AttributeType Enum"))
                {
                    AddOrRelocateEnum(newAttributeTypeName);
                }
            }

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Open Active AttributeType.cs"))
            {
                string activePath = ResolveTargetPathOnly();
                if (!string.IsNullOrEmpty(activePath))
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(activePath);
                    if (script != null)
                    {
                        AssetDatabase.OpenAsset(script);
                    }
                    else
                    {
                        Debug.LogWarning("Could not open script at path: " + activePath);
                    }
                }
                else
                {
                    Debug.LogWarning("No AttributeType.cs found and no target folder selected.");
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFolderSelector()
        {
            EditorGUI.BeginChangeCheck();
            targetFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
                "Target Folder",
                targetFolderAsset,
                typeof(DefaultAsset),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                string folderPath = string.Empty;

                if (targetFolderAsset != null)
                {
                    folderPath = AssetDatabase.GetAssetPath(targetFolderAsset);
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        Debug.LogWarning("Selected asset is not a folder.");
                        targetFolderAsset = null;
                        folderPath = string.Empty;
                    }
                }

                EditorPrefs.SetString(EditorPrefsFolderKey, folderPath);
            }

            string currentFolderPath = GetSelectedFolderPath();
            EditorGUILayout.LabelField(
                "Resolved Folder",
                string.IsNullOrEmpty(currentFolderPath) ? "(Auto-detect existing AttributeType.cs)" : currentFolderPath
            );

            string targetPathPreview = ResolveTargetPathOnly();
            EditorGUILayout.LabelField(
                "Resolved File",
                string.IsNullOrEmpty(targetPathPreview) ? "(No target yet)" : targetPathPreview
            );
        }

        private void AddOrRelocateEnum(string rawEnumEntry)
        {
            string cleanEntry = SanitizeEnumName(rawEnumEntry);
            if (string.IsNullOrWhiteSpace(cleanEntry))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Name",
                    "The enum name is invalid. Use letters, numbers, or underscore, and do not start with a number.",
                    "OK"
                );
                return;
            }

            List<string> existingFiles = FindAllAttributeTypeFiles();
            string selectedFolderPath = GetSelectedFolderPath();

            string sourcePathForReading = existingFiles.FirstOrDefault();
            List<string> entries = new List<string>();

            if (!string.IsNullOrEmpty(sourcePathForReading) && File.Exists(sourcePathForReading))
            {
                entries = ReadEnumEntriesFromFile(sourcePathForReading);
            }

            if (entries.Count == 0)
            {
                entries = new List<string>(DefaultEntries);
            }

            if (entries.Contains(cleanEntry))
            {
                EditorUtility.DisplayDialog(
                    "Duplicate Entry",
                    $"'{cleanEntry}' already exists in {EnumName}.",
                    "OK"
                );
                return;
            }

            entries.Add(cleanEntry);

            string targetPath;

            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                targetPath = existingFiles.FirstOrDefault();

                if (string.IsNullOrEmpty(targetPath))
                {
                    EditorUtility.DisplayDialog(
                        "No Existing AttributeType.cs Found",
                        "No existing AttributeType.cs was found in the project.\n\n" +
                        "Assign a target folder first if you want to create a new one.",
                        "OK"
                    );
                    return;
                }

                WriteEnumFile(targetPath, entries);
                AssetDatabase.Refresh();

                Debug.Log($"Rewrote existing enum at: {targetPath}");
            }
            else
            {
                if (!AssetDatabase.IsValidFolder(selectedFolderPath))
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Folder",
                        "Selected target folder is invalid.",
                        "OK"
                    );
                    return;
                }

                targetPath = CombineToAssetPath(selectedFolderPath, EnumFileName);

                DeleteAttributeTypeFilesOutsideTarget(existingFiles, targetPath);
                WriteEnumFile(targetPath, entries);

                AssetDatabase.Refresh();

                Debug.Log($"Created/Rewrote enum at: {targetPath}");
            }

            newAttributeTypeName = string.Empty;
        }

        private string ResolveTargetPathOnly()
        {
            string selectedFolderPath = GetSelectedFolderPath();
            List<string> existingFiles = FindAllAttributeTypeFiles();

            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                if (!AssetDatabase.IsValidFolder(selectedFolderPath))
                    return string.Empty;

                return CombineToAssetPath(selectedFolderPath, EnumFileName);
            }

            return existingFiles.FirstOrDefault() ?? string.Empty;
        }

        private string GetSelectedFolderPath()
        {
            if (targetFolderAsset == null)
                return string.Empty;

            string path = AssetDatabase.GetAssetPath(targetFolderAsset);
            return AssetDatabase.IsValidFolder(path) ? path : string.Empty;
        }

        private static List<string> FindAllAttributeTypeFiles()
        {
            string[] guids = AssetDatabase.FindAssets("AttributeType t:MonoScript");
            List<string> results = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) != EnumFileName)
                    continue;

                results.Add(path.Replace("\\", "/"));
            }

            return results
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        private static void DeleteAttributeTypeFilesOutsideTarget(List<string> existingFiles, string targetPath)
        {
            foreach (string file in existingFiles)
            {
                string normalized = file.Replace("\\", "/");
                if (string.Equals(normalized, targetPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool deleted = AssetDatabase.DeleteAsset(normalized);
                if (deleted)
                {
                    Debug.Log("Deleted old AttributeType.cs: " + normalized);
                }
                else
                {
                    Debug.LogWarning("Failed to delete old AttributeType.cs: " + normalized);
                }
            }
        }

        private static List<string> ReadEnumEntriesFromFile(string assetPath)
        {
            try
            {
                string fullPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fullPath))
                    return new List<string>();

                string content = File.ReadAllText(fullPath);

                int enumKeywordIndex = content.IndexOf($"enum {EnumName}", StringComparison.Ordinal);
                if (enumKeywordIndex < 0)
                    return new List<string>();

                int openBraceIndex = content.IndexOf('{', enumKeywordIndex);
                if (openBraceIndex < 0)
                    return new List<string>();

                int closeBraceIndex = FindMatchingBrace(content, openBraceIndex);
                if (closeBraceIndex < 0)
                    return new List<string>();

                string body = content.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1);

                string[] rawLines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                List<string> entries = new List<string>();

                foreach (string rawLine in rawLines)
                {
                    string line = rawLine;

                    int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                    if (commentIndex >= 0)
                        line = line.Substring(0, commentIndex);

                    line = line.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    line = line.TrimEnd(',');

                    if (line.Contains("="))
                    {
                        line = line.Substring(0, line.IndexOf('=')).Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        entries.Add(line);
                    }
                }

                return entries.Distinct().ToList();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to read enum file: " + e.Message);
                return new List<string>();
            }
        }

        private static void WriteEnumFile(string assetPath, List<string> entries)
        {
            string fullPath = Path.GetFullPath(assetPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(directory))
                throw new Exception("Invalid directory for enum file.");

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"namespace {EnumNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {EnumName}");
            sb.AppendLine("    {");

            for (int i = 0; i < entries.Count; i++)
            {
                string suffix = i < entries.Count - 1 ? "," : "";
                sb.AppendLine($"        {entries[i]}{suffix}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        }

        private static int FindMatchingBrace(string text, int openBraceIndex)
        {
            int depth = 0;

            for (int i = openBraceIndex; i < text.Length; i++)
            {
                if (text[i] == '{')
                    depth++;
                else if (text[i] == '}')
                    depth--;

                if (depth == 0)
                    return i;
            }

            return -1;
        }

        private static string CombineToAssetPath(string folderPath, string fileName)
        {
            return $"{folderPath.TrimEnd('/')}/{fileName}";
        }

        private static string SanitizeEnumName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.Trim();

            StringBuilder sb = new StringBuilder();

            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }

            string result = sb.ToString();

            if (string.IsNullOrWhiteSpace(result))
                return string.Empty;

            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }
    }
}
#endif