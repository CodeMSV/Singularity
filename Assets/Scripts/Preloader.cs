using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class Preloader : MonoBehaviour
{
    [Header("⚠️ IMPORTANTE: Arrastra TODOS los Canvas Prefabs")]
    [SerializeField] private GameObject[] uiPrefabsToPrewarm;

    [Header("Configuración de Carga")]
    [SerializeField] private float minDisplayTime = 3.0f;
    [SerializeField] private string nextSceneName = "Menu";

    [Header("Configuración de Precarga")]
    [Tooltip("Tiempo que cada Canvas permanece activo (0.05-0.1 recomendado)")]
    [SerializeField] private float holdTimePerPrefab = 0.05f;
    [Tooltip("Frames extra de espera (1-2 suficiente)")]
    [SerializeField] private int extraFramesToWait = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Logo de Carga (Opcional)")]
    [SerializeField] private Canvas loadingLogoCanvas;
    [Tooltip("Texto que muestra el progreso (opcional)")]
    [SerializeField] private Text loadingText;

    private Canvas prewarmCanvas;
    private int totalPrewarmed = 0;

    void Start()
    {
        Time.timeScale = 1f;
        
        Log("========================================");
        Log("PRELOADER INICIADO");
        Log($"Escena actual: {SceneManager.GetActiveScene().name}");
        Log($"Escena objetivo: {nextSceneName}");
        Log($"Prefabs a precargar: {(uiPrefabsToPrewarm != null ? uiPrefabsToPrewarm.Length : 0)}");
        Log("========================================");
        
        if (uiPrefabsToPrewarm == null || uiPrefabsToPrewarm.Length == 0)
        {
            LogError("⚠️ ARRAY DE PREFABS VACÍO!");
        }
        
        if (!SceneExists(nextSceneName))
        {
            LogError($"❌ La escena '{nextSceneName}' NO está en Build Settings!");
            return;
        }
        
        CreatePrewarmCanvas();
        StartCoroutine(LoadGameFlow());
    }

    void CreatePrewarmCanvas()
    {
        GameObject canvasObj = new GameObject("_PrewarmCanvas_");
        prewarmCanvas = canvasObj.AddComponent<Canvas>();
        prewarmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        prewarmCanvas.sortingOrder = -9999;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        CanvasGroup group = canvasObj.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        
        Log("✓ Canvas de precarga creado");
    }

    IEnumerator LoadGameFlow()
    {
        float startTime = Time.realtimeSinceStartup;

        Log(">>> INICIANDO PRECARGA DE UI <<<");
        yield return PrewarmAllUI();
        
        float prewarmTime = Time.realtimeSinceStartup - startTime;
        Log($"✓✓✓ PRECARGA COMPLETADA en {prewarmTime:F2}s");
        Log($"Total precargado: {totalPrewarmed} prefabs");

        float remainingTime = minDisplayTime - prewarmTime;
        
        if (remainingTime > 0)
        {
            Log($"Esperando {remainingTime:F2}s...");
            float timer = 0;
            while (timer < remainingTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (prewarmCanvas != null)
        {
            Destroy(prewarmCanvas.gameObject);
        }

        Log($">>> CARGANDO ESCENA: {nextSceneName} <<<");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;
        
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        
        Log("✓ Escena lista, activando...");
        asyncLoad.allowSceneActivation = true;
    }

    IEnumerator PrewarmAllUI()
    {
        if (uiPrefabsToPrewarm == null || uiPrefabsToPrewarm.Length == 0)
        {
            LogWarning("No hay prefabs!");
            yield break;
        }

        totalPrewarmed = 0;

        for (int i = 0; i < uiPrefabsToPrewarm.Length; i++)
        {
            GameObject prefab = uiPrefabsToPrewarm[i];
            
            if (prefab == null)
            {
                LogWarning($"Prefab #{i} es NULL");
                continue;
            }

            Log($"[{i+1}/{uiPrefabsToPrewarm.Length}] Precargando: {prefab.name}");
            
            float prefabStartTime = Time.realtimeSinceStartup;
            
            // Actualizar texto de progreso (opcional)
            if (loadingText != null)
            {
                loadingText.text = $"Cargando... {i+1}/{uiPrefabsToPrewarm.Length}";
            }

            GameObject instance = Instantiate(prefab, prewarmCanvas.transform);
            instance.name = $"[PREWARM] {prefab.name}";
            instance.SetActive(true);
            
            Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas c in canvases)
            {
                if (c != null) c.enabled = true;
            }
            
            Canvas.ForceUpdateCanvases();
            ForceLoadGraphics(instance);
            
            yield return null;
            
            for (int f = 0; f < extraFramesToWait; f++)
            {
                yield return null;
            }
            
            if (holdTimePerPrefab > 0)
            {
                float timer = 0;
                while (timer < holdTimePerPrefab)
                {
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            
            float prefabTime = Time.realtimeSinceStartup - prefabStartTime;
            Log($"  ✓ {prefab.name} OK (tardó {prefabTime:F3}s)");
            totalPrewarmed++;
            
            Destroy(instance);
        }
    }

    void ForceLoadGraphics(GameObject obj)
    {
        int total = 0;

        Image[] images = obj.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img != null)
            {
                img.enabled = true;
                var s = img.sprite;
                var m = img.material;
                if (m != null) m.SetPass(0);
                total++;
            }
        }

        RawImage[] raws = obj.GetComponentsInChildren<RawImage>(true);
        foreach (RawImage raw in raws)
        {
            if (raw != null)
            {
                raw.enabled = true;
                var t = raw.texture;
                total++;
            }
        }

        TextMeshProUGUI[] tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps)
        {
            if (tmp != null)
            {
                tmp.enabled = true;
                tmp.ForceMeshUpdate(true, true);
                total++;
            }
        }

        Text[] texts = obj.GetComponentsInChildren<Text>(true);
        foreach (Text txt in texts)
        {
            if (txt != null)
            {
                txt.enabled = true;
                total++;
            }
        }

        Button[] btns = obj.GetComponentsInChildren<Button>(true);
        foreach (Button btn in btns)
        {
            if (btn != null)
            {
                btn.enabled = true;
                var tg = btn.targetGraphic;
                total++;
            }
        }

        Animator[] anims = obj.GetComponentsInChildren<Animator>(true);
        foreach (Animator anim in anims)
        {
            if (anim != null)
            {
                anim.enabled = true;
                var ctrl = anim.runtimeAnimatorController;
                total++;
            }
        }

        Log($"    → Componentes: {total}");
    }

    bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (name == sceneName)
            {
                Log($"✓ Escena '{sceneName}' encontrada");
                return true;
            }
        }
        return false;
    }

    void Log(string msg)
    {
        if (showDebugLogs) Debug.Log($"[Preloader] {msg}");
    }

    void LogWarning(string msg)
    {
        if (showDebugLogs) Debug.LogWarning($"[Preloader] {msg}");
    }

    void LogError(string msg)
    {
        Debug.LogError($"[Preloader] {msg}");
    }

    #if UNITY_EDITOR
    void OnValidate()
    {
        if (minDisplayTime < 0) minDisplayTime = 0;
        if (holdTimePerPrefab < 0) holdTimePerPrefab = 0;
        if (extraFramesToWait < 0) extraFramesToWait = 0;
    }
    #endif
}