// ----------------------------------------------------
// ARCHIVO: Enemy_Sphere.cs (Clase Derivada)
// ----------------------------------------------------
using UnityEngine;
using UnityEngine.AI;
using System.Collections; // ¡CORREGIDO! Para que IEnumerator funcione

// Hereda de la clase Enemy
public class Enemy_Sphere : Enemy
{
    [Header("Ataque de la Esfera")]
    [Tooltip("Tiempo de cadencia entre disparos")]
    public float fireRate = 5f;
    public float shootRange = 15f;

    [Header("Disparo Rápido (Pánico)")]
    public float panicRange = 5f; // Rango para disparar más rápido
    public float panicFireRate = 1f; // Cadencia en modo pánico

    public GameObject enemyBulletPrefab; // Prefab de la bala enemiga

    private float nextFireTime;
    private bool isShooting = false;

    // ¡CORREGIDO! Usamos 'override'
    protected override void Start()
    {
        // 1. Llama al Start() de la clase base para Awake, agente y jugador
        base.Start();

        // 2. Lógica específica de la Esfera:

        // Asignamos la velocidad al 40% del jugador
        if (playerMovement != null && agent != null)
        {
            agent.speed = playerMovement.moveSpeed * 0.4f;
        }

        // Cancelamos el movimiento errático del Cubo (el Cubo tiene InvokeRepeating)
        CancelInvoke(nameof(UpdateErraticMovement));

        nextFireTime = Time.time + fireRate;
    }

    void Update()
    {
        // ... (El resto del código es el mismo para la Esfera) ...
        AimAtPlayer();

        if (playerTransform != null && agent != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            float currentFireRate = fireRate;
            if (distance <= panicRange)
            {
                currentFireRate = panicFireRate;
            }

            if (Time.time >= nextFireTime && distance <= shootRange && !isShooting)
            {
                StartCoroutine(StopAimAndShoot(currentFireRate));
            }

            if (agent.enabled && !isShooting)
            {
                agent.SetDestination(playerTransform.position);
            }
        }
    }

    private void AimAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    // EN: Enemy_Sphere.cs

    // EN: Enemy_Sphere.cs

    IEnumerator StopAimAndShoot(float currentFireRate)
    {
        isShooting = true;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true; // 1. SE DETIENE
        }

        yield return new WaitForSeconds(0.5f); // Espera de Apuntado

        if (enemyBulletPrefab != null && playerTransform != null)
        {
            // --- ¡¡LÓGICA DE ALTURA CORREGIDA!! ---

            // 1. Obtenemos la altura Y del jugador (ej. -0.5)
            float playerHeightY = playerTransform.position.y;

            // 2. Obtenemos la posición del enemigo, PERO forzada a la altura del jugador
            Vector3 enemyFirePos = transform.position;
            enemyFirePos.y = playerHeightY;

            // 3. Obtenemos la posición del objetivo, TAMBIÉN forzada a la altura del jugador
            Vector3 targetPoint = playerTransform.position;
            targetPoint.y = playerHeightY;

            // 4. Calculamos la dirección (ahora es 100% plana en el eje Y del jugador)
            Vector3 fireDirection = (targetPoint - enemyFirePos).normalized;

            // 5. Calculamos la posición de spawn (un poco delante del enemigo, pero en la Y del jugador)
            Vector3 spawnPosition = enemyFirePos + fireDirection * 1.0f;

            // 6. Creamos la bala en la altura Y correcta
            GameObject bullet = Instantiate(enemyBulletPrefab, spawnPosition, Quaternion.LookRotation(fireDirection));

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                // Usamos .velocity (ya que arreglamos linearVelocity)
                bulletRb.linearVelocity = fireDirection * 10f;
            }
        }

        nextFireTime = Time.time + currentFireRate;

        if (agent != null)
        {
            agent.isStopped = false;
        }

        isShooting = false;
    }
}