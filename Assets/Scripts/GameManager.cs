using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Gestiona el estado global del juego: kills, poder Nova, pausa y game over.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Constants
    private const float GAME_OVER_MUSIC_VOLUME = 0.1f;
    private const float GAME_OVER_DELAY = 0.05f;
    private const float PAUSED_TIME_SCALE = 0f;
    private const float NORMAL_TIME_SCALE = 1f;
    private const string PLAYER_TAG = "Player";
    #endregion

    #region Serialized Fields
    [Header("Nova Power")]
    [SerializeField] private Image novaFillImage;
    [SerializeField, Min(1)] private int novaKillsThreshold = 30;
    [SerializeField, Min(0f)] private float novaRadius = 15f;
    [SerializeField, Min(0f)] private float novaDamage = 9999f;
    [SerializeField, Range(0f, 3f)] private float novaUnleashVolume = 1.5f;
    [SerializeField] private GameObject novaEffectPrefab;
    [SerializeField] private AudioClip novaReadySFX;
    [SerializeField] private AudioClip novaUnleashSFX;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI hudKillsText;

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Game Over UI")]
    [SerializeField] private TextMeshProUGUI killCountText;
    [SerializeField] private AudioSource musicSource;

    [Header("Debug Panels (Optional)")]
    [SerializeField] private GameObject panelDePruebaRojo;
    [SerializeField] private GameObject panelDePruebaVerde;
    #endregion

    #region Private Fields
    private Transform playerTransform;
    private int killCount;
    private int currentNovaKills;
    private bool isGameOver;
    private bool isPaused;
    private bool isNovaReady;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (!InitializeSingleton()) return;
        
        InitializePlayer();
        InitializeUI();
        ResetTimeScale();
    }

    private void Update()
    {
        HandlePauseInput();
        HandleNovaInput();
    }
    #endregion

    #region Initialization
    private bool InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            return true;
        }

        Destroy(gameObject);
        return false;
    }

    private void InitializePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("GameManager: Player not found. Ensure player has 'Player' tag.", this);
        }
    }

    private void InitializeUI()
    {
        HidePanel(gameOverPanel);
        HidePanel(pausePanel);
        HidePanel(panelDePruebaRojo);
        HidePanel(panelDePruebaVerde);
    }

    private void HidePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void ResetTimeScale()
    {
        Time.timeScale = NORMAL_TIME_SCALE;
    }
    #endregion

    #region Input Handling
    private void HandlePauseInput()
    {
        if (!isGameOver && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void HandleNovaInput()
    {
        if (isNovaReady && Input.GetMouseButtonDown(1))
        {
            UnleashNova();
        }
    }
    #endregion

    #region Kill Tracking
    public void AddKill(bool fromNova = false)
    {
        if (isGameOver) return;

        killCount++;
        UpdateKillsHUD();

        if (ShouldChargeNova(fromNova))
        {
            ChargeNova();
        }
    }

    private bool ShouldChargeNova(bool fromNova)
    {
        return !isNovaReady && !fromNova;
    }

    private void ChargeNova()
    {
        currentNovaKills++;
        UpdateNovaFillBar();

        if (currentNovaKills >= novaKillsThreshold)
        {
            ActivateNova();
        }
    }

    private void UpdateKillsHUD()
    {
        if (hudKillsText != null)
        {
            hudKillsText.text = $"KILLS: {killCount}";
        }
    }

    private void UpdateNovaFillBar()
    {
        if (novaFillImage != null)
        {
            float fillAmount = (float)currentNovaKills / novaKillsThreshold;
            novaFillImage.fillAmount = fillAmount;
        }
    }

    private void ActivateNova()
    {
        isNovaReady = true;
        PlayNovaReadySound();
    }

    private void PlayNovaReadySound()
    {
        DamageFeedback feedback = FindFirstObjectByType<DamageFeedback>();
        
        if (feedback != null && novaReadySFX != null)
        {
            feedback.PlaySFX(novaReadySFX);
        }
    }
    #endregion

    #region Nova Power
    private void UnleashNova()
    {
        ResetNovaCharge();
        PlayNovaEffects();
        DamageEnemiesInRadius();
    }

    private void ResetNovaCharge()
    {
        isNovaReady = false;
        currentNovaKills = 0;

        if (novaFillImage != null)
        {
            novaFillImage.fillAmount = 0f;
        }
    }

    private void PlayNovaEffects()
    {
        SpawnNovaVisualEffect();
        PlayNovaSound();
    }

    private void SpawnNovaVisualEffect()
    {
        if (novaEffectPrefab != null && playerTransform != null)
        {
            Instantiate(novaEffectPrefab, playerTransform.position, Quaternion.identity);
        }
    }

    private void PlayNovaSound()
    {
        DamageFeedback feedback = FindFirstObjectByType<DamageFeedback>();
        
        if (feedback != null && novaUnleashSFX != null)
        {
            feedback.PlaySFX(novaUnleashSFX, novaUnleashVolume);
        }
    }

    private void DamageEnemiesInRadius()
    {
        if (playerTransform == null) return;

        Collider[] hits = Physics.OverlapSphere(playerTransform.position, novaRadius);

        foreach (Collider hit in hits)
        {
            TryDamageEnemy(hit);
        }
    }

    private void TryDamageEnemy(Collider collider)
    {
        Enemy enemy = collider.GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.TakeDamage(novaDamage, isNovaKill: true);
        }
    }
    #endregion

    #region Game Over
    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        LowerMusicVolume();
        ShowGameOverUI();
        
        yield return new WaitForSecondsRealtime(GAME_OVER_DELAY);
        
        FreezeGame();
    }

    private void LowerMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = GAME_OVER_MUSIC_VOLUME;
        }
    }

    private void ShowGameOverUI()
    {
        UpdateFinalKillCount();
        ShowPanel(gameOverPanel);
        ShowPanel(panelDePruebaRojo);
        ShowPanel(panelDePruebaVerde);
    }

    private void UpdateFinalKillCount()
    {
        if (killCountText != null)
        {
            killCountText.text = $"ENEMIGOS ELIMINADOS: {killCount}";
        }
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void FreezeGame()
    {
        Time.timeScale = PAUSED_TIME_SCALE;
    }
    #endregion

    #region Pause
    public void TogglePause()
    {
        isPaused = !isPaused;

        UpdatePauseUI();
        UpdateTimeScale();
        UpdateMusicState();
    }

    private void UpdatePauseUI()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
        }
    }

    private void UpdateTimeScale()
    {
        Time.timeScale = isPaused ? PAUSED_TIME_SCALE : NORMAL_TIME_SCALE;
    }

    private void UpdateMusicState()
    {
        if (musicSource == null) return;

        if (isPaused)
        {
            musicSource.Pause();
        }
        else
        {
            musicSource.UnPause();
        }
    }
    #endregion

    #region Scene Management
    public void ReloadScene()
    {
        Time.timeScale = NORMAL_TIME_SCALE;
        ResetEnemySpawner();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ResetEnemySpawner()
    {
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.ResetSpawner();
        }
    }
    #endregion
}