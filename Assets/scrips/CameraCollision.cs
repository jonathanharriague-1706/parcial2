using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    // ===============================================
    // Configuración de Posición y TPS (Control Manual)
    // ===============================================
    [Header("Posición Ideal y Distancia (Control Manual)")]
    
    [Tooltip("Distancia Z deseada (valor positivo, el script lo hace negativo). Controla qué tan lejos está la cámara.")]
    public float distanciaDeseada = 3.0f; // La magnitud Z de la posicion local
    
    [Tooltip("Altura Y deseada. Controla qué tan alta está la cámara.")]
    public float alturaDeseada = 1.7f; 

    [Tooltip("Desplazamiento X deseado. Positivo = Derecha, Negativo = Izquierda (Vista sobre el hombro).")]
    public float desplazamientoDeseado = 0.5f; 
    
    // ===============================================
    // Configuración de Colisión y Suavizado
    // ===============================================
    [Header("Configuración de Colisión y Suavizado")]
    [Tooltip("Distancia mínima que debe mantenerse del obstáculo.")]
    public float distanciaMinima = 0.5f; 
    
    [Tooltip("Suavidad con la que la cámara se mueve al colisionar.")]
    public float suavidadMovimiento = 10f; 

    [Tooltip("Máscara de capas que la cámara debe evitar (paredes, objetos, etc.).")]
    public LayerMask capasAEvitar; 

    // --- Variables Internas ---
    private float distanciaColisionActual; // Distancia dinámica (Z) basada en colisión
    private Vector3 posicionInicialLocal; // Almacena la posicion local configurada en Start

    void Start()
    {
        // 1. Inicializa la distancia dinámica con la deseada.
        distanciaColisionActual = distanciaDeseada;
        
        // 2. Establece la posición inicial local con los valores deseados (X, Y) y la Z negativa.
        posicionInicialLocal = new Vector3(desplazamientoDeseado, alturaDeseada, -distanciaColisionActual);
        transform.localPosition = posicionInicialLocal;
    }

    void LateUpdate()
    {
        // -------------------------------------------------------------------------
        // 1. Configurar Puntos de Referencia para el Raycast
        // -------------------------------------------------------------------------
        
        // El Raycast se lanza desde el centro de rotación del Jugador.
        Vector3 origenRaycast = transform.parent.position;
        
        // Punto final ideal (en el mundo): Dónde la cámara quiere estar.
        Vector3 posicionIdealGlobal = transform.parent.TransformPoint(
            desplazamientoDeseado, 
            alturaDeseada, 
            -distanciaDeseada
        );
        
        // Dirección del Raycast: Desde el origen hacia la posición ideal.
        Vector3 direccionRaycast = (posicionIdealGlobal - origenRaycast).normalized;
        // La longitud máxima que el rayo debe recorrer.
        float longitudRayo = Vector3.Distance(origenRaycast, posicionIdealGlobal);

        // -------------------------------------------------------------------------
        // 2. Detección de Colisión (Raycast)
        // -------------------------------------------------------------------------

        RaycastHit hit;
        float distanciaObjetivo = longitudRayo; // Distancia ideal si no hay colisión

        // Lanzamos el Raycast
        if (Physics.Raycast(origenRaycast, direccionRaycast, out hit, longitudRayo, capasAEvitar))
        {
            // Colisión detectada: La distancia objetivo es la distancia del golpe menos un margen.
            distanciaObjetivo = Mathf.Clamp(hit.distance - distanciaMinima, 0f, longitudRayo);
        }
        
        // -------------------------------------------------------------------------
        // 3. Aplicar el Movimiento (Detención Instantánea en Colisión)
        // -------------------------------------------------------------------------
        
        // La posición final global de la cámara basada en la distanciaObjetivo.
        Vector3 posicionFinalGlobal = origenRaycast + (direccionRaycast * distanciaObjetivo);

        // CONDICIÓN CLAVE: Si la distancia dinámica actual es mayor que la distancia objetivo, 
        // significa que estamos CHOCANDO o intentando movernos a través de algo.
        if (distanciaColisionActual > distanciaObjetivo)
        {
            // Detención: Mueve la cámara inmediatamente (sin Lerp) a la posición de colisión.
            transform.position = posicionFinalGlobal;
            // Actualizamos la distancia dinámica para el siguiente frame
            distanciaColisionActual = distanciaObjetivo; 
        }
        else
        {
            // No hay colisión: Mueve la cámara suavemente a la posición ideal.
            transform.position = Vector3.Lerp(transform.position, posicionFinalGlobal, Time.deltaTime * suavidadMovimiento);
            // Actualizamos la distancia dinámica para el siguiente frame (basada en donde terminó el Lerp)
            distanciaColisionActual = Vector3.Distance(transform.position, origenRaycast);
        }
    }
}