using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class SimpleConditionalGerstnerWave : MonoBehaviour
{
    [Serializable]
    public struct Wave
    {
        [Tooltip("Direction of wave travel in XZ")]
        public Vector2 direction;
        [Tooltip("Vertical height of this wave component")]
        [Range(0f, 2f)] public float amplitude;
        [Tooltip("Distance between crests")]
        [Min(0.1f)] public float wavelength;
        [Tooltip("Sharpness of crest")]
        [Range(0f, 1f)] public float steepness;
        [Tooltip("Extra phase offset (radians)")]
        public float phaseOffset;
    }

    [Header("General")]
    [Tooltip("When checked, animates in Edit Mode")]
    public bool simulateInEditMode = false;

    [Header("Randomization")]
    [Tooltip("Generate random waves on Awake")]
    public bool randomizeOnStart = true;
    [Tooltip("How many random waves to create")]
    public int waveCount = 5;

    [Header("Global Speed")]
    [Tooltip("Uniform propagation speed for all wave components")]
    [Range(0f, 10f)] public float globalSpeed = 1f;

    [Header("Wave Components")]
    [Tooltip("List of individual Gerstner wave sources")]
    public List<Wave> waves = new List<Wave>();

    // Internal mesh data
    private Mesh      meshInstance;
    private Vector3[] baseVerts, dispVerts;

    void Awake()
    {
        if (randomizeOnStart)
            RandomizeWaves();

        // Clone mesh so we don't overwrite the asset
        var mf = GetComponent<MeshFilter>();
        meshInstance = Instantiate(mf.sharedMesh);
        mf.mesh      = meshInstance;

        baseVerts = meshInstance.vertices;
        dispVerts = new Vector3[baseVerts.Length];
    }

    [ContextMenu("Randomize Waves")]
    void RandomizeWaves()
    {
        waves.Clear();
        for (int i = 0; i < waveCount; i++)
        {
            waves.Add(new Wave
            {
                direction   = UnityEngine.Random.insideUnitCircle.normalized,
                amplitude   = UnityEngine.Random.Range(0.5f, 2.0f),
                wavelength  = UnityEngine.Random.Range(3f, 15f),
                steepness   = UnityEngine.Random.Range(0.1f, 0.8f),
                phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
            });
        }
    }

    void Update()
    {
        float t;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!simulateInEditMode) return;
            t = (float)EditorApplication.timeSinceStartup;
        }
        else t = Time.time;
#else
        t = Time.time;
#endif
        AnimateWave(t);
    }

    void AnimateWave(float time)
    {
        Array.Copy(baseVerts, dispVerts, baseVerts.Length);

        foreach (var w in waves)
        {
            float k         = 2f * Mathf.PI / w.wavelength;
            float Qi        = w.steepness * w.amplitude * k;
            Vector2 dirNorm = w.direction.normalized;
            float phaseTime = time * globalSpeed * k;

            for (int i = 0; i < baseVerts.Length; i++)
            {
                Vector3 v      = baseVerts[i];
                Vector3 worldV = transform.TransformPoint(v);

                float worldPhase = (worldV.x * dirNorm.x + worldV.z * dirNorm.y) * k;
                float phase      = worldPhase - phaseTime + w.phaseOffset;

                float c = Mathf.Cos(phase);
                float s = Mathf.Sin(phase);

                dispVerts[i].x += Qi * dirNorm.x * c;
                dispVerts[i].y += w.amplitude * s;
                dispVerts[i].z += Qi * dirNorm.y * c;
            }
        }

        meshInstance.vertices = dispVerts;
        meshInstance.RecalculateNormals();
    }

    /// <summary>
    /// Returns the combined height of all waves at world (x,z) right now.
    /// </summary>
    public float GetHeightAtPosition(float worldX, float worldZ)
    {
        float y = 0f;
        float t = Application.isPlaying ? Time.time : (float)EditorApplication.timeSinceStartup;

        foreach (var w in waves)
        {
            float k     = 2f * Mathf.PI / w.wavelength;
            Vector2 dir = w.direction.normalized;
            float phase = (worldX*dir.x + worldZ*dir.y) * k
                        - t * globalSpeed * k
                        + w.phaseOffset;
            y += Mathf.Sin(phase) * w.amplitude;
        }
        return y;
    }

    /// <summary>
    /// Approximates the combined normal by summing each wave's contribution.
    /// </summary>
    public Vector3 GetNormalAtPosition(float worldX, float worldZ)
    {
        Vector3 n = Vector3.zero;
        float t = Application.isPlaying ? Time.time : (float)EditorApplication.timeSinceStartup;

        foreach (var w in waves)
        {
            float k  = 2f * Mathf.PI / w.wavelength;
            float Qi = w.steepness * w.amplitude * k;
            Vector2 dirNorm = w.direction.normalized;
            float phase = (worldX*dirNorm.x + worldZ*dirNorm.y) * k
                        - t * globalSpeed * k
                        + w.phaseOffset;

            float s  = Mathf.Sin(phase);
            float nx = -Qi * dirNorm.x * s;
            float nz = -Qi * dirNorm.y * s;
            Vector3 wn = new Vector3(nx, 1f, nz).normalized;

            n += wn;
        }

        return n.normalized;
    }
}
