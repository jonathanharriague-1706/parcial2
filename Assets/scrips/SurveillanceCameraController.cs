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
            // Si hay visión clara, inmediatamente pasa a Detectado.
            ActualizarEstado(CameraState.Detectado);
            tiempoPerdidaVision = datosCamara.duracionDeteccion; // Reinicia el temporizador de persistencia
        }
        else if (estadoActual == CameraState.Detectado)
        {
            // Si no hay visión clara, pero estaba Detectado, inicia el temporizador.
            tiempoPerdidaVision -= Time.deltaTime;
            
            if (tiempoPerdidaVision <= 0)
            {
                // Vuelve a Normal después de un pequeño tiempo de persistencia.
                ActualizarEstado(CameraState.Normal);
            }
        }
        
        // La cámara no tiene lógica de movimiento, solo detección.
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
        
        // La cámara de vigilancia no tiene "ojos" específicos, usamos su centro.
        Vector3 puntoInicial = transform.position; 

        // Vector y distancia al jugador
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
        
        // Convertimos la mitad del ángulo a su Coseno.
        float cosenoAnguloMaximo = Mathf.Cos(mitadAngulo * Mathf.Deg2Rad); 
        
        // Calculamos el Producto Punto entre la dirección de la cámara y la dirección al jugador.
        float productoPunto = Vector3.Dot(transform.forward, direccionAlJugador);
        
        // Si el Producto Punto es mayor o igual al coseno máximo, el jugador está dentro del cono.
        if (productoPunto >= cosenoAnguloMaximo) 
        {
            // 3. RAYCAST (Comprobación de Oclusión/Obstrucción)
            RaycastHit hit;
            
            // Verificamos si hay algún obstáculo en el camino.
            if (Physics.Raycast(puntoInicial, direccionAlJugador, out hit, distanciaAlJugador, datosCamara.capasBloqueo))
            {
                // Obstrucción: dibujamos en rojo
                Debug.DrawRay(puntoInicial, direccionAlJugador * hit.distance, Color.red, 0.1f);
                jugadorEnFOV = false; 
                return false; 
            }
            
            // Vista CLARA: dibujamos en verde
            Debug.DrawRay(puntoInicial, direccionAlJugador * distanciaAlJugador, Color.green, 0.1f);
            jugadorEnFOV = true; 
            return true; 
        }
        
        // No pasó la prueba de ángulo
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
        
        if (vidaActual <= 0) 
        {
            Morir();
        } 
        else 
        {
            // Notificar que está recibiendo daño si es necesario (ej. para un efecto visual)
        }
    }

    void Morir()
    {
        ActualizarEstado(CameraState.Destruido);
        Debug.Log($"Cámara Destruida. Vida restante: {vidaActual}");
        // Aquí podrías agregar efectos de explosión, sonido o deshabilitar visualmente el GameObject.
        // Por ahora solo deshabilitamos la detección y mostramos un log.
        // gameObject.SetActive(false); 
    }
    
    void ActualizarEstado(CameraState nuevoEstado)
    {
        if (estadoActual != nuevoEstado)
        {
            estadoActual = nuevoEstado;
            
            // Puedes agregar lógica de feedback visual aquí (ej. cambiar el color de una luz).
            switch (estadoActual)
            {
                case CameraState.Normal:
                    Debug.Log("Cámara en estado NORMAL.");
                    break;
                case CameraState.Detectado:
                    Debug.Log("Cámara ha DETECTADO al jugador.");
                    // Aquí se puede activar una alarma, Spawn de enemigos, etc.
                    break;
                case CameraState.Destruido:
                    Debug.Log("Cámara DESTRUIDA.");
                    break;
            }
        }
    }
    
    // ===========================================
    // GIZMOS (Visualización del cono de visión) - ¡REQUISITO CUMPLIDO!
    // ===========================================
    private void OnDrawGizmos()
    {
        if (datosCamara == null) return;
        
        float alcance = datosCamara.alcanceVision;
        float mitadAngulo = datosCamara.anguloVision / 2f;
        Vector3 puntoInicial = transform.position;
        
        // Color para el cono: Rojo si detecta, Amarillo si está activo, Gris si está destruida
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

        // 1. Dibuja el círculo de alcance
        Gizmos.DrawWireSphere(puntoInicial, alcance);

        // 2. Dibuja el cono de visión.
        Vector3 direccionFrente = transform.forward * alcance;

        // Dibuja la línea central
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionFrente);

        // Dibuja las líneas de apertura
        
        // Rotación de ángulo negativo (izquierda):
        Quaternion rotacionIzquierda = Quaternion.AngleAxis(-mitadAngulo, Vector3.up);
        Vector3 direccionIzquierda = rotacionIzquierda * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionIzquierda);

        // Rotación de ángulo positivo (derecha):
        Quaternion rotacionDerecha = Quaternion.AngleAxis(mitadAngulo, Vector3.up);
        Vector3 direccionDerecha = rotacionDerecha * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionDerecha);

        // 3. Dibuja la tapa del cono (un arco para mejor visualización)
        // Dibuja un arco de línea entre los límites del FOV
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