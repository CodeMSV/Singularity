using UnityEngine;

/// <summary>
/// Script de testing para verificar reproducción de audio en Mac.
/// Proporciona múltiples métodos de prueba para diagnosticar problemas de audio.
/// </summary>
public class MacAudioTest : MonoBehaviour
{
    #region Constants
    private const float DEFAULT_VOLUME = 1f;
    private const float BOOSTED_VOLUME = 2f;
    private const float SPATIAL_BLEND_2D = 0f;
    private const int MAX_PRIORITY = 0;
    
    private const string FEEDBACK_MESSAGE = "¿Escuchaste algo?";
    #endregion

    #region Serialized Fields
    [Header("Audio Configuration")]
    [SerializeField, Tooltip("Clip de audio para las pruebas")]
    private AudioClip testClip;
    
    [SerializeField, Tooltip("Mostrar logs detallados en consola")]
    private bool enableVerboseLogs = true;
    #endregion

    #region Private Fields
    private AudioSource audioSource;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeAudioSource();
        
        if (enableVerboseLogs)
        {
            LogAudioSetup();
        }
        
        LogInstructions();
    }

    private void Update()
    {
        if (!ValidateTestClip()) return;

        HandleTestInputs();
    }

    private void OnDestroy()
    {
        // Cleanup: Destruir el AudioSource si fue creado dinámicamente
        if (audioSource != null && audioSource != GetComponent<AudioSource>())
        {
            Destroy(audioSource);
        }
    }
    #endregion

    #region Initialization
    private void InitializeAudioSource()
    {
        // Intentar obtener AudioSource existente o crear uno nuevo
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.volume = DEFAULT_VOLUME;
        audioSource.spatialBlend = SPATIAL_BLEND_2D;
        audioSource.priority = MAX_PRIORITY;
    }
    #endregion

    #region Validation
    private bool ValidateTestClip()
    {
        if (testClip == null)
        {
            if (enableVerboseLogs)
            {
                Debug.LogError("¡Asigna un AudioClip en el Inspector!", this);
            }
            return false;
        }
        return true;
    }
    #endregion

    #region Input Handling
    private void HandleTestInputs()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestPlayOneShot();
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            TestPlayOneShotBoosted();
        }
        else if (Input.GetKeyDown(KeyCode.Y))
        {
            TestPlayDirect();
        }
        else if (Input.GetKeyDown(KeyCode.U))
        {
            TestPlayClipAtPoint();
        }
    }
    #endregion

    #region Audio Tests
    /// <summary>
    /// Test 1: PlayOneShot con configuración estándar
    /// </summary>
    private void TestPlayOneShot()
    {
        LogTest("[TEST 1] PlayOneShot básico");
        audioSource.PlayOneShot(testClip);
        LogTest($"Ejecutado. {FEEDBACK_MESSAGE}");
    }

    /// <summary>
    /// Test 2: PlayOneShot con volumen aumentado
    /// </summary>
    private void TestPlayOneShotBoosted()
    {
        LogTest("[TEST 2] PlayOneShot con volumen aumentado");
        audioSource.volume = DEFAULT_VOLUME;
        audioSource.PlayOneShot(testClip, BOOSTED_VOLUME);
        LogTest($"Ejecutado con volumen x{BOOSTED_VOLUME}. {FEEDBACK_MESSAGE}");
    }

    /// <summary>
    /// Test 3: Reproducción directa usando Play()
    /// </summary>
    private void TestPlayDirect()
    {
        LogTest("[TEST 3] Play() directo");
        audioSource.clip = testClip;
        audioSource.Play();
        
        if (enableVerboseLogs)
        {
            Debug.Log($"Playing: {audioSource.isPlaying}");
            Debug.Log($"Time: {audioSource.time}s");
        }
        
        LogTest(FEEDBACK_MESSAGE);
    }

    /// <summary>
    /// Test 4: PlayClipAtPoint (método estático sin AudioSource)
    /// </summary>
    private void TestPlayClipAtPoint()
    {
        LogTest("[TEST 4] PlayClipAtPoint (método estático)");
        
        Vector3 position = Camera.main != null 
            ? Camera.main.transform.position 
            : transform.position;
            
        AudioSource.PlayClipAtPoint(testClip, position, DEFAULT_VOLUME);
        LogTest($"Ejecutado. {FEEDBACK_MESSAGE}");
    }
    #endregion

    #region Logging
    private void LogAudioSetup()
    {
        Debug.Log("=== MAC AUDIO TEST - SETUP ===", this);
        Debug.Log($"AudioClip asignado: {(testClip != null ? "✅" : "❌")}", this);

        if (testClip != null)
        {
            Debug.Log($"Clip: {testClip.name} | " +
                     $"Length: {testClip.length:F2}s | " +
                     $"Samples: {testClip.samples} | " +
                     $"Channels: {testClip.channels} | " +
                     $"Frequency: {testClip.frequency}Hz | " +
                     $"LoadState: {testClip.loadState}", this);
        }

        AudioListener listener = FindObjectOfType<AudioListener>();
        string listenerStatus = listener != null 
            ? $"✅ en {listener.gameObject.name}" 
            : "❌ NO ENCONTRADO";
        Debug.Log($"Audio Listener: {listenerStatus}", this);
    }

    private void LogInstructions()
    {
        Debug.Log("\n=== CONTROLES ===\n" +
                 "ESPACIO: Test PlayOneShot básico\n" +
                 "T: Test con volumen aumentado\n" +
                 "Y: Test Play() directo\n" +
                 "U: Test PlayClipAtPoint\n", this);
    }

    private void LogTest(string message)
    {
        if (enableVerboseLogs)
        {
            Debug.Log(message, this);
        }
    }
    #endregion

    #region Editor GUI (Development Only)
#if UNITY_EDITOR
    private void OnGUI()
    {
        DrawTestUI();
    }

    private void DrawTestUI()
    {
        const float boxWidth = 350f;
        const float boxHeight = 120f;
        const float padding = 10f;
        const float lineHeight = 20f;

        Rect boxRect = new Rect(padding, padding, boxWidth, boxHeight);
        GUI.Box(boxRect, "");

        float yPos = padding + 10f;
        GUI.Label(new Rect(padding + 10f, yPos, boxWidth - 20f, lineHeight), 
                 "=== MAC AUDIO TEST ===");
        
        yPos += lineHeight;
        GUI.Label(new Rect(padding + 10f, yPos, boxWidth - 20f, lineHeight), 
                 "ESPACIO: Test PlayOneShot básico");
        
        yPos += lineHeight;
        GUI.Label(new Rect(padding + 10f, yPos, boxWidth - 20f, lineHeight), 
                 "T: Test con volumen x2");
        
        yPos += lineHeight;
        GUI.Label(new Rect(padding + 10f, yPos, boxWidth - 20f, lineHeight), 
                 "Y: Test Play() directo");
        
        yPos += lineHeight;
        GUI.Label(new Rect(padding + 10f, yPos, boxWidth - 20f, lineHeight), 
                 "U: Test PlayClipAtPoint");
    }
#endif
    #endregion
}