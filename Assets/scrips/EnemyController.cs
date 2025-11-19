using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // ===========================================
    // SCRIPTABLE OBJECT Y UI (TEXT MESH)
    // ===========================================
    [Header("Configuracion de Datos")]
    // Referencia al Scriptable Object que contiene todas las estadísticas
    public Soldier datosSoldado;
    
    [Header("UI de Estado")]
    public TextMesh textoEstadoUI; 

    // --- Variables de Control Interno ---
    private Vector3 posicionInicial; // ALMACENA la posición de inicio
    private PlayerMovement jugador; 
    
    // Estados de la IA: Normal (patrulla/espera), Chase (persecución), Dead (muerto)
    private EnemyState estadoActual = EnemyState.Normal; 
    
    // VARIABLES DE MOVIMIENTO Y DAÑO
    private CharacterController controlador; 
    public float gravedad = -9.81f;
    public float rangoColisionFrontal = 0.2f; 
    private Vector3 velocidadVertical;
    private float vidaActual;
    
    // CRÍTICO: TEMPORIZADOR DE PERSISTENCIA DE PERSECUCIÓN (5 segundos)
    private float tiempoPersecucionForzada = 0f; 
    public float duracionPersecucionForzada = 5.0f; // El enemigo persigue por 5s después de recibir daño
    
    // TEMPORIZADOR DE FLASH VISUAL (0.5 segundos)
    private float tiempoFlashDanio = 0f; 
    public float duracionFlashDanio = 0.5f; 
    
    // CRÍTICO: Temporizador para la cadencia de ataque (se usa tanto para Raycast como para Contacto)
    private float siguienteTiempoAtaque = 0f;
    
    // Estado para los Gizmos (se actualiza en la función de visión)
    private bool jugadorDetectado = false; 

    public enum EnemyState { Normal, Chase, Dead }

    void Start()
    {
        if (datosSoldado == null)
        {
            // CRÍTICO: Si falta el SO, detener la ejecución.
            Debug.LogError("ERROR: El Scriptable Object 'Soldier Data' no está asignado.");
            enabled = false; // Desactiva el script
            return;
        }

        posicionInicial = transform.position; 
        vidaActual = datosSoldado.vidaMaxima;
        
        // CRÍTICO: FindAnyObjectByType está obsoleto, pero se mantiene si estás en una versión de Unity más antigua.
        // Si usas una versión moderna, FindObjectOfType<PlayerMovement>() o FindAnyObjectByType<PlayerMovement>() es lo correcto.
        jugador = FindAnyObjectByType<PlayerMovement>(); 
        if (jugador == null) Debug.LogError("ERROR: PlayerMovement no encontrado en la escena.");

        controlador = GetComponent<CharacterController>(); 
        if (controlador == null) 
        {
             Debug.LogError("ERROR: EnemyController requiere un CharacterController.");
             enabled = false; 
             return;
        }
        
        if (textoEstadoUI == null) Debug.LogError("ERROR: El componente Text Mesh no está asignado en el Inspector.");
        
        ActualizarEstado(EnemyState.Normal, true); 
    }

    void Update()
    {
        // Si el jugador no se ha encontrado o el enemigo está muerto, no hacemos nada.
        if (jugador == null || estadoActual == EnemyState.Dead) return;
        
        // CRÍTICO: Si el jugador está muerto, el enemigo debe detenerse.
        if (jugador.estaMuerto) 
        {
             if (estadoActual != EnemyState.Normal)
             {
                 ActualizarEstado(EnemyState.Normal);
             }
             return;
        }
        
        // 1. Manejo de temporizadores
        bool enFlashDanio = tiempoFlashDanio > 0;
        if (enFlashDanio)
        {
            tiempoFlashDanio -= Time.deltaTime;
        }
        
        bool enPersecucionForzada = tiempoPersecucionForzada > 0;
        if (enPersecucionForzada)
        {
            tiempoPersecucionForzada -= Time.deltaTime;
        }

        // 2. Aplicar Gravedad
        if (controlador.isGrounded) velocidadVertical.y = -0.5f; 
        else velocidadVertical.y += gravedad * Time.deltaTime;
        controlador.Move(velocidadVertical * Time.deltaTime);
        
        // 3. LÓGICA DE DETECCIÓN Y PERSECUCIÓN
        // Comprobamos si el jugador está en rango y no obstruido (Raycast limpio).
        bool jugadorTieneVisionClara = JugadorEnConoDeVisionClara(); 
        
        // El enemigo debe actuar si: 
        // a) Hay visión clara.
        // b) Ya estaba en modo CHASE.
        // c) Está en persecución forzada (acaba de recibir daño).
        bool debeActuar = jugadorTieneVisionClara || estadoActual == EnemyState.Chase || enPersecucionForzada;
        
        if (debeActuar)
        {
            // Transición a CHASE si estamos en Normal y detectamos o estamos forzados.
            if (estadoActual == EnemyState.Normal && (jugadorTieneVisionClara || enPersecucionForzada))
            {
                ActualizarEstado(EnemyState.Chase);
            }

            float distanciaAlJugador = Vector3.Distance(transform.position, jugador.transform.position);

            // 3a. Lógica de ATAQUE (por Raycast/Disparo)
            if (distanciaAlJugador <= datosSoldado.distanciaAtaque && estadoActual == EnemyState.Chase)
            {
                AtacarJugador();
            }
            else if (estadoActual == EnemyState.Chase)
            {
                // Perseguir si estamos en Chase y fuera de rango de ataque.
                PerseguirJugador();
            }

            // 3b. Lógica de Detención CRÍTICA (Bloqueo y Persistencia)
            if (estadoActual == EnemyState.Chase)
            {
                 // Volvemos a Normal SÓLO si:
                 // 1. No tenemos visión clara Y
                 // 2. El temporizador de persecución forzada terminó.
                 if (!jugadorTieneVisionClara && !enPersecucionForzada)
                 {
                   ActualizarEstado(EnemyState.Normal);
                 }
            }
        }
        
        // 4. LÓGICA DE LA UI DE ESTADO (Billboard)
        if (textoEstadoUI != null && Camera.main != null)
        {
            textoEstadoUI.transform.LookAt(Camera.main.transform);
            textoEstadoUI.transform.Rotate(0, 180, 0); 

            if (estadoActual == EnemyState.Dead)
            {
                textoEstadoUI.text = "MUERTO";
                textoEstadoUI.color = Color.gray;
            }
            else if (estadoActual == EnemyState.Normal)
            {
                textoEstadoUI.text = "NORMAL";
                textoEstadoUI.color = Color.green;
            }
            else // EnemyState.Chase
            {
                // CRÍTICO: Si está persiguiendo Y tiene el flash de daño activo, mostramos AMARILLO.
                if (tiempoFlashDanio > 0)
                {
                    textoEstadoUI.text = "CHASE (DAÑO)";
                    textoEstadoUI.color = Color.yellow;
                }
                else
                {
                    // Si está persiguiendo, pero el flash terminó, mostramos ROJO.
                    textoEstadoUI.text = "CHASE";
                    textoEstadoUI.color = Color.red;
                }
            }
        }
    }
    
    // ===========================================
    // MANEJO DE COLISIONES DEL CHARACTER CONTROLLER (DAÑO POR CONTACTO FÍSICO)
    // ===========================================
    /// <summary>
    /// Se llama cuando el CharacterController golpea otro Collider durante su movimiento.
    /// Utilizamos esto para aplicar el daño por contacto al jugador si estamos en modo Chase.
    /// </summary>
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Solo aplicamos daño de contacto si estamos persiguiendo y ha pasado el tiempo de cadencia.
        if (estadoActual != EnemyState.Chase || Time.time < siguienteTiempoAtaque)
        {
            return;
        }

        // Intentar obtener el componente PlayerMovement del objeto golpeado.
        PlayerMovement playerHit;
        if (hit.gameObject.TryGetComponent(out playerHit))
        {
            // CRÍTICO: Aplicamos el daño por contacto (el valor del disparo).
            // Corregido: Usar 'dañoDeAtaque' ya que 'dañoPorContacto' no existe en Soldier.cs
            playerHit.RecibirDanio(datosSoldado.dañoDeAtaque);
            
            Debug.Log($"ENEMIGO: DAÑO POR CONTACTO FÍSICO! Daño a jugador: {datosSoldado.dañoDeAtaque}");

            // Reinicia el temporizador de ataque para respetar la cadencia (cooldown) y no hacer daño cada frame
            siguienteTiempoAtaque = Time.time + datosSoldado.tiempoEntreAtaques;
        }
    }
    
    // ===========================================
    // FUNCIÓN CRÍTICA: VISIÓN CLARA (USANDO PRODUCTO PUNTO Y RAYCAST)
    // ===========================================
    /// <summary>
    /// Comprueba si el jugador está visible, usando Producto Punto y Raycast.
    /// </summary>
    bool JugadorEnConoDeVisionClara()
    {
        Vector3 posicionOjosEnemigo = transform.position + Vector3.up * 1.8f; 
        Vector3 posicionJugadorCentrada = jugador.transform.position + Vector3.up * 0.9f; 

        Vector3 direccionAlJugador = (posicionJugadorCentrada - posicionOjosEnemigo).normalized;
        float distanciaAlJugador = Vector3.Distance(posicionOjosEnemigo, posicionJugadorCentrada);

        // 1. RANGO DE VISIÓN
        // Usamos rango ampliado si estamos persiguiendo.
        float rangoLimite = (estadoActual == EnemyState.Chase || tiempoPersecucionForzada > 0) 
            ? datosSoldado.alcanceDeVision * 3f 
            : datosSoldado.alcanceDeVision; 
        
        if (distanciaAlJugador > rangoLimite) 
        {
            jugadorDetectado = false; 
            return false; 
        }

        // 2. DETECCIÓN ANGULAR (PRODUCTO PUNTO)
        float mitadAngulo = datosSoldado.mitadAnguloDeVision; 
        float cosenoAnguloMaximo = Mathf.Cos(mitadAngulo * Mathf.Deg2Rad); 
        float productoPunto = Vector3.Dot(transform.forward, direccionAlJugador);
        
        // En Chase, no requerimos estar dentro del ángulo (persecución de 360 grados) o si está forzado.
        bool dentroAngulo = (estadoActual == EnemyState.Chase || tiempoPersecucionForzada > 0) || (productoPunto >= cosenoAnguloMaximo);
        
        if (dentroAngulo) 
        {
            // 3. RAYCAST (Comprobación de Oclusión/Obstrucción)
            RaycastHit hit;
            
            // Si el Raycast golpea ALGO ANTES que el jugador (obstrucción)
            if (Physics.Raycast(posicionOjosEnemigo, direccionAlJugador, out hit, distanciaAlJugador, datosSoldado.capasBloqueo))
            {
                Debug.DrawRay(posicionOjosEnemigo, direccionAlJugador * hit.distance, Color.red, 0.1f);
                jugadorDetectado = false; 
                return false; 
            }
            
            // Si pasamos la visibilidad (Raycast limpio): JUGADOR DETECTADO
            Debug.DrawRay(posicionOjosEnemigo, direccionAlJugador * distanciaAlJugador, Color.green, 0.1f);
            jugadorDetectado = true; 
            return true; 
        }
        
        jugadorDetectado = false; 
        return false; 
    }
    
    // ===========================================
    // FUNCIÓN CRÍTICA: ATACAR JUGADOR (Disparo y Daño con Raycast)
    // ===========================================
    /// <summary>
    /// El enemigo intenta realizar un ataque de Raycast (simulando un disparo)
    /// que puede golpear al jugador o ser bloqueado por un obstáculo.
    /// </summary>
    void AtacarJugador()
    {
        // Gira para mirar al jugador (solo rotación horizontal)
        Vector3 posicionSinY = new Vector3(jugador.transform.position.x, transform.position.y, jugador.transform.position.z);
        transform.LookAt(posicionSinY); 
        
        // Comprueba la cadencia de fuego
        if (Time.time >= siguienteTiempoAtaque)
        {
            if (jugador != null)
            {
                // 1. Definir los puntos y dirección del Raycast
                // Punto de origen del disparo (altura aproximada del pecho/hombros del enemigo)
                Vector3 puntoDisparo = transform.position + Vector3.up * 1.5f;
                // Posición objetivo (centro del jugador)
                Vector3 posicionJugador = jugador.transform.position + Vector3.up * 1.0f;
                Vector3 direccion = (posicionJugador - puntoDisparo).normalized;
                
                float rangoAtaque = datosSoldado.distanciaAtaque; 
                
                // CRÍTICO: La máscara del disparo debe incluir TODAS las capas (`~0`) 
                LayerMask mascaraDisparo = ~0; 

                RaycastHit hit;
                
                // 2. Ejecutar el Raycast
                if (Physics.Raycast(puntoDisparo, direccion, out hit, rangoAtaque, mascaraDisparo))
                {
                    // 3. Comprobar qué fue golpeado
                    PlayerMovement playerHit;
                    
                    // a) ¿Golpeó al jugador? (Usamos TryGetComponent para una comprobación más limpia)
                    if (hit.transform.TryGetComponent(out playerHit))
                    {
                        // Si golpeamos al jugador, aplicamos daño
                        // Corregido: Usar 'dañoDeAtaque' ya que 'dañoPorContacto' no existe en Soldier.cs
                        playerHit.RecibirDanio(datosSoldado.dañoDeAtaque); 
                        Debug.Log($"ENEMIGO: DISPARO CERTERO! Daño a jugador: {datosSoldado.dañoDeAtaque}");
                        // Dibujar línea VERDE si golpea al jugador
                        Debug.DrawRay(puntoDisparo, direccion * hit.distance, Color.green, 0.5f);
                    }
                    // b) ¿Golpeó un obstáculo que bloquea el disparo?
                    else if (((1 << hit.collider.gameObject.layer) & datosSoldado.capasBloqueo) != 0) 
                    {
                         // El objeto golpeado está en la capa de bloqueo definida.
                         Debug.DrawRay(puntoDisparo, direccion * hit.distance, Color.red, 0.5f);
                         Debug.Log($"ENEMIGO: Disparo bloqueado por obstáculo: {hit.collider.gameObject.name}");
                    }
                    else
                    {
                        // c) Golpeó otro objeto (ej: otro enemigo, un objeto neutral)
                        Debug.DrawRay(puntoDisparo, direccion * hit.distance, Color.blue, 0.5f);
                        Debug.Log($"ENEMIGO: Disparo golpeó objeto neutral: {hit.collider.gameObject.name}");
                    }
                }
                else
                {
                    // 4. Falló el Raycast (simplemente no golpeó a nada dentro del rango)
                    Debug.DrawRay(puntoDisparo, direccion * rangoAtaque, Color.yellow, 0.5f);
                    Debug.Log("ENEMIGO: Raycast de ataque fallido/no golpeó nada.");
                }

                // 5. Reinicia el temporizador de cadencia
                siguienteTiempoAtaque = Time.time + datosSoldado.tiempoEntreAtaques;
            }
        }
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
        // Comprueba si hay un obstáculo que bloquea el movimiento justo en frente
        if (Physics.Raycast(transform.position, transform.forward, out hit, controlador.radius + rangoColisionFrontal, datosSoldado.capasBloqueo))
        {
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
            // Si hay un obstáculo cerca, no nos movemos pero seguimos en CHASE
            return; 
        }
        
        controlador.Move(direccionHaciaJugador * datosSoldado.velocidadPersecucion * Time.deltaTime);
    }
    
    // ===========================================
    // FUNCIONES DE VIDA Y ESTADO
    // ===========================================

    /// <summary>
    /// Recibe daño, asegura el estado CHASE y activa los temporizadores de persistencia.
    /// </summary>
    public void RecibirDanio(float cantidad)
    {
        if (estadoActual == EnemyState.Dead) return;
        
        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); 
        
        // 1. Activa la persistencia de persecución (5 segundos, sin importar visión)
        tiempoPersecucionForzada = duracionPersecucionForzada; 
        
        // 2. Activa el flash visual de daño (0.5 segundos)
        tiempoFlashDanio = duracionFlashDanio; 
        
        // 3. FUERZA el estado a CHASE (movimiento asegurado)
        ActualizarEstado(EnemyState.Chase); 
        
        if (vidaActual <= 0) Morir();
    }

    void Morir()
    {
        ActualizarEstado(EnemyState.Dead);
        Debug.Log("Enemigo muerto.");
        // Ocultar al enemigo, en lugar de desactivarlo completamente (para evitar perder referencias de componentes)
        // Pero mantendré 'gameObject.SetActive(false)' si así es como manejas la muerte.
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
        siguienteTiempoAtaque = 0f; 
        velocidadVertical = Vector3.zero; 
        tiempoPersecucionForzada = 0f;
        tiempoFlashDanio = 0f;
        Debug.Log("Enemigo Reiniciado a la Posición Inicial.");
    }
    
    void ActualizarEstado(EnemyState nuevoEstado, bool force = false)
    {
        if (estadoActual != nuevoEstado || force)
        {
            estadoActual = nuevoEstado;
        }
    }
    
    // ===========================================
    // GIZMOS (Visualización en el Editor)
    // ===========================================
    private void OnDrawGizmosSelected()
    {
        if (datosSoldado == null) return;
        
        // 1. Dibuja el círculo de alcance (radio de visión en el SO)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, datosSoldado.alcanceDeVision);
        
        // 2. Dibuja el círculo de rango de ataque
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, datosSoldado.distanciaAtaque);

        // 3. Dibuja el cono de visión.
        float mitadAngulo = datosSoldado.mitadAnguloDeVision; 
        Vector3 puntoInicial = transform.position;
        Vector3 direccionFrente = transform.forward * datosSoldado.alcanceDeVision;

        // Dibuja la línea central
        Gizmos.color = Color.white;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionFrente);

        // Dibuja las líneas de apertura
        Quaternion rotacionIzquierda = Quaternion.AngleAxis(-mitadAngulo, Vector3.up);
        Vector3 direccionIzquierda = rotacionIzquierda * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionIzquierda);

        Quaternion rotacionDerecha = Quaternion.AngleAxis(mitadAngulo, Vector3.up);
        Vector3 direccionDerecha = rotacionDerecha * direccionFrente;
        Gizmos.DrawLine(puntoInicial, puntoInicial + direccionDerecha);

        // 4. Si el jugador está detectado, dibuja una línea roja de detección.
        if (jugadorDetectado && jugador != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.0f, jugador.transform.position + Vector3.up * 1.0f);
        }
    }
}