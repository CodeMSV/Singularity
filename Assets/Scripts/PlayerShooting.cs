using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private Transform firePoint;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSFX;

    private float nextFireTime;
    private DamageFeedback damageFeedback;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        damageFeedback = GetComponent<DamageFeedback>();
        
        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
            PlayShootSound();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("Bullet prefab is missing in PlayerShooting script!");
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Vector3 direction = GetShootDirection();

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * bulletSpeed;
        }
    }

    private Vector3 GetShootDirection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
            targetPoint.y = firePoint.position.y;
        }

        return (targetPoint - firePoint.position).normalized;
    }

    private void PlayShootSound()
    {
        if (damageFeedback != null && shootSFX != null)
        {
            damageFeedback.PlaySFX(shootSFX);
        }
    }
}