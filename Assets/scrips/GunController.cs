using UnityEngine;
using UnityEngine.UI; // Mantener solo para 'Image' (retícula)
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
    // VARIABLES DE MUNICIÓN Y RECARGA
    // ===============================================
    [Header("Municion y Recarga")]
    [Tooltip("Máximo de balas por cargador.")]
    public int balasPorCargador = 15;
    [Tooltip("Tiempo que tarda la recarga en segundos.")]
    public float tiempoRecarga = 1.5f;

    // --- Variables de Control Interno ---
    private float siguienteTiempoDisparo = 0f;
    private Camera tpsCamera; 
    
    private int balasActuales;
    private bool estaRecargando = false;
    
    // **SE ELIMINA: playerMovement (ya no se usa la UI de vida)**

    void Start()
    {
        tpsCamera = Camera.main;
        if (tpsCamera == null)
        {
            Debug.LogError("GunController: No se encontro la camara etiquetada como 'MainCamera'.");
        }
        
        // **SE ELIMINA: La referencia a PlayerMovement ya que no se usa para la UI**

        // Inicializa el cargador lleno
        balasActuales = balasPorCargador;

        // **SE ELIMINA: La actualización inicial de la UI**
    }

    void Update()
    {
        // Bloquea cualquier acción si el arma está recargando
        if (estaRecargando) return;

        ActualizarReticula(); // Llama a la logica de cambio de color
        
        // **SE ELIMINA: ActualizarTextoVida();**
        
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
                // El cargador está vacío: Bloqueamos el disparo.
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
        
        // Para debug, mostramos las balas en consola
        if (Input.GetKeyDown(KeyCode.LeftShift)) 
        {
            Debug.Log($"ESTADO: Balas: {balasActuales}/{balasPorCargador}, Recargando: {estaRecargando}");
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
            // Nota: Se asume que 'EnemyController' sigue existiendo para este script.
            if (hit.transform.GetComponent<EnemyController>() != null)
            {
                // Apuntando a un enemigo: Color rojo
                reticulaImagen.color = Color.red; 
            }
            else
            {
                // Golpeando algo mas: Color verde
                reticulaImagen.color = Color.green;
            }
        }
        else
        {
            // Si no golpea nada: Color verde
            reticulaImagen.color = Color.green;
        }
    }
    
    void Disparar()
    {
        // Consumir una bala del cargador
        balasActuales--; 
        
        // **SE ELIMINA: ActualizarTextoBalas();**
        Debug.Log($"Disparo! Balas restantes: {balasActuales}");

        RaycastHit hit;
        
        if (tpsCamera != null && Physics.Raycast(tpsCamera.transform.position, tpsCamera.transform.forward, out hit, rango))
        {
            Debug.Log($"Impacto en: {hit.collider.name}");

            // Busca el EnemyController para aplicar el dano
            EnemyController targetEnemy = hit.transform.GetComponent<EnemyController>();
            
            if (targetEnemy != null)
            {
                // Nota: Asume que el EnemyController tiene el método RecibirDanio
                targetEnemy.RecibirDanio(danio); 
            }
        }
    }
    
    // ===============================================
    // LÓGICA DE RECARGA
    // ===============================================
    void Recargar()
    {
        // Solo se recarga si no estamos recargando ya
        if (estaRecargando) return; 

        // Iniciamos la coroutine de recarga
        StartCoroutine(RecargarConRetraso());
    }

    // Coroutine para manejar el tiempo de recarga
    IEnumerator RecargarConRetraso()
    {
        estaRecargando = true;
        Debug.Log("Iniciando recarga...");

        // **SE ELIMINA: La línea para mostrar "RECARGANDO..." en la UI**

        // Esperamos el tiempo de recarga
        yield return new WaitForSeconds(tiempoRecarga);

        // Recarga completa
        balasActuales = balasPorCargador;
        estaRecargando = false;
        
        // **SE ELIMINA: ActualizarTextoBalas();**
        Debug.Log("Recarga completa. Balas actuales: " + balasActuales);
    }
    
    // **SE ELIMINAN: Los métodos ActualizarTextoBalas() y ActualizarTextoVida()**
}