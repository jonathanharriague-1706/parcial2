using UnityEngine;
using UnityEngine.UI; // ¡IMPORTANTE! Necesario para usar el componente Text
using UnityEngine.SceneManagement; // <-- Necesario para reiniciar la escena (F2)

public class PlayerMovement : MonoBehaviour
{
    // ===============================================
    // REQUERIDO: REFERENCIAS DE JUEGO
    // ===============================================
    [Header("Referencias de Juego")]
    // Array para almacenar a TODOS los enemigos de la escena.
    private EnemyController[] todosLosEnemigos; 

    // <--- AÑADIDO: Variable para el requisito F1 --->
    private Vector3 posicionInicialPlayer; 
    // <--- FIN AÑADIDO: Variable para el requisito F1 --->

    // ===============================================
    // Configuración de MOVIMIENTO y SIGILO (Requisitos Parcial 2)
    // ===============================================
    [Header("Configuracion de Movimiento y Sigilo")]
    public float velocidadBase = 5f; 		
    private float velocidadActual; 		 
    
    // REQUISITOS DE SIGILO
    public float multiplicadorSigilo = 0.75f; // Reduce la velocidad un 25%
    public float alturaBase = 2.0f; 		// Altura normal del CharacterController
    public float alturaAgachado = 1.0f; 	// 50% de reduccion (si alturaBase es 2.0f)

    // ===============================================
    // Variables de Vida y UI
    // ===============================================
    [Header("Configuracion de Vida y UI")]
    public float vidaActual = 100f; 
    public float vidaMaxima = 100f; 
    
    [Tooltip("El componente Text de Unity para mostrar la vida actual.")]
    public Text textoVida; 
    
    // Color base para la vida
    [Tooltip("Color base del texto de vida cuando está por encima del 50%.")]
    public Color colorBaseVida = Color.white; // Configúralo como violeta en Unity
    
    // ===============================================
    // CRÍTICO: ESTADO DE MUERTE
    // ===============================================
    [HideInInspector] 
    public bool estaMuerto = false; 

    // --- Variables de Control Interno ---
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
        
        // Guarda la posición inicial del jugador para F1
        posicionInicialPlayer = transform.position; 
        
        // Encuentra TODOS los objetos con el script EnemyController en la escena al inicio.
        todosLosEnemigos = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        if (todosLosEnemigos.Length == 0)
        {
             Debug.LogWarning("No se encontraron objetos con el script EnemyController en la escena. La reaparicion (F3) no funcionara.");
        }
        else
        {
             Debug.Log($"[INFO] Se encontraron {todosLosEnemigos.Length} enemigos para el sistema de reaparición (F3).");
        }
        
        vidaActual = vidaMaxima;
        velocidadActual = velocidadBase; 
        controlador.height = alturaBase;
        
