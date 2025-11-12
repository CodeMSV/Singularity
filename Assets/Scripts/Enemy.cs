using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Comportamiento base para enemigos. Persigue al jugador con movimiento errático
/// y gestiona daño, muerte y efectos de explosión.
/// </summary>
public class Enemy : MonoBehaviour
{
    #region Serialized Fields
    [Header("Stats")]
    [SerializeField, Min(0f)] 
    private float health = 1f;
    
    [SerializeField, Range(0f, 1f), Tooltip("Porcentaje de la velocidad del jugador")]
    private float speedPercent = 0.6f;

    [Header("Audio")]
    [SerializeField] 
    private AudioClip deathSFX;
    
    [SerializeField, Range(0f, 3f)] 
    private float deathVolume = 1f;

    [Header("Identification")]
    [SerializeField, Tooltip("Tipo usado para recargar dash del jugador")]
    private string enemyType = "Cube";

    [Header("Erratic Movement")]
    [SerializeField, Min(0f)] 
    private float erraticDistance = 3f;
    
    [SerializeField, Min(0f)] 
    private float erraticUpdateSpeed = 1f;

    [Header("Death Effects")]
    [SerializeField] 
    private GameObject miniCubePrefab;
    
    [SerializeField, Min(0)] 
    private int cubeAmount = 10;
    
    [SerializeField, Min(0f)] 
    private float explosionForce = 150f;
    
    [SerializeField, Min(0f)] 
    private float explosionRadius = 3f;
    #endregion

    #region Protected Fields
    protected NavMeshAgent agent;
    protected Transform playerTransform;
    protected PlayerMovement playerMovement;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializePlayerReferences();
        InitializeNavMeshAgent();
    }

    protected virtual void Start()
    {
        ConfigureAgentSpeed();
        StartErraticMovement();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            TriggerPlayerDeath();
        }
    }
    #endregion

    #region Initialization
    private void InitializePlayerReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObject == null)
        {
            Debug.LogError("Player not found! Ensure the player has the 'Player' tag.", this);
            return;
        }

        playerTransform = playerObject.transform;
        playerMovement = playerObject.GetComponent<PlayerMovement>();
    }

    private void InitializeNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError($"NavMeshAgent missing on {gameObject.name}", this);
        }
    }

    private void ConfigureAgentSpeed()
    {
        if (playerMovement != null && agent != null)
        {
            agent.speed = playerMovement.moveSpeed * speedPercent;
        }
    }

    private void StartErraticMovement()
    {
        InvokeRepeating(nameof(UpdateErraticMovement), 0f, erraticUpdateSpeed);
    }
    #endregion

    #region Movement
    protected virtual void UpdateErraticMovement()
    {
        if (!CanMove()) return;

        Vector3 targetPosition = CalculateErraticTarget();
        agent.SetDestination(targetPosition);
    }

    private bool CanMove()
    {
        return playerTransform != null 
               && agent != null 
               && agent.isActiveAndEnabled;
    }

    private Vector3 CalculateErraticTarget()
    {
        Vector3 randomOffset = Random.insideUnitSphere * erraticDistance;
        randomOffset.y = 0f;
        return playerTransform.position + randomOffset;
    }
    #endregion

    #region Damage & Death
    public void TakeDamage(float damageAmount, bool isNovaKill = false)
    {
        health -= damageAmount;
        
        if (health <= 0f)
        {
            Die(isNovaKill);
        }
    }

    protected virtual void Die(bool isNovaKill = false)
    {
        NotifyPlayerKill();
        NotifyGameManager(isNovaKill);
        PlayDeathSound();
        SpawnDeathEffect();
        
        Destroy(gameObject);
    }
    #endregion

    #region Death Notifications
    private void NotifyPlayerKill()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        
        if (player != null)
        {
            player.AddKillCharge(enemyType);
        }
    }

    private void NotifyGameManager(bool isNovaKill)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill(isNovaKill);
        }
    }
    #endregion

    #region Death Effects
    private void PlayDeathSound()
    {
        if (deathSFX == null) return;

        DamageFeedback feedback = FindFirstObjectByType<DamageFeedback>();
        
        if (feedback != null)
        {
            feedback.PlaySFX(deathSFX, deathVolume);
        }
        else
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position, deathVolume);
        }
    }

    private void SpawnDeathEffect()
    {
        if (miniCubePrefab == null) return;

        for (int i = 0; i < cubeAmount; i++)
        {
            SpawnMiniCube();
        }
    }

    private void SpawnMiniCube()
    {
        GameObject miniCube = Instantiate(
            miniCubePrefab, 
            transform.position, 
            Random.rotation
        );

        Rigidbody rb = miniCube.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.AddExplosionForce(
                explosionForce, 
                transform.position, 
                explosionRadius
            );
        }
    }
    #endregion

    #region Player Collision
    private void TriggerPlayerDeath()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
    #endregion
}