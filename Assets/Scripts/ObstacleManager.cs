using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ObstacleManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(50f, 50f);
    [SerializeField] private LayerMask obstacleLayer; 

    [Header("Configuración Aleatoria Global")]
    [SerializeField] private int minObstacles = 3;
    [SerializeField] private int maxObstacles = 10;
    [SerializeField] private float minSpawnDelay = 0.2f;
    [SerializeField] private float maxSpawnDelay = 2.0f;
    [SerializeField] private float obstacleLifeTime = 12f;
    [SerializeField] private Vector2 sizeRange = new Vector2(2f, 7f);

    private int lastProcessedStage = -1;

    private void Update()
    {
        CheckEnemySpawnerPhase();
    }

    private void CheckEnemySpawnerPhase()
    {
        if (EnemySpawner.Instance == null) return;

        int currentStage = EnemySpawner.Instance.CurrentStageIndex;

        if (currentStage > lastProcessedStage)
        {
            if (currentStage > 0)
            {
                StartCoroutine(SpawnRoutine());
            }
            lastProcessedStage = currentStage;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        int totalToSpawn = Random.Range(minObstacles, maxObstacles + 1);

        for (int i = 0; i < totalToSpawn; i++)
        {
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
            
            TrySpawnObstacle();
        }
    }

    private void TrySpawnObstacle()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 candidatePos = GetRandomNavMeshPosition();
            
            if (candidatePos == Vector3.zero) continue;

            float randomScale = Random.Range(sizeRange.x, sizeRange.y);
            Vector3 size = new Vector3(randomScale, randomScale * 2f, randomScale);

            if (!Physics.CheckBox(candidatePos + new Vector3(0, size.y/2, 0), size / 2, Quaternion.identity, obstacleLayer))
            {
                SpawnObstacleAt(candidatePos, size);
                return; 
            }
        }
    }

    private void SpawnObstacleAt(Vector3 position, Vector3 size)
    {
        GameObject obj = Instantiate(obstaclePrefab);
        obj.tag = "Obstacle";

        RisingObstacle obstacleScript = obj.GetComponent<RisingObstacle>();
        if (obstacleScript != null)
        {
            obstacleScript.Initialize(obstacleLifeTime, position, size);
        }
    }

    private Vector3 GetRandomNavMeshPosition()
    {
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomZ = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        
        Vector3 randomPoint = new Vector3(randomX, 0f, randomZ);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize.x, 1f, spawnAreaSize.y));
    }
}