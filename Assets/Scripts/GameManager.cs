using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private int killCount = 0;
    private bool isGameOver = false;

    private Transform playerTransform; // Para saber dónde crear la onda

    [Header("Poder Nova")]
    public Image novaFillImage;
    public int novaKillsThreshold = 30;
    private int currentNovaKills = 0;
    private bool isNovaReady = false;

    [Range(0f, 3f)]
    public float novaUnleashVolume = 1.5f;

    public float novaRadius = 15f;
    public float novaDamage = 9999f;
    public GameObject novaEffectPrefab;
    public AudioClip novaReadySFX;
    public AudioClip novaUnleashSFX;

    [Header("Referencias de HUD")]
    public TextMeshProUGUI hudKillsText;

    [Header("Referencias de UI")]
    public GameObject pausePanel;
    private bool isPaused = false;

    [Header("Referencias de UI (OPCIONALES)")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI killCountText;
    public AudioSource musicSource;
    public GameObject panelDePruebaRojo;
    public GameObject panelDePruebaVerde;

    void Awake()
    {
        Debug.Log("GameManager: ¡Awake() ejecutado!");
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager: Instancia creada.");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Ocultar paneles al inicio
        if (gameOverPanel != null) { gameOverPanel.SetActive(false); }
        if (panelDePruebaRojo != null) { panelDePruebaRojo.SetActive(false); }
        if (panelDePruebaVerde != null) { panelDePruebaVerde.SetActive(false); } // <-- AÑADIDO (para que no falle)
        if (pausePanel != null) { pausePanel.SetActive(false); } // <-- AÑADIDO (para que no falle)


        Time.timeScale = 1f;
    }

    // AHORA ACEPTA EL MARCADOR "fromNova"
    public void AddKill(bool fromNova = false) // <-- MODIFICADO
    {
        if (isGameOver) return;
        killCount++;

        if (hudKillsText != null)
        {
            hudKillsText.text = "KILLS: " + killCount;
        }

        // --- LÓGICA DE NOVA MODIFICADA ---
        // Solo suma a la barra de Nova si NO está cargada Y la muerte NO vino de una Nova
        if (!isNovaReady && !fromNova) // <-- MODIFICADO
        {
            currentNovaKills++;

            // Actualiza la barra de Nova
            float fillAmount = (float)currentNovaKills / (float)novaKillsThreshold;
            if (novaFillImage != null)
            {
                novaFillImage.fillAmount = fillAmount;
            }

            // ¿Se acaba de cargar?
            if (currentNovaKills >= novaKillsThreshold)
            {
                isNovaReady = true;
                // Reproduce sonido de "Lista"
                DamageFeedback feedback = FindFirstObjectByType<DamageFeedback>(); // <-- MODIFICADO (FindObjectOfType obsoleto)
                if (feedback != null && novaReadySFX != null)
                {
                    feedback.PlaySFX(novaReadySFX);
                }
            }
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("--- ¡GAME OVER TRIGGERED! ---");
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        Debug.Log("Game Over Routine: Empezando...");

        if (musicSource != null)
        {
            musicSource.volume = 0.1f;
            Debug.Log("Game Over Routine: Música bajada.");
        }
        else { Debug.LogWarning("Game Over Routine: musicSource es NULL."); }

        if (killCountText != null)
        {
            killCountText.text = "ENEMIGOS ELIMINADOS: " + killCount;
            Debug.Log("Game Over Routine: Texto de kills actualizado.");
        }
        else { Debug.LogWarning("Game Over Routine: killCountText es NULL."); }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("Game Over Routine: gameOverPanel ACTIVADO.");
        }
        else { Debug.LogWarning("Game Over Routine: gameOverPanel es NULL."); }

        if (panelDePruebaRojo != null)
        {
            panelDePruebaRojo.SetActive(true);
            Debug.Log("Game Over Routine: panelDePruebaRojo ACTIVADO.");
        }
        if (panelDePruebaVerde != null)
        {
            panelDePruebaVerde.SetActive(true);
            Debug.Log("Game Over Routine: panelDePruebaVerde ACTIVADO.");
        }

        yield return new WaitForSecondsRealtime(0.05f);

        Time.timeScale = 0f;
        Debug.Log("Game Over Routine: Tiempo CONGELADO. Fin.");
    }

    public void ReloadScene()
    {
        Time.timeScale = 1f;

        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ResetSpawner();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        if (!isGameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isNovaReady && Input.GetMouseButtonDown(1)) // 1 = Clic Derecho
        {
            UnleashNova();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f;

        // --- ¡AÑADE ESTE BLOQUE! ---
        // Le decimos a la música que hacer
        if (musicSource != null)
        {
            if (isPaused)
            {
                // Pausa la música
                musicSource.Pause();
            }
            else
            {
                // Reanuda la música
                musicSource.UnPause();
            }
        }
    }

    void UnleashNova()
    {
        isNovaReady = false;
        currentNovaKills = 0;

        // Resetea la UI
        if (novaFillImage != null)
        {
            novaFillImage.fillAmount = 0f;
        }

        // Efectos visuales y de sonido
        if (novaEffectPrefab != null && playerTransform != null)
        {
            Instantiate(novaEffectPrefab, playerTransform.position, Quaternion.identity);
        }

        DamageFeedback feedback = FindFirstObjectByType<DamageFeedback>(); // <-- MODIFICADO (FindObjectOfType obsoleto)
        if (feedback != null && novaUnleashSFX != null)
        {
            // Le pasamos el volumen (asegúrate de que DamageFeedback tiene la función con 2 parámetros)
            feedback.PlaySFX(novaUnleashSFX, novaUnleashVolume);
        }

        // --- ¡LA LÓGICA DE MATAR! ---
        if (playerTransform == null) return;

        Collider[] hits = Physics.OverlapSphere(playerTransform.position, novaRadius);

        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                // ¡AQUÍ MARCAMOS LA MUERTE COMO "TRUE" PARA LA NOVA!
                enemy.TakeDamage(novaDamage, true); // <-- MODIFICADO
            }
        }
    }
}