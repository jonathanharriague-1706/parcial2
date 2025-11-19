using UnityEngine;
using UnityEngine.UI; // ¡IMPORTANTE! Necesario para usar el componente Text

public class PlayerMovement : MonoBehaviour
{
    // ===============================================
    // REQUERIDO: REFERENCIAS DE JUEGO
    // ===============================================
    [Header("Referencias de Juego")]
    // [MODIFICADO] Ahora es un ARRAY para almacenar a TODOS los enemigos de la escena.
    private EnemyController[] todosLosEnemigos; 

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
    
    // [NUEVO] Color base para la vida (puedes poner violeta aquí en el Inspector)
    [Tooltip("Color base del texto de vida cuando está por encima del 50%.")]
    public Color colorBaseVida = Color.white; // Configúralo como violeta en Unity
    
    // ===============================================
    // CRÍTICO: ESTADO DE MUERTE (Soluciona error CS1061)
    // ===============================================
    [HideInInspector] 
    public bool estaMuerto = false; // <--- ¡Variable añadida para que EnemyController la pueda leer!

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
        
        // [NUEVO] Encuentra TODOS los objetos activos o inactivos 
        // con el script EnemyController en la escena al inicio.
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
        
        // ¡LLAMADA INICIAL A LA UI! Muestra la vida al empezar el juego.
        ActualizarTextoVida(); 
    }

    void Update()
    {
        // ----------------------------------------------------
        // LÓGICA DE CONTROL DE MUERTE (Para evitar movimiento al morir)
        // ----------------------------------------------------
        if (estaMuerto)
        {
             // Si está muerto, no permite el movimiento ni agacharse, solo chequea F3 si lo deseas
             // Puedes añadir aqui la lógica de Game Over si lo prefieres
             if (Input.GetKeyDown(KeyCode.F3)) {
                 // Reiniciar el estado del jugador si usas F3 para revivirlo
                 ReaparecerPlayer(); 
             }
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
        // LOGICA DE MOVIMIENTO (Simple, sin salto, sin correr)
        // ----------------------------------------------------
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movimiento = transform.right * x + transform.forward * z;
        controlador.Move(movimiento * velocidadActual * Time.deltaTime);

        // Aplicar Gravedad
        velocidadVertical.y += Physics.gravity.y * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);

        // ----------------------------------------------------
        // LOGICA DE MUERTE DEL JUGADOR
        // ----------------------------------------------------
        if (vidaActual <= 0)
        {
            Morir();
            // Ya no es necesario el Debug.Log aquí, está en Morir()
        }
        
        // ----------------------------------------------------
        // LOGICA DE REAPARICION DE TODOS LOS ENEMIGOS (F3)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.F3) && todosLosEnemigos != null)
        {
            // [MODIFICADO] Itera sobre el ARRAY de enemigos y llama a Reaparecer() en cada uno.
            foreach (EnemyController enemigo in todosLosEnemigos)
            {
                if (enemigo != null) // Asegura que el objeto no haya sido destruido
                {
                    // Asumiendo que EnemyController tiene el método Reaparecer()
                    enemigo.Reaparecer(); 
                }
            }
            // También reaparece al jugador (si se desea)
            ReaparecerPlayer(); 
            Debug.Log("Todos los enemigos y el jugador han reaparecido.");
        }
    }
    
    private void AlternarAgachado()
    {
        // Lógica de agachado original (sin ajustes de center):
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            controlador.height = alturaAgachado;
            // FALTA: controlador.center = new Vector3(controlador.center.x, alturaAgachado / 2f, controlador.center.z);
            velocidadActual = velocidadBase * multiplicadorSigilo; 
        }
        else
        {
            controlador.height = alturaBase;
            // FALTA: controlador.center = new Vector3(controlador.center.x, alturaBase / 2f, controlador.center.z);
            velocidadActual = velocidadBase;
        }
    }
    
    /// <summary>
    /// Marca al jugador como muerto y realiza acciones de limpieza.
    /// </summary>
    private void Morir()
    {
        if (estaMuerto) return; // Evitar morir dos veces

        estaMuerto = true;
        vidaActual = 0f; 
        ActualizarTextoVida();
        
        // Desactivar el controlador y el GameObject para simular la "muerte"
        if (controlador != null && controlador.enabled) 
        {
             controlador.enabled = false;
        }
        // Puedes desactivar el GameObject si quieres que desaparezca visualmente
        // gameObject.SetActive(false); 

        Debug.Log("JUGADOR: ¡El jugador ha muerto!");
    }
    
    /// <summary>
    /// Resetea el estado del jugador a "vivo".
    /// </summary>
    public void ReaparecerPlayer()
    {
        estaMuerto = false;
        vidaActual = vidaMaxima;
        ActualizarTextoVida();
        
        // Reactivar el controlador y el GameObject si estaban desactivados
        if (controlador != null && !controlador.enabled) 
        {
             controlador.enabled = true;
        }
        gameObject.SetActive(true);
        
        // [OPCIONAL] Teletransportar a un punto seguro si es necesario
        // transform.position = posicionInicialPlayer; 
    }
    
    // --- Metodo de Daño (Mantenemos para que el enemigo pueda interactuar) ---
    public void RecibirDanio(float cantidad)
    {
        if (estaMuerto) return; // No recibir daño si ya está muerto

        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); // Asegura que la vida no baje de 0
        
        Debug.Log($"Vida restante: {vidaActual}");
        
        // ¡LLAMADA A LA UI! Actualiza el texto cada vez que recibes daño.
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
            // Muestra la vida actual redondeada al entero más cercano, seguida de la vida máxima.
            textoVida.text = $"VIDA: {Mathf.CeilToInt(vidaActual)}/{vidaMaxima}";
            
            // [MODIFICADO] Aplica el color base PRIMERO.
            textoVida.color = colorBaseVida; 
            
            // Lógica de color condicional: SOLO aplica los colores de peligro
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
