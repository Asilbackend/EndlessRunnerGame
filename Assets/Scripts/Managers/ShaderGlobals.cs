using UnityEngine;
using System.Collections.Generic;

public class ShaderGlobals : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Only materials using this shader will be edited. Leave null to edit any material that has matching properties.")]
    public Shader targetShader;

    [Header("Bend")]
    public float EarthRadius = -400f;
    public float SideBendStrength = 0f;

    [Header("Fade")]
    public float FadeStart = 40f;
    public float FadeEnd = 150f;

    [Header("Toon/Lighting")]
    public Color RevealColor = new Color(1f, 0.5f, 0.2f, 1f);
    public Color ShadowColor = Color.black;
    public int Steps = 4;
    public float LightIntensity = 3f;

    [Header("Rim")]
    public float RimPower = 0f;
    public float RimStrength = 0f;
    public Color RimColor = Color.white;

    // IMPORTANT: these must match the *actual* shader property names
    public string EarthRadiusProp = "_EarthRadius";
    public string SideBendStrengthProp = "_SideBendStrength";
    public string FadeStartProp = "_FadeStart";
    public string FadeEndProp = "_FadeEnd";
    public string RevealColorProp = "_RevealColor";
    public string ShadowColorProp = "_ShadowColor";
    public string StepsProp = "_Steps";
    public string LightIntensityProp = "_LightIntensity";
    public string RimPowerProp = "_RimPower";
    public string RimStrengthProp = "_RimStrength";
    public string RimColorProp = "_RimColor";
}
