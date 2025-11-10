using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // --- Variables Regulables ---
    [Header("Estadisticas del Enemigo")]
    public float vidaActual = 100f;
    public float vidaMaxima = 100f;

    // ===========================================
    // CONO DE VISION Y PERSECUCION
    // ===========================================
    [Header("Cono de Vision y Persecucion")]
    [Tooltip("El radio maximo de vision (alcance del cono).")]
    public float rangoVision = 10f; 
    
    [Tooltip("El angulo del cono de vision (ej: 90 para 45 grados a cada lado).")]
    [Range(0, 360)]
    public float anguloVision = 90f; 

    [Tooltip("Velocidad de movimiento del enemigo al perseguir.")]
    public float velocidadPersecucion = 3f;

    // --- Variables de Control Interno ---
    private Vector3 posicionInicial;
    private PlayerMovement jugador; 
    private EnemyState estadoActual = EnemyState.Normal;

    public enum EnemyState { Normal, Chase, Damage, Dead }

    void Start()
    {
        posicionInicial = transform.position; 
        vidaActual = vidaMaxima;
        
        jugador = FindAnyObjectByType<PlayerMovement>(); 
        if (jugador == null) Debug.LogError("ERROR: PlayerMovement no encontrado.");

        ActualizarEstado(EnemyState.Normal);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("F3 detectado. Intentando reaparecer.");
            Reaparecer();
        }
        
        if (jugador == null || estadoActual == EnemyState.Dead) return;

        // Logica de deteccion y persecucion
        if (JugadorEnConoDeVision())
        {
            // DENTRO DEL CONO: Penalizar Stamina Y DETENER REGENERACION
            ActualizarEstado(EnemyState.Chase);
            PerseguirJugador();
            jugador.PenalizarStamina(1f); 
            jugador.DetenerRegeneracion(); 
        }
        else
        {
            // FUERA DEL CONO: Detener persecucion y PERMITIR REGENERACION
            ActualizarEstado(EnemyState.Normal);
            jugador.PermitirRegeneracion(); 
        }
    }
    
    bool JugadorEnConoDeVision()
    {
        float distanciaAlJugador = Vector3.Distance(transform.position, jugador.transform.position);
        if (distanciaAlJugador > rangoVision)
        {
            return false; 
        }

        Vector3 direccionAlJugador = (jugador.transform.position - transform.position).normalized;
        float angulo = Vector3.Angle(transform.forward, direccionAlJugador);

        if (angulo < anguloVision / 2f)
        {
            return true; 
        }
        
        return false; 
    }
    
    void PerseguirJugador()
    {
        Vector3 direccionHaciaJugador = (jugador.transform.position - transform.position).normalized;
        transform.position += direccionHaciaJugador * velocidadPersecucion * Time.deltaTime;
        
        transform.LookAt(jugador.transform); 
    }

    public void RecibirDanio(float cantidad)
    {
        if (estadoActual == EnemyState.Dead) return;

        vidaActual -= cantidad;
        ActualizarEstado(EnemyState.Damage); 

        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        ActualizarEstado(EnemyState.Dead);
        Debug.Log("Enemigo muerto. Presiona F3 para reaparecer.");
        gameObject.SetActive(false); 
        jugador.PermitirRegeneracion(); 
    }

    public void Reaparecer()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); 
            transform.position = posicionInicial; 
            vidaActual = vidaMaxima; 
            ActualizarEstado(EnemyState.Normal); 
            Debug.Log("Enemigo reaparece forzadamente.");
        }
        else if (gameObject.activeSelf)
        {
            Debug.Log("El enemigo ya esta activo. No se puede reaparecer.");
        }
    }
    
    void ActualizarEstado(EnemyState nuevoEstado)
    {
        if (estadoActual != nuevoEstado)
        {
            estadoActual = nuevoEstado;
            Debug.Log($"Estado del Enemigo: {estadoActual}");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (estadoActual == EnemyState.Chase)
        {
            Gizmos.color = Color.red; 
        }
        else
        {
            Gizmos.color = Color.yellow; 
        }

        Gizmos.DrawWireSphere(transform.position, rangoVision);

        Vector3 posicionEnemigo = transform.position + Vector3.up * 0.1f; 
        Vector3 direccionVision = transform.forward;
        
        Gizmos.DrawRay(posicionEnemigo, direccionVision * rangoVision);

        float mitadAngulo = anguloVision / 2f;
        
        Quaternion rotacionIzquierda = Quaternion.AngleAxis(-mitadAngulo, Vector3.up);
        Vector3 direccionIzquierda = rotacionIzquierda * direccionVision;
        Gizmos.DrawRay(posicionEnemigo, direccionIzquierda * rangoVision);

        Quaternion rotacionDerecha = Quaternion.AngleAxis(mitadAngulo, Vector3.up);
        Vector3 direccionDerecha = rotacionDerecha * direccionVision;
        Gizmos.DrawRay(posicionEnemigo, direccionDerecha * rangoVision);
    }
}
