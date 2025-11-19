using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // ===========================================
    // SCRIPTABLE OBJECT Y UI (TEXT MESH)
    // ===========================================
    [Header("Configuracion de Datos")]
    public Soldier datosSoldado;
    
    [Header("UI de Estado")]
    public TextMesh textoEstadoUI; 

   
    
    
    // --- Variables de Control Interno ---
    private Vector3 posicionInicial; // ALMACENA la posición de inicio
    private PlayerMovement jugador; 
    private EnemyState estadoActual = EnemyState.Normal;
    
    // VARIABLES DE MOVIMIENTO Y DAÑO
    private CharacterController controlador; 
    public float gravedad = -9.81f;
    public float rangoColisionFrontal = 0.2f; 
    private Vector3 velocidadVertical;
    private float vidaActual;
    
    // Control del color de daño (SOLO visual, NO detiene el movimiento)
    private float tiempoDañoVisual = 0f; 
    public float duracionDañoVisual = 0.2f; 
    
    // Estado para los Gizmos (se actualiza en la función de visión)
    private bool jugadorDetectado = false; 

    public enum EnemyState { Normal, Chase, Damage, Dead }

    void Start()
    {
        if (datosSoldado == null)
        {
            Debug.LogError("ERROR: El Scriptable Object 'Soldier Data' no está asignado.");
            return;
        }

        // CRÍTICO: Guardamos la posición inicial DEL ENEMIGO en el momento en que inicia la escena.
        posicionInicial = transform.position; 
        vidaActual = datosSoldado.vidaMaxima;
        
        jugador = FindAnyObjectByType<PlayerMovement>(); 
        if (jugador == null) Debug.LogError("ERROR: PlayerMovement no encontrado.");

        controlador = GetComponent<CharacterController>(); 
        if (controlador == null) Debug.LogError("ERROR: EnemyController requiere un CharacterController.");
        
        if (textoEstadoUI == null) Debug.LogError("ERROR: El componente Text Mesh no está asignado en el Inspector.");
        
        ActualizarEstado(EnemyState.Normal, true); 
    }

    void Update()
    {
        if (jugador == null || estadoActual == EnemyState.Dead) return;
        
        // 1. Lógica para Reiniciar el Enemigo (F3)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Reaparecer();
            return;
        }

        // 2. Manejo del tiempo visual de daño
        bool enDañoVisual = tiempoDañoVisual > 0;
        if (enDañoVisual)
        {
            tiempoDañoVisual -= Time.deltaTime;
        }

        // 3. Aplicar Gravedad
        if (controlador.isGrounded) velocidadVertical.y = -0.5f; 
        else velocidadVertical.y += gravedad * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);
        
        // 4. LÓGICA DE DETECCIÓN Y TRANSICIÓN 
        
        // ** Llamada CRÍTICA: La función de visión ahora también actualiza 'jugadorDetectado' para los Gizmos **
        bool jugadorTieneVisionClara = JugadorEnConoDeVisionClara(); 
        
        // El enemigo actúa si hay visión O si ya estaba persiguiendo O si está en daño visual (persistencia).
        if (jugadorTieneVisionClara || estadoActual == EnemyState.Chase || enDañoVisual)
        {
            // Lógica de Detención CRÍTICA:
            if ((estadoActual == EnemyState.Chase || estadoActual == EnemyState.Damage) && !jugadorTieneVisionClara && !enDañoVisual)
            {
                ActualizarEstado(EnemyState.Normal);
                return; // Detiene la persecución y el movimiento.
            }

            // Lógica de Persecución y Actualización de Estado:
            
            PerseguirJugador();

            // Actualizamos el estado para la UI: el estado visual de daño tiene prioridad.
            if (!enDañoVisual)
            {
                // Si no hay daño visual, forzamos el estado a CHASE.
                ActualizarEstado(EnemyState.Chase);
            }
        }
        else if (estadoActual != EnemyState.Normal)
        {
            // Transición a Normal si no hay visión, no está en Chase, y no está en daño visual.
            ActualizarEstado(EnemyState.Normal);
        }
        
        // 5. LÓGICA DE LA UI DE ESTADO (Billboard)
        if (textoEstadoUI != null)
        {
            textoEstadoUI.transform.LookAt(Camera.main.transform);
            textoEstadoUI.transform.Rotate(0, 180, 0); 
        }
    }
    
    // ===========================================
    // FUNCIÓN CRÍTICA: VISIÓN CLARA (USANDO PRODUCTO PUNTO)
    // ===========================================
    /// <summary>
    /// Comprueba si el jugador está visible, usando Producto Punto para el ángulo.
    /// </summary>
    bool JugadorEnConoDeVisionClara()
    {
        Vector3 posicionOjosEnemigo = transform.position + Vector3.up * 1.8f; 
        Vector3 posicionJugadorCentrada = jugador.transform.position + Vector3.up * 0.9f; 

        Vector3 direccionAlJugador = (posicionJugadorCentrada - posicionOjosEnemigo).normalized;
        float distanciaAlJugador = Vector3.Distance(posicionOjosEnemigo, posicionJugadorCentrada);

        // 1. RANGO DE VISIÓN
        // Si ya está persiguiendo, mantenemos la visión a un rango ampliado (usando el rango del SO).
        // Si no está persiguiendo, usamos el alcance de 10m requerido en el prompt.
        float rangoLimite = (estadoActual == EnemyState.Chase || estadoActual == EnemyState.Damage || tiempoDañoVisual > 0) 
            ? datosSoldado.alcanceDeVision * 2f // Rango ampliado para persistencia
            :datosSoldado.alcanceDeVision; // 10 metros del requisito inicial
        
        if (distanciaAlJugador > rangoLimite) 
        {
            jugadorDetectado = false; // Actualizar estado para Gizmos
            return false; 
        }

        // 2. DETECCIÓN ANGULAR (PRODUCTO PUNTO) - ¡REQUISITO CLAVE!
        // El Producto Punto entre dos vectores unitarios (como transform.forward y direccionAlJugador) 
        // es igual al coseno del ángulo entre ellos.
        
        // a) Convertimos la mitad del ángulo a su Coseno.
        float cosenoAnguloMaximo = Mathf.Cos(datosSoldado.mitadAnguloDeVision * Mathf.Deg2Rad); 
        
        // b) Calculamos el Producto Punto.
        float productoPunto = Vector3.Dot(transform.forward, direccionAlJugador);
        
        // c) Si el Producto Punto es mayor o igual al coseno máximo, el jugador está dentro del cono.
        if (productoPunto >= cosenoAnguloMaximo) 
        {
            // 3. RAYCAST (Comprobación de Oclusión/Obstrucción)
            RaycastHit hit;
            
            // Raycast SÓLO busca la capa de Bloqueo ('detectable') definida en el Scriptable Object
            if (Physics.Raycast(posicionOjosEnemigo, direccionAlJugador, out hit, distanciaAlJugador, datosSoldado.capasBloqueo))
            {
                Debug.DrawRay(posicionOjosEnemigo, direccionAlJugador * hit.distance, Color.red, 0.1f);
                jugadorDetectado = false; // Obstrucción
                return false; 
            }
            
            // Si pasamos el ángulo y el raycast: JUGADOR DETECTADO
            Debug.DrawRay(posicionOjosEnemigo, direccionAlJugador * distanciaAlJugador, Color.green, 0.1f);
            jugadorDetectado = true; // Detectado
            return true; 
        }
        
        // No pasó el ángulo
        jugadorDetectado = false; 
        return false; 
    }
    
    // ===========================================
    // FUNCION CRÍTICA: PERSECUCIÓN (Movimiento)
    // ===========================================
    void PerseguirJugador()
    {
        Vector3 direccionHaciaJugador = (jugador.transform.position - transform.position).normalized;
        
        Vector3 posicionSinY = new Vector3(jugador.transform.position.x, transform.position.y, jugador.transform.position.z);
        transform.LookAt(posicionSinY); 
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, controlador.radius + rangoColisionFrontal, datosSoldado.capasBloqueo))
        {
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
            return; 
        }
        
        controlador.Move(direccionHaciaJugador * datosSoldado.velocidadPersecucion * Time.deltaTime);
    }
    
    // ===========================================
    // FUNCIONES DE VIDA Y ESTADO
    // ===========================================
    public void RecibirDanio(float cantidad)
    {
        if (estadoActual == EnemyState.Dead) return;
        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); 
        
        tiempoDañoVisual = duracionDañoVisual; 
        ActualizarEstado(EnemyState.Damage); 
        
        if (vidaActual <= 0) Morir();
    }

    void Morir()
    {
        ActualizarEstado(EnemyState.Dead);
        Debug.Log("Enemigo muerto.");
        gameObject.SetActive(false); 
    }
    
    // **FUNCIÓN CRÍTICA DE REAPARICIÓN**
    public void Reaparecer()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); 
        }
        
        // Restablece la posición al punto de inicio guardado en Start().
        transform.position = posicionInicial; 
        
        vidaActual = datosSoldado.vidaMaxima; 
        ActualizarEstado(EnemyState.Normal, true); 
        // Es importante resetear la velocidad vertical (gravedad) para evitar que se caiga.
        velocidadVertical = Vector3.zero; 
        Debug.Log("Enemigo Reiniciado a la Posición Inicial.");
    }
    
    void ActualizarEstado(EnemyState nuevoEstado, bool force = false)
    {
        if (estadoActual != nuevoEstado || force)
        {
            estadoActual = nuevoEstado;
            
            if (textoEstadoUI != null)
            {
                textoEstadoUI.text = estadoActual.ToString().ToUpper();
                
                switch (estadoActual)
                {
                    case EnemyState.Normal:
                        textoEstadoUI.color = Color.green;
                        break;
                    case EnemyState.Chase:
                        textoEstadoUI.color = Color.red;
                        break;
                    case EnemyState.Damage:
                        textoEstadoUI.color = Color.yellow;
                        break;
                    case EnemyState.Dead:
                        textoEstadoUI.color = Color.gray;
                        break;
                }
            }
        }
    }
    
    // ===========================================
    // GIZMOS (Visualización en el Editor) - ¡REQUISITO CUMPLIDO!
    // ===========================================
    private void OnDrawGizmosSelected()
    {
        // 1. Dibuja el círculo de alcance (radio de 10 mts) en el suelo (Color Amarillo).
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, datosSoldado.alcanceDeVision);

        // 2. Dibuja el cono de visión.
        Vector3 puntoInicial = transform.position;
        Vector3 direccionFrente = transform.forward * datosSoldado.alcanceDeVision;

        // Dibuja la línea central
        Gizmos.color = Color.white;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionFrente);

        // Dibuja las líneas de apertura (30 grados a cada lado).
        // La rotación se aplica sobre el eje Y (Vector3.up)
        
        // Rotación de -30 grados (izquierda):
        Quaternion rotacionIzquierda = Quaternion.AngleAxis(-datosSoldado.mitadAnguloDeVision, Vector3.up);
        Vector3 direccionIzquierda = rotacionIzquierda * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionIzquierda);

        // Rotación de +30 grados (derecha):
        Quaternion rotacionDerecha = Quaternion.AngleAxis(datosSoldado.mitadAnguloDeVision, Vector3.up);
        Vector3 direccionDerecha = rotacionDerecha * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionDerecha);

        // 3. Si el jugador está detectado, dibuja una línea roja de detección.
        if (jugadorDetectado && jugador != null)
        {
            Gizmos.color = Color.red;
            // Dibujamos la línea a la altura aproximada del centro del cuerpo del jugador
            Gizmos.DrawLine(transform.position + Vector3.up * 1.0f, jugador.transform.position + Vector3.up * 1.0f);
        }
    }
}