using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;              // The ship
    public Vector3 offset = new Vector3(0f, 8f, -15f);
    public float followSpeed = 5f;
    public float rotateSpeed = 5f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (!target) return;

        // Take ONLY yaw (ignore pitch/roll)
        float yaw = target.eulerAngles.y;
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);

        // Position behind ship, but stable
        Vector3 desiredPosition = target.position + yawRotation * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / followSpeed);

        // Look at the ship, but keep horizon level
        Vector3 lookDir = target.position - transform.position;
        lookDir.y = 0f; // flatten so camera doesn't tilt
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }
}
