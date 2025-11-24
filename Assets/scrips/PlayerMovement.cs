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
    // Configuración de MOVIMIENTO (SIGILO ELIMINADO)
    // ===============================================
    [Header("Configuracion de Movimiento")]
    public float velocidadBase = 5f; 		
    private float velocidadActual; 		 
    
    // Altura fija (ya no hay agachado)
    public float alturaBase = 2.0f; 		 
    
    // CRÍTICO: Offset del centro del CharacterController para la altura normal
    private Vector3 centroBase; 

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
    private CharacterController controlador;
    private Vector3 velocidadVertical;
    
    void Start()
    {
        controlador = GetComponent<CharacterController>();
        if (controlador == null)
        {
            Debug.LogError("PlayerMovement requiere un CharacterController.");
        }
        
        // Guardamos la posición inicial. Esto es la posición del objeto en la escena al iniciar.
        posicionInicialPlayer = transform.position; 
        
        // CRÍTICO: Calcular el centro del CharacterController (a la mitad de la altura)
        float centroX = controlador.center.x;
        float centroZ = controlador.center.z;
        centroBase = new Vector3(centroX, alturaBase / 2f, centroZ);
        
        // Buscar todos los enemigos y cámaras (lógica de reaparición F3)
        todosLosEnemigos = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        todosLosCameras = FindObjectsByType<SurveillanceCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // Inicializamos la vida.
        vidaActual = vidaMaxima;
        ActualizarTextoVida(); 
        
        // Inicializa el estado del Player (siempre de pie)
        InicializarPlayerState();

        // Forzamos una reaparición limpia al inicio para asegurar la posición y la altura correctas.
        ReaparecerPlayer(); 

        Debug.Log("[INFO] Inicialización completa. El jugador siempre está DE PIE.");
    }

    void Update()
    {
        // ----------------------------------------------------
        // LÓGICA DE CONTROL DE MUERTE
        // ----------------------------------------------------
        if (estaMuerto)
        {
             HandleDebugKeys(); 
             return; 
        }

        // ----------------------------------------------------
        // LOGICA DE MOVIMIENTO
        // ----------------------------------------------------
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Usamos la velocidadBase, ya que no hay sigilo
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

            // 2. Reaparece a todas las cámaras
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
    /// Inicializa la altura y velocidad del CharacterController (siempre DE PIE).
    /// </summary>
    private void InicializarPlayerState()
    {
        // Si el controlador no está activo (ej: jugador muerto), no podemos ajustarlo.
        if (controlador == null || !controlador.enabled) 
        {
            velocidadActual = velocidadBase; 
            return;
        }
        
        controlador.height = alturaBase;
        controlador.center = centroBase;
        velocidadActual = velocidadBase;
        Debug.Log("ESTADO: Inicializado a DE PIE.");
    }

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
        
        // Desactivar el CharacterController
        if (controlador.enabled) 
        {
             controlador.enabled = false;
        }
        
        // Desactivar el GameObject
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
        
        Debug.Log("JUGADOR: ¡El jugador ha muerto!");
    }
    
    /// <summary>
    /// Restaura la vida, el estado y la posición del jugador.
    /// Se utiliza para reapariciones (Start, F1, F3).
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
        
        // 3. Restaurar la altura y velocidad
        InicializarPlayerState();

        // 4. Mover el jugador a la posición inicial (teletransporte seguro)
        if (controlador != null)
        {
            // CRÍTICO: Desactivamos el controlador antes de teletransportar para evitar 
            // problemas de colisión.
            controlador.enabled = false;
            
            controlador.transform.position = posicionInicialPlayer; 
            velocidadVertical = Vector3.zero; // Resetear la gravedad
            
            // Restauramos el estado del controlador
            controlador.enabled = true; 
        }
    }

    // ===============================================
    // MÉTODOS DE UI (Vida)
    // ===============================================
    void ActualizarTextoVida()
    {
        if (textoVida != null)
        {
            // Usamos Mathf.CeilToInt para redondear hacia arriba y evitar decimales feos
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
