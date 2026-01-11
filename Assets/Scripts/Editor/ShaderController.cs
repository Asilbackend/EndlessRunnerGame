using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShaderGlobals))]
public class ShaderController : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ctrl = (ShaderGlobals)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Apply", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply to ALL Materials in Project"))
        {
            ApplyToAllMaterialsInProject(ctrl);
        }

        if (GUILayout.Button("Apply to Materials Used in THIS Scene"))
        {
            ApplyToSceneMaterials(ctrl);
        }
    }

    static void ApplyToAllMaterialsInProject(ShaderGlobals ctrl)
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int changed = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) continue;

            if (ctrl.targetShader != null && mat.shader != ctrl.targetShader)
                continue;

            if (ApplyToMaterial(mat, ctrl))
            {
                EditorUtility.SetDirty(mat);
                changed++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"MaterialMasterController: Updated {changed} materials (Project).");
    }

    static void ApplyToSceneMaterials(ShaderGlobals ctrl)
    {
        var renderers = Object.FindObjectsOfType<Renderer>(true);
        int changed = 0;

        foreach (var r in renderers)
        {
            if (!r) continue;

            // sharedMaterials edits material assets referenced by renderers (not instances)
            var mats = r.sharedMaterials;
            foreach (var mat in mats)
            {
                if (!mat) continue;

                if (ctrl.targetShader != null && mat.shader != ctrl.targetShader)
                    continue;

                if (ApplyToMaterial(mat, ctrl))
                {
                    EditorUtility.SetDirty(mat);
                    changed++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"MaterialMasterController: Updated {changed} materials (Scene).");
    }

    static bool ApplyToMaterial(Material mat, ShaderGlobals c)
    {
        bool touched = false;

        touched |= SetFloatIfExists(mat, c.EarthRadiusProp, c.EarthRadius);
        touched |= SetFloatIfExists(mat, c.SideBendStrengthProp, c.SideBendStrength);

        touched |= SetFloatIfExists(mat, c.FadeStartProp, c.FadeStart);
        touched |= SetFloatIfExists(mat, c.FadeEndProp, c.FadeEnd);

        touched |= SetColorIfExists(mat, c.RevealColorProp, c.RevealColor);
        touched |= SetColorIfExists(mat, c.ShadowColorProp, c.ShadowColor);

        touched |= SetIntIfExists(mat, c.StepsProp, c.Steps);
        touched |= SetFloatIfExists(mat, c.LightIntensityProp, c.LightIntensity);

        touched |= SetFloatIfExists(mat, c.RimPowerProp, c.RimPower);
        touched |= SetFloatIfExists(mat, c.RimStrengthProp, c.RimStrength);
        touched |= SetColorIfExists(mat, c.RimColorProp, c.RimColor);

        return touched;
    }

    static bool SetFloatIfExists(Material mat, string prop, float value)
    {
        if (string.IsNullOrEmpty(prop) || !mat.HasProperty(prop)) return false;
        mat.SetFloat(prop, value);
        return true;
    }

    static bool SetIntIfExists(Material mat, string prop, int value)
    {
        if (string.IsNullOrEmpty(prop) || !mat.HasProperty(prop)) return false;
        mat.SetInt(prop, value);
        return true;
    }

    static bool SetColorIfExists(Material mat, string prop, Color value)
    {
        if (string.IsNullOrEmpty(prop) || !mat.HasProperty(prop)) return false;
        mat.SetColor(prop, value);
        return true;
    }
}
