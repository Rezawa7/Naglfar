using UnityEngine;
using System.Collections;

public class AISingleShipSpawner : MonoBehaviour
{
    [Header("AI Ship Prefab (drag your stylship prefab here)")]
    public GameObject aiShipPrefab;

    [Header("Spawn Area (world coordinates)")]
    public Vector2 spawnXRange = new Vector2(-100f, 100f);
    public Vector2 spawnZRange = new Vector2(-100f, 100f);
    public float spawnY = 0f; // Y-level for ship spawn (can be surface or slightly above water)

    [Header("Respawn")]
    public float respawnDelay = 5f;

    private GameObject currentAIShip;

    void Start()
    {
        SpawnAIShip();
    }

    void Update()
    {
        // Check if the AI ship was destroyed (missing from scene)
        if (currentAIShip == null)
        {
            StartCoroutine(RespawnAIShip());
        }
    }

    IEnumerator RespawnAIShip()
    {
        // Wait before respawning
        yield return new WaitForSeconds(respawnDelay);

        // Only spawn if there is still none (avoid double-spawn)
        if (currentAIShip == null)
            SpawnAIShip();
    }

    void SpawnAIShip()
    {
        Vector3 pos = new Vector3(
            Random.Range(spawnXRange.x, spawnXRange.y),
            spawnY,
            Random.Range(spawnZRange.x, spawnZRange.y)
        );
        currentAIShip = Instantiate(aiShipPrefab, pos, Quaternion.identity);

        // OPTIONAL: Tag or layer as "AI" if needed
        currentAIShip.name = "AI_Ship";
    }
}
