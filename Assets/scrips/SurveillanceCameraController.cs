using UnityEngine;

public class SurveillanceCameraController : MonoBehaviour
{
    // ===========================================
    // SCRIPTABLE OBJECT Y REFERENCIAS
    // ===========================================
    [Header("Configuracion de Datos")]
    [Tooltip("Asigna el Scriptable Object de CameraData aquí.")]
    public CameraData datosCamara;
    
    // --- Variables de Control Interno ---
    private PlayerMovement jugador; 
    private float vidaActual;
    private CameraState estadoActual = CameraState.Normal;
    private float tiempoPerdidaVision = 0f;

    // CRÍTICO PARA REAPARICIÓN (F3)
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    // Estado para la visualización de Gizmos
    private bool jugadorEnFOV = false; 

    public enum CameraState { Normal, Detectado, Destruido }

    void Start()
    {
        if (datosCamara == null)
        {
            Debug.LogError("ERROR: El Scriptable Object 'Camera Data' no está asignado. La cámara no funcionará.");
            enabled = false; 
            return;
        }

        // GUARDAR ESTADO INICIAL
        posicionInicial = transform.position; 
        rotacionInicial = transform.rotation;

        vidaActual = datosCamara.vidaMaxima;
        
        // Busca al jugador una sola vez al inicio.
        jugador = FindAnyObjectByType<PlayerMovement>(); 
        if (jugador == null) Debug.LogError("ERROR: PlayerMovement no encontrado.");

        ActualizarEstado(CameraState.Normal);
    }

    void Update()
    {
        if (estadoActual == CameraState.Destruido || jugador == null) return;

        // 1. LÓGICA DE DETECCIÓN VECTORIAL
        bool visionClara = JugadorEnConoDeVisionClara(); 

        if (visionClara)
        {
            ActualizarEstado(CameraState.Detectado);
            tiempoPerdidaVision = datosCamara.duracionDeteccion; 
            
            // [OPCIONAL] Alerta a enemigos cercanos o llama a otra función de reacción aquí
        }
        else if (estadoActual == CameraState.Detectado)
        {
            tiempoPerdidaVision -= Time.deltaTime;
            
            if (tiempoPerdidaVision <= 0)
            {
                ActualizarEstado(CameraState.Normal);
            }
        }
    }
    
    // ===========================================
    // FUNCIÓN CRÍTICA: VISIÓN CLARA (USANDO PRODUCTO PUNTO)
    // ===========================================
    /// <summary>
    /// Comprueba si el jugador está en el cono de visión y si hay un Raycast libre.
    /// </summary>
    bool JugadorEnConoDeVisionClara()
    {
        // Vector de la posición del jugador centrado (asumimos altura similar)
        Vector3 posicionJugadorCentrada = jugador.transform.position + Vector3.up * 0.9f; 
        Vector3 puntoInicial = transform.position; 

        Vector3 direccionAlJugador = (posicionJugadorCentrada - puntoInicial).normalized;
        float distanciaAlJugador = Vector3.Distance(puntoInicial, posicionJugadorCentrada);

        // 1. RANGO DE VISIÓN
        if (distanciaAlJugador > datosCamara.alcanceVision) 
        {
            jugadorEnFOV = false; 
            return false; 
        }

        // 2. DETECCIÓN ANGULAR (PRODUCTO PUNTO)
        float mitadAngulo = datosCamara.anguloVision / 2f;
        float cosenoAnguloMaximo = Mathf.Cos(mitadAngulo * Mathf.Deg2Rad); 
        float productoPunto = Vector3.Dot(transform.forward, direccionAlJugador);
        
        if (productoPunto >= cosenoAnguloMaximo) 
        {
            // 3. RAYCAST (Comprobación de Oclusión/Obstrucción)
            RaycastHit hit;
            
            if (Physics.Raycast(puntoInicial, direccionAlJugador, out hit, distanciaAlJugador, datosCamara.capasBloqueo))
            {
                Debug.DrawRay(puntoInicial, direccionAlJugador * hit.distance, Color.red, 0.1f);
                jugadorEnFOV = false; 
                return false; 
            }
            
            Debug.DrawRay(puntoInicial, direccionAlJugador * distanciaAlJugador, Color.green, 0.1f);
            jugadorEnFOV = true; 
            return true; 
        }
        
        jugadorEnFOV = false; 
        return false; 
    }
    
