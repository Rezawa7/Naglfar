using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour
{
    public float explosionRadius = 5f;
    public float explosionDamage = 35f;
    public GameObject explosionEffectPrefab;
    public LayerMask damageLayer = ~0;

    private bool hasExploded = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, damageLayer);
        foreach (Collider hit in hits)
        {
            Health hp = hit.GetComponent<Health>();
            if (hp != null)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                float damageScale = 1f - (dist / explosionRadius);
                float finalDamage = explosionDamage * damageScale;
                hp.TakeDamage(finalDamage);
            }

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                rb.AddExplosionForce(500f, transform.position, explosionRadius);
            }
        }

        Destroy(gameObject);
    }
}
