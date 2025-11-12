using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Controla el movimiento del jugador, dash con invencibilidad y sistema de recarga por kills.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Constants
    private const float MIN_INPUT_MAGNITUDE = 0.1f;
    private const float EMPTY_FILL = 0f;
    private const string DASH_ANIMATION_TRIGGER = "OnFull";
    private const string ENEMY_TYPE_CUBE = "Cube";
    private const string ENEMY_TYPE_SPHERE = "Sphere";
    #endregion

    #region Serialized Fields
    [Header("Movement")]
    [SerializeField, Min(0f)] 
    public float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField, Min(0f)] 
    private float dashSpeed = 30f;
    
    [SerializeField, Min(0f)] 
    private float dashDuration = 0.15f;
    
    [SerializeField, Min(0f)] 
    private float dashCooldownTime = 0.5f;

    [Header("Dash UI & Effects")]
    [SerializeField] 
    private Image dashFillImage;
    
    [SerializeField] 
    private Animator dashBarAnimator;
    
    [SerializeField] 
    private AudioClip dashReadySFX;

    [Header("Audio")]
    [SerializeField] 
    private AudioClip dashSFX;

    [Header("Kill Charge System")]
    [SerializeField, Min(1)] 
    private int cubeKillsNeeded = 10;
    
    [SerializeField, Min(1)] 
    private int sphereKillsNeeded = 2;
    
    private int currentDashKills;

    [Header("Particles")]
    [SerializeField] 
    private ParticleSystem playerDustParticles;
    
    [SerializeField, Min(0f)] 
    private float dustEmissionRate = 50f;
    #endregion

    #region Private Fields
    private Rigidbody rb;
    private Collider playerCollider;
    private DamageFeedback damageFeedback;
    private ParticleSystem.EmissionModule emissionModule;
    
    private Vector3 movementInput;
    private bool canDash = true;
    private bool isDashing;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeParticles();
        ResetDashFillBar();
    }

    private void Update()
    {
        HandleMovementInput();
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            ApplyMovement();
        }
        
        UpdateParticles();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        damageFeedback = GetComponent<DamageFeedback>();
    }

    private void InitializeParticles()
    {
        if (playerDustParticles == null) return;

        emissionModule = playerDustParticles.emission;
        emissionModule.rateOverTime = 0;
        playerDustParticles.Play();
    }

    private void ResetDashFillBar()
    {
        if (dashFillImage != null)
        {
            dashFillImage.fillAmount = EMPTY_FILL;
        }
    }
    #endregion

    #region Input Handling
    private void HandleMovementInput()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.z = Input.GetAxisRaw("Vertical");
        movementInput.Normalize();
    }

    private void HandleDashInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            TryStartDash();
        }
    }
    #endregion

    #region Movement
    private void ApplyMovement()
    {
        float verticalVelocity = rb.linearVelocity.y;
        Vector3 targetVelocity = movementInput * moveSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, verticalVelocity, targetVelocity.z);
    }
    #endregion

    #region Dash System
    private void TryStartDash()
    {
        if (!CanPerformDash()) return;

        ExecuteDash();
    }

    private bool CanPerformDash()
    {
        return HasMovementInput() && HasEnoughKills();
    }

    private bool HasMovementInput()
    {
        return movementInput.magnitude >= MIN_INPUT_MAGNITUDE;
    }

    private bool HasEnoughKills()
    {
        return currentDashKills >= cubeKillsNeeded;
    }

    private void ExecuteDash()
    {
        SetDashState();
        ApplyDashVelocity();
        ConsumeDashCharge();
        PlayDashEffects();
        StartCoroutine(DashSequence());
    }

    private void SetDashState()
    {
        canDash = false;
        isDashing = true;
    }

    private void ApplyDashVelocity()
    {
        Vector3 dashVelocity = movementInput * dashSpeed;
        rb.linearVelocity = dashVelocity;
    }

    private void ConsumeDashCharge()
    {
        currentDashKills = 0;
        ResetDashFillBar();
    }

    private void PlayDashEffects()
    {
        if (damageFeedback == null) return;

        damageFeedback.Flash(dashDuration);
        
        if (dashSFX != null)
        {
            damageFeedback.PlaySFX(dashSFX);
        }
    }

    private IEnumerator DashSequence()
    {
        EnableInvincibility();
        
        yield return new WaitForSeconds(dashDuration);
        
        isDashing = false;
        DisableInvincibility();
        
        yield return new WaitForSeconds(dashCooldownTime);
    }

    private void EnableInvincibility()
    {
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }
    }

    private void DisableInvincibility()
    {
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }
    }
    #endregion

    #region Kill Charge System
    public void AddKillCharge(string enemyType)
    {
        bool wasReady = IsDashReady();

        AddKillsForEnemyType(enemyType);
        ClampKills();
        UpdateDashUI();

        CheckAndActivateDash(wasReady);
    }

    private bool IsDashReady()
    {
        return currentDashKills >= cubeKillsNeeded;
    }

    private void AddKillsForEnemyType(string enemyType)
    {
        if (enemyType == ENEMY_TYPE_CUBE)
        {
            currentDashKills += 1;
        }
        else if (enemyType == ENEMY_TYPE_SPHERE)
        {
            currentDashKills += CalculateSphereKillValue();
        }
    }

    private int CalculateSphereKillValue()
    {
        if (sphereKillsNeeded > 0)
        {
            return cubeKillsNeeded / sphereKillsNeeded;
        }
        return 0;
    }

    private void ClampKills()
    {
        currentDashKills = Mathf.Min(currentDashKills, cubeKillsNeeded);
    }

    private void UpdateDashUI()
    {
        if (dashFillImage == null) return;

        float fillAmount = CalculateFillAmount();
        dashFillImage.fillAmount = fillAmount;
    }

    private float CalculateFillAmount()
    {
        if (cubeKillsNeeded > 0)
        {
            return (float)currentDashKills / cubeKillsNeeded;
        }
        return EMPTY_FILL;
    }

    private void CheckAndActivateDash(bool wasReady)
    {
        bool isReadyNow = IsDashReady();

        if (isReadyNow && !wasReady)
        {
            ActivateDash();
        }
    }

    private void ActivateDash()
    {
        canDash = true;
        PlayDashReadyEffects();
    }

    private void PlayDashReadyEffects()
    {
        PlayDashReadySound();
        TriggerDashReadyAnimation();
    }

    private void PlayDashReadySound()
    {
        if (damageFeedback != null && dashReadySFX != null)
        {
            damageFeedback.PlaySFX(dashReadySFX);
        }
    }

    private void TriggerDashReadyAnimation()
    {
        if (dashBarAnimator != null)
        {
            dashBarAnimator.SetTrigger(DASH_ANIMATION_TRIGGER);
        }
    }
    #endregion

    #region Particles
    private void UpdateParticles()
    {
        if (playerDustParticles == null) return;

        bool shouldEmit = ShouldEmitParticles();
        emissionModule.rateOverTime = shouldEmit ? dustEmissionRate : 0;
    }

    private bool ShouldEmitParticles()
    {
        return movementInput.magnitude > MIN_INPUT_MAGNITUDE || isDashing;
    }
    #endregion
}