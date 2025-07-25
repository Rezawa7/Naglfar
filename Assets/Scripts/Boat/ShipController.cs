using UnityEngine;
using UnityEngine.InputSystem;  // ← new Input System namespace

public class ShipController : MonoBehaviour
{
    [Header("Speed")]
    public float maxSpeed = 20f;
    public float acceleration = 10f;
    public float deceleration = 15f;

    [Header("Steering")]
    public float turnSpeed = 60f;
    [Range(0,1)] public float driftFactor = 0.3f;

    [Header("Tilt/Roll")]
    public float maxRollAngle = 15f;
    public float rollSpeed = 3f;

    [Header("Cameras")]
    public Camera mainCam;
    public Camera cannonLeftCam;
    public Camera cannonRightCam;

    // runtime state
    float currentSpeed = 0f;
    float yawAngle     = 0f;
    float rollAngle    = 0f;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null)
            return; // no keyboard connected

        // ─── INPUT ───────────────────────────────────────
        float inpF = 0f;
        if (kb.wKey.isPressed)   inpF = +1f;
        if (kb.sKey.isPressed) inpF = -1f;

        float inpT = 0f;
        if (kb.dKey.isPressed) inpT = +1f;
        if (kb.aKey.isPressed)  inpT = -1f;

        // ─── SPEED CONTROL ────────────────────────────────
        if (inpF >  0f)
            currentSpeed += acceleration * Time.deltaTime;
        else if (inpF < 0f)
            currentSpeed -= deceleration * Time.deltaTime;
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * 0.5f * Time.deltaTime);

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

        // ─── YAW (HEADING) ────────────────────────────────
        yawAngle += inpT * turnSpeed * Time.deltaTime;

        // ─── ROLL (BANK) ──────────────────────────────────
        float targetRoll = -inpT * maxRollAngle;
        rollAngle = Mathf.Lerp(rollAngle, targetRoll, Time.deltaTime * rollSpeed);

        // ─── APPLY ROTATION ───────────────────────────────
        Quaternion yawQ  = Quaternion.Euler(0f, yawAngle, 0f);
        Quaternion rollQ = Quaternion.Euler(0f, 0f, rollAngle);
        transform.rotation = yawQ * rollQ;

        // ─── MOVE WITH DRIFT ──────────────────────────────
        Vector3 forwardMove = transform.forward * currentSpeed;
        Vector3 driftMove   = transform.right   * currentSpeed * inpT * driftFactor;
        transform.position += (forwardMove + driftMove) * Time.deltaTime;

        // Camera switch
        if (kb.qKey.isPressed)
            SetCamera(cannonLeftCam);
        else if (kb.eKey.isPressed)
            SetCamera(cannonRightCam);
        else
            SetCamera(mainCam);

    }

    private void SetCamera(Camera active)
    {
        if (mainCam) mainCam.enabled = (active == mainCam);
        if (cannonLeftCam) cannonLeftCam.enabled = (active == cannonLeftCam);
        if (cannonRightCam) cannonRightCam.enabled = (active == cannonRightCam);
    }
}
