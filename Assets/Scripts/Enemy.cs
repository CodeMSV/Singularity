using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Estadísticas Base")]
    public float health = 1f;
    [Tooltip("Qué porcentaje de la velocidad del jugador tendrá.")]
    public float speedPercent = 0.6f;

    [Header("Audio")]
    public AudioClip deathSFX;
    [Range(0f, 3f)] 
    public float deathVolume = 1f; 

    [Header("Identificador")]
    [Tooltip("Usado para recargar el Dash del jugador.")]
    public string enemyType = "Cube"; 

    [Header("Movimiento Errático")]
    public float erraticDistance = 3f;
    public float erraticUpdateSpeed = 1f;

    [Header("Efectos de Muerte (Físicas)")]
    public GameObject miniCubePrefab;
    public int cubeAmount = 10;
    public float explosionForce = 150f;
    public float explosionRadius = 3f;

    // Componentes
    protected NavMeshAgent agent;
    protected Transform playerTransform;
    protected PlayerMovement playerMovement;

    void Awake()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError("¡No se encuentra al jugador! Asegúrate de que tiene el Tag 'Player'.");
        }
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void Start()
    {
        if (playerMovement != null && agent != null)
        {
            agent.speed = playerMovement.moveSpeed * speedPercent;
        }
        InvokeRepeating(nameof(UpdateErraticMovement), 0f, erraticUpdateSpeed);
    }

    protected virtual void UpdateErraticMovement()
    {
        if (playerTransform == null || agent == null) return;

        if (agent.isActiveAndEnabled)
        {
            Vector3 randomOffset = Random.insideUnitSphere * erraticDistance;
            randomOffset.y = 0;
            Vector3 targetPosition = playerTransform.position + randomOffset;
            agent.SetDestination(targetPosition);
        }
    }

    // AHORA ACEPTA EL MARCADOR "isNovaKill"
    public void TakeDamage(float damageAmount, bool isNovaKill = false) // <-- MODIFICADO
    {
        health -= damageAmount;
        if (health <= 0)
        {
            // PASA EL MARCADOR A LA FUNCIÓN "Die"
            Die(isNovaKill); // <-- MODIFICADO
        }
    }

    // AHORA "Die" TAMBIÉN ACEPTA EL MARCADOR
    protected virtual void Die(bool isNovaKill = false) // <-- MODIFICADO
    {
        // --- Lógica de Recarga del Dash ---
        PlayerMovement playerMovementScript = FindFirstObjectByType<PlayerMovement>(); // <-- MODIFICADO (FindObjectOfType obsoleto)
        if (playerMovementScript != null)
        {
            playerMovementScript.AddKillCharge(enemyType);
        }
    
        // AVISA AL GAMEMANAGER PASANDO EL MARCADOR
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill(isNovaKill); // <-- MODIFICADO
        }

        if (deathSFX != null)
        {
            DamageFeedback feedback = FindFirstObjectByType<DamageFeedback>(); // <-- MODIFICADO (FindObjectOfType obsoleto)
            if (feedback != null)
            {
                feedback.PlaySFX(deathSFX, deathVolume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(deathSFX, transform.position, deathVolume);
            }
        }
        
        if (miniCubePrefab != null)
        {
            for (int i = 0; i < cubeAmount; i++)
            {
                GameObject miniCube = Instantiate(miniCubePrefab, transform.position, Random.rotation);
                Rigidbody rb = miniCube.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}