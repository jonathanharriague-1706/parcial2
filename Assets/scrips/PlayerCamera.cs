using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Configuracion de Rotacion")]
    [Tooltip("Sensibilidad de giro del mouse.")]
    public float sensibilidad = 500f;
    
    [Tooltip("Limite superior e inferior de la camara (vertical).")]
    public float limiteVertical = 80f; // Limita la mirada a 80 grados arriba/abajo

    // --- Variables de Control Interno ---
    private float rotacionVertical = 0f;

    void Start()
    {
        // Bloquea el cursor para un control de juego estandarizado
        Cursor.lockState = CursorLockMode.Locked; 
    }

    void Update()
    {
        // 1. ROTACION HORIZONTAL (Player)
        // El Player (padre) gira en el eje Y
        float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 2. ROTACION VERTICAL (Camara Hija)
        // La camara (hija del Player) gira en el eje X
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime;
        
        rotacionVertical -= mouseY;
        // Limita la rotacion para que no pueda mirar mas alla del limite (mirar detras)
        rotacionVertical = Mathf.Clamp(rotacionVertical, -limiteVertical, limiteVertical);

        // Aplica la rotacion a la camara hija
        Camera.main.transform.localRotation = Quaternion.Euler(rotacionVertical, 0f, 0f);
    }
}