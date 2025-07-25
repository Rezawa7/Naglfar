using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CPUWaveProvider : MonoBehaviour
{
    [Serializable]
    public struct Wave
    {
        public Vector2 direction;
        [Range(2f, 10f)] public float amplitude;
        [Range(25f, 1000f)] public float wavelength;
        [Range(0f, 1f)] public float steepness;
        public float phaseOffset;
    }

    [Header("Global Speed")]
    public float globalSpeed = 5f;

    [Header("Waves (CUSTOM SEA)")]
    public List<Wave> waves = new List<Wave>
    {
        new Wave { direction = new Vector2(1f, 0.25f).normalized, amplitude = 2f, wavelength = 950f, steepness = 0.12f, phaseOffset = 0f },
        new Wave { direction = new Vector2(-0.6f, 1f).normalized, amplitude = 3f, wavelength = 1200f, steepness = 0.09f, phaseOffset = 1.1f },
        new Wave { direction = new Vector2(0.7f, -1f).normalized, amplitude = 2.5f, wavelength = 720f, steepness = 0.16f, phaseOffset = 2.6f },
        new Wave { direction = new Vector2(0.15f, 1f).normalized, amplitude = 1.5f, wavelength = 58f, steepness = 0.5f, phaseOffset = 0.45f }
    };

    public Material targetMaterial; // Assign your Ocean material here in Inspector

#if UNITY_EDITOR
    float TimeValue => Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
    float TimeValue => Time.time;
#endif

    public float GetHeightAtPosition(float x, float z)
    {
        float y = 0f, t = TimeValue;
        for (int i = 0; i < waves.Count; i++)
        {
            var w = waves[i];
            float k = 2f * Mathf.PI / w.wavelength;
            Vector2 d = w.direction.normalized;
            float phase = (x * d.x + z * d.y) * k - t * globalSpeed * k + w.phaseOffset;
            float s = Mathf.Sin(phase);
            float contrib = s * w.amplitude;
            Debug.Log($"[CPUWaveProvider] Wave[{i}] at ({x:F2},{z:F2}): phase={phase:F2}, sin(phase)={s:F2}, amplitude={w.amplitude:F2}, contrib={contrib:F2}");
            y += contrib;
        }
        Debug.Log($"[CPUWaveProvider] Total height at ({x:F2},{z:F2}) = {y:F2}");
        return y;
    }

    public Vector3 GetNormalAtPosition(float x, float z)
    {
        Vector3 n = Vector3.zero;
        float t = TimeValue;
        for (int i = 0; i < waves.Count; i++)
        {
            var w = waves[i];
            float k = 2f * Mathf.PI / w.wavelength;
            float Qi = w.steepness * w.amplitude * k;
            Vector2 d = w.direction.normalized;
            float phase = (x * d.x + z * d.y) * k - t * globalSpeed * k + w.phaseOffset;
            float s = Mathf.Sin(phase);
            n.x += -Qi * d.x * s;
            n.y += 1f;
            n.z += -Qi * d.y * s;
            Debug.Log($"[CPUWaveProvider] Norm[{i}] at ({x:F2},{z:F2}): k={k:F2}, Qi={Qi:F2}, d=({d.x:F2}, {d.y:F2}), phase={phase:F2}, s={s:F2}");
        }
        Vector3 norm = n.normalized;
        Debug.Log($"[CPUWaveProvider] Normal at ({x:F2},{z:F2}): {n} => normalized {norm}");
        return norm;
    }

    void Update() { SyncToMaterial(); }

    void SyncToMaterial()
    {
        if (targetMaterial == null || waves == null || waves.Count == 0) return;
        int n = Mathf.Min(4, waves.Count);

        targetMaterial.SetFloat("_GlobalSpeed", globalSpeed);

        for (int i = 0; i < n; i++)
        {
            var w = waves[i];
            targetMaterial.SetVector($"_Dir{i+1}", new Vector4(w.direction.x, w.direction.y, 0, 0));
            targetMaterial.SetFloat($"_Amp{i+1}", w.amplitude);
            targetMaterial.SetFloat($"_Wave{i+1}", w.wavelength);
            targetMaterial.SetFloat($"_Steep{i+1}", w.steepness);
            targetMaterial.SetFloat($"_Phase{i+1}", w.phaseOffset);
        }
        for (int i = n; i < 4; i++)
        {
            targetMaterial.SetVector($"_Dir{i+1}", Vector4.zero);
            targetMaterial.SetFloat($"_Amp{i+1}", 0);
            targetMaterial.SetFloat($"_Wave{i+1}", 1);
            targetMaterial.SetFloat($"_Steep{i+1}", 0);
            targetMaterial.SetFloat($"_Phase{i+1}", 0);
        }
        Debug.Log("[CPUWaveProvider] Synced values to material: " + targetMaterial.name);
    }
}
