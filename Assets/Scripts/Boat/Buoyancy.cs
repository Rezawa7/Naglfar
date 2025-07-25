using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    private const float MAX_ANGLE = 30f; // Maximum allowed pitch/roll in degrees

    [Header("Float Points (drag from hierarchy!)")]
    public Transform floatPointCenter; // for Y
    public Transform floatPointFront;  // for pitch
    public Transform floatPointBack;   // for pitch
    public Transform floatPointLeft;   // for roll
    public Transform floatPointRight;  // for roll

    [Tooltip("Your CPUWaveProvider component; if left blank, auto-finds closest in scene.")]
    public CPUWaveProvider wave;

    [Header("Buoyancy Settings")]
    [Tooltip("Physical strength of buoyancy; try 30–100 for realistic ships")]
    public float buoyancyStrength = 40f; // << LOWER VALUE
    [Range(0f, 1f)] public float buoyancyDamp = 0.12f;
    public float extraGravity = 20f;

    [Header("Rotation Lerp (optional)")]
    public float rotationLerpSpeed = 2.5f; // How quickly ship aligns with water tilt

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 20f;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.8f;
    }

    void FixedUpdate()
    {
        // Auto-find wave provider if needed
        if (wave == null)
        {
            var providers = Object.FindObjectsByType<CPUWaveProvider>(FindObjectsSortMode.None);
            if (providers.Length == 0) return;
            float minDist = float.MaxValue;
            foreach (var p in providers)
            {
                float dist = Vector3.Distance(transform.position, p.transform.position);
                if (dist < minDist) { minDist = dist; wave = p; }
            }
        }

        // --- Center point: physical bounce ---
        if (floatPointCenter)
        {
            Vector3 pos = floatPointCenter.position;
            float waterY = wave.GetHeightAtPosition(pos.x, pos.z);
            float depth = waterY - pos.y;
            if (depth > 0f)
            {
                float lift = buoyancyStrength * depth;
                float vpt = Vector3.Dot(rb.GetPointVelocity(pos), Vector3.up);
                float damp = -vpt * buoyancyDamp;
                Vector3 force = Vector3.up * (lift + damp);
                rb.AddForceAtPosition(force, pos, ForceMode.Force);
            }
        }

        // --- Pitch (X) and Roll (Z) from water plane ---
        float pitchDeg = 0, rollDeg = 0;

        // Pitch: front/back
        if (floatPointFront && floatPointBack)
        {
            float frontWaterY = wave.GetHeightAtPosition(floatPointFront.position.x, floatPointFront.position.z);
            float backWaterY = wave.GetHeightAtPosition(floatPointBack.position.x, floatPointBack.position.z);
            float dist = Vector3.Distance(floatPointFront.position, floatPointBack.position);
            float delta = frontWaterY - backWaterY;
            pitchDeg = Mathf.Clamp(Mathf.Atan2(delta, dist) * Mathf.Rad2Deg, -MAX_ANGLE, MAX_ANGLE);
        }

        // Roll: left/right
        if (floatPointLeft && floatPointRight)
        {
            float leftWaterY = wave.GetHeightAtPosition(floatPointLeft.position.x, floatPointLeft.position.z);
            float rightWaterY = wave.GetHeightAtPosition(floatPointRight.position.x, floatPointRight.position.z);
            float dist = Vector3.Distance(floatPointLeft.position, floatPointRight.position);
            float delta = leftWaterY - rightWaterY;
            rollDeg = Mathf.Clamp(Mathf.Atan2(delta, dist) * Mathf.Rad2Deg, -MAX_ANGLE, MAX_ANGLE);
        }

        // --- Smoothly Lerp ship rotation towards water tilt ---
        Vector3 currentEuler = rb.rotation.eulerAngles;
        float yaw = currentEuler.y; // preserve heading

        // Convert euler to [-180, 180] for smooth lerp
        float currPitch = Mathf.DeltaAngle(0, currentEuler.x);
        float currRoll = Mathf.DeltaAngle(0, currentEuler.z);

        float newPitch = Mathf.LerpAngle(currPitch, pitchDeg, Time.fixedDeltaTime * rotationLerpSpeed);
        float newRoll = Mathf.LerpAngle(currRoll, -rollDeg, Time.fixedDeltaTime * rotationLerpSpeed);

        Quaternion targetRot = Quaternion.Euler(newPitch, yaw, newRoll);
        rb.MoveRotation(targetRot);

        // --- Extra gravity ---
        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
    }
}
