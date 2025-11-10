using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // ===============================================
    // REQUERIDO: REFERENCIAS DE JUEGO
    // ===============================================
    [Header("Referencias de Juego")]
    [Tooltip("Referencia al script del enemigo para forzar la reaparicion.")]
    public EnemyController enemigo;

    // ===============================================
    // Variables de Movimiento
    // ===============================================
    [Header("Configuracion de Movimiento")]
    public float velocidadMovimiento = 5f;
    public float fuerzaSalto = 7f;
    private bool estaEnTierra;

    // ===============================================
    // Variables de Vida
    // ===============================================
    [Header("Configuracion de Vida")]
    public float vidaActual = 100f; 
    public float vidaMaxima = 100f; 
    
    // ===============================================
    // Variables de Stamina
    // ===============================================
    [Header("Configuracion de Stamina")]
    public float staminaActual = 100f;
    public float staminaMaxima = 100f;
    public float tasaRegeneracion = 5f;
    
    // --- Bandera de control de regeneracion ---
    private bool puedeRegenerar = true; 
    private float velocidadBase = 5f; 

    // --- Variables de Control Interno ---
    private CharacterController controlador;
    private Vector3 velocidadVertical;
    
    void Start()
    {
        controlador = GetComponent<CharacterController>();
        if (controlador == null)
        {
            Debug.LogError("PlayerMovement requiere un CharacterController.");
        }
        
        vidaActual = vidaMaxima;
        velocidadBase = velocidadMovimiento; 
    }

    void Update()
    {
        // ----------------------------------------------------
        // LOGICA DE REAPARICION (F3)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.F3) && enemigo != null)
        {
            Debug.Log("Jugador detecta F3, llama a Reaparecer.");
            enemigo.Reaparecer();
        }
        
        // ----------------------------------------------------
        // LOGICA DE MOVIMIENTO
        // ----------------------------------------------------
        estaEnTierra = controlador.isGrounded;

        if (estaEnTierra && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f; 
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 movimiento = transform.right * x + transform.forward * z;
        controlador.Move(movimiento * velocidadMovimiento * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && estaEnTierra)
        {
            velocidadVertical.y = Mathf.Sqrt(fuerzaSalto * -2f * Physics.gravity.y);
        }

        velocidadVertical.y += Physics.gravity.y * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);

        // ----------------------------------------------------
        // LOGICA DE STAMINA (REGENERACION CORREGIDA)
        // ----------------------------------------------------
        if (puedeRegenerar && staminaActual < staminaMaxima) // SOLO REGENERA SI LA BANDERA ES TRUE
        {
            staminaActual += tasaRegeneracion * Time.deltaTime;
            staminaActual = Mathf.Clamp(staminaActual, 0f, staminaMaxima);
        }

        // ----------------------------------------------------
        // LOGICA DE MUERTE DEL JUGADOR
        // ----------------------------------------------------
        if (vidaActual <= 0)
        {
            Debug.Log("El jugador ha muerto!");
        }
    }

    // ----------------------------------------------------
    // METODOS DE INTERACCION DEL ENEMIGO
    // ----------------------------------------------------

    // Llamado cuando el enemigo nos ve: APLICA DAÑO Y DETIENE REGENERACION
    public void PenalizarStamina(float cantidad)
    {
        staminaActual -= cantidad * Time.deltaTime;
        staminaActual = Mathf.Clamp(staminaActual, 0f, staminaMaxima);
        
        // Penalizar velocidad si la stamina llega a cero
        if (staminaActual <= 0)
        {
            velocidadMovimiento = 2f; 
        }
    }

    // NUEVO METODO: Llamado por el enemigo para DETENER la regeneracion.
    public void DetenerRegeneracion()
    {
        puedeRegenerar = false;
    }

    // NUEVO METODO: Llamado por el enemigo para PERMITIR la regeneracion.
    public void PermitirRegeneracion()
    {
        puedeRegenerar = true;
        // Restaurar la velocidad cuando se permite regenerar 
        if (staminaActual > 0)
        {
            velocidadMovimiento = velocidadBase; 
        }
    }
}