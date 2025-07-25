using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class OceanManager : MonoBehaviour
{
    [Header("Tile Setup")]
    public GameObject tilePrefab;
    public Transform ship;
    public Camera shipCamera;

    [Header("Grid Dimensions")]
    public int gridWidth = 6;
    public int gridHeight = 6;
    public float tileWorldSize = 50f;

    [Header("Culling Radius")]
    public float cullDistance = 200f;

    private List<GameObject> tiles = new List<GameObject>();

    void OnEnable()
    {
        if (Application.isPlaying) Initialize();
    }

    void OnDisable()
    {
        ClearTiles();
    }

    void Initialize()
    {
        if (tilePrefab == null || ship == null) return;
        if (shipCamera == null) shipCamera = Camera.main;
        if (shipCamera != null)
            shipCamera.transform.SetParent(ship, false);

        ClearTiles();
        // Pool the required number of tiles once
        for (int i = 0; i < gridWidth * gridHeight; i++)
        {
            var go = Instantiate(tilePrefab, transform);
            go.name = $"Tile_{i}";
            tiles.Add(go);
        }
    }

    void Update()
    {
        if (!Application.isPlaying || tilePrefab == null || ship == null) return;
        if (tiles.Count != gridWidth * gridHeight) Initialize();

        Vector3 shipPos = ship.position;
        int centerX = Mathf.RoundToInt(shipPos.x / tileWorldSize);
        int centerZ = Mathf.RoundToInt(shipPos.z / tileWorldSize);

        int halfW = gridWidth / 2;
        int halfH = gridHeight / 2;

        int tileIndex = 0;
        for (int dz = -halfH; dz <= halfH; dz++)
        {
            for (int dx = -halfW; dx <= halfW; dx++)
            {
                int gridX = centerX + dx;
                int gridZ = centerZ + dz;
                var wp = new Vector3(gridX * tileWorldSize, 0, gridZ * tileWorldSize);

                // Always assign from pool; never exceed tile count
                if (tileIndex < tiles.Count)
                {
                    var tile = tiles[tileIndex];
                    tile.transform.position = wp;

                    // Optional: cull if too far, otherwise keep all visible
                    if ((wp - shipPos).sqrMagnitude <= cullDistance * cullDistance)
                        tile.SetActive(true);
                    else
                        tile.SetActive(false);

                    tileIndex++;
                }
            }
        }
    }

    void ClearTiles()
    {
        foreach (var t in tiles)
            if (t) DestroyImmediate(t);
        tiles.Clear();
    }

    // --------- Grid GIZMOS (editor) ---------
    void OnDrawGizmosSelected()
    {
        if (tilePrefab == null || ship == null) return;

        float s = tileWorldSize;
        Vector3 sp = ship.position;
        int centerX = Mathf.RoundToInt(sp.x / s);
        int centerZ = Mathf.RoundToInt(sp.z / s);
        int halfW = gridWidth / 2;
        int halfH = gridHeight / 2;
        float sqrCull = cullDistance * cullDistance;

        Gizmos.color = Color.cyan;

    #if UNITY_EDITOR
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
    #endif

        for (int dz = -halfH; dz <= halfH; dz++)
        {
            for (int dx = -halfW; dx <= halfW; dx++)
            {
                int gridX = centerX + dx;
                int gridZ = centerZ + dz;
                Vector3 p = new Vector3(gridX * s, 0f, gridZ * s);
                bool inCull = (p - sp).sqrMagnitude <= sqrCull;
                Gizmos.color = inCull ? Color.cyan : Color.gray;
                Gizmos.DrawWireCube(p + Vector3.up * 0.1f, new Vector3(s, 0.1f, s));
    #if UNITY_EDITOR
                Handles.Label(p + Vector3.up * 0.25f,
                              $"({gridX},{gridZ})",
                              style);
    #endif
            }
        }
    }
}
