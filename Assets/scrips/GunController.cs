using UnityEngine;
using UnityEngine.UI; // ¡IMPORTANTE! Necesario para usar los componentes Text e Image (retícula)
using System.Collections; 

public class GunController : MonoBehaviour
{
    // ===============================================
    // REQUISITO: Variables de Pistola Regulables
    // ===============================================
    [Header("Configuracion de Pistola")]
    public float rango = 50f;
    public float danio = 25f;
    public float cadencia = 0.5f;

    [Header("Feedback Visual")]
    [Tooltip("Referencia a la imagen de la reticula en el Canvas.")]
    public Image reticulaImagen; // Mira (Crosshair)
    
    // ===============================================
    // VARIABLES DE MUNICIÓN, RECARGA Y UI
    // ===============================================
    [Header("Municion y Recarga")]
    [Tooltip("Máximo de balas por cargador.")]
    public int balasPorCargador = 15;
    [Tooltip("Tiempo que tarda la recarga en segundos.")]
    public float tiempoRecarga = 1.5f;

    [Header("Referencias de UI")]
    [Tooltip("El componente Text de Unity para mostrar las balas actuales.")]
    public Text textoBalas; // <-- ¡NUEVA VARIABLE PARA EL TEXTO DE BALAS!
    
    // --- Variables de Control Interno ---
    private float siguienteTiempoDisparo = 0f;
    private Camera tpsCamera; 
    
    private int balasActuales;
    private bool estaRecargando = false;
    
    void Start()
    {
        tpsCamera = Camera.main;
        if (tpsCamera == null)
        {
            Debug.LogError("GunController: No se encontro la camara etiquetada como 'MainCamera'.");
        }
        
        // Inicializa el cargador lleno
        balasActuales = balasPorCargador;

        // ¡LLAMADA INICIAL A LA UI! Muestra las balas al empezar.
        ActualizarTextoBalas();
    }

    void Update()
    {
        // Bloquea cualquier acción si el arma está recargando
        if (estaRecargando) return;

        ActualizarReticula(); // Llama a la logica de cambio de color
        
        // 1. Lógica de Disparo (Click Izquierdo / Fire1)
        if (Input.GetButtonDown("Fire1") && Time.time >= siguienteTiempoDisparo)
        {
            if (balasActuales > 0)
            {
                Disparar();
                siguienteTiempoDisparo = Time.time + cadencia; 
            }
            else
            {
                // El cargador está vacío
                Debug.Log("¡Cargador vacío! Presiona R para recargar.");
            }
        }
        
        // 2. Lógica de Recarga (Tecla R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Solo se puede recargar si el cargador NO está lleno
            if (balasActuales < balasPorCargador)
            {
                Recargar();
            }
            else
            {
                // Bloquea la recarga si está lleno
                Debug.Log("Cargador lleno. No necesita recargar.");
            }
        }
    }

    // Método para cambiar el color de la mira (feedback)
    void ActualizarReticula()
    {
        // Si no hay camara o no hay reticula asignada, salimos
        if (tpsCamera == null || reticulaImagen == null) return;

        RaycastHit hit;
        
        // Lanzamos un raycast desde el centro de la camara (la mira)
        if (Physics.Raycast(tpsCamera.transform.position, tpsCamera.transform.forward, out hit, rango))
        {
            // Apuntando a un enemigo: Color rojo
            if (hit.transform.GetComponent<EnemyController>() != null)
            {
                reticulaImagen.color = Color.red; 
            }
            else
            {
                // Golpeando algo mas: Color verde semi-transparente
                reticulaImagen.color = new Color(0, 1, 0, 0.7f); 
            }
        }
        else
        {
            // Si no golpea nada: Color blanco semi-transparente
            reticulaImagen.color = new Color(1, 1, 1, 0.7f);
        }
    }
    
    void Disparar()
    {
        balasActuales--; 
        
        // ¡LLAMADA A LA UI! Actualiza el texto de balas inmediatamente después del disparo
        ActualizarTextoBalas();
        Debug.Log($"Disparo! Balas restantes: {balasActuales}");

        RaycastHit hit;
        
        if (tpsCamera != null && Physics.Raycast(tpsCamera.transform.position, tpsCamera.transform.forward, out hit, rango))
        {
            // Busca el EnemyController para aplicar el dano
            EnemyController targetEnemy = hit.transform.GetComponent<EnemyController>();
            
            if (targetEnemy != null)
            {
                targetEnemy.RecibirDanio(danio); 
            }
        }
    }
    
    // ===============================================
    // LÓGICA DE RECARGA
    // ===============================================
    void Recargar()
    {
        if (estaRecargando) return; 
        StartCoroutine(RecargarConRetraso());
    }

    // Coroutine para manejar el tiempo de recarga
    IEnumerator RecargarConRetraso()
    {
        estaRecargando = true;
        Debug.Log("Iniciando recarga...");

        // Muestra el estado de recarga en la UI
        if (textoBalas != null)
        {
             textoBalas.text = "RECARGANDO...";
        }

        yield return new WaitForSeconds(tiempoRecarga);

        // Recarga completa
        balasActuales = balasPorCargador;
        estaRecargando = false;
        
        // ¡LLAMADA A LA UI! Actualiza el texto al finalizar la recarga.
        ActualizarTextoBalas();
        Debug.Log("Recarga completa. Balas actuales: " + balasActuales);
    }

    // ===============================================
    // MÉTODOS DE UI (Munición)
    // ===============================================

    /// <summary>
    /// Actualiza el componente de texto de la UI con la munición actual.
    /// </summary>
    void ActualizarTextoBalas()
    {
        if (textoBalas != null)
        {
            textoBalas.text = $"MUNICIÓN: {balasActuales}/{balasPorCargador}";
        }
    }
}