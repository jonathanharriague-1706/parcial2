using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con la clase Image (la mira)

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
    public Image reticulaImagen; // Variable para conectar la mira
    
    // --- Variables de Control Interno ---
    private float siguienteTiempoDisparo = 0f;
    private Camera tpsCamera; 
    
    void Start()
    {
        tpsCamera = Camera.main;
        if (tpsCamera == null)
        {
            Debug.LogError("GunController: No se encontro la camara etiquetada como 'MainCamera'.");
        }
    }

    void Update()
    {
        ActualizarReticula(); // Llama a la logica de cambio de color
        
        // Disparo al apretar Click Izquierdo (Fire1)
        if (Input.GetButtonDown("Fire1") && Time.time >= siguienteTiempoDisparo)
        {
            Disparar();
            siguienteTiempoDisparo = Time.time + cadencia; 
        }
    }

    // Metodo para cambiar el color de la mira (feedback)
    void ActualizarReticula()
    {
        // Si no hay camara o no hay reticula asignada, salimos
        if (tpsCamera == null || reticulaImagen == null) return;

        RaycastHit hit;
        
        // Lanzamos un raycast desde el centro de la camara (la mira)
        if (Physics.Raycast(tpsCamera.transform.position, tpsCamera.transform.forward, out hit, rango))
        {
            // Intentamos obtener el componente EnemyController del objeto golpeado
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
        RaycastHit hit;
        
        if (tpsCamera != null && Physics.Raycast(tpsCamera.transform.position, tpsCamera.transform.forward, out hit, rango))
        {
            Debug.Log($"Impacto en: {hit.collider.name}");

            // Busca el EnemyController para aplicar el dano
            EnemyController targetEnemy = hit.transform.GetComponent<EnemyController>();
            
            if (targetEnemy != null)
            {
                targetEnemy.RecibirDanio(danio);
            }
        }
    }
}
