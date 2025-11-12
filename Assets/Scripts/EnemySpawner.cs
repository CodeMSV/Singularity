using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.AI;
using System.Linq;

/// <summary>
/// Sistema de spawn de enemigos por etapas temporales. Gestiona oleadas progresivas
/// con spawning optimizado y distribuido en frames.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region Singleton
    public static EnemySpawner Instance { get; private set; }
    #endregion

    #region Constants
    private const string PLAYER_TAG = "Player";
    private const string INITIAL_STAGE_NAME = "Iniciando";
    private const int SPAWN_POSITION_CACHE_SIZE = 10;
    #endregion

    #region Serialized Fields
    [Header("Stages")]
    [SerializeField] private List<TemporalStage> stages;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hudStageText;

    [Header("Performance")]
    [SerializeField, Min(0f)] private float frameTimeBudgetMs = 4f;
    [SerializeField, Min(1)] private int maxEnemiesPerFrame = 1;
    [SerializeField, Min(0f)] private float delayBetweenSpawns = 0.05f;
    
    [Header("Spawn Settings")]
    [SerializeField, Min(0f)] private float minSpawnDistance = 20f;
    [SerializeField, Min(0f)] private float maxSpawnDistance = 25f;
    [SerializeField, Min(0f)] private float navMeshSampleDistance = 5f;
    [SerializeField, Min(1)] private int maxSpawnAttemptsPerEnemy = 3;
    #endregion

    #region Private Fields
    private Transform playerTransform;
    private Coroutine currentGoteoLoop;
    private int currentStageIndex;
    private float gameTimeClock;
    private bool isPaused;
    private readonly Vector3[] spawnPositionCache = new Vector3[SPAWN_POSITION_CACHE_SIZE];
    private int cacheIndex;
    #endregion

    #region Properties
    public float GameTime => gameTimeClock;
    public int CurrentStageIndex => currentStageIndex;
    public bool IsSpawning => currentGoteoLoop != null;
    #endregion

    #region Nested Classes
    [System.Serializable]
    public class TemporalStage
    {
        public string stageName;
        public float triggerTimestamp;
        public float goteoRateForThisStage = 3f;
        public List<EnemyGroup> goteoContent;
        [HideInInspector] public bool hasBeenTriggered;
    }

    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int amount;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeSpawner();
    }

    private void FixedUpdate()
    {
        UpdateGameClock();
    }

    private void Update()
    {
        if (isPaused || playerTransform == null) return;
        
        CheckAndTriggerNextStage();
    }

    private void OnDestroy()
    {
        CleanupSingleton();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSpawner()
    {
        FindPlayer();
        PrepareStages();
        ResetSpawner();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("EnemySpawner: Player GameObject not found. Ensure it has 'Player' tag.", this);
        }
    }

    private void PrepareStages()
    {
        if (stages == null || stages.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: No stages configured", this);
            return;
        }

        stages = stages.OrderBy(stage => stage.triggerTimestamp).ToList();
        ValidateStages();
    }

    private void ValidateStages()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            ValidateStage(stages[i], i);
        }
    }

    private void ValidateStage(TemporalStage stage, int index)
    {
        if (string.IsNullOrEmpty(stage.stageName))
        {
            Debug.LogWarning($"EnemySpawner: Stage {index} has no name", this);
        }
        
        if (stage.goteoContent == null || stage.goteoContent.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner: Stage '{stage.stageName}' has no content", this);
            return;
        }

        ValidateStageContent(stage);
    }

    private void ValidateStageContent(TemporalStage stage)
    {
        foreach (var group in stage.goteoContent)
        {
            if (group.enemyPrefab == null)
            {
                Debug.LogError($"EnemySpawner: Group in stage '{stage.stageName}' has null prefab", this);
            }
        }
    }

    private void CleanupSingleton()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region Game Clock
    private void UpdateGameClock()
    {
        if (!isPaused && Time.timeScale > 0f)
        {
            gameTimeClock += Time.fixedDeltaTime;
        }
    }
    #endregion

    #region Public Control Methods
    public void ResetSpawner()
    {
        gameTimeClock = 0f;
        currentStageIndex = 0;
        isPaused = false;

        StopCurrentGoteoLoop();
        ResetStageFlags();
        UpdateHUD(INITIAL_STAGE_NAME);
    }

    public void PauseSpawner()
    {
        isPaused = true;
        StopCurrentGoteoLoop();
    }

    public void ResumeSpawner()
    {
        isPaused = false;
        ResumeCurrentStage();
    }

    public void SetGameTime(float time)
    {
        gameTimeClock = time;
    }

    public void ForceNextStage()
    {
        if (currentStageIndex < stages.Count)
        {
            TriggerStage(stages[currentStageIndex]);
            currentStageIndex++;
        }
    }
    #endregion

    #region Stage Management
    private void CheckAndTriggerNextStage()
    {
        if (currentStageIndex >= stages.Count) return;

        TemporalStage nextStage = stages[currentStageIndex];
        
        if (gameTimeClock >= nextStage.triggerTimestamp)
        {
            TriggerStage(nextStage);
            currentStageIndex++;
        }
    }

    private void TriggerStage(TemporalStage stage)
    {
        if (stage.hasBeenTriggered)
        {
            Debug.LogWarning($"EnemySpawner: Stage '{stage.stageName}' already triggered", this);
            return;
        }

        stage.hasBeenTriggered = true;
        UpdateHUD(stage.stageName);
        StartGoteoLoop(stage);
    }

    private void StartGoteoLoop(TemporalStage stage)
    {
        StopCurrentGoteoLoop();
        currentGoteoLoop = StartCoroutine(SpawnGoteoLoop(stage));
    }

    private void StopCurrentGoteoLoop()
    {
        if (currentGoteoLoop != null)
        {
            StopCoroutine(currentGoteoLoop);
            currentGoteoLoop = null;
        }
    }

    private void ResetStageFlags()
    {
        foreach (var stage in stages)
        {
            stage.hasBeenTriggered = false;
        }
    }

    private void ResumeCurrentStage()
    {
        if (currentStageIndex > 0 && currentStageIndex <= stages.Count)
        {
            var currentStage = stages[currentStageIndex - 1];
            if (currentStage.hasBeenTriggered)
            {
                StartGoteoLoop(currentStage);
            }
        }
    }
    #endregion

    #region Spawning Coroutines
    private IEnumerator SpawnGoteoLoop(TemporalStage stage)
    {
        yield return StartCoroutine(SpawnWaveOptimized(stage.goteoContent));
        
        while (!isPaused)
        {
            yield return new WaitForSeconds(stage.goteoRateForThisStage);
            
            if (!isPaused && stage.goteoContent != null)
            {
                yield return StartCoroutine(SpawnWaveOptimized(stage.goteoContent));
            }
        }
    }

    private IEnumerator SpawnWaveOptimized(List<EnemyGroup> content)
    {
        foreach (var group in content)
        {
            if (group.enemyPrefab == null) continue;

            for (int i = 0; i < group.amount; i++)
            {
                SpawnEnemy(group.enemyPrefab);
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }
    }
    #endregion

    #region Spawn Logic
    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (!CanSpawn()) return;

        if (TryGetValidSpawnPosition(out Vector3 spawnPosition))
        {
            if (IsValidDistanceFromPlayer(spawnPosition))
            {
                Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }

    private bool CanSpawn()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("EnemySpawner: Cannot spawn, player transform is null", this);
            return false;
        }
        return true;
    }

    private bool TryGetValidSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;

        for (int attempt = 0; attempt < maxSpawnAttemptsPerEnemy; attempt++)
        {
            Vector3 targetPosition = CalculateRandomPositionAroundPlayer();

            if (TryFindNavMeshPosition(targetPosition, out Vector3 navMeshPosition))
            {
                if (IsValidDistanceFromPlayer(navMeshPosition))
                {
                    position = navMeshPosition;
                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 CalculateRandomPositionAroundPlayer()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
        
        return playerTransform.position + offset;
    }

    private bool TryFindNavMeshPosition(Vector3 targetPosition, out Vector3 navMeshPosition)
    {
        navMeshPosition = Vector3.zero;

        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            navMeshPosition = hit.position;
            return true;
        }

        return false;
    }

    private bool IsValidDistanceFromPlayer(Vector3 position)
    {
        float distance = Vector3.Distance(position, playerTransform.position);
        return distance >= minSpawnDistance;
    }
    #endregion

    #region UI
    private void UpdateHUD(string stageName)
    {
        if (hudStageText != null)
        {
            hudStageText.text = $"ETAPA: {stageName}";
        }
    }
    #endregion

    #region Debug Helpers
#if UNITY_EDITOR
    [ContextMenu("Debug: Show Current Info")]
    private void DebugShowCurrentInfo()
    {
        Debug.Log($"=== EnemySpawner Info ===\n" +
                  $"Game Time: {gameTimeClock:F2}s\n" +
                  $"Current Stage: {currentStageIndex}/{stages.Count}\n" +
                  $"Paused: {isPaused}\n" +
                  $"Spawning: {IsSpawning}", this);
    }

    [ContextMenu("Debug: Force Next Stage")]
    private void DebugForceNextStage()
    {
        ForceNextStage();
    }

    [ContextMenu("Debug: Reset Spawner")]
    private void DebugResetSpawner()
    {
        ResetSpawner();
    }
#endif
    #endregion
}