using UnityEngine;

public class MacAudioTest : MonoBehaviour
{
    public AudioClip testClip;
    private AudioSource audioSource;
    
    void Start()
    {
        // Crear AudioSource con configuración forzada
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f;
        audioSource.priority = 0; // Máxima prioridad
        
        Debug.Log("=== MAC AUDIO TEST ===");
        Debug.Log($"AudioClip asignado: {(testClip != null ? "✅" : "❌")}");
        
        if (testClip != null)
        {
            Debug.Log($"Clip name: {testClip.name}");
            Debug.Log($"Clip length: {testClip.length} segundos");
            Debug.Log($"Clip samples: {testClip.samples}");
            Debug.Log($"Clip channels: {testClip.channels}");
            Debug.Log($"Clip frequency: {testClip.frequency}");
            Debug.Log($"Clip loadState: {testClip.loadState}");
        }
        
        AudioListener listener = FindObjectOfType<AudioListener>();
        Debug.Log($"Audio Listener: {(listener != null ? $"✅ en {listener.gameObject.name}" : "❌")}");
        
        Debug.Log("\nPresiona ESPACIO para test básico");
        Debug.Log("Presiona T para test con volumen forzado");
        Debug.Log("Presiona Y para test Play() directo");
    }
    
    void Update()
    {
        if (testClip == null)
        {
            Debug.LogError("¡Asigna un AudioClip en el Inspector!");
            return;
        }
        
        // Test 1: PlayOneShot básico
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("\n[TEST 1] PlayOneShot básico...");
            audioSource.PlayOneShot(testClip);
            Debug.Log("Ejecutado. ¿Escuchaste algo?");
        }
        
        // Test 2: PlayOneShot con volumen máximo
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("\n[TEST 2] PlayOneShot con volumen MAX...");
            audioSource.volume = 1f;
            audioSource.PlayOneShot(testClip, 2f); // Volumen x2
            Debug.Log("Ejecutado con volumen x2. ¿Escuchaste algo?");
        }
        
        // Test 3: Play directo
        if (Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("\n[TEST 3] Play() directo...");
            audioSource.clip = testClip;
            audioSource.Play();
            Debug.Log($"Playing: {audioSource.isPlaying}");
            Debug.Log($"Time: {audioSource.time}");
            Debug.Log("¿Escuchaste algo?");
        }
        
        // Test 4: PlayClipAtPoint (método estático)
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("\n[TEST 4] PlayClipAtPoint (sin AudioSource)...");
            AudioSource.PlayClipAtPoint(testClip, Camera.main.transform.position, 1f);
            Debug.Log("Ejecutado. ¿Escuchaste algo?");
        }
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 350, 120), "");
        GUI.Label(new Rect(20, 20, 330, 20), "=== MAC AUDIO TEST ===");
        GUI.Label(new Rect(20, 40, 330, 20), "ESPACIO: Test PlayOneShot básico");
        GUI.Label(new Rect(20, 60, 330, 20), "T: Test con volumen x2");
        GUI.Label(new Rect(20, 80, 330, 20), "Y: Test Play() directo");
        GUI.Label(new Rect(20, 100, 330, 20), "U: Test PlayClipAtPoint");
    }
}