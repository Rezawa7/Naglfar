using UnityEngine;

public class CannonballDamage : MonoBehaviour
{
    public float damage = 25f;
    public GameObject shooter;
    private bool canDamage = false;

    void Start()
    {
        Invoke(nameof(EnableDamage), 0.1f);
    }

    void EnableDamage()
    {
        canDamage = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!canDamage) return;

        GameObject hitObject = collision.gameObject;

        if (hitObject == shooter || hitObject.transform.root == shooter.transform) return;

        Health target = hitObject.GetComponent<Health>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
