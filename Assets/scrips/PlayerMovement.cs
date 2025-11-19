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
    public float alturaBase = 2.0f;          // Altura normal del CharacterController
    public float alturaAgachado = 1.0f;      // 50% de reduccion (si alturaBase es 2.0f)

    // ===============================================
    // Variables de Vida y UI
    // ===============================================
    [Header("Configuracion de Vida y UI")]
    public float vidaActual = 100f; 
    public float vidaMaxima = 100f; 
    
    [Tooltip("El componente Text de Unity para mostrar la vida actual.")]
    public Text textoVida; // <-- ¡NUEVA VARIABLE PARA EL TEXTO DE VIDA!
    
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
            Debug.Log("El jugador ha muerto!");
            // Aqui puedes reiniciar la escena o cargar un Game Over
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
                    enemigo.Reaparecer(); 
                }
            }
            Debug.Log("Todos los enemigos han reaparecido en su punto de origen.");
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
    
    // --- Metodo de Daño (Mantenemos para que el enemigo pueda interactuar) ---
    public void RecibirDanio(float cantidad)
    {
        vidaActual -= cantidad;
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
            
            // Opcional: Cambiar color para feedback visual
            if (vidaActual <= 25)
            {
                textoVida.color = Color.red;
            }
            else if (vidaActual <= 50)
            {
                textoVida.color = Color.yellow;
            }
            else
            {
                textoVida.color = Color.white;
            }
        }
    }
}