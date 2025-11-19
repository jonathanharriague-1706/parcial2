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
    
    // [CORREGIDO] Array para almacenar referencias al tipo de script correcto.
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
        
        posicionInicialPlayer = transform.position; 

        // Buscar todos los enemigos
        todosLosEnemigos = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (todosLosEnemigos.Length == 0)
        {
             Debug.LogWarning("No se encontraron enemigos.");
        }
        else
        {
             Debug.Log($"[INFO] Se encontraron {todosLosEnemigos.Length} enemigos.");
        }

        // [CORREGIDO] Buscar todas las cámaras usando SurveillanceCameraController
        todosLosCameras = FindObjectsByType<SurveillanceCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (todosLosCameras.Length == 0)
        {
             Debug.LogWarning("No se encontraron objetos con el script SurveillanceCameraController.");
        }
        else
        {
             Debug.Log($"[INFO] Se encontraron {todosLosCameras.Length} cámaras para el sistema de reaparición (F3).");
        }
        
        vidaActual = vidaMaxima;
        velocidadActual = velocidadBase; 
        controlador.height = alturaBase;
        
        ActualizarTextoVida(); 
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
        // LOGICA DE SIGILO (AGACHADO)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            AlternarAgachado();
        }
        
        // ----------------------------------------------------
        // LOGICA DE MOVIMIENTO
        // ----------------------------------------------------
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movimiento = transform.right * x + transform.forward * z;
        controlador.Move(movimiento * velocidadActual * Time.deltaTime);

        // Aplicar Gravedad
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
            ReaparecerPlayer();
            
            // 1. Reaparece a todos los enemigos
            if (todosLosEnemigos != null)
            {
                foreach (EnemyController enemigo in todosLosEnemigos)
                {
                    if (enemigo != null) 
                    {
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
        Debug.Log("JUGADOR: ¡El jugador ha muerto!");
    }
    
    /// <summary>
    /// Restaura la vida, el estado y la posición del jugador.
    /// </summary>
    private void ReaparecerPlayer()
    {
        // 1. Resetear estado y vida
        vidaActual = vidaMaxima;
        estaMuerto = false;
        ActualizarTextoVida();

        // 2. Mover el jugador a la posición inicial
        if (controlador != null)
        {
            controlador.transform.position = posicionInicialPlayer; 
            velocidadVertical = Vector3.zero; 
        }
        
        // 3. Asegurarse de que no esté agachado
        if (estaAgachado) 
        {
            estaAgachado = false;
            controlador.height = alturaBase;
            controlador.center = new Vector3(controlador.center.x, alturaBase / 2f, controlador.center.z);
            velocidadActual = velocidadBase;
        }
    }
    
    // ===============================================
    // MÉTODOS DE MOVIMIENTO SECUNDARIO
    // ===============================================
    private void AlternarAgachado()
    {
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            controlador.height = alturaAgachado;
            // Ajustar el centro para evitar que el jugador atraviese el suelo
            controlador.center = new Vector3(controlador.center.x, alturaAgachado / 2f, controlador.center.z);
            velocidadActual = velocidadBase * multiplicadorSigilo; 
        }
        else
        {
            controlador.height = alturaBase;
            // Ajustar el centro de vuelta a la normalidad
            controlador.center = new Vector3(controlador.center.x, alturaBase / 2f, controlador.center.z);
            velocidadActual = velocidadBase;
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
