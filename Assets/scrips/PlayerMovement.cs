using UnityEngine;

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
    // Variables de Vida (Mantenemos por la logica de DANO y MUERTE)
    // ===============================================
    [Header("Configuracion de Vida")]
    public float vidaActual = 100f; 
    public float vidaMaxima = 100f; 
    
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
        // NOTA: El agachado está sin corregir aquí.
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
    }
}