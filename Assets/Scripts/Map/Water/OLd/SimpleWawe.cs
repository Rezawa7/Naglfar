using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SimpleWave : MonoBehaviour
{
    [Header("Wave Settings")]
    public float amplitude = 1f;
    public float wavelength = 1f;
    public float speed = 1f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] workingVertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        // make a copy so we don't modify the asset
        mesh = Instantiate(mesh);
        GetComponent<MeshFilter>().mesh = mesh;

        baseVertices    = mesh.vertices;
        workingVertices = new Vector3[baseVertices.Length];
    }

    void Update()
    {
        float t = Time.time * speed;
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];
            // simple sine along the X axis:
            v.y = Mathf.Sin(v.x * wavelength + t) * amplitude;
            workingVertices[i] = v;
        }

        mesh.vertices = workingVertices;
        mesh.RecalculateNormals();
    }
}
