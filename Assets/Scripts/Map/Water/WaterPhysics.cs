using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class WaterPhysics : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float springStrength = 20f;
    public float damping       = 5f;
    public float spread        = 0.25f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;
    private float[] velocity;
    private int xSize, zSize;
    private bool initialized = false;

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        Initialize();
    }

    /// <summary>Called at runtime on first frame to kick the water.</summary>
    void Start()
    {
        Initialize();
        Splash(transform.position, springStrength);
    }

    #if UNITY_EDITOR
    /// <summary>In Edit Mode, whenever you tweak values, give a small preview splash.</summary>
    void OnValidate()
    {
        // ensure mesh has been cloned already
        Initialize();

        // only in Edit Mode
        if (!Application.isPlaying)
        {
            // capture data now so the lambda doesn't touch destroyed objects
            Vector3 splashPos   = transform.position;
            float   splashForce = springStrength * 0.5f;

            // schedule a delayed splash _after_ OnValidate finishes
            EditorApplication.delayCall += () =>
            {
                // this null-check uses Unity's overridden == operator
                if (this != null)
                {
                    Splash(splashPos, splashForce);
                }
            };
        }
    }
    #endif

    void Initialize()
    {
        if (initialized) return;
        initialized = true;

        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        // clone the mesh once
        mesh = Instantiate(mf.sharedMesh);
        mf.mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        baseVertices     = mesh.vertices;
        displacedVertices = new Vector3[baseVertices.Length];
        velocity         = new float[baseVertices.Length];
        baseVertices.CopyTo(displacedVertices, 0);

        // infer grid dims
        var xs = new System.Collections.Generic.HashSet<float>();
        foreach (var v in baseVertices)
            xs.Add(Mathf.Round(v.x * 100f) / 100f);
        xSize = xs.Count;
        zSize = baseVertices.Length / xSize;
    }

    void FixedUpdate()
    {
        if (Application.isPlaying)
            SimulateWaves(Time.fixedDeltaTime);
    }

    /// <summary>Called by our Editor ticker to animate in Edit Mode.</summary>
    public void EditorTick(float dt)
    {
        #if UNITY_EDITOR
        if (Application.isPlaying || EditorApplication.isPaused) return;
        #endif
        SimulateWaves(dt);
    }

    void SimulateWaves(float dt)
    {
        if (!initialized || displacedVertices == null) return;

        // 1) spring + damping
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float y     = displacedVertices[i].y;
            float accel = -springStrength * y - damping * velocity[i];
            velocity[i] += accel * dt;
        }

        // 2) spread
        var temp = (Vector3[])displacedVertices.Clone();
        for (int z = 0; z < zSize; z++)
        for (int x = 0; x < xSize; x++)
        {
            int i = x + z * xSize;
            float sumY = 0f; int count = 0;
            if (x > 0)         { sumY += temp[i - 1].y;      count++; }
            if (x < xSize - 1) { sumY += temp[i + 1].y;      count++; }
            if (z > 0)         { sumY += temp[i - xSize].y;  count++; }
            if (z < zSize - 1) { sumY += temp[i + xSize].y;  count++; }
            float avgY = sumY / count;
            velocity[i] += (avgY - temp[i].y) * spread;
        }

        // 3) integrate & write back
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            displacedVertices[i].y = displacedVertices[i].y + velocity[i] * dt;
            mesh.vertices[i]       = displacedVertices[i];
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    /// <summary>Create a splash at worldPos.</summary>
    public void Splash(Vector3 worldPos, float force)
    {
        Initialize();
        if (displacedVertices == null) return;

        Vector3 local = transform.InverseTransformPoint(worldPos);
        float bestDist = float.MaxValue; int bestIdx = 0;
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 v = displacedVertices[i];
            float d = (v.x - local.x)*(v.x - local.x)
                    + (v.z - local.z)*(v.z - local.z);
            if (d < bestDist) { bestDist = d; bestIdx = i; }
        }
        velocity[bestIdx] += force;
    }

    /// <summary>Get the water height at worldPos.</summary>
    public float GetHeightAt(Vector3 worldPos)
    {
        Initialize();
        if (displacedVertices == null) return transform.position.y;

        Vector3 local = transform.InverseTransformPoint(worldPos);
        float fx = ((local.x + (xSize - 1) / 2f) / (xSize - 1)) * (xSize - 1);
        float fz = ((local.z + (zSize - 1) / 2f) / (zSize - 1)) * (zSize - 1);
        fx = Mathf.Clamp(fx, 0, xSize - 1);
        fz = Mathf.Clamp(fz, 0, zSize - 1);

        int x0 = Mathf.FloorToInt(fx), z0 = Mathf.FloorToInt(fz);
        int x1 = Mathf.Min(x0 + 1, xSize - 1), z1 = Mathf.Min(z0 + 1, zSize - 1);
        float tx = fx - x0, tz = fz - z0;

        float h00 = displacedVertices[x0 + z0 * xSize].y;
        float h10 = displacedVertices[x1 + z0 * xSize].y;
        float h01 = displacedVertices[x0 + z1 * xSize].y;
        float h11 = displacedVertices[x1 + z1 * xSize].y;

        float h0 = Mathf.Lerp(h00, h10, tx);
        float h1 = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(h0, h1, tz) + transform.position.y;
    }
}
