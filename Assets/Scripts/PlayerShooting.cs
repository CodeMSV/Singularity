using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    // 1. Arrastra tu "Bullet_Player" (prefab) aquí
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f; // Velocidad del proyectil
    public float fireRate = 0.5f;   // Cadencia de tiro (0.5 segundos entre disparos)

    [Header("Audio")]
    public AudioClip shootSFX; // Archivo de sonido del disparo

    private float nextFireTime; // Para controlar la cadencia
    private DamageFeedback damageFeedback; // Conexión para reproducir el audio

    // La posición de donde saldrán los disparos (ej. el centro del jugador)
    public Transform firePoint;

    void Start()
    {
        if (firePoint == null)
        {
            firePoint = this.transform;
        }

        // ¡CLAVE! Obtenemos el script DamageFeedback para usar el altavoz
        damageFeedback = GetComponent<DamageFeedback>();
    }

    void Update()
    {
        // 2. Detectar el clic del ratón y la cadencia
        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            Shoot();

            // Reproducir SFX del disparo
            if (damageFeedback != null && shootSFX != null)
            {
                damageFeedback.PlaySFX(shootSFX);
            }
        }

        // (He quitado el código de TEST que tenías aquí)
    }

    void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("¡Falta el prefab de la bala en el script PlayerShooting!");
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;

        // Esta lógica de Raycast sigue siendo correcta para OBTENER LA DIRECCIÓN
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100);
            targetPoint.y = firePoint.position.y;
        }

        Vector3 direction = (targetPoint - firePoint.position).normalized;

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            // ¡CORRECCIÓN DE FÍSICA! Usamos .velocity
            // CORRECTO:
            bulletRb.linearVelocity = direction * bulletSpeed;
        }

        // ¡CORRECCIÓN! HEMOS QUITADO EL TEMPORIZADOR
        // Destroy(bullet, 5f); 
    }
}