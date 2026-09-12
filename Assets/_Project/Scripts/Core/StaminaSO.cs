using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject com todos os parâmetros de stamina e dodge do player.
    /// Crie um asset em ScriptableObjects/Player via menu Assets > Create > Palsoul > Stamina Data.
    /// Nunca coloque magic numbers no StaminaSystem ou PlayerController — toda constante vem daqui.
    /// </summary>
    [CreateAssetMenu(fileName = "StaminaData", menuName = "Palsoul/Stamina Data")]
    public class StaminaSO : ScriptableObject
    {
        [Header("Barra de Stamina")]
        [Tooltip("Stamina máxima do player.")]
        [Min(1f)]
        public float maxStamina = 100f;

        [Tooltip("Taxa de regeneração de stamina por segundo.")]
        [Min(0f)]
        public float regenRate = 20f;

        [Tooltip("Delay em segundos após gastar stamina antes de começar a regenerar.")]
        [Min(0f)]
        public float regenDelay = 1f;

        [Header("Dodge Roll")]
        [Tooltip("Custo de stamina para executar um dodge roll.")]
        [Min(0f)]
        public float dodgeCost = 25f;

        [Tooltip("Duração total da animação de dodge em segundos.")]
        [Min(0.05f)]
        public float dodgeDuration = 0.4f;

        [Tooltip("Velocidade de deslocamento durante o dodge (unidades Unity/s).")]
        [Min(0f)]
        public float dodgeSpeed = 10f;

        [Header("I-Frames (Invencibilidade)")]
        [Tooltip("Tempo em segundos após o início do dodge em que começa a janela de i-frames.")]
        [Min(0f)]
        public float iFrameStart = 0.05f;

        [Tooltip("Tempo em segundos após o início do dodge em que termina a janela de i-frames.")]
        [Min(0f)]
        public float iFrameEnd = 0.30f;

        [Header("Cooldown")]
        [Tooltip("Tempo mínimo em segundos entre dois dodges consecutivos (evita spam).")]
        [Min(0f)]
        public float dodgeCooldown = 0.1f;

        // Validação no Inspector para garantir que iFrameEnd > iFrameStart
        private void OnValidate()
        {
            if (iFrameEnd <= iFrameStart)
                iFrameEnd = iFrameStart + 0.05f;

            if (iFrameEnd > dodgeDuration)
                iFrameEnd = dodgeDuration;
        }
    }
}
