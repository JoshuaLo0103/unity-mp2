#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Converts Built-in Render Pipeline materials to URP Lit.
/// Fixes magenta materials in URP projects (e.g. imported asset packs).
/// Menu: Tools > Convert Built-in Materials to URP
/// </summary>
public static class ConvertToURPMaterials
{
    private const string URP_LIT_SHADER = "Universal Render Pipeline/Lit";
    private const string URP_SIMPLE_LIT_SHADER = "Universal Render Pipeline/Simple Lit";

    [MenuItem("Tools/Convert Built-in Materials to URP")]
    public static void ConvertAll()
    {
        Shader urpLit = Shader.Find(URP_LIT_SHADER);
        if (urpLit == null)
        {
            Debug.LogError("[ConvertToURP] URP Lit shader not found. Is URP installed?");
            return;
        }

        Shader urpSimpleLit = Shader.Find(URP_SIMPLE_LIT_SHADER);
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int converted = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Skip if already using URP shader
            if (mat.shader.name.StartsWith("Universal Render Pipeline/") ||
                mat.shader.name.StartsWith("TextMeshPro/") ||
                mat.shader.name.StartsWith("Shader Graphs/"))
                continue;

            // Built-in shaders: Standard, Legacy, Nature, Particles, etc.
            if (IsBuiltInShader(mat.shader))
            {
                mat.shader = urpLit;
                EditorUtility.SetDirty(mat);
                converted++;
                Debug.Log($"[ConvertToURP] Converted: {mat.name} ({path})");
            }
        }

        if (converted > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[ConvertToURP] Done. Converted {converted} material(s).");
    }

    private static bool IsBuiltInShader(Shader shader)
    {
        if (shader == null) return false;
        string name = shader.name;

        // Standard pipeline shaders
        return name == "Standard" ||
               name.StartsWith("Standard (") ||
               name.StartsWith("Legacy Shaders/") ||
               name.StartsWith("Nature/") ||
               name.StartsWith("Particles/") ||
               name.StartsWith("Mobile/") ||
               name.StartsWith("Unlit/") ||
               name == "Diffuse" ||
               name == "Specular" ||
               name == "Bumped Diffuse" ||
               name == "Bumped Specular";
    }
}
#endif
