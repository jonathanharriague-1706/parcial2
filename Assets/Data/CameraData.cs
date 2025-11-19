using UnityEngine;

[CreateAssetMenu(fileName = "NuevaCamaraData", menuName = "Enemigos/Datos de Camara de Vigilancia", order = 2)]
public class CameraData : ScriptableObject
{
    [Header("Configuración de Vida")]
    [Tooltip("Vida máxima de la cámara (Requisito: 100).")]
    public float vidaMaxima = 100f;
    
    [Header("Configuración de Visión")]
    [Tooltip("Alcance de visión en metros (Requisito: 5mts).")]
    public float alcanceVision = 5f;
    
    [Tooltip("Ángulo de apertura total en grados (Requisito: 60°).")]
    public float anguloVision = 60f;

    [Tooltip("Capas que pueden bloquear la línea de visión (muros, obstáculos).")]
    public LayerMask capasBloqueo;
    
    [Header("Configuración de Detección")]
    [Tooltip("Tiempo que la cámara permanece en estado 'Detectado' después de perder la visión.")]
    public float duracionDeteccion = 1f;
}
