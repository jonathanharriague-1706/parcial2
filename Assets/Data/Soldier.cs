using UnityEngine;

// Crea el menú de creación en el editor de Unity
[CreateAssetMenu(fileName = "NewSoldierData", menuName = "Enemy/Soldier Data")]
public class Soldier : ScriptableObject
{
    // Datos de combate
    public float vidaMaxima = 100f;
    // CRÍTICO: Renombrado de daño a un término más general (Ataque a Distancia)
    public float dañoDeAtaque = 10f; 
    
    // Datos de IA y Movimiento
    public float alcanceDeVision = 10f; 
    public float mitadAnguloDeVision = 30f; 
    public float velocidadPersecucion = 3f;
    // La distanciaAtaque se usa ahora como el rango mínimo para considerarse "cerca"
    // para los Gizmos, aunque el ataque real usa el Raycast dentro del alcance de visión.
    public float distanciaAtaque = 1.5f; 
    
    // Cadencia de disparo/ataque
    public float tiempoEntreAtaques = 1.5f;
    
    // Capas que el Raycast de visión y ataque bloquearán
    public LayerMask capasBloqueo; 
}