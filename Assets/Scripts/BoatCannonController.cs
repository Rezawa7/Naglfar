using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BoatCannonController : MonoBehaviour
{
    public CannonController[] leftCannons;
    public CannonController[] rightCannons;
    public Slider[] leftCooldownBars;
    public Slider[] rightCooldownBars;

    public float fireCooldown = 5f;
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    private float[] leftCooldowns;
    private float[] rightCooldowns;

    void Start()
    {
        leftCooldowns = new float[leftCannons.Length];
        rightCooldowns = new float[rightCannons.Length];

        for (int i = 0; i < leftCooldowns.Length; i++) leftCooldowns[i] = 0f;
        for (int i = 0; i < rightCooldowns.Length; i++) rightCooldowns[i] = 0f;
    }

    void Update()
    {
        // Move the boat with WASD only
        float moveInput = 0f;
        if (Input.GetKey(KeyCode.W)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.S)) moveInput = -1f;

        float turnInput = 0f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;
        else if (Input.GetKey(KeyCode.A)) turnInput = -1f;

        transform.Translate(Vector3.forward * moveInput * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, turnInput * rotationSpeed * Time.deltaTime);

        // Update cooldowns and handle cannon shooting inputs
        UpdateCooldowns(leftCooldowns, leftCooldownBars);
        UpdateCooldowns(rightCooldowns, rightCooldownBars);

        // Left cannons Numpad 7,8,9
        HandleInput(KeyCode.Keypad5, leftCannons, leftCooldowns);

        // Right cannons Numpad 1,2,3
        HandleInput(KeyCode.Keypad0, rightCannons, rightCooldowns);

        // Shoot all cannons with Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootAllCannons();
        }
    }

    void ShootAllCannons()
    {
        bool anyCannonFired = false;

        for (int i = 0; i < leftCannons.Length; i++)
        {
            if (leftCooldowns[i] <= 0f && leftCannons[i].isMyTurn)
            {
                leftCannons[i].Fire();
                leftCooldowns[i] = fireCooldown;
                anyCannonFired = true;
            }
        }

        for (int i = 0; i < rightCannons.Length; i++)
        {
            if (rightCooldowns[i] <= 0f && rightCannons[i].isMyTurn)
            {
                rightCannons[i].Fire();
                rightCooldowns[i] = fireCooldown;
                anyCannonFired = true;
            }
        }

        if (!anyCannonFired)
        {
            Debug.Log("All cannons are cooling down, cannot fire all.");
        }
    }

    public float CurrentAngle
    {
        get => leftCannons.Length > 0 ? leftCannons[0].angle : 0f; // assumes all cannons use the same angle
    }

    public float CurrentStrength
    {
        get => leftCannons.Length > 0 ? leftCannons[0].strength : 0f;
    }

    public void SetAngle(float value)
    {
        foreach (var cannon in leftCannons.Concat(rightCannons))
            cannon.angle = value;
    }

    public void SetStrength(float value)
    {
        foreach (var cannon in leftCannons.Concat(rightCannons))
            cannon.strength = value;
    }

    public void SetShotType(int type)
    {
        foreach (var cannon in leftCannons.Concat(rightCannons))
            cannon.SetShotType(type);
    }

    void UpdateCooldowns(float[] cooldowns, Slider[] bars)
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0f)
                cooldowns[i] -= Time.deltaTime;

            if (bars != null && i < bars.Length && bars[i] != null)
            {
                bars[i].value = Mathf.Clamp01(1f - (cooldowns[i] / fireCooldown));
            }
        }
    }

    void HandleInput(KeyCode startKey, CannonController[] cannons, float[] cooldowns)
    {
        for (int i = 0; i < cannons.Length; i++)
        {
            if (Input.GetKeyDown(startKey + i))
            {
                if (cooldowns[i] <= 0f && cannons[i].isMyTurn)
                {
                    cannons[i].Fire();
                    cooldowns[i] = fireCooldown;
                }
                else
                {
                    Debug.Log($"Cannon {i + 1} on {(startKey == KeyCode.Alpha1 ? "Left" : "Right")} is cooling down.");
                }
            }
        }
    }
}
