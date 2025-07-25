using UnityEngine;

public class ExplosiveSmokeProjectile : MonoBehaviour
{
    public GameObject smokeCloudPrefab;
    public float smokeDuration = 10f;
    public float explosionRadius = 3f;

    private bool hasExploded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        if (smokeCloudPrefab != null)
        {
            GameObject smoke = Instantiate(smokeCloudPrefab, transform.position, Quaternion.identity);
            Destroy(smoke, smokeDuration);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
                rb.AddExplosionForce(300f, transform.position, explosionRadius);
        }

        Destroy(gameObject);
    }
}
