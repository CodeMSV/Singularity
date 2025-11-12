using UnityEngine;
using System.Collections;
using UnityEngine.UI; 

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;
    private Vector3 movementInput;

    [Header("Dash")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.15f; 
    public float dashCooldownTime = 0.5f; 

    [Header("HUD y Efectos del Dash")]
    public Image dashFillImage; 
    public Animator dashBarAnimator; 
    public AudioClip dashReadySFX; 

    [Header("Audio")]
    public AudioClip dashSFX; 

    [Header("Recarga por Kills")]
    public int cubeKillsNeeded = 10;
    public int sphereKillsNeeded = 2;
    public int currentDashKills = 0;
    
    private bool canDash = true;
    private bool isDashing = false;
    
    private Collider playerCollider; 
    private DamageFeedback damageFeedback; 
    
    public ParticleSystem playerDustParticles; 
    private ParticleSystem.EmissionModule emissionModule;
    public float dustEmissionRate = 50f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        damageFeedback = GetComponent<DamageFeedback>();
    }

    void Start()
    {
        if (playerDustParticles != null)
        {
            emissionModule = playerDustParticles.emission;
            emissionModule.rateOverTime = 0;
            playerDustParticles.Play();
        }
        
        // --- AÑADIDO ---
        // Asegurarse de que la barra empieza vacía al cargar la escena
        if (dashFillImage != null)
        {
            dashFillImage.fillAmount = 0f;
        }
    }

    void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.z = Input.GetAxisRaw("Vertical");
        movementInput.Normalize();

        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            // (La comprobación de '>= cubeKillsNeeded' ya la hace 'StartDash' implícitamente)
            StartDash(); 
        }
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            // --- ¡CORREGIDO! ---
            // 'linearVelocity' no existe, es 'velocity'
            float verticalVelocity = rb.linearVelocity.y; 
            Vector3 targetVelocity = movementInput * moveSpeed;
            rb.linearVelocity = new Vector3(targetVelocity.x, verticalVelocity, targetVelocity.z);
        }
        UpdateParticles();
    }
    
    // --- LÓGICA DEL DASH ---
    void StartDash()
    {
        // Añadida comprobación de recarga aquí
        if (movementInput.magnitude < 0.1f || currentDashKills < cubeKillsNeeded) return;
        
        canDash = false;
        isDashing = true;
        
        // --- ¡CORREGIDO! ---
        // 'linearVelocity' no existe, es 'velocity'
        Vector3 dashVelocity = movementInput * dashSpeed;
        rb.linearVelocity = dashVelocity;

        currentDashKills = 0;
        
        // --- ¡¡AQUÍ ESTÁ LA LÍNEA QUE FALTABA!! ---
        // Resetea la barra visual a 0
        if (dashFillImage != null)
        {
            dashFillImage.fillAmount = 0f;
        }
        // ------------------------------------

        if (damageFeedback != null)
        {
            damageFeedback.Flash(dashDuration);
            if (dashSFX != null)
            {
                damageFeedback.PlaySFX(dashSFX);
            }
        }
        
        StartCoroutine(DashInvincibilityRoutine());
    }

    IEnumerator DashInvincibilityRoutine()
    {
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }
        
        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }
        
        yield return new WaitForSeconds(dashCooldownTime);
        
        // (La lógica de 'canDash = true' se mueve a 'AddKillCharge'
        //  para que solo se active al RECARGAR, no al terminar el cooldown)
    }

    // --- LÓGICA DE KILLS ---
    public void AddKillCharge(string enemyType)
    {
        bool wasReady = (currentDashKills >= cubeKillsNeeded);

        if (enemyType == "Cube")
        {
            currentDashKills += 1;
        }
        else if (enemyType == "Sphere")
        {
            if (sphereKillsNeeded > 0)
            {
                currentDashKills += cubeKillsNeeded / sphereKillsNeeded;
            }
        }

        currentDashKills = Mathf.Min(currentDashKills, cubeKillsNeeded);

        float fillAmount = 0f;
        if (cubeKillsNeeded > 0)
        {
            fillAmount = (float)currentDashKills / (float)cubeKillsNeeded;
        }

        if (dashFillImage != null)
        {
            dashFillImage.fillAmount = fillAmount;
        }

        bool isReady = (currentDashKills >= cubeKillsNeeded);

        if (isReady && !wasReady)
        {
            Debug.Log("¡Dash Recargado!");
            canDash = true; // <-- La habilidad se activa AQUÍ

            if (damageFeedback != null && dashReadySFX != null)
            {
                damageFeedback.PlaySFX(dashReadySFX);
            }

            if (dashBarAnimator != null)
            {
                dashBarAnimator.SetTrigger("OnFull");
            }
        }

        // (Bloque 'if (currentDashKills >= cubeKillsNeeded)' eliminado de aquí
        //  porque era redundante con el bloque 'isReady && !wasReady')
    }
    
    // --- LÓGICA DE PARTÍCULAS ---
    void UpdateParticles()
    {
        if (playerDustParticles == null) return;
        
        if (movementInput.magnitude > 0.1f || isDashing) 
        {
            emissionModule.rateOverTime = dustEmissionRate;
        }
        else
        {
            emissionModule.rateOverTime = 0;
        }
    }
}