    // ===========================================
    // FUNCIONES DE VIDA Y ESTADO
    // ===========================================
    
    /// <summary>
    /// La cámara puede recibir daño.
    /// </summary>
    public void RecibirDanio(float cantidad)
    {
        if (estadoActual == CameraState.Destruido) return;
        
        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); 
        
        Debug.Log($"CÁMARA: Daño recibido: {cantidad}. Vida restante: {vidaActual}");
        
        if (vidaActual <= 0) 
        {
            Morir();
        } 
    }

    void Morir()
    {
        ActualizarEstado(CameraState.Destruido);
        Debug.Log($"CÁMARA: Objeto Destruido. Vida final: {vidaActual}");
        
        // CRÍTICO: Desactiva los componentes que podrían ser necesarios para la detección/colisión.
        if (GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().enabled = false;
        }
        if (GetComponent<Renderer>() != null)
        {
            GetComponent<Renderer>().enabled = false;
        }
        enabled = false; // Desactiva este script
    }

    /// <summary>
    /// Restaura la vida, el estado y la posición de la cámara (Usado por F3 en PlayerMovement).
    /// </summary>
    public void Reaparecer()
    {
        // 1. Resetear el estado
        vidaActual = datosCamara.vidaMaxima;
        tiempoPerdidaVision = 0f;

        // 2. Reactivar componentes
        if (GetComponent<Collider>() != null)
        {
             GetComponent<Collider>().enabled = true;
        }
        if (GetComponent<Renderer>() != null)
        {
             GetComponent<Renderer>().enabled = true;
        }
        enabled = true; // Reactiva este script
        
        // 3. Resetear posición y rotación a su estado inicial
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;

        ActualizarEstado(CameraState.Normal);
        Debug.Log("CÁMARA: Reaparecida y restaurada a estado Normal.");
    }
    
    void ActualizarEstado(CameraState nuevoEstado)
    {
        if (estadoActual != nuevoEstado)
        {
            estadoActual = nuevoEstado;
            
            switch (estadoActual)
            {
                case CameraState.Normal:
                    // Lógica para estado normal (ej. luz verde)
                    Debug.Log("Cámara en estado NORMAL.");
                    break;
                case CameraState.Detectado:
                    // Lógica para detección (ej. luz roja)
                    Debug.Log("Cámara ha DETECTADO al jugador.");
                    break;
                case CameraState.Destruido:
                    // Lógica para destrucción (ej. desactivar luz)
                    Debug.Log("Cámara DESTRUIDA.");
                    break;
            }
        }
    }
    
    // ===========================================
    // GIZMOS (Visualización del cono de visión)
    // ===========================================
    private void OnDrawGizmos()
    {
        if (datosCamara == null) return;
        
        float alcance = datosCamara.alcanceVision;
        float mitadAngulo = datosCamara.anguloVision / 2f;
        Vector3 puntoInicial = transform.position;
        
        if (estadoActual == CameraState.Destruido)
        {
            Gizmos.color = Color.gray;
        }
        else if (jugadorEnFOV || estadoActual == CameraState.Detectado)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }

        Gizmos.DrawWireSphere(puntoInicial, alcance);
        Vector3 direccionFrente = transform.forward * alcance;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionFrente);

        Quaternion rotacionIzquierda = Quaternion.AngleAxis(-mitadAngulo, Vector3.up);
        Vector3 direccionIzquierda = rotacionIzquierda * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionIzquierda);

        Quaternion rotacionDerecha = Quaternion.AngleAxis(mitadAngulo, Vector3.up);
        Vector3 direccionDerecha = rotacionDerecha * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionDerecha);

        int segmentos = 16;
        Vector3 puntoAnterior = puntoInicial + direccionIzquierda;
        
        for (int i = 1; i <= segmentos; i++)
        {
            float anguloActual = -mitadAngulo + (datosCamara.anguloVision / segmentos) * i;
            Quaternion rotacionActual = Quaternion.AngleAxis(anguloActual, Vector3.up);
            Vector3 puntoActual = puntoInicial + (rotacionActual * transform.forward * alcance);
            
            Gizmos.DrawLine(puntoAnterior, puntoActual);
            puntoAnterior = puntoActual;
        }
    }
}