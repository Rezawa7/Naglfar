using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreview : MonoBehaviour
{
    public Transform firePoint;
    public Slider angleSlider;
    public Slider strengthSlider;
    public CannonController cannon;
    public GameObject impactMarker;
    public int resolution = 30;

    private LineRenderer lineRenderer;
    private float gravity;
    private GameObject currentImpactMarker;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        gravity = Mathf.Abs(Physics.gravity.y);
    }

    void Update()
    {
        cannon.angle = angleSlider.value;
        cannon.strength = strengthSlider.value;
        DrawTrajectory();
    }

    void DrawTrajectory()
    {
        Vector3[] points = new Vector3[resolution];
        Vector3 velocity = Quaternion.AngleAxis(-(cannon.angle), firePoint.right) * firePoint.forward * cannon.strength;
        Vector3 prevPoint = firePoint.position;

        bool hitFound = false;
        Vector3 hitPoint = Vector3.zero;

        int groundMask = LayerMask.GetMask("Ground");

        for (int i = 0; i < resolution; i++)
        {
            float t = i * 0.1f;
            Vector3 point = firePoint.position + velocity * t;
            point.y -= 0.5f * gravity * t * t;
            points[i] = point;

            if (!hitFound)
            {
                Vector3 dir = point - prevPoint;
                float dist = dir.magnitude;

                if (Physics.Raycast(prevPoint, dir.normalized, out RaycastHit hit, dist, groundMask))
                {
                    hitFound = true;
                    hitPoint = hit.point;
                }
            }

            prevPoint = point;
        }

        lineRenderer.positionCount = resolution;
        lineRenderer.SetPositions(points);

        if (hitFound)
        {
            if (currentImpactMarker == null)
            {
                currentImpactMarker = Instantiate(impactMarker);
            }
            currentImpactMarker.SetActive(true);
            currentImpactMarker.transform.position = hitPoint + Vector3.up * 0.05f;
        }
        else if (currentImpactMarker != null)
        {
            currentImpactMarker.SetActive(false);
        }
    }
}
