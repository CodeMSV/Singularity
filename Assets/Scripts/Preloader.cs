using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Preloader : MonoBehaviour
{
    [Header("UI Prefabs Configuration")]
    [SerializeField] private GameObject[] uiPrefabsToPrewarm;

    [Header("Loading Settings")]
    [SerializeField] private float minDisplayTime = 3.0f;
    [SerializeField] private string nextSceneName = "Menu";

    [Header("Preload Settings")]
    [Tooltip("Time each Canvas remains active (0.05-0.1 recommended)")]
    [SerializeField] private float holdTimePerPrefab = 0.05f;
    [Tooltip("Extra frames to wait (1-2 sufficient)")]
    [SerializeField] private int extraFramesToWait = 1;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Loading UI (Optional)")]
    [SerializeField] private Canvas loadingLogoCanvas;
    [SerializeField] private Text loadingText;

    private Canvas prewarmCanvas;
    private int totalPrewarmed;

    private const int PREWARM_SORTING_ORDER = -9999;
    private static readonly Vector2 REFERENCE_RESOLUTION = new Vector2(1920, 1080);

    private void Start()
    {
        Time.timeScale = 1f;
        
        LogHeader();
        
        if (!ValidateConfiguration())
        {
            return;
        }
        
        CreatePrewarmCanvas();
        StartCoroutine(LoadGameFlow());
    }

    private void LogHeader()
    {
        Log("========================================");
        Log("PRELOADER STARTED");
        Log($"Current scene: {SceneManager.GetActiveScene().name}");
        Log($"Target scene: {nextSceneName}");
        Log($"Prefabs to preload: {(uiPrefabsToPrewarm?.Length ?? 0)}");
        Log("========================================");
    }

    private bool ValidateConfiguration()
    {
        if (uiPrefabsToPrewarm == null || uiPrefabsToPrewarm.Length == 0)
        {
            LogError("Prefabs array is empty!");
            return false;
        }
        
        if (!SceneExists(nextSceneName))
        {
            LogError($"Scene '{nextSceneName}' is not in Build Settings!");
            return false;
        }

        return true;
    }

    private void CreatePrewarmCanvas()
    {
        GameObject canvasObj = new GameObject("_PrewarmCanvas_");
        prewarmCanvas = canvasObj.AddComponent<Canvas>();
        prewarmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        prewarmCanvas.sortingOrder = PREWARM_SORTING_ORDER;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = REFERENCE_RESOLUTION;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        CanvasGroup group = canvasObj.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        
        Log("Prewarm canvas created");
    }

    private IEnumerator LoadGameFlow()
    {
        float startTime = Time.realtimeSinceStartup;

        Log("Starting UI preload");
        yield return PrewarmAllUI();
        
        float prewarmTime = Time.realtimeSinceStartup - startTime;
        Log($"Preload completed in {prewarmTime:F2}s");
        Log($"Total preloaded: {totalPrewarmed} prefabs");

        yield return WaitForMinimumDisplayTime(startTime);

        CleanupPrewarmCanvas();

        yield return LoadTargetScene();
    }

    private IEnumerator WaitForMinimumDisplayTime(float startTime)
    {
        float elapsedTime = Time.realtimeSinceStartup - startTime;
        float remainingTime = minDisplayTime - elapsedTime;
        
        if (remainingTime > 0)
        {
            Log($"Waiting {remainingTime:F2}s...");
            float timer = 0;
            while (timer < remainingTime)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    private void CleanupPrewarmCanvas()
    {
        if (prewarmCanvas != null)
        {
            Destroy(prewarmCanvas.gameObject);
        }
    }

    private IEnumerator LoadTargetScene()
    {
        Log($"Loading scene: {nextSceneName}");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;
        
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }
        
        Log("Scene ready, activating...");
        asyncLoad.allowSceneActivation = true;
    }

    private IEnumerator PrewarmAllUI()
    {
        if (uiPrefabsToPrewarm == null || uiPrefabsToPrewarm.Length == 0)
        {
            LogWarning("No prefabs to prewarm!");
            yield break;
        }

        totalPrewarmed = 0;

        for (int i = 0; i < uiPrefabsToPrewarm.Length; i++)
        {
            yield return PrewarmSinglePrefab(uiPrefabsToPrewarm[i], i);
        }
    }

    private IEnumerator PrewarmSinglePrefab(GameObject prefab, int index)
    {
        if (prefab == null)
        {
            LogWarning($"Prefab #{index} is NULL");
            yield break;
        }

        Log($"[{index + 1}/{uiPrefabsToPrewarm.Length}] Preloading: {prefab.name}");
        
        float prefabStartTime = Time.realtimeSinceStartup;
        
        UpdateLoadingText(index);

        GameObject instance = InstantiateAndActivatePrefab(prefab);
        
        Canvas.ForceUpdateCanvases();
        ForceLoadGraphics(instance);
        
        yield return null;
        yield return WaitExtraFrames();
        yield return HoldPrefabInstance();
        
        LogPrefabCompletion(prefab, prefabStartTime);
        
        Destroy(instance);
        totalPrewarmed++;
    }

    private void UpdateLoadingText(int index)
    {
        if (loadingText != null)
        {
            loadingText.text = $"Loading... {index + 1}/{uiPrefabsToPrewarm.Length}";
        }
    }

    private GameObject InstantiateAndActivatePrefab(GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, prewarmCanvas.transform);
        instance.name = $"[PREWARM] {prefab.name}";
        instance.SetActive(true);
        
        Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }

        return instance;
    }

    private IEnumerator WaitExtraFrames()
    {
        for (int i = 0; i < extraFramesToWait; i++)
        {
            yield return null;
        }
    }

    private IEnumerator HoldPrefabInstance()
    {
        if (holdTimePerPrefab > 0)
        {
            float timer = 0;
            while (timer < holdTimePerPrefab)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    private void LogPrefabCompletion(GameObject prefab, float startTime)
    {
        float prefabTime = Time.realtimeSinceStartup - startTime;
        Log($"  {prefab.name} completed ({prefabTime:F3}s)");
    }

    private void ForceLoadGraphics(GameObject obj)
    {
        int totalComponents = 0;

        totalComponents += ForceLoadImages(obj);
        totalComponents += ForceLoadRawImages(obj);
        totalComponents += ForceLoadTextMeshPro(obj);
        totalComponents += ForceLoadTexts(obj);
        totalComponents += ForceLoadButtons(obj);
        totalComponents += ForceLoadAnimators(obj);

        Log($"    Components processed: {totalComponents}");
    }

    private int ForceLoadImages(GameObject obj)
    {
        Image[] images = obj.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img != null)
            {
                img.enabled = true;
                var sprite = img.sprite;
                var material = img.material;
                if (material != null)
                {
                    material.SetPass(0);
                }
            }
        }
        return images.Length;
    }

    private int ForceLoadRawImages(GameObject obj)
    {
        RawImage[] rawImages = obj.GetComponentsInChildren<RawImage>(true);
        foreach (RawImage raw in rawImages)
        {
            if (raw != null)
            {
                raw.enabled = true;
                var texture = raw.texture;
            }
        }
        return rawImages.Length;
    }

    private int ForceLoadTextMeshPro(GameObject obj)
    {
        TextMeshProUGUI[] textMeshes = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in textMeshes)
        {
            if (tmp != null)
            {
                tmp.enabled = true;
                tmp.ForceMeshUpdate(true, true);
            }
        }
        return textMeshes.Length;
    }

    private int ForceLoadTexts(GameObject obj)
    {
        Text[] texts = obj.GetComponentsInChildren<Text>(true);
        foreach (Text txt in texts)
        {
            if (txt != null)
            {
                txt.enabled = true;
            }
        }
        return texts.Length;
    }

    private int ForceLoadButtons(GameObject obj)
    {
        Button[] buttons = obj.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn != null)
            {
                btn.enabled = true;
                var targetGraphic = btn.targetGraphic;
            }
        }
        return buttons.Length;
    }

    private int ForceLoadAnimators(GameObject obj)
    {
        Animator[] animators = obj.GetComponentsInChildren<Animator>(true);
        foreach (Animator anim in animators)
        {
            if (anim != null)
            {
                anim.enabled = true;
                var controller = anim.runtimeAnimatorController;
            }
        }
        return animators.Length;
    }

    private bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (name == sceneName)
            {
                Log($"Scene '{sceneName}' found in build settings");
                return true;
            }
        }
        return false;
    }

    private void Log(string msg)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[Preloader] {msg}");
        }
    }

    private void LogWarning(string msg)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning($"[Preloader] {msg}");
        }
    }

    private void LogError(string msg)
    {
        Debug.LogError($"[Preloader] {msg}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minDisplayTime = Mathf.Max(0, minDisplayTime);
        holdTimePerPrefab = Mathf.Max(0, holdTimePerPrefab);
        extraFramesToWait = Mathf.Max(0, extraFramesToWait);
    }
#endif
}