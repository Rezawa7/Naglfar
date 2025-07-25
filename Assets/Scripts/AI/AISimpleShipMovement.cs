using UnityEngine;

public class AISimpleShipMovement : MonoBehaviour
{
    [Header("Speed")]
    public float minSpeed = 4f;
    public float maxSpeed = 10f;

    [Header("Turning")]
    public float turnSpeed = 20f;
    public float directionChangeInterval = 3f;

    private float currentSpeed;
    private float targetTurnDirection = 0f;
    private float timeSinceDirectionChange = 0f;

    void Start()
    {
        PickNewDirection();
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        // Move forward
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // Turn toward target direction (left/right)
        transform.Rotate(Vector3.up, targetTurnDirection * turnSpeed * Time.deltaTime);

        // Change direction every few seconds for randomness
        timeSinceDirectionChange += Time.deltaTime;
        if (timeSinceDirectionChange > directionChangeInterval)
        {
            PickNewDirection();
            timeSinceDirectionChange = 0f;
        }
    }

    void PickNewDirection()
    {
        // -1 for left, 0 for straight, 1 for right (randomly, with some straight bias)
        float[] choices = new float[] { -1f, 0f, 1f, 0f, 0f };
        targetTurnDirection = choices[Random.Range(0, choices.Length)];
    }
}