        ActualizarTextoVida(); 
    }

    void Update()
    {
        // ----------------------------------------------------
        // LÓGICA DE CONTROL DE MUERTE (Permite depuración incluso muerto)
        // ----------------------------------------------------
        if (estaMuerto)
        {
             // Si está muerto, solo chequea las teclas de depuración.
             if (Input.GetKeyDown(KeyCode.F3)) { HandleF3Debug(); }
             if (Input.GetKeyDown(KeyCode.F1)) { ReaparecerPlayer(); }
             if (Input.GetKeyDown(KeyCode.F2)) { HandleF2Debug(); }
             return; 
        }

        // ----------------------------------------------------
        // LOGICA DE SIGILO, MOVIMIENTO Y MUERTE (Original)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            AlternarAgachado();
        }
        
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movimiento = transform.right * x + transform.forward * z;
        controlador.Move(movimiento * velocidadActual * Time.deltaTime);

        velocidadVertical.y += Physics.gravity.y * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);

        if (vidaActual <= 0)
        {
            Morir();
        }
        
        // ----------------------------------------------------
        // LOGICA DE DEPURACIÓN (F1, F2, F3)
        // ----------------------------------------------------

        // F1: Reaparecer jugador en punto de inicio con vida y balas recargadas
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ReaparecerPlayer(); 
            Debug.Log("DEBUG: Jugador reaparecido en inicio (F1).");
        }
        
        // F2: Reiniciar toda la escena
        if (Input.GetKeyDown(KeyCode.F2))
        {
            HandleF2Debug();
        }

        // F3: Reaparición de solo enemigos
        if (Input.GetKeyDown(KeyCode.F3))
        {
            HandleF3Debug();
        }
    }
    
    // ===============================================
    // MÉTODOS DE DEPURACIÓN
    // ===============================================
    
    /// <summary>
    /// Reinicia toda la escena actual. (F2)
    /// </summary>
    private void HandleF2Debug()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("DEBUG: Escena Reiniciada (F2).");
    }

    /// <summary>
    /// Reaparece a todos los enemigos a sus puntos iniciales. (F3)
    /// </summary>
    private void HandleF3Debug()
    {
        // 1. Reaparición de Enemigos SOLAMENTE
        if (todosLosEnemigos != null)
        {
            foreach (EnemyController enemigo in todosLosEnemigos)
            {
                if (enemigo != null) 
                {
                    // Asumiendo que EnemyController tiene el método Reaparecer()
                    enemigo.Reaparecer(); 
                }
            }
        }
        
        Debug.Log("DEBUG: Solo enemigos reaparecidos (F3).");
    }
    
    // ===============================================
    // MÉTODOS DE JUGADOR
    // ===============================================

    private void AlternarAgachado()
    {
        // Lógica de agachado original
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            controlador.height = alturaAgachado;
            velocidadActual = velocidadBase * multiplicadorSigilo; 
        }
        else
        {
            controlador.height = alturaBase;
            velocidadActual = velocidadBase;
        }
    }
    
    /// <summary>
    /// Marca al jugador como muerto y realiza acciones de limpieza.
    /// </summary>
    private void Morir()
    {
        if (estaMuerto) return; 

        estaMuerto = true;
        vidaActual = 0f; 
        ActualizarTextoVida();
        
        if (controlador != null && controlador.enabled) 
        {
             controlador.enabled = false;
        }
        Debug.Log("JUGADOR: ¡El jugador ha muerto!");
    }
    
    /// <summary>
    /// Resetea el estado del jugador a "vivo", lo teletransporta a la posición inicial, 
    /// y recarga vida y balas. (F1)
    /// </summary>
    public void ReaparecerPlayer()
    {
        // 1. Resetear el estado y vida
        estaMuerto = false;
        vidaActual = vidaMaxima;
        
        // ** PENDIENTE DE CONEXIÓN: Recarga de Balas/Munición **
        // Si tienes un script de munición (ej: WeaponController), deberías llamar aquí:
        // GetComponent<WeaponController>().RecargarMunicionCompleta();
        
        ActualizarTextoVida();
        
        // 2. Teletransporte Seguro al Inicio
        if (controlador != null)
        {
             // Desactivar CharacterController para moverlo directamente
             controlador.enabled = false;
             
             // Teletransportar a la posición inicial guardada en Start()
             transform.position = posicionInicialPlayer; 
             velocidadVertical = Vector3.zero; // Resetear la gravedad/velocidad vertical
             
             // Reactivar CharacterController
             controlador.enabled = true;
        }
        
        // Reactivar el GameObject si estaba desactivado
        gameObject.SetActive(true);
    }
    
    // --- Metodo de Daño ---
    public void RecibirDanio(float cantidad)
    {
        if (estaMuerto) return; 

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); 
        
        Debug.Log($"Vida restante: {vidaActual}");
        
        ActualizarTextoVida(); 
    }
    
    // ===============================================
    // MÉTODOS DE UI (Vida)
    // ===============================================

    /// <summary>
    /// Actualiza el componente de texto de la UI con la vida del jugador.
    /// </summary>
    void ActualizarTextoVida()
    {
        if (textoVida != null)
        {
            // Muestra la vida actual redondeada al entero más cercano.
            textoVida.text = $"VIDA: {Mathf.CeilToInt(vidaActual)}/{vidaMaxima}";
            
            // Aplica el color base
            textoVida.color = colorBaseVida; 
            
            // Lógica de color condicional: aplica los colores de peligro
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