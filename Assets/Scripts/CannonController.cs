using UnityEngine;
using System.Linq;

public enum ShotType
{
    Standard,
    Heavy,
    Bouncy,
    Explosive,
    Scatter,
    Laser,
    Smoke,
    Shield
}


public class CannonController : MonoBehaviour
{
    public Transform firePoint;
    public GameObject projectilePrefab;
    public GameObject explosivePrefab;
    public GameObject scatterProjectilePrefab;
    public GameObject laserBeamPrefab;
    public GameObject laserImpactEffect;
    public GameObject shieldPrefab;
    public GameObject smokeBombPrefab;
    public AudioSource audioSource;
    public float sliderAdjustSpeed = 20f;
    public UnityEngine.UI.Slider angleSlider;
    public UnityEngine.UI.Slider strengthSlider;
    public bool isMyTurn = false;
    public bool isPlayerControlled = true;

    public float strength = 10f;
    public float angle = 45f;
    public float moveSpeed = 5f;
    public float rotateSpeed = 50f;
    public ShotType currentShot = ShotType.Standard;

    void Update()
    {

            if (angleSlider != null)
            {
                angle = angleSlider.value;
                if (Input.GetKey(KeyCode.UpArrow))
                    angleSlider.value += (sliderAdjustSpeed + 20f) * Time.deltaTime;
                if (Input.GetKey(KeyCode.DownArrow))
                    angleSlider.value -= (sliderAdjustSpeed + 20f) * Time.deltaTime;
            }

            if (strengthSlider != null)
            {
                strength = strengthSlider.value;
                if (Input.GetKey(KeyCode.RightArrow))
                    strengthSlider.value += sliderAdjustSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.LeftArrow))
                    strengthSlider.value -= sliderAdjustSpeed * Time.deltaTime;
            }
    }

    public void SetShotType(int index)
    {
        currentShot = (ShotType)index;
        Debug.Log("Shot type set to: " + currentShot);
    }

    public void Fire()
    {
        switch (currentShot)
        {
            case ShotType.Standard:
                FireStandard();
                break;
            case ShotType.Heavy:
                FireHeavy();
                break;
            case ShotType.Bouncy:
                FireBouncy();
                break;
            case ShotType.Explosive:
                FireExplosive();
                break;
            case ShotType.Scatter:
                FireScatter();
                break;
            case ShotType.Laser:
                FireLaser();
                break;
            case ShotType.Smoke:
                FireSmokeBomb();
                break;
            case ShotType.Shield:
                FireShieldDeploy();
                break;
        }

        if (audioSource) audioSource.Play();
    }

    void FireStandard()
    {
        Launch(firePoint.position, projectilePrefab, strength, angle, 25f);
    }

    void FireHeavy()
    {
        GameObject heavy = Launch(firePoint.position, projectilePrefab, strength * 0.8f, angle, 50f);
        Rigidbody rb = heavy.GetComponent<Rigidbody>();
        rb.mass = 3f;
        rb.linearDamping = 0.1f;
    }

    void FireBouncy()
    {
        GameObject bouncy = Launch(firePoint.position, projectilePrefab, strength, angle, 15f);
        PhysicsMaterial bounceMat = new PhysicsMaterial();
        bounceMat.bounciness = 0.8f;
        bounceMat.bounceCombine = PhysicsMaterialCombine.Maximum;

        Collider col = bouncy.GetComponent<Collider>();
        if (col) col.material = bounceMat;
    }

    void FireExplosive()
    {
        GameObject explosive = Launch(firePoint.position, explosivePrefab, strength, angle, 0f);
    }

    void FireScatter()
    {
        int pellets = 40;
        float spreadAngle = 30f;
        float pelletDamage = 1f;

        Collider[] shooterCols = GetComponentsInChildren<Collider>();

        for (int i = 0; i < pellets; i++)
        {
            float pitchOffset = Random.Range(-spreadAngle, spreadAngle);
            float yawOffset = Random.Range(-spreadAngle, spreadAngle);
            Quaternion spreadRotation = Quaternion.AngleAxis(-angle + pitchOffset, firePoint.right) *
                                        Quaternion.AngleAxis(yawOffset, firePoint.up);
            Vector3 direction = spreadRotation * firePoint.forward;

            Vector3 spawnPos = firePoint.position + direction.normalized * 0.5f;

            GameObject pellet = Instantiate(scatterProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

            Collider[] pelletCols = pellet.GetComponentsInChildren<Collider>();
            foreach (var shooterCol in shooterCols)
                foreach (var pelletCol in pelletCols)
                    Physics.IgnoreCollision(shooterCol, pelletCol);

            Rigidbody rb = pellet.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = direction.normalized * strength;

            CannonballDamage dmgComponent = pellet.GetComponent<CannonballDamage>();
            if (dmgComponent != null)
            {
                dmgComponent.damage = pelletDamage;
                dmgComponent.shooter = this.gameObject;
            }
        }
    }

    void FireLaser()
    {
        float laserRange = 100f;
        float laserDamage = 30f;

        Vector3 start = firePoint.position;
        Vector3 direction = firePoint.forward;
        Vector3 end = start + direction * laserRange;

        Ray ray = new Ray(start, direction);

        // Collect all my own colliders (boat + shield + cannon etc.)
        Collider[] myColliders = GetComponentsInChildren<Collider>(true);

        if (Physics.Raycast(ray, out RaycastHit hit, laserRange))
        {
            // If we hit ourselves, skip this hit
            if (myColliders.Contains(hit.collider))
            {
                // Try again but ignore our own colliders
                if (Physics.Raycast(ray, out RaycastHit hit2, laserRange, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!myColliders.Contains(hit2.collider))
                    {
                        hit = hit2;
                    }
                    else
                    {
                        return; // only hit ourselves, abort
                    }
                }
            }

            end = hit.point;

            // Damage only if it's not us
            Health hp = hit.collider.GetComponent<Health>();
            if (hp != null && !myColliders.Contains(hit.collider))
                hp.TakeDamage(laserDamage);

            if (laserImpactEffect != null)
            {
                Quaternion impactRotation = Quaternion.LookRotation(hit.normal);
                GameObject fx = Instantiate(laserImpactEffect, hit.point, impactRotation);
                Destroy(fx, 1f);
            }
        }

        // Draw beam
        if (laserBeamPrefab != null)
        {
            GameObject beam = Instantiate(laserBeamPrefab);
            LineRenderer lr = beam.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, start);
                lr.SetPosition(1, end);
            }
            Destroy(beam, 0.2f);
        }

        Debug.DrawLine(start, end, Color.red, 1f);
    }

    void FireSmokeBomb()
    {
        GameObject smokeBomb = Launch(firePoint.position, smokeBombPrefab, strength, angle, 0f);
    }

    void FireShieldDeploy()
    {
        Vector3 spawnPos = firePoint.position + firePoint.forward * 1.5f;
        Quaternion spawnRot = firePoint.rotation;

        GameObject shield = Instantiate(shieldPrefab, spawnPos, spawnRot);

        // Stick to the boat
        shield.transform.SetParent(this.transform);

        // Setup health
        Health hp = shield.GetComponent<Health>();
        if (hp == null)
            hp = shield.AddComponent<Health>();

        hp.maxHealth = 50f;
        hp.currentHealth = 50f;

        // Destroy after 20s max
        Destroy(shield, 5f);

        // Ignore collisions with own projectiles
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        Collider[] shieldColliders = shield.GetComponentsInChildren<Collider>();

        foreach (var myCol in myColliders)
        {
            foreach (var shieldCol in shieldColliders)
            {
                Physics.IgnoreCollision(myCol, shieldCol);
            }
        }
    }

    GameObject Launch(Vector3 position, GameObject prefab, float force, float launchAngle, float damage = 25f)
    {
        Vector3 spawnPos = firePoint.position + firePoint.forward * 0.5f; 
        GameObject projectile = Instantiate(prefab, spawnPos, Quaternion.identity);


        Collider[] myCols = GetComponentsInChildren<Collider>(true);
        Collider[] projCols = projectile.GetComponentsInChildren<Collider>(true);

        foreach (var myCol in myCols)
        {
            foreach (var projCol in projCols)
            {
                if (myCol && projCol)
                    Physics.IgnoreCollision(myCol, projCol);
            }
        }

        // Launch
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        Vector3 launchDirection = Quaternion.AngleAxis(-launchAngle, firePoint.right) * firePoint.forward;
        rb.linearVelocity = launchDirection.normalized * force;

        // Set damage
        CannonballDamage dmgComponent = projectile.GetComponent<CannonballDamage>();
        if (dmgComponent != null)
        {
            dmgComponent.damage = damage;
            dmgComponent.shooter = this.gameObject;
        }

        projectile.AddComponent<AutoDestroy>();
        return projectile;
    }
}
