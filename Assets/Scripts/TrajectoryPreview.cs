using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreview : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;
    public Slider angleSlider;          // optional
    public Slider strengthSlider;       // optional
    public CannonController cannon;     // optional (will try to auto-find)
    public GameObject impactMarker;     // optional

    [Header("Settings")]
    [Min(3)] public int resolution = 30;
    [Tooltip("Layer used for impact detection (e.g., Ground).")]
    public string groundLayerName = "Ground";

    private LineRenderer lineRenderer;
    private float gravity;
    private GameObject currentImpactMarker;
    private int groundMask;

    // log-once helpers
    private bool warnedMissingFirePoint;
    private bool warnedMissingCannonAndSliders;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        gravity = Mathf.Abs(Physics.gravity.y);

        // Cache layer mask (0 if layer doesn't exist)
        groundMask = LayerMask.GetMask(groundLayerName);

        // Try to auto-find CannonController if not assigned
        if (cannon == null)
        {
            cannon = GetComponent<CannonController>();
            if (cannon == null) cannon = GetComponentInParent<CannonController>();
        }
    }

    void OnValidate()
    {
        if (resolution < 3) resolution = 3;
    }

    void Update()
    {
        // Critical: need a fire point
        if (firePoint == null)
        {
            if (!warnedMissingFirePoint)
            {
                Debug.LogWarning("[TrajectoryPreview] FirePoint is not assigned; trajectory cannot be drawn.", this);
                warnedMissingFirePoint = true;
            }
            return;
        }

        // Determine angle & strength inputs
        // Priority: UI sliders (if assigned) → cannon fields (if present) → safe defaults
        float angle = 45f;
        float strength = 20f;

        if (angleSlider != null) angle = angleSlider.value;
        else if (cannon != null) angle = cannon.angle;

        if (strengthSlider != null) strength = strengthSlider.value;
        else if (cannon != null) strength = cannon.strength;

        if (cannon != null)
        {
            cannon.angle = angle;
            cannon.strength = strength;
        }
        else if (angleSlider == null || strengthSlider == null)
        {
            // No cannon and at least one slider missing → warn once (we still draw with defaults)
            if (!warnedMissingCannonAndSliders)
            {
                Debug.LogWarning("[TrajectoryPreview] CannonController and one/both sliders are not assigned. Using defaults (angle 45°, strength 20).", this);
                warnedMissingCannonAndSliders = true;
            }
        }

        DrawTrajectory(angle, strength);
    }

    void DrawTrajectory(float angleDeg, float strength)
    {
        if (resolution < 3) resolution = 3;

        Vector3[] points = new Vector3[resolution];

        // Rotate around local right axis so you can aim up/down from the muzzle
        Quaternion pitch = Quaternion.AngleAxis(-angleDeg, firePoint.right);
        Vector3 velocity = pitch * firePoint.forward * Mathf.Max(0f, strength);

        Vector3 prevPoint = firePoint.position;

        bool hitFound = false;
        Vector3 hitPoint = Vector3.zero;

        // Time step: distribute samples over a reasonable horizon (t grows linearly with i)
        // You can tweak 0.1f to change arc length displayed
        const float dt = 0.1f;

        for (int i = 0; i < resolution; i++)
        {
            float t = i * dt;

            // Basic ballistic equation under constant gravity
            Vector3 point = firePoint.position + velocity * t;
            point.y -= 0.5f * gravity * t * t;

            points[i] = point;

            // Segment raycast from prevPoint → this point to detect the first hit
            if (!hitFound && groundMask != 0)
            {
                Vector3 seg = point - prevPoint;
                float dist = seg.magnitude;

                if (dist > 0.0001f && Physics.Raycast(prevPoint, seg.normalized, out RaycastHit hit, dist, groundMask))
                {
                    hitFound = true;
                    hitPoint = hit.point;

                    // Trim the line so the last visible point is the hit (optional)
                    points[i] = hitPoint;
                    // If you want to stop drawing beyond the hit:
                    // System.Array.Resize(ref points, i + 1);
                    // lineRenderer.positionCount = points.Length; (and return after)
                }
            }

            prevPoint = point;
        }

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);

        // Impact marker (optional)
        if (hitFound)
        {
            if (impactMarker != null)
            {
                if (currentImpactMarker == null)
                    currentImpactMarker = Instantiate(impactMarker);

                currentImpactMarker.SetActive(true);
                currentImpactMarker.transform.position = hitPoint + Vector3.up * 0.05f;
            }
        }
        else if (currentImpactMarker != null)
        {
            currentImpactMarker.SetActive(false);
        }
    }
}
