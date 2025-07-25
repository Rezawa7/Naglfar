using UnityEngine;

public class ShieldProjectile : MonoBehaviour
{
    public float lifetime = 10f;
    public float moveSpeed = 15f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
    }
}