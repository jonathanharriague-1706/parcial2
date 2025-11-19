using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement; 

public class PlayerMovement : MonoBehaviour
{
    // ===============================================
    // REQUERIDO: REFERENCIAS DE JUEGO
    // ===============================================
    [Header("Referencias de Juego")]
    private EnemyController[] todosLosEnemigos; 
    
    // Array para almacenar referencias al tipo de script correcto.
    private SurveillanceCameraController[] todosLosCameras; 

    // ===============================================
    // Configuración de MOVIMIENTO y SIGILO
    // ===============================================
    [Header("Configuracion de Movimiento y Sigilo")]
    public float velocidadBase = 5f;        
    private float velocidadActual;          
    
    // REQUISITOS DE SIGILO
    public float multiplicadorSigilo = 0.75f; 
    public float alturaBase = 2.0f;         
    public float alturaAgachado = 1.0f;     
    
    // CRÍTICO: Offset del centro del CharacterController para la altura normal
    private Vector3 centroBase; 
    // CRÍTICO: Offset del centro del CharacterController para la altura agachado
    private Vector3 centroAgachado;

    // ===============================================
    // Variables de Vida y UI
    // ===============================================
    [Header("Configuracion de Vida y UI")]
    public float vidaActual = 100f; 
    public float vidaMaxima = 100f; 
    
    [Tooltip("El componente Text de Unity para mostrar la vida actual.")]
    public Text textoVida; 
    
    [Tooltip("Color base del texto de vida cuando está por encima del 50%.")]
    public Color colorBaseVida = Color.white; 

    // ===============================================
    // CRÍTICO: ESTADO DE MUERTE (Accedido por el enemigo)
    // ===============================================
    [HideInInspector] 
    public bool estaMuerto = false; 

    // --- Variables de Control Interno ---
    private Vector3 posicionInicialPlayer; 
    private bool estaAgachado = false; 
    private CharacterController controlador;
    private Vector3 velocidadVertical;
    
    void Start()
    {
        controlador = GetComponent<CharacterController>();
        if (controlador == null)
        {
            Debug.LogError("PlayerMovement requiere un CharacterController.");
        }
        
        // Guardamos la posición inicial antes de cualquier modificación
        posicionInicialPlayer = transform.position; 
        
        // CRÍTICO: Calcular los centros del CharacterController
        // El centro debe estar a la mitad de la altura para que el pie quede en el suelo
        float centroX = controlador.center.x;
        float centroZ = controlador.center.z;
        
        centroBase = new Vector3(centroX, alturaBase / 2f, centroZ);
        centroAgachado = new Vector3(centroX, alturaAgachado / 2f, centroZ);

        // Buscar todos los enemigos y cámaras (lógica de reaparición F3)
        todosLosEnemigos = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        todosLosCameras = FindObjectsByType<SurveillanceCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // Inicializamos la vida.
        vidaActual = vidaMaxima;
        ActualizarTextoVida(); 
        
        // CORRECCIÓN CRÍTICA: Aseguramos el estado inicial de PIE y la posición al inicio
        ReaparecerPlayer(); 
        Debug.Log("[INFO] Inicialización completa. Jugador en estado DE PIE.");
    }

    void Update()
    {
        // ----------------------------------------------------
        // LÓGICA DE CONTROL DE MUERTE
        // ----------------------------------------------------
        // La lógica de movimiento solo debe ejecutarse si no está muerto.
        if (estaMuerto)
        {
             HandleDebugKeys(); 
             return; // Si está muerto, salir de Update (solo maneja F1, F2, F3)
        }

        // ----------------------------------------------------
        // LOGICA DE SIGILO (AGACHADO)
        // ----------------------------------------------------
        // Se ejecuta solo si está vivo
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            AlternarAgachado();
        }
        
        // ----------------------------------------------------
        // LOGICA DE MOVIMIENTO
        // ----------------------------------------------------
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Utilizar la velocidad Actual (que tiene en cuenta el sigilo)
        Vector3 movimiento = transform.right * x + transform.forward * z;
        controlador.Move(movimiento * velocidadActual * Time.deltaTime);

