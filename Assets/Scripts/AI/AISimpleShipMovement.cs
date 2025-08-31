using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI ship movement with planned, smooth steering segments.
/// - Only moves on XZ (no vertical drift from movement).
/// - Rotation constrained to YAW only (no pitch/roll).
/// - Physics-friendly: uses Rigidbody + constraints (Freeze Rot X/Z).
/// Keep your buoyancy script as-is; it can still control Y height.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AIPlannedShipMovementRB : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("Randomized at Start() between min/max.")]
    public float minSpeed = 4f;
    public float maxSpeed = 10f;

    [Header("Turning")]
    [Tooltip("Base yaw speed in deg/sec when steer = ±1.")]
    public float turnSpeed = 50f;

    [Tooltip("How long each planned steering segment lasts (seconds).")]
    public float segmentDuration = 2.25f;

    [Tooltip("How quickly to blend toward the active segment's steering (seconds).")]
    public float steerSmoothing = 0.25f;

    [Header("Planning")]
    [Tooltip("How many upcoming segments to keep planned (>= 2).")]
    [Min(2)] public int plannedSegments = 3;

    [Tooltip("Max change between adjacent segment steer values (0..1). Example: 0.35 → next steer differs by at most ±0.35.")]
    [Range(0.05f, 1f)] public float maxSteerChange = 0.35f;

    [Tooltip("Absolute cap for any segment's steering magnitude (0..1). Lower = gentler arcs.")]
    [Range(0.2f, 1f)] public float steerMagnitudeCap = 0.75f;

    [Header("Debug")]
    public bool drawGizmos = true;

    // --- internals ---
    private Rigidbody rb;
    private float currentSpeed;

    // steering is in [-1, 1]: -1 left, 0 straight, 1 right
    private float targetSteer;    // current segment target
    private float blendedSteer;   // smoothed toward target
    private float steerVel;       // SmoothDamp velocity helper

    private float timeInSegment;
    private readonly Queue<float> steerPlan = new Queue<float>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure physics can't tilt us; we only yaw:
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // smoother interpolation when using MovePosition/MoveRotation:
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnValidate()
    {
        if (plannedSegments < 2) plannedSegments = 2;
        steerSmoothing = Mathf.Max(0.01f, steerSmoothing);
        segmentDuration = Mathf.Max(0.05f, segmentDuration);
        minSpeed = Mathf.Max(0f, minSpeed);
        maxSpeed = Mathf.Max(minSpeed, maxSpeed);
    }

    void Start()
    {
        // pick forward speed
        currentSpeed = Random.Range(minSpeed, maxSpeed);

        // seed a steering plan
        float first = Random.Range(-steerMagnitudeCap, steerMagnitudeCap);
        steerPlan.Clear();
        steerPlan.Enqueue(first);
        while (steerPlan.Count < plannedSegments)
            steerPlan.Enqueue(NextSteer(LastInPlan(steerPlan)));

        targetSteer = steerPlan.Dequeue();
        blendedSteer = targetSteer; // start aligned
        timeInSegment = 0f;

        // also flatten any initial rotation to yaw only (in case prefab had tilt)
        Vector3 e = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, e.y, 0f);
    }

    void FixedUpdate()
    {
        // --- PLANAR MOVEMENT (XZ only) ---
        // Use current facing, but projected to the XZ plane
        Vector3 forwardPlanar = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // translate on XZ, preserve current Y (your buoyancy controls Y)
        Vector3 pos = rb.position;
        Vector3 delta = forwardPlanar * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(new Vector3(pos.x + delta.x, pos.y, pos.z + delta.z));

        // --- SMOOTH YAW ROTATION ONLY ---
        blendedSteer = Mathf.SmoothDamp(blendedSteer, targetSteer, ref steerVel, steerSmoothing);
        float yawDelta = blendedSteer * turnSpeed * Time.fixedDeltaTime;

        // apply yaw around world up, then force-flat (no pitch/roll)
        Quaternion yawRot = Quaternion.AngleAxis(yawDelta, Vector3.up);
        Quaternion nextRot = yawRot * rb.rotation;
        Vector3 e = nextRot.eulerAngles;
        nextRot = Quaternion.Euler(0f, e.y, 0f);
        rb.MoveRotation(nextRot);

        // --- SEGMENT TIMER / PLANNING ---
        timeInSegment += Time.fixedDeltaTime;
        if (timeInSegment >= segmentDuration)
        {
            timeInSegment = 0f;

            // consume next
            targetSteer = steerPlan.Dequeue();

            // keep the plan filled
            while (steerPlan.Count < plannedSegments - 1)
                steerPlan.Enqueue(NextSteer(LastInPlanOr(targetSteer)));
        }
    }

    /// <summary>
    /// Generate the next steering value close to the previous one (bounded random walk).
    /// </summary>
    private float NextSteer(float previous)
    {
        // small random step
        float step = Random.Range(-maxSteerChange, maxSteerChange);
        float candidate = Mathf.Clamp(previous + step, -steerMagnitudeCap, steerMagnitudeCap);

        // small relaxation toward straight so it doesn't orbit forever
        // (scale with segmentDuration so it’s time-consistent)
        float relaxPerSecond = 0.1f; // tweak if you want it to straighten faster/slower
        float relax = Mathf.Sign(-candidate) * relaxPerSecond * segmentDuration * 0.02f;
        candidate = Mathf.Clamp(candidate + relax, -steerMagnitudeCap, steerMagnitudeCap);

        return candidate;
    }

    private static float LastInPlan(Queue<float> q)
    {
        float last = 0f;
        foreach (var s in q) last = s;
        return last;
    }

    private float LastInPlanOr(float fallback)
    {
        if (steerPlan.Count == 0) return fallback;
        return LastInPlan(steerPlan);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * 3f);

        // show intended yaw direction based on blendedSteer
        Vector3 yawDir = Quaternion.Euler(0f, 25f * Mathf.Clamp(blendedSteer, -1f, 1f), 0f) * Vector3.forward;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + yawDir * 2.4f);
    }
#endif
}
