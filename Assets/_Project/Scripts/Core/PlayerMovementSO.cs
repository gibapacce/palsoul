using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject com todos os parâmetros de movimento do player.
    /// Crie um asset em ScriptableObjects/Player via menu Assets > Create > Palsoul > Player Movement Data.
    /// Nunca coloque magic numbers no PlayerController — toda constante vem daqui.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementData", menuName = "Palsoul/Player Movement Data")]
    public class PlayerMovementSO : ScriptableObject
    {
        [Header("Velocidade")]
        [Tooltip("Velocidade de movimento em unidades Unity por segundo.")]
        [Min(0f)]
        public float moveSpeed = 5f;

        [Header("Física")]
        [Tooltip("Desaceleração aplicada ao Rigidbody2D quando não há input (0 = desliza, 1 = para imediatamente).")]
        [Range(0f, 1f)]
        public float deceleration = 0.85f;
    }
}