        // Aplicar Gravedad
        if (controlador.isGrounded && velocidadVertical.y < 0)
        {
             velocidadVertical.y = -2f; // Pequeña fuerza para pegarse al suelo
        }
        velocidadVertical.y += Physics.gravity.y * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);
        
        // ----------------------------------------------------
        // LOGICA DE TECLAS DE DEBUG (F1, F2, F3)
        // ----------------------------------------------------
        HandleDebugKeys();
    }
    
    // ===============================================
    // MÉTODOS DE DEBUG
    // ===============================================
    private void HandleDebugKeys()
    {
        // REAPARICIÓN SOLO JUGADOR (F1)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ReaparecerPlayer();
            Debug.Log("JUGADOR: Reaparición rápida (F1).");
        }

        // REINICIO DE ESCENA (F2)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log("SISTEMA: Reiniciando escena (F2).");
        }
        
        // REAPARICION JUGADOR, ENEMIGOS Y CÁMARAS (F3)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ReaparecerPlayer(); // Primero reaparece el jugador
            
            // 1. Reaparece a todos los enemigos
            if (todosLosEnemigos != null)
            {
                foreach (EnemyController enemigo in todosLosEnemigos)
                {
                    if (enemigo != null) 
                    {
                        // Se asume que EnemyController tiene un método Reaparecer()
                        enemigo.Reaparecer(); 
                    }
                }
            }

            // 2. Reaparece a todas las cámaras (usando el tipo corregido)
            if (todosLosCameras != null)
            {
                foreach (SurveillanceCameraController camara in todosLosCameras)
                {
                    if (camara != null) 
                    {
                        camara.Reaparecer(); 
                    }
                }
            }
            
            Debug.Log("JUGADOR, ENEMIGOS Y CÁMARAS: Reinicio completo (F3).");
        }
    }

    // ===============================================
    // MÉTODOS DE ESTADO Y DAÑO
    // ===============================================

    /// <summary>
    /// Se llama cuando el jugador recibe daño de una fuente externa (ej: EnemyController).
    /// </summary>
    public void RecibirDanio(float cantidad)
    {
        if (estaMuerto) return; 

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); 
        
        Debug.Log($"Vida restante: {vidaActual}");
        
        ActualizarTextoVida(); 

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    /// <summary>
    /// Marca al jugador como muerto y realiza las acciones pertinentes.
    /// </summary>
    private void Morir()
    {
        estaMuerto = true;
        vidaActual = 0f; 
        ActualizarTextoVida();
        
        // Desactivar el CharacterController y el GameObject para "desaparecer"
        if (gameObject.activeSelf && controlador.enabled) 
        {
             controlador.enabled = false;
             gameObject.SetActive(false);
        }
        
        Debug.Log("JUGADOR: ¡El jugador ha muerto!");
    }
    
    /// <summary>
    /// Restaura la vida, el estado y la posición del jugador.
    /// </summary>
    private void ReaparecerPlayer()
    {
        // 1. Reactivar el GameObject (si estaba muerto)
        if (!gameObject.activeSelf)
        {
             gameObject.SetActive(true);
        }

        // 2. Resetear estado y vida
        vidaActual = vidaMaxima;
        estaMuerto = false;
        ActualizarTextoVida();
        
        // 3. Restaurar el estado de sigilo a DE PIE (CORRECCIÓN CRÍTICA)
        ForzarEstadoDePie();

        // 4. Mover el jugador a la posición inicial (CORRECCIÓN CRÍTICA DE ATRAVIESO)
        if (controlador != null)
        {
            // Guardamos el estado actual para restaurarlo
            bool wasEnabled = controlador.enabled;
            
            // Desactivamos el controlador antes de teletransportar para evitar atravesar el suelo
            controlador.enabled = false;
            
            controlador.transform.position = posicionInicialPlayer; 
            velocidadVertical = Vector3.zero; // Resetear la gravedad
            
            // Restauramos el estado del controlador
            controlador.enabled = true; 
        }
    }
    
    /// <summary>
    /// Fuerza el CharacterController al estado DE PIE.
    /// Esto garantiza un inicio correcto y evita problemas de atravesar el suelo.
    /// </summary>
    private void ForzarEstadoDePie()
    {
        estaAgachado = false; 
        controlador.height = alturaBase;
        controlador.center = centroBase;
        velocidadActual = velocidadBase;
        Debug.Log("ESTADO: Forzado a DE PIE.");
    }

    // ===============================================
    // MÉTODOS DE MOVIMIENTO SECUNDARIO
    // ===============================================
    private void AlternarAgachado()
    {
        // Invertimos el estado de agachado
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            // TRANSICIÓN A AGACHADO
            controlador.height = alturaAgachado;
            controlador.center = centroAgachado;
            velocidadActual = velocidadBase * multiplicadorSigilo; 
            Debug.Log("ESTADO: AGACHADO");
        }
        else
        {
            // TRANSICIÓN A DE PIE
            // Usamos el método de forzado para mantener la consistencia
            ForzarEstadoDePie();
        }
    }

    // ===============================================
    // MÉTODOS DE UI (Vida)
    // ===============================================
    void ActualizarTextoVida()
    {
        if (textoVida != null)
        {
            textoVida.text = $"VIDA: {Mathf.CeilToInt(vidaActual)}/{vidaMaxima}";
            textoVida.color = colorBaseVida; 
            
            if (vidaActual <= 25)
            {
                textoVida.color = Color.red;
            }
            else if (vidaActual <= 50)
            {
                textoVida.color = Color.yellow;
            }
        }
    }
}
