using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifetime = 20f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
