using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShaderGlobals : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Only materials using this shader will be edited. Leave null to edit any material that has matching properties.")]
    public Shader targetShader;

    [Header("Bend")]
    public float EarthRadius = -400f;
    public float SideBendStrength =0f; // baseline offset

    // Runtime bend algorithm settings (kept for convenience; world code can own the algorithm)
    [Tooltip("Maximum amplitude added/subtracted around the baseline (SideBendStrength)")]
    public float SideBendAmplitude =1f;
    [Tooltip("How fast the bend oscillates (cycles per second)")]
    public float SideBendFrequency =0.1f;
    [Tooltip("Amount of smooth Perlin noise mixed into the target (-1..1)")]
    public float SideBendNoiseStrength =0.2f;
    [Tooltip("Smooth time used by SmoothDamp when transitioning the shader value")]
    public float SmoothTime =0.5f;
    [Tooltip("If true will set a global shader float in addition to per-material values")]
    public bool UseGlobalShaderFloat = false;

    [Header("Fade")]
    public float FadeStart =40f;
    public float FadeEnd =150f;

    [Header("Toon/Lighting")]
    public Color RevealColor = new Color(1f,0.5f,0.2f,1f);
    public Color ShadowColor = Color.black;
    public int Steps =4;
    public float LightIntensity =3f;

    [Header("Rim")]
    public float RimPower =0f;
    public float RimStrength =0f;
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

    [Header("Material source")]
    [Tooltip("Assign materials directly to control them.")]
    public List<Material> manualMaterials = new List<Material>();

    // internal
    private readonly List<Material> _materials = new List<Material>();
    private float _currentSideBend =0f;
    private float _sideBendVelocity =0f;

    // pulse support
    private float _pulseOffset =0f;
    private Coroutine _pulseCoroutine;

    void Start()
    {
        _currentSideBend = SideBendStrength;
        // Populate controlled materials from explicit manualMaterials list only
        CollectMaterials();
        // set initial values
        ApplyAllShaderProperties();
    }

    // Allows other systems to change the baseline at runtime
    public void SetSideBendBaseline(float baseline)
    {
        SideBendStrength = baseline;
    }

    // Minimal public API: directly set the current side-bend value (applies immediately)
    public void SetSideBendValue(float value)
    {
        _currentSideBend = value;
        // ensure materials are collected
        if (_materials.Count ==0) CollectMaterials();
        // apply to currently collected materials
        if (_materials.Count >0)
            ApplySideBendToMaterials(_currentSideBend + _pulseOffset);
    }

    // Trigger a temporary pulse added to the current value. Pulse coroutine applies updates directly.
    public void PulseSideBend(float amplitude, float duration)
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseCoroutine(amplitude, duration));
    }

    private IEnumerator PulseCoroutine(float amplitude, float duration)
    {
        float elapsed =0f;
        while (elapsed < duration)
        {
            _pulseOffset = Mathf.Lerp(amplitude,0f, elapsed / Mathf.Max(duration,0.0001f));
            // apply current value + pulse
            if (_materials.Count >0)
                ApplySideBendToMaterials(_currentSideBend + _pulseOffset);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _pulseOffset =0f;
        // ensure final value is applied
        if (_materials.Count >0)
            ApplySideBendToMaterials(_currentSideBend);
        _pulseCoroutine = null;
    }

    // Collect materials from the explicitly provided manualMaterials only
    private void CollectMaterials()
    {
        _materials.Clear();
        var set = new HashSet<Material>();

        if (manualMaterials != null && manualMaterials.Count >0)
        {
            foreach (var m in manualMaterials)
            {
                if (m == null) continue;
                if (set.Contains(m)) continue;
                if (targetShader != null)
                {
                    if (m.shader == targetShader && m.HasProperty(SideBendStrengthProp))
                    {
                        set.Add(m);
                        _materials.Add(m);
                    }
                }
                else
                {
                    if (m.HasProperty(SideBendStrengthProp))
                    {
                        set.Add(m);
                        _materials.Add(m);
                    }
                }
            }
        }
    }

    private void ApplySideBendToMaterials(float value)
    {
        if (UseGlobalShaderFloat)
        {
            Shader.SetGlobalFloat(SideBendStrengthProp, value);
        }

        for (int i =0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m == null) continue;
            if (m.HasProperty(SideBendStrengthProp))
            {
                m.SetFloat(SideBendStrengthProp, value);
            }
        }
    }

    // Also set other shader properties initially (optional)
    private void ApplyAllShaderProperties()
    {
        // Earth radius
        if (!string.IsNullOrEmpty(EarthRadiusProp))
            Shader.SetGlobalFloat(EarthRadiusProp, EarthRadius);

        // Fade
        if (!string.IsNullOrEmpty(FadeStartProp))
            Shader.SetGlobalFloat(FadeStartProp, FadeStart);
        if (!string.IsNullOrEmpty(FadeEndProp))
            Shader.SetGlobalFloat(FadeEndProp, FadeEnd);

        // Colors and other properties could be applied per-material if needed
        // For convenience apply to all collected materials
        for (int i =0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m == null) continue;

            if (m.HasProperty(RevealColorProp)) m.SetColor(RevealColorProp, RevealColor);
            if (m.HasProperty(ShadowColorProp)) m.SetColor(ShadowColorProp, ShadowColor);
            if (m.HasProperty(StepsProp)) m.SetInt(StepsProp, Steps);
            if (m.HasProperty(LightIntensityProp)) m.SetFloat(LightIntensityProp, LightIntensity);
            if (m.HasProperty(RimPowerProp)) m.SetFloat(RimPowerProp, RimPower);
            if (m.HasProperty(RimStrengthProp)) m.SetFloat(RimStrengthProp, RimStrength);
            if (m.HasProperty(RimColorProp)) m.SetColor(RimColorProp, RimColor);
            if (m.HasProperty(SideBendStrengthProp)) m.SetFloat(SideBendStrengthProp, _currentSideBend + _pulseOffset);
        }
    }

    // Keep values updated in editor when changed
    private void OnValidate()
    {
        _currentSideBend = SideBendStrength;
        if (Application.isPlaying)
        {
            CollectMaterials();
            ApplyAllShaderProperties();
        }
    }
}
