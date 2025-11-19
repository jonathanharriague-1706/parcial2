using UnityEngine;
using TMPro; // Necesario para usar TextMeshPro

public class EnemyController : MonoBehaviour
{
    // ===========================================
    // SCRIPTABLE OBJECT DE DATOS
    // ===========================================
    [Header("Configuracion de Datos")]
    [Tooltip("Asigna el Scriptable Object Soldier aquí.")]
    public Soldier datosSoldado;
    
    // ===========================================
    // UI Y ESTADOS
    // ===========================================
    [Header("UI de Estado")]
    // CRÍTICO: La variable es AHORA PRIVADA y se asignará automáticamente en Start()
    private TextMeshProUGUI textoEstadoUI; 
    
    // --- Variables de Control Interno ---
    private Vector3 posicionInicial;
    private PlayerMovement jugador; 
    private EnemyState estadoActual = EnemyState.Normal;
    
    // VARIABLES DE COLISIÓN Y GRAVEDAD:
    private CharacterController controlador; 
    public float gravedad = -9.81f;
    private Vector3 velocidadVertical;
    public float rangoColisionFrontal = 0.2f; 

    private float vidaActual;

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
        
        // CRÍTICO: Asignación automática por código (busca el componente en los hijos)
        textoEstadoUI = GetComponentInChildren<TextMeshProUGUI>();
        if (textoEstadoUI == null)
        {
            Debug.LogError("ERROR: No se encontró el componente TextMeshProUGUI en los hijos del enemigo.");
        }
        
        ActualizarEstado(EnemyState.Normal);
    }

    void Update()
    {
        if (jugador == null || estadoActual == EnemyState.Dead) return;

        // Aplicar Gravedad (Necesario para CharacterController)
        if (controlador.isGrounded)
        {
            velocidadVertical.y = -0.5f; 
        }
        else
        {
            velocidadVertical.y += gravedad * Time.deltaTime;
        }
        controlador.Move(velocidadVertical * Time.deltaTime);
        
        // LÓGICA DE PERSECUCIÓN PERSISTENTE Y DETECCIÓN
        bool jugadorDetectado = JugadorEnConoDeVision();

        if (estadoActual == EnemyState.Chase || jugadorDetectado)
        {
            ActualizarEstado(EnemyState.Chase);
            PerseguirJugador();
            
            // Lógica de Daño al Jugador
            if (Vector3.Distance(transform.position, jugador.transform.position) <= datosSoldado.distanciaAtaque)
            {
                jugador.RecibirDanio(datosSoldado.dañoPorContacto * Time.deltaTime);
                
                // NOTA IMPORTANTE: Si tienes errores sobre 'PenalizaStamina', 'DetenerRegeneracion', etc.,
                // significa que esos métodos deben ser implementados en PlayerMovement.cs o eliminados aquí.
                // Ejemplo de código que causa error si no existe en PlayerMovement:
                // jugador.PenalizaStamina(datosSoldado.dañoPorContacto); 
            }
        }
        else
        {
            ActualizarEstado(EnemyState.Normal);
        }

        // LÓGICA DE LA UI DE ESTADO (Billboard: Mira a la cámara)
        if (textoEstadoUI != null)
        {
            textoEstadoUI.transform.LookAt(Camera.main.transform);
            textoEstadoUI.transform.Rotate(0, 180, 0); 
        }
    }
    
    // ===========================================
    // FUNCION CRÍTICA: VISIÓN Y RAYCAST (Obstrucción)
    // ===========================================
    bool JugadorEnConoDeVision()
    {
        Vector3 posicionOjosEnemigo = transform.position + Vector3.up * 1.5f; 
        Vector3 posicionJugadorCentrada = jugador.transform.position + Vector3.up * 0.9f; 

        Vector3 direccionAlJugador = (posicionJugadorCentrada - posicionOjosEnemigo).normalized;
        float distanciaAlJugador = Vector3.Distance(posicionOjosEnemigo, posicionJugadorCentrada);

        if (distanciaAlJugador > datosSoldado.rangoVision) return false; 

        float angulo = Vector3.Angle(transform.forward, direccionAlJugador);
        if (angulo < datosSoldado.anguloVision / 2f)
        {
            RaycastHit hit;
            if (Physics.Raycast(posicionOjosEnemigo, direccionAlJugador, out hit, datosSoldado.rangoVision, datosSoldado.capasBloqueo))
            {
                if (hit.transform.root.GetComponent<PlayerMovement>() != jugador)
                {
                    return false; // Vista bloqueada por un obstáculo.
                }
            }
            return true;
        }
        return false; 
    }
    
    // ===========================================
    // FUNCION CRÍTICA: PERSECUCIÓN (Colisión rígida)
    // ===========================================
    void PerseguirJugador()
    {
        Vector3 direccionHaciaJugador = (jugador.transform.position - transform.position).normalized;
        
        Vector3 posicionSinY = new Vector3(jugador.transform.position.x, transform.position.y, jugador.transform.position.z);
        transform.LookAt(posicionSinY); 
        
        // Detección de Colisión Frontal (Raycast preventivo)
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, controlador.radius + rangoColisionFrontal, datosSoldado.capasBloqueo))
        {
            if (hit.transform.root.GetComponent<PlayerMovement>() != jugador)
            {
                Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
                return; // Detiene el movimiento
            }
        }
        
        controlador.Move(direccionHaciaJugador * datosSoldado.velocidadPersecucion * Time.deltaTime);
    }
    
    // ===========================================
    // FUNCIÓN: RECIBIR DAÑO (Llamado desde GunController)
    // ===========================================
    public void RecibirDanio(float cantidad)
    {
        if (estadoActual == EnemyState.Dead) return;
        vidaActual -= cantidad;
        vidaActual = Mathf.Max(vidaActual, 0f); 
        ActualizarEstado(EnemyState.Damage); 

        if (vidaActual <= 0)
        {
            Morir();
        }
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
            transform.position = posicionInicial; 
            vidaActual = datosSoldado.vidaMaxima; 
            ActualizarEstado(EnemyState.Normal); 
        }
    }
    
    // ===========================================
    // FUNCIÓN CRÍTICA: ACTUALIZAR ESTADO Y UI
    // ===========================================
    void ActualizarEstado(EnemyState nuevoEstado)
    {
        if (estadoActual != nuevoEstado)
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
        // ... (Lógica de dibujo de Gizmos)
    }
}