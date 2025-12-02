# 📘 Documentación Técnica de Arquitectura y Sistemas: Singularity

**Motor:** Unity 6+ (API de física actualizada `linearVelocity`)

**Arquitectura:** Híbrida (Singleton Managers + Component-Based Entities)

**Patrones de Diseño:** Object Pooling Implícito, Pre-Warming, State Machines, Dependency Injection (Manual).

**Descargar Windows:** [Singularity app](https://drive.google.com/file/d/1oBBlOGJzcBpP_fRnnYBu77KgCx9DC9UV/view?usp=sharing)


**Descargar macOS:** [Singularity app](https://drive.google.com/drive/folders/1jQ4HmwRmKR56vPmqj3M0MRoGNU-zrqZS?usp=sharing)

**Formulario sobre el juego:**
[Formulario](https://docs.google.com/forms/d/1Vp4Kn7vL2Emy-B0_B0YSLfb5JW-facvg1alzl9lH3Mc/edit?usp=drive_web&ouid=107477045156746065283)

---

## 1. Pipeline de Inicialización y Gestión de Memoria (Startup & Loading)

El juego implementa una estrategia de carga en dos fases diseñada para eliminar la latencia en tiempo de ejecución (*runtime stuttering*) asociada a la instanciación tardía de assets gráficos y la gestión de memoria de Unity.

### 1.1. Secuencia de Arranque (`Intro` -> `Info` -> `Game`)
El punto de entrada no es el juego directamente, sino una escena ligera (`Intro`) que asegura que el motor esté inicializado antes de cargar assets pesados.
* **Transición Automática:** El script `AutoSceneChanger` utiliza una corrutina para gestionar el tiempo de espera (1.0s) y la carga asíncrona hacia la escena de información (`InfoScene`).
* **Menú Intermedio:** La escena de información actúa como buffer, permitiendo al usuario iniciar la carga pesada de la escena principal (`Juego`) mediante el método `LoadGameScene` del `InfoGameController`.

### 1.2. Sistema de Pre-Calentamiento Gráfico (Preloader System)
El script `Preloader.cs` es una pieza crítica de ingeniería para la optimización de la Interfaz de Usuario (UI). Unity a menudo causa picos de lag al renderizar fuentes (TMPro) o texturas de UI por primera vez.

* **Canvas Fantasma:** Se genera programáticamente un `GameObject` llamado `_PrewarmCanvas_` con `SortingOrder = -9999` y `CanvasGroup.alpha = 0f` para que sea invisible al ojo humano pero visible para el motor de renderizado.
* **Forzado de GPU (GPU Upload Force):**
    * El sistema itera sobre un array configurado `uiPrefabsToPrewarm`.
    * Para cada prefab, busca recursivamente componentes gráficos (`Image`, `TextMeshProUGUI`, `Animator`).
    * **Técnica de Acceso a Propiedades:** El script accede a propiedades "getter" como `.sprite` o `.material`, y ejecuta `ForceMeshUpdate(true, true)` en los textos TextMeshPro. Esto obliga a Unity a subir la geometría y texturas a la VRAM de la tarjeta gráfica inmediatamente, antes de que el jugador las necesite.
* **Gestión de Tiempos:** Mantiene cada prefab vivo durante un tiempo definido (`holdTimePerPrefab` de 0.05s) más unos cuadros extra (`extraFramesToWait`) para asegurar que el ciclo de renderizado se complete antes de destruirlo y liberar la memoria RAM, manteniendo los assets "calientes" en la VRAM.

---

## 2. Arquitectura del Núcleo de Juego (Core Game Loop)

El juego opera bajo un patrón de **Singleton Centralizado** (`GameManager`) apoyado por gestores satélite (`EnemySpawner`, `ObstacleManager`).

### 2.1. GameManager (Estado Global)
Actúa como la máquina de estados finitos (FSM) del juego y punto de acceso global.
* **Control de Tiempo:** Utiliza constantes internas `PAUSED_TIME_SCALE` (0f) y `NORMAL_TIME_SCALE` (1f) para congelar la simulación física y lógica durante las pausas o secuencias de Game Over, asegurando consistencia determinista.
* **Ciclo de Fin de Juego:** Ejecuta `TriggerGameOver()` para activar la UI de derrota, reducir la música y coordinar el guardado de puntuaciones.

### 2.2. EnemySpawner (Algoritmo de Oleadas)
Este sistema abandona el spawn aleatorio puro en favor de una estructura de datos personalizada para un diseño de niveles controlado.

* **Estructura de Datos:** Utiliza una clase anidada serializable `TemporalStage`, que permite al diseñador definir "Momentos Clave" (`triggerTimestamp`) en la línea de tiempo donde cambia la composición de enemigos.
* **Optimización por Goteo (Time-Slicing):**
    * El spawner no instancia toda la oleada en un solo frame.
    * Implementa una corrutina `SpawnGoteoLoop` que distribuye la creación de enemigos basada en `goteoRateForThisStage` y `delayBetweenSpawns`.
    * **Beneficio:** Esto distribuye la carga de CPU de `Instantiate()` y `NavMeshAgent.Warp()` a lo largo de varios segundos, manteniendo los FPS estables incluso en oleadas densas.
* **Validación de Posición (NavMesh Sampling):**
    * **Algoritmo:** Genera un punto aleatorio en un anillo alrededor del jugador (radio entre `minSpawnDistance` y `maxSpawnDistance`).
    * **Verificación:** Realiza hasta 3 intentos (`maxSpawnAttemptsPerEnemy`) usando `NavMesh.SamplePosition` con un radio de muestreo de 5f. Si falla, descarta el spawn para evitar que los enemigos aparezcan atrapados en la geometría estática.

---

## 3. Entidad Jugador: Física, Habilidades y Controles

El jugador es un cuerpo físico rígido (`Rigidbody`) sin animación esquelética, confiando en feedback procedimental y un sistema de habilidades basado en la "Economía de Asesinatos" (Kill-Economy).

### 3.1. Locomoción Física (Unity 6 Physics)
* **Movimiento:** Se utiliza la API moderna `rb.linearVelocity` (reemplazando a la obsoleta `velocity`) para asignar el vector de movimiento directo, evitando la inercia flotante de `AddForce` para un control "snappy" (reactivo).
* **Emisión de Partículas:** El sistema de partículas `playerDustParticles` se modula dinámicamente. Si la magnitud del input es mayor a `MIN_INPUT_MAGNITUDE` (0.1f), se activa la emisión (`rateOverTime`), vinculando visualmente el esfuerzo del movimiento con el polvo levantado.

### 3.2. Sistema de Habilidades (Kill-Economy)
El jugador no gestiona tiempos de enfriamiento (Cooldowns), sino que debe jugar agresivamente para recargar sus habilidades.

#### A. Habilidad Evasiva: Dash
* **Recarga:**
    * Coste total: 10 puntos (`cubeKillsNeeded`).
    * Valor "Cube": 1 punto.
    * Valor "Sphere": Calculado dinámicamente como `cubeKillsNeeded / sphereKillsNeeded` (5 puntos). Incentiva priorizar enemigos difíciles.
* **Ejecución:**
    * **Invencibilidad:** Desactiva el `Collider` (`enabled = false`) durante 0.15s (`dashDuration`) para atravesar peligros.
    * **Impulso:** Sobrescribe la velocidad lineal a 30f (`dashSpeed`) en dirección del movimiento.

#### B. Habilidad Definitiva: Nova
* **Recarga:**
    * Gestionada centralmente por el `GameManager`.
    * Requiere un umbral fijo de **30 muertes** (`novaKillsThreshold`).
    * Feedback Visual: El `novaFillImage` en la UI se llena progresivamente.
* **Ejecución:**
    * **Lógica Física:** Al activarse, no instancia proyectiles. Utiliza `Physics.OverlapSphere` con radio de 15u (`novaRadius`).
    * **Efecto:** Aplica 9999 de daño a todos los enemigos en el radio, marcando la muerte con el flag `isNovaKill = true` (lo cual evita que estas muertes recarguen la Nova inmediatamente, previniendo bucles infinitos).

### 3.3. Sistema de Combate (Raycasting)
* **Puntería 3D:** Utiliza `mainCamera.ScreenPointToRay` proyectando desde la posición del ratón en pantalla.
* **Plano de Intersección:** Si el Raycast no golpea geometría física válida, calcula un punto objetivo teórico a 100 unidades de distancia (`ray.GetPoint(100f)`) y fuerza su altura Y para que coincida con el `firePoint`. Esto asegura que el disparo sea siempre paralelo al suelo.

### 3.4. Mapa de Controles (Input Mapping)

| Acción | Input (Hardware) | Código Relacionado | Contexto |
| :--- | :--- | :--- | :--- |
| **Moverse** | W, A, S, D | `Input.GetAxisRaw` | Movimiento relativo a la cámara |
| **Apuntar** | Ratón (Posición) | `ScreenPointToRay` | Define dirección de disparo |
| **Disparar** | Click Izquierdo | `Input.GetMouseButton(0)` | Cadencia automática |
| **Dash** | Barra Espaciadora | `Input.GetKeyDown(KeyCode.Space)` | Requiere cargas completas |
| **Nova** | Click Derecho | `Input.GetMouseButtonDown(1)` | Requiere 30 kills |
| **Pausar** | Escape (ESC) | `Input.GetKeyDown(KeyCode.Escape)` | Alterna TimeScale 0/1 |

---

## 4. Inteligencia Artificial y Navegación (AI Stack)

La IA está construida sobre `NavMeshAgent` pero con configuraciones físicas extremas para evitar la sensación de "deslizamiento" típica de Unity.



### 4.1. Configuración "Anti-Hielo"
En el script base `Enemy.cs`, se sobrescriben los valores por defecto del agente en el `Awake`:
* **Aceleración:** 60f (Muy alta, permite arranque instantáneo).
* **Velocidad Angular:** 1000f (Permite giros de orientación en un solo frame).
* **Auto-Braking:** Activado para precisión milimétrica en la llegada al destino.

### 4.2. Comportamientos Específicos (Herencia)
* **Enemigo Melee (`Enemy`):**
    * Implementa una rutina de "Movimiento Errático". En lugar de `agent.SetDestination(player)`, calcula un `randomOffset` dentro de una esfera (`erraticDistance`) alrededor del jugador. Esto crea un comportamiento de enjambre orgánico.
* **Enemigo Ranged (`Enemy_Sphere`):**
    * **Máquina de Estados Implícita:** Evalúa la distancia al jugador en cada frame.
    * **Panic Logic:** Si la distancia es menor o igual a `panicRange` (5m), cambia su `fireRate` de 5s a 1s (`panicFireRate`) para presionar al jugador.
    * **Aiming:** Realiza una rotación manual usando `Quaternion.Slerp` hacia el jugador, ignorando la rotación automática del NavMeshAgent para mantener el encaramiento constante.

---

## 5. Generación Procedimental de Entorno

### 5.1. ObstacleManager
Sincroniza la dificultad del terreno con la progresión temporal de las oleadas.
* **Algoritmo de Colocación:**
    * Muestrea una posición aleatoria en el NavMesh.
    * **Collision Check:** Utiliza `Physics.CheckBox` con el tamaño propuesto del obstáculo antes de instanciarlo. Esto es crucial para evitar que un muro aparezca *dentro* del jugador o enemigos.
* **Ciclo de Vida (`RisingObstacle`):**
    * Los obstáculos tienen una animación programática controlada por corrutinas: `Initialize` -> `Sequence` (Subir) -> `MoveTo` -> `WaitForSeconds` -> `MoveTo` (Bajar) -> `Destroy`.
    * Utilizan componentes **NavMesh Obstacle** con la opción **Carve** activada, lo que obliga a los enemigos a recalcular sus rutas en tiempo real.

---

## 6. Subsistemas de Feedback (Game Juice)

### 6.1. DamageFeedback (Visual & Audio)
Centraliza la respuesta sensorial al daño para mantener el código del jugador limpio.
* **Flash de Material:** Manipula la propiedad `_EmissionColor` del shader estándar. Guarda el color original al inicio y realiza un cambio hacia `Color.white * 10f` (intensidad HDR) volviendo al original tras la duración especificada.
* **Audio Seguro:** Verifica la existencia de un `AudioSource`. Si falta, lo crea dinámicamente con `AddComponent<AudioSource>()` y configura su `spatialBlend` a 0 (2D) para asegurar que el jugador escuche el impacto independientemente de la posición de la cámara.

### 6.2. Efectos Visuales (VFX)
* **FadeAndDie:** Un script utilitario para escombros ("Gibs"). Implementa un doble desvanecimiento: reduce progresivamente el Alpha del color base y simultáneamente la intensidad de la emisión (`_EmissionColor`) a negro, logrando que los restos se "enfríen" y desaparezcan suavemente.

---

## 7. Persistencia de Datos (High Scores)

El sistema de guardado es local, basado en serialización JSON ligera para competiciones arcade.

* **Estructura de Datos:**
    * `HighScoreEntry`: Struct simple (Nombre + Score).
    * `ScoreList`: Wrapper de lista para facilitar la serialización JSON de Unity.
* **Lógica de Inserción:**
    1.  Carga datos existentes desde `PlayerPrefs`.
    2.  Añade la nueva entrada temporalmente.
    3.  **Ordenamiento:** Aplica `OrderByDescending(e => e.score)` mediante LINQ para ordenar de mayor a menor.
    4.  **Truncado:** Si la lista supera `MaxEntries` (5), elimina el rango excedente (`RemoveRange`).
    5.  Guarda de nuevo a disco.

---

## 8. Herramientas de Debugging y QA

El proyecto incluye herramientas específicas para diagnóstico en desarrollo y builds.

* **MacAudioTest:** Script de diagnóstico diseñado para resolver problemas de drivers de audio en plataformas macOS.
* **Paneles de Debug:** Referencias en `GameManager` a `panelDePruebaRojo` y `panelDePruebaVerde`, permitiendo visualizar estados internos del juego en builds de desarrollo.
* **KillFloor:** Trigger de seguridad (Bounds) situado bajo el nivel que limpia basura (objetos caídos) y mata al jugador si escapa del mapa por un error de colisión.
* **Modo Ventana:** Script `ForceWindow` que fuerza la resolución 1920x1080 en modo ventana al inicio, útil para entornos de desarrollo o quioscos.

---

## 9. Demostración de Gameplay

El siguiente video ilustra las mecánicas descritas anteriormente: el sistema de Dash con invencibilidad, la habilidad Nova, el comportamiento de enjambre de la IA y la generación dinámica de obstáculos.

**[ [Singularity](https://www.youtube.com/watch?v=a9vfFvUDq2U) ]**

---


# LICENCIA DE USO NO COMERCIAL - SINGULARITY

**Versión 1.0 - Noviembre 2025**

---

## TÉRMINOS Y CONDICIONES DE USO

Copyright © 2025. Todos los derechos reservados.

### 1. CONCESIÓN DE LICENCIA

Se concede permiso para descargar, instalar y ejecutar el videojuego "Singularity" (en adelante, "el Software") en plataformas Windows y macOS únicamente para uso personal y no comercial, sujeto a las restricciones establecidas en esta licencia.

### 2. USOS PERMITIDOS

Bajo esta licencia, se permite:

- **Descargar** el Software desde los enlaces oficiales proporcionados
- **Instalar** el Software en dispositivos personales
- **Jugar** y disfrutar del Software de manera individual
- **Compartir** enlaces oficiales de descarga con terceros

### 3. RESTRICCIONES Y USOS PROHIBIDOS

Queda **EXPRESAMENTE PROHIBIDO**:

- ❌ **Comercializar** el Software de cualquier forma, incluyendo pero no limitado a: venta, alquiler, licenciamiento o distribución por la cual se obtenga beneficio económico
- ❌ **Copiar, reproducir o duplicar** el Software o cualquiera de sus componentes
- ❌ **Modificar, alterar o crear obras derivadas** basadas en el Software
- ❌ **Descompilar, realizar ingeniería inversa o desensamblar** el código fuente del Software
- ❌ **Distribuir copias** del Software por canales no autorizados
- ❌ **Extraer, separar o aislar** assets, recursos gráficos, sonoros o de código del Software
- ❌ **Utilizar el Software** en contextos comerciales, eventos de pago o con fines publicitarios sin autorización explícita por escrito
- ❌ **Remover, alterar u ocultar** avisos de copyright, marcas registradas o atribuciones del Software

### 4. PROPIEDAD INTELECTUAL

Todos los derechos de propiedad intelectual sobre el Software, incluyendo pero no limitado a: código fuente, arquitectura de software, assets visuales, audio, diseño de niveles, mecánicas de juego y documentación técnica, son y permanecerán como propiedad exclusiva del titular de derechos.

### 5. DISTRIBUCIÓN

El Software solo puede ser distribuido mediante:
- Enlaces oficiales a las descargas autorizadas
- Google Drive: Windows ([link oficial](https://drive.google.com/file/d/1oBBlOGJzcBpP_fRnnYBu77KgCx9DC9UV/view?usp=sharing))
- Google Drive: macOS ([link oficial](https://drive.google.com/drive/folders/1jQ4HmwRmKR56vPmqj3M0MRoGNU-zrqZS?usp=sharing))

Cualquier otra forma de distribución requiere autorización previa y por escrito.

### 6. AUSENCIA DE GARANTÍAS

EL SOFTWARE SE PROPORCIONA "TAL CUAL" ("AS IS"), SIN GARANTÍAS DE NINGÚN TIPO, EXPRESAS O IMPLÍCITAS, INCLUYENDO PERO NO LIMITADO A GARANTÍAS DE COMERCIABILIDAD, IDONEIDAD PARA UN PROPÓSITO PARTICULAR Y NO INFRACCIÓN.

El titular de derechos no garantiza que:
- El Software esté libre de errores o funcione ininterrumpidamente
- Los defectos serán corregidos
- El Software sea compatible con todo hardware o configuración

### 7. LIMITACIÓN DE RESPONSABILIDAD

EN NINGÚN CASO EL TITULAR DE DERECHOS SERÁ RESPONSABLE DE:
- Daños directos, indirectos, incidentales, especiales o consecuentes
- Pérdida de datos, beneficios o uso
- Interrupciones del negocio
- Cualquier daño derivado del uso o imposibilidad de uso del Software

### 8. TERMINACIÓN DE LA LICENCIA

Esta licencia es efectiva hasta su terminación. Se terminará automáticamente sin previo aviso si:
- Se incumple cualquier término de esta licencia
- Se utiliza el Software de manera no autorizada

Tras la terminación, deberá:
- Cesar inmediatamente todo uso del Software
- Eliminar todas las copias del Software en su posesión

### 9. LEGISLACIÓN APLICABLE

Esta licencia se regirá e interpretará de acuerdo con las leyes aplicables en la jurisdicción del titular de derechos, sin considerar conflictos de disposiciones legales.

### 10. MODIFICACIONES

El titular de derechos se reserva el derecho de modificar estos términos en cualquier momento. El uso continuado del Software tras dichas modificaciones constituye la aceptación de los nuevos términos.

### 11. ACUERDO COMPLETO

Esta licencia constituye el acuerdo completo entre las partes con respecto al uso del Software y reemplaza todos los acuerdos previos, escritos u orales.

---

## ACEPTACIÓN DE TÉRMINOS

Al descargar, instalar o utilizar Singularity, usted reconoce haber leído, comprendido y aceptado estar obligado por los términos y condiciones de esta licencia.

Si no está de acuerdo con estos términos, no está autorizado a usar el Software.

---

**Para consultas sobre licenciamiento comercial o permisos especiales, contacte al titular de derechos.**

*Singularity - Desarrollado con Unity 6+ | Motor Gráfico y Sistema de Física Avanzado*