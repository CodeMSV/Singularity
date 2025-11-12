using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Enemigo esférico que dispara proyectiles al jugador. Aumenta su cadencia de fuego
/// cuando el jugador está cerca (modo pánico).
/// </summary>
public class Enemy_Sphere : Enemy
{
    #region Constants
    private const float SPHERE_SPEED_PERCENT = 0.4f;
    private const float AIM_ROTATION_SPEED = 5f;
    private const float AIM_DELAY = 0.5f;
    private const float BULLET_SPEED = 10f;
    private const float BULLET_SPAWN_OFFSET = 1f;
    #endregion

    #region Serialized Fields
    [Header("Shooting")]
    [SerializeField, Min(0f), Tooltip("Tiempo entre disparos (segundos)")]
    private float fireRate = 5f;
    
    [SerializeField, Min(0f), Tooltip("Rango máximo de disparo")]
    private float shootRange = 15f;

    [Header("Panic Mode")]
    [SerializeField, Min(0f), Tooltip("Distancia para activar modo pánico")]
    private float panicRange = 5f;
    
    [SerializeField, Min(0f), Tooltip("Cadencia de disparo en modo pánico")]
    private float panicFireRate = 1f;

    [Header("Projectile")]
    [SerializeField]
    private GameObject enemyBulletPrefab;
    #endregion

    #region Private Fields
    private float nextFireTime;
    private bool isShooting;
    #endregion

    #region Unity Lifecycle
    protected override void Start()
    {
        base.Start();
        
        ConfigureSphereSpeed();
        DisableErraticMovement();
        InitializeFireTime();
    }

    private void Update()
    {
        AimAtPlayer();
        UpdateShooting();
        UpdateMovement();
    }
    #endregion

    #region Initialization
    private void ConfigureSphereSpeed()
    {
        if (playerMovement != null && agent != null)
        {
            agent.speed = playerMovement.moveSpeed * SPHERE_SPEED_PERCENT;
        }
    }

    private void DisableErraticMovement()
    {
        CancelInvoke(nameof(UpdateErraticMovement));
    }

    private void InitializeFireTime()
    {
        nextFireTime = Time.time + fireRate;
    }
    #endregion

    #region Aiming
    private void AimAtPlayer()
    {
        if (playerTransform == null) return;

        Vector3 direction = GetHorizontalDirection();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            Time.deltaTime * AIM_ROTATION_SPEED
        );
    }

    private Vector3 GetHorizontalDirection()
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        return direction;
    }
    #endregion

    #region Shooting
    private void UpdateShooting()
    {
        if (!CanShoot()) return;

        float distanceToPlayer = GetDistanceToPlayer();
        float currentFireRate = GetCurrentFireRate(distanceToPlayer);

        if (IsReadyToShoot(distanceToPlayer))
        {
            StartCoroutine(ShootSequence(currentFireRate));
        }
    }

    private bool CanShoot()
    {
        return playerTransform != null 
               && agent != null 
               && !isShooting;
    }

    private float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    private float GetCurrentFireRate(float distanceToPlayer)
    {
        return distanceToPlayer <= panicRange ? panicFireRate : fireRate;
    }

    private bool IsReadyToShoot(float distanceToPlayer)
    {
        return Time.time >= nextFireTime 
               && distanceToPlayer <= shootRange;
    }

    private IEnumerator ShootSequence(float currentFireRate)
    {
        isShooting = true;

        StopAgent();
        yield return new WaitForSeconds(AIM_DELAY);
        
        FireBullet();
        
        ScheduleNextShot(currentFireRate);
        ResumeAgent();

        isShooting = false;
    }

    private void StopAgent()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }
    }

    private void ResumeAgent()
    {
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    private void FireBullet()
    {
        if (enemyBulletPrefab == null || playerTransform == null) return;

        Vector3 spawnPosition = CalculateBulletSpawnPosition();
        Vector3 fireDirection = CalculateFireDirection();
        
        GameObject bullet = Instantiate(
            enemyBulletPrefab, 
            spawnPosition, 
            Quaternion.LookRotation(fireDirection)
        );

        ApplyBulletVelocity(bullet, fireDirection);
    }

    private Vector3 CalculateBulletSpawnPosition()
    {
        float playerHeight = playerTransform.position.y;
        Vector3 fireDirection = CalculateFireDirection();
        
        Vector3 spawnPosition = transform.position;
        spawnPosition.y = playerHeight;
        spawnPosition += fireDirection * BULLET_SPAWN_OFFSET;
        
        return spawnPosition;
    }

    private Vector3 CalculateFireDirection()
    {
        float playerHeight = playerTransform.position.y;
        
        Vector3 enemyPosition = transform.position;
        enemyPosition.y = playerHeight;
        
        Vector3 targetPosition = playerTransform.position;
        targetPosition.y = playerHeight;
        
        return (targetPosition - enemyPosition).normalized;
    }

    private void ApplyBulletVelocity(GameObject bullet, Vector3 direction)
    {
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = direction * BULLET_SPEED;
        }
    }

    private void ScheduleNextShot(float currentFireRate)
    {
        nextFireTime = Time.time + currentFireRate;
    }
    #endregion

    #region Movement
    private void UpdateMovement()
    {
        if (!CanMove()) return;

        agent.SetDestination(playerTransform.position);
    }

    private bool CanMove()
    {
        return agent != null 
               && agent.enabled 
               && !isShooting 
               && playerTransform != null;
    }
    #endregion
}