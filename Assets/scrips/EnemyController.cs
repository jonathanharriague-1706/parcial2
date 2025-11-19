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
    private Vector3 posicionInicial;
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

    public enum EnemyState { Normal, Chase, Damage, Dead }

    void Start()
    {
        if (datosSoldado == null)
        {
            Debug.LogError("ERROR: El Scriptable Object 'Soldier Data' no está asignado.");
            return;
        }

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
        
        bool jugadorTieneVisionClara = JugadorEnConoDeVisionClara(); 
        
        // El enemigo actúa si hay visión O si ya estaba persiguiendo O si está en daño visual (persistencia).
        if (jugadorTieneVisionClara || estadoActual == EnemyState.Chase || enDañoVisual)
        {
            // Lógica de Detención CRÍTICA:
            // **SOLO** vuelve a Normal si: 
            // 1. Estaba persiguiendo O en daño.
            // 2. La visión CLARA se pierde (obstrucción).
            // 3. El efecto visual de daño terminó.
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
            // Si enDañoVisual es true, el estado se mantiene en DAMAGE para el color amarillo.
            
            // Lógica de Daño al Jugador (Ataque)
            // ...
        }
        else if (estadoActual != EnemyState.Normal)
        {
            // Transición a Normal si no hay visión, no está en Chase, y no está en daño visual.
            // Esto solo pasa si el enemigo nunca detectó al jugador o después de una reaparición.
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
    // FUNCIÓN CRÍTICA: VISIÓN CLARA (Ahora ignora el rango si está en CHASE/DAMAGE)
    // ===========================================
    bool JugadorEnConoDeVisionClara()
    {
        Vector3 posicionOjosEnemigo = transform.position + Vector3.up * 1.8f; 
        Vector3 posicionJugadorCentrada = jugador.transform.position + Vector3.up * 0.9f; 

        Vector3 direccionAlJugador = (posicionJugadorCentrada - posicionOjosEnemigo).normalized;
        float distanciaAlJugador = Vector3.Distance(posicionOjosEnemigo, posicionJugadorCentrada);

        // **CRÍTICO:** Rango Ilimitado (1000f) si ya está persiguiendo o en daño visual.
        // Si está en Normal, usa el rango de visión configurado.
        float rangoDeChequeo = (estadoActual == EnemyState.Chase || estadoActual == EnemyState.Damage || tiempoDañoVisual > 0) 
            ? 1000f // Rango prácticamente infinito para persecución.
            : datosSoldado.rangoVision; // Rango limitado para detección inicial.
        
        if (distanciaAlJugador > rangoDeChequeo) return false; 

        float angulo = Vector3.Angle(transform.forward, direccionAlJugador);
        
        // **CRÍTICO:** El ángulo solo importa si está en estado Normal.
        if (estadoActual == EnemyState.Chase || estadoActual == EnemyState.Damage || tiempoDañoVisual > 0 || angulo < datosSoldado.anguloVision / 2f)
        {
            RaycastHit hit;
            
            // Raycast SÓLO busca la capa de Bloqueo ('detectable')
            if (Physics.Raycast(posicionOjosEnemigo, direccionAlJugador, out hit, distanciaAlJugador, datosSoldado.capasBloqueo))
            {
                // La visión se pierde ÚNICAMENTE por una obstrucción.
                Debug.DrawRay(posicionOjosEnemigo, direccionAlJugador * hit.distance, Color.red, 0.1f);
                return false; 
            }
            
            Debug.DrawRay(posicionOjosEnemigo, direccionAlJugador * distanciaAlJugador, Color.green, 0.1f);
            return true; // Vista CLARA.
        }
        
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
    
    public void Reaparecer()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); 
        }
        transform.position = posicionInicial; 
        vidaActual = datosSoldado.vidaMaxima; 
        ActualizarEstado(EnemyState.Normal, true); 
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
    
    private void OnDrawGizmosSelected()
    {
        // Lógica para dibujar el cono de visión
    }
}