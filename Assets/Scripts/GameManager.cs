using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const float GAME_OVER_MUSIC_VOLUME = 0.1f;
    private const float GAME_OVER_DELAY = 0.5f;
    private const float PAUSED_TIME_SCALE = 0f;
    private const float NORMAL_TIME_SCALE = 1f;
    private const string PLAYER_TAG = "Player";

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

    [Header("High Scores System")]
    [SerializeField] private HighScoresData highScoresData; 
    [SerializeField] private HighScoresDisplay highScoresDisplay; 

    [Header("Game Over UI")]
    [SerializeField] private AudioSource musicSource;

    [Header("Debug Panels")]
    [SerializeField] private GameObject panelDePruebaRojo;
    [SerializeField] private GameObject panelDePruebaVerde;

    private Transform playerTransform;
    private int killCount;
    private int currentNovaKills;
    private bool isGameOver;
    private bool isPaused;
    private bool isNovaReady;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        if (highScoresData != null) highScoresData.LoadScores();

        InitializePlayer();
        InitializeUI();
        Time.timeScale = NORMAL_TIME_SCALE;
    }

    private void Update()
    {
        if (!isGameOver && Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (isNovaReady && Input.GetMouseButtonDown(1)) UnleashNova();
    }

    private void InitializePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (player != null) playerTransform = player.transform;
    }

    private void InitializeUI()
    {
        if(gameOverPanel) gameOverPanel.SetActive(false);
        if(pausePanel) pausePanel.SetActive(false);
        if(panelDePruebaRojo) panelDePruebaRojo.SetActive(false);
        if(panelDePruebaVerde) panelDePruebaVerde.SetActive(false);
    }

    public void AddKill(bool fromNova = false)
    {
        if (isGameOver) return;
        killCount++;
        if (hudKillsText) hudKillsText.text = $"KILLS: {killCount}";
        if (!isNovaReady && !fromNova) ChargeNova();
    }

    private void ChargeNova()
    {
        currentNovaKills++;
        if (novaFillImage) novaFillImage.fillAmount = (float)currentNovaKills / novaKillsThreshold;
        if (currentNovaKills >= novaKillsThreshold)
        {
            isNovaReady = true;
            PlaySound(novaReadySFX);
        }
    }

    private void UnleashNova()
    {
        isNovaReady = false;
        currentNovaKills = 0;
        if (novaFillImage) novaFillImage.fillAmount = 0f;
        if (novaEffectPrefab && playerTransform) Instantiate(novaEffectPrefab, playerTransform.position, Quaternion.identity);
        PlaySound(novaUnleashSFX, novaUnleashVolume);

        if (playerTransform)
        {
            var hits = Physics.OverlapSphere(playerTransform.position, novaRadius);
            foreach (var hit in hits) hit.GetComponent<Enemy>()?.TakeDamage(novaDamage, true);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        if (musicSource) musicSource.volume = GAME_OVER_MUSIC_VOLUME;

        if(gameOverPanel) gameOverPanel.SetActive(true);
        if(panelDePruebaRojo) panelDePruebaRojo.SetActive(true);
        if(panelDePruebaVerde) panelDePruebaVerde.SetActive(true);
        
        if (highScoresData != null && highScoresDisplay != null)
        {
            bool isRecord = highScoresData.CheckForNewHighScore(killCount);
            highScoresDisplay.Setup(killCount, isRecord);
        }

        yield return new WaitForSecondsRealtime(GAME_OVER_DELAY);
        Time.timeScale = PAUSED_TIME_SCALE;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if(pausePanel) pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? PAUSED_TIME_SCALE : NORMAL_TIME_SCALE;
        if (musicSource) { if (isPaused) musicSource.Pause(); else musicSource.UnPause(); }
    }

    public void ReloadScene()
    {
        Time.timeScale = NORMAL_TIME_SCALE;
        if (EnemySpawner.Instance) EnemySpawner.Instance.ResetSpawner();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        var fb = FindFirstObjectByType<DamageFeedback>();
        if (fb && clip) fb.PlaySFX(clip, volume);
    }
}