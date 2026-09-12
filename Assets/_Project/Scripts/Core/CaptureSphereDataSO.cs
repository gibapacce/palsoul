using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject que define um tier de Esfera de Contenção.
    /// GDD seção 5.2: múltiplos tiers (Comum / Aprimorada / Rara / Lendária).
    /// Crie assets em ScriptableObjects/Items via Assets > Create > Palsoul > Capture Sphere Data.
    /// </summary>
    [CreateAssetMenu(fileName = "CaptureSphere_Common", menuName = "Palsoul/Capture Sphere Data")]
    public class CaptureSphereDataSO : ScriptableObject
    {
        [Header("Identificação")]
        public string sphereName = "Esfera Comum";
        public Sprite icon;

        [Header("Fórmula de Captura")]
        [Tooltip("Chance base de captura (0–1). Multiplicada pelos demais modificadores.")]
        [Range(0f, 1f)]
        public float baseChance = 0.30f;

        [Header("Visual")]
        [Tooltip("Cor da esfera para VFX de arremesso (placeholder até ter sprite dedicado).")]
        public Color sphereColor = Color.white;

        [Header("Tier")]
        [Tooltip("Tier numérico (0 = Comum, 1 = Aprimorada, 2 = Rara, 3 = Lendária). Usado para ordenação.")]
        [Min(0)]
        public int tier = 0;
    }
}
