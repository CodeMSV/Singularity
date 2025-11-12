using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.AI;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Etapas Temporales (Eventos)")]
    [SerializeField] private List<TemporalStage> stages;

    [Header("Conexión HUD")]
    [SerializeField] private TextMeshProUGUI hudStageText;

    [Header("Optimización")]
    [SerializeField] private float frameTimeBudgetMs = 4f; // Más agresivo
    [SerializeField] private int maxEnemiesPerFrame = 1; // SOLO 1 enemigo por frame
    [SerializeField] private float delayBetweenSpawns = 0.05f; // Delay adicional entre spawns
    
    [Header("Configuración de Spawn")]
    [SerializeField] private float minSpawnDistance = 20f;
    [SerializeField] private float maxSpawnDistance = 25f;
    [SerializeField] private float navMeshSampleDistance = 5f;
    [SerializeField] private int maxSpawnAttemptsPerEnemy = 1; // Reducido de 5 a 3

    [System.Serializable]
    public class TemporalStage
    {
        public string stageName;
        public float triggerTimestamp;
        public float goteoRateForThisStage = 3f;
        public List<EnemyGroup> goteoContent;
        [HideInInspector] public bool hasBeenTriggered = false;
    }

    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int amount;
    }

    // --- Variables privadas ---
    private Transform playerTransform;
    private Coroutine currentGoteoLoop;
    private int currentStageIndex = 0;
    private float gameTimeClock = 0f;
    private bool isPaused = false;

    // Pool para reutilizar Vector3 y reducir asignaciones
    private readonly Vector3[] spawnPositionCache = new Vector3[10];
    private int cacheIndex = 0;

    // --- Propiedades públicas ---
    public float GameTime => gameTimeClock;
    public int CurrentStageIndex => currentStageIndex;
    public bool IsSpawning => currentGoteoLoop != null;

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeSpawner();
    }

    void FixedUpdate()
    {
        // Solo avanza el reloj si el juego no está pausado
        if (!isPaused && Time.timeScale > 0f)
        {
            gameTimeClock += Time.fixedDeltaTime;
        }
    }

    void Update()
    {
        if (isPaused || playerTransform == null) return;
        
        CheckAndTriggerNextStage();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Initialization

    private void InitializeSpawner()
    {
        // Buscar jugador
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogError("EnemySpawner: No se encontró GameObject con tag 'Player'");
        }

        // Ordenar etapas por timestamp
        if (stages != null && stages.Count > 0)
        {
            stages = stages.OrderBy(stage => stage.triggerTimestamp).ToList();
            ValidateStages();
        }
        else
        {
            Debug.LogWarning("EnemySpawner: No hay etapas configuradas");
        }

        ResetSpawner();
    }

    private void ValidateStages()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            
            if (string.IsNullOrEmpty(stage.stageName))
            {
                Debug.LogWarning($"EnemySpawner: Etapa {i} no tiene nombre");
            }
            
            if (stage.goteoContent == null || stage.goteoContent.Count == 0)
            {
                Debug.LogWarning($"EnemySpawner: Etapa '{stage.stageName}' no tiene contenido de goteo");
            }
            else
            {
                foreach (var group in stage.goteoContent)
                {
                    if (group.enemyPrefab == null)
                    {
                        Debug.LogError($"EnemySpawner: Grupo en etapa '{stage.stageName}' tiene prefab nulo");
                    }
                }
            }
        }
    }

    #endregion

    #region Public Methods

    public void ResetSpawner()
    {
        Debug.Log("EnemySpawner: Reseteando spawner");
        
        gameTimeClock = 0f;
        currentStageIndex = 0;
        isPaused = false;

        StopCurrentGoteoLoop();

        // Resetear flags de etapas
        foreach (var stage in stages)
        {
            stage.hasBeenTriggered = false;
        }

        UpdateHUD("Iniciando");
    }

    public void PauseSpawner()
    {
        isPaused = true;
        StopCurrentGoteoLoop();
        Debug.Log("EnemySpawner: Pausado");
    }

    public void ResumeSpawner()
    {
        isPaused = false;
        
        // Reanudar la etapa actual si existe
        if (currentStageIndex > 0 && currentStageIndex <= stages.Count)
        {
            var currentStage = stages[currentStageIndex - 1];
            if (currentStage.hasBeenTriggered)
            {
                StartGoteoLoop(currentStage);
            }
        }
        
        Debug.Log("EnemySpawner: Reanudado");
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
            Debug.LogWarning($"EnemySpawner: Intentando activar etapa '{stage.stageName}' que ya fue activada");
            return;
        }

        stage.hasBeenTriggered = true;
        UpdateHUD(stage.stageName);
        Debug.Log($"EnemySpawner: Etapa activada '{stage.stageName}' en t={gameTimeClock:F2}s");

        StartGoteoLoop(stage);
    }

    private void StartGoteoLoop(TemporalStage stage)
    {
        StopCurrentGoteoLoop();
        currentGoteoLoop = StartCoroutine(SpawnGoteoLoop_ForStage(stage));
    }

    private IEnumerator SpawnInitialWaveDelayed(TemporalStage stage)
    {
        // Esperar un frame antes de empezar
        yield return null;
        
        // Spawnear la primera oleada de forma optimizada
        if (stage.goteoContent != null && stage.goteoContent.Count > 0)
        {
            yield return StartCoroutine(SpawnWave_Optimized(stage.goteoContent));
        }
    }

    private void StopCurrentGoteoLoop()
    {
        if (currentGoteoLoop != null)
        {
            StopCoroutine(currentGoteoLoop);
            currentGoteoLoop = null;
        }
    }

    #endregion

    #region Spawning Coroutines

    private IEnumerator SpawnGoteoLoop_ForStage(TemporalStage stage)
    {
        // Primera oleada inmediata pero distribuida en frames
        yield return StartCoroutine(SpawnWave_Optimized(stage.goteoContent));
        
        // Luego continuar con el ciclo normal
        while (!isPaused)
        {
            yield return new WaitForSeconds(stage.goteoRateForThisStage);
            
            if (!isPaused && stage.goteoContent != null)
            {
                yield return StartCoroutine(SpawnWave_Optimized(stage.goteoContent));
            }
        }
    }

    private IEnumerator SpawnWave_Optimized(List<EnemyGroup> goteoContent)
    {
        foreach (var group in goteoContent)
        {
            if (group.enemyPrefab == null) continue;

            for (int i = 0; i < group.amount; i++)
            {
                SpawnEnemy(group.enemyPrefab);
                
                // Ceder control después de CADA spawn individual
                yield return new WaitForSeconds(delayBetweenSpawns);
            }
        }
    }

    #endregion

    #region Spawn Logic

    private void SpawnEnemy(GameObject enemyPrefab)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("EnemySpawner: PlayerTransform es null, no se puede spawnear enemigo");
            return;
        }

        Vector3 spawnPosition;
        bool validPositionFound = TryGetValidSpawnPosition(out spawnPosition);

        if (validPositionFound)
        {
            // Verificación final: asegurar que NO está demasiado cerca del player
            float distanceToPlayer = Vector3.Distance(spawnPosition, playerTransform.position);
            
            if (distanceToPlayer >= minSpawnDistance)
            {
                Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"EnemySpawner: Posición muy cerca del jugador ({distanceToPlayer:F1}m), spawn cancelado");
            }
        }
        else
        {
            Debug.LogWarning("EnemySpawner: No se pudo encontrar posición válida tras múltiples intentos");
        }
    }

    private bool TryGetValidSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;

        for (int attempt = 0; attempt < maxSpawnAttemptsPerEnemy; attempt++)
        {
            // Calcular posición aleatoria en un anillo alrededor del jugador
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            
            Vector3 targetPosition = playerTransform.position + offset;

            // Buscar en el NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                // Verificación FINAL de distancia
                float finalDistance = Vector3.Distance(hit.position, playerTransform.position);
                if (finalDistance >= minSpawnDistance)
                {
                    position = hit.position;
                    return true;
                }
            }
        }

        return false;
    }

    private Vector3 GetRandomPositionAroundPlayer()
    {
        // Esta función ya no es necesaria pero la mantengo por compatibilidad
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
        
        return playerTransform.position + offset;
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
    [ContextMenu("Debug: Mostrar Info Actual")]
    private void DebugShowCurrentInfo()
    {
        Debug.Log($"=== EnemySpawner Info ===\n" +
                  $"Tiempo de juego: {gameTimeClock:F2}s\n" +
                  $"Etapa actual: {currentStageIndex}/{stages.Count}\n" +
                  $"Pausado: {isPaused}\n" +
                  $"Spawneando: {IsSpawning}");
    }

    [ContextMenu("Debug: Forzar Siguiente Etapa")]
    private void DebugForceNextStage()
    {
        ForceNextStage();
    }
#endif

    #endregion
}