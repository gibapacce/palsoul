using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// ScriptableObject que define um ataque (leve, pesado ou habilidade especial de criatura).
    /// Crie assets em ScriptableObjects/Attacks via menu Assets > Create > Palsoul > Attack Data.
    ///
    /// Usado por:
    ///   - PlayerAttackState (ataques do jogador / Forma Ativa)
    ///   - CreatureController (ataques autônomos da criatura do squad)
    ///   - Inimigos (MVP 4)
    /// </summary>
    [CreateAssetMenu(fileName = "AttackData", menuName = "Palsoul/Attack Data")]
    public class AttackDataSO : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Nome legível do ataque (ex.: 'Golpe Leve', 'Rasteira Pesada').")]
        public string attackName = "Ataque";

        [Header("Dano")]
        [Tooltip("Dano base aplicado ao alvo.")]
        [Min(0f)]
        public float baseDamage = 10f;

        [Tooltip("Multiplicador de dano furtivo (ataque pelas costas / não alertado). GDD seção 5.1.")]
        [Min(1f)]
        public float stealthMultiplier = 2.5f;

        [Header("Stamina")]
        [Tooltip("Custo de stamina para executar este ataque.")]
        [Min(0f)]
        public float staminaCost = 15f;

        [Header("Timing (segundos)")]
        [Tooltip("Duração total da animação de ataque.")]
        [Min(0.05f)]
        public float duration = 0.5f;

        [Tooltip("Momento em que o hitbox começa a estar ativo (dentro de 'duration').")]
        [Min(0f)]
        public float hitboxActiveStart = 0.15f;

        [Tooltip("Momento em que o hitbox para de estar ativo (dentro de 'duration').")]
        [Min(0f)]
        public float hitboxActiveEnd = 0.30f;

        [Tooltip("Janela de recovery após o fim da animação em que o jogador não pode agir (segundos).")]
        [Min(0f)]
        public float recoveryTime = 0.1f;
        [Min(0)] public float comboWindowOffset = .05f;

        [Header("Hitbox")]
        [Tooltip("Tamanho da caixa de hitbox em unidades Unity (largura × altura).")]
        public Vector2 hitboxSize = new Vector2(1.0f, 0.5f);

        [Tooltip("Offset da hitbox em relação ao centro do atacante (para frente = X positivo).")]
        public Vector2 hitboxOffset = new Vector2(0.6f, 0f);

        [Header("Knockback")]
        [Tooltip("Força de knockback aplicada ao alvo na direção do ataque.")]
        [Min(0f)]
        public float knockbackForce = 3f;

        [Tooltip("Duração do knockback no alvo (segundos).")]
        [Min(0f)]
        public float knockbackDuration = 0.15f;

        [Header("Elemento (opcional)")]
        [Tooltip("Elemento deste ataque. Null = Físico/Neutro. Usado pela ElementMatrix (MVP 5).")]
        public ElementSO element; // referência ao SO de elemento — pode ser null no MVP 3

        // Validação no Inspector
        private void OnValidate()
        {
            if (hitboxActiveEnd <= hitboxActiveStart)
                hitboxActiveEnd = hitboxActiveStart + 0.05f;
            if (hitboxActiveEnd > duration)
                hitboxActiveEnd = duration;
        }
    }
}
