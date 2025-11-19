using UnityEngine;

// Crea el menú de creación en el editor de Unity
[CreateAssetMenu(fileName = "NewSoldierData", menuName = "Enemy/Soldier Data")]
public class Soldier : ScriptableObject
{
    [Header("Estadisticas Base")]
    public float vidaMaxima = 100f;
    public float dañoPorContacto = 10f;
    
    [Header("Vision y Comportamiento")]
    public float rangoVision = 10f; 
    [Range(0, 360)]
    public float anguloVision = 90f; 
    public float velocidadPersecucion = 3f;
    public float distanciaAtaque = 1.5f; 
    
    [Tooltip("Capas que bloquean la visión y el movimiento (paredes).")]
    public LayerMask capasBloqueo; 
}