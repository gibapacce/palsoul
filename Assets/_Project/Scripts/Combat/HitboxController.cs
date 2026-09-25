using System.Collections.Generic;
using UnityEngine;
using Palsoul.Core;

namespace Palsoul.Combat
{
    /// <summary>
    /// Gerencia a hitbox de um atacante (player, criatura ou inimigo).
    ///
    /// Funcionamento:
    ///   - Usa Physics2D.OverlapBox por janela de tempo (não um collider permanente).
    ///   - Garantia de hit único por alvo por swing (evita multi-hit involuntário).
    ///   - A direção do ataque é passada pelo estado de ataque para calcular o offset correto.
    ///
    /// Setup no prefab:
    ///   - Adicione ao mesmo GameObject do atacante.
    ///   - Configure a LayerMask "hurtboxLayer" para a layer dos alvos (ex.: "Hurtbox").
    ///   - Crie as layers "Hitbox" e "Hurtbox" no projeto e configure a Collision Matrix
    ///     (Project Settings → Physics 2D) para que Hitbox colida com Hurtbox.
    /// </summary>
    public class HitboxController : MonoBehaviour
    {
        [Header("Detecção")]
        [Tooltip("Layer mask dos alvos que este hitbox pode acertar.")]
        [SerializeField] private LayerMask hurtboxLayer;

        [Tooltip("Referência ao Transform usado como origem do hitbox (geralmente o próprio GO ou um filho 'HitboxOrigin').")]
        [SerializeField] private Transform hitboxOrigin;
        [SerializeField, Range(-1, 1)] private float stealthFacingThreshold = .3f;

        // ── Estado da hitbox ativa ─────────────────────────────────────────────
        private bool          _isActive;
        private AttackDataSO  _currentAttack;
        private Vector2       _attackDirection;
        private float         _activeTimer;

        // Garante hit único por alvo por swing
        private readonly HashSet<HealthSystem> _hitTargets = new HashSet<HealthSystem>();

        // ── Gizmo de debug ─────────────────────────────────────────────────────
        private bool    _gizmoVisible;
        private Vector2 _gizmoCenter;
        private Vector2 _gizmoSize;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (hitboxOrigin == null)
                hitboxOrigin = transform;
        }

        private void Update()
        {
            if (!_isActive || _currentAttack == null) return;

            float previousTimer = _activeTimer;
            _activeTimer += Time.deltaTime;

            // Janela ativa: dispara OverlapBox a cada frame dentro da janela
            bool inWindow = _activeTimer >= _currentAttack.hitboxActiveStart
                         && previousTimer < _currentAttack.hitboxActiveEnd;

            if (inWindow)
                DetectHits();

            // Fim do swing
            if (_activeTimer >= _currentAttack.hitboxActiveEnd)
                Deactivate();
        }

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>
        /// Inicia um swing com os dados do ataque e a direção do atacante.
        /// Chamado pelo PlayerAttackState (ou CreatureAttackState futuramente).
        /// </summary>
        public void Activate(AttackDataSO attack, Vector2 direction)
        {
            _currentAttack   = attack;
            _attackDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
            _activeTimer     = 0f;
            _isActive        = true;
            _hitTargets.Clear();
        }

        /// <summary>Cancela o hitbox imediatamente (ex.: stagger do atacante).</summary>
        public void Deactivate()
        {
            _isActive      = false;
            _gizmoVisible  = false;
            _currentAttack = null;
            _hitTargets.Clear();
        }

        public bool IsActive => _isActive;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Detecção

        private void DetectHits()
        {
            if (_currentAttack == null) return;

            // Calcula o centro do hitbox: offset rotacionado na direção do ataque
            float   angle  = Mathf.Atan2(_attackDirection.y, _attackDirection.x) * Mathf.Rad2Deg;
            Vector2 rotatedOffset = RotateVector(_currentAttack.hitboxOffset, angle);
            Vector2 center = (Vector2)hitboxOrigin.position + rotatedOffset;

            _gizmoVisible = true;
            _gizmoCenter  = center;
            _gizmoSize    = _currentAttack.hitboxSize;

            // OverlapBox retorna todos os colliders dentro da área
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, _currentAttack.hitboxSize, angle, hurtboxLayer);

            foreach (Collider2D hit in hits)
            {
                // Ignora self e alvos já atingidos neste swing
                if (hit.transform.IsChildOf(transform) || hit.transform == transform) continue;
                var targetHealth = hit.GetComponentInParent<HealthSystem>();
                if (targetHealth == null || !_hitTargets.Add(targetHealth)) continue;

                // Repassa para o HurtboxController do alvo
                var hurtbox = hit.GetComponentInParent<HurtboxController>();
                if (hurtbox != null)
                {
                    bool isStealth = IsStealthHit(hit.transform);
                    var creature = GetComponent<Palsoul.Creatures.CreatureController>();
                    hurtbox.ReceiveHit(_currentAttack, _attackDirection, isStealth,
                        creature != null ? creature.EnrageDamageMultiplier : 1f);
                }
            }
        }

        /// <summary>
        /// Verifica se o hit é furtivo (alvo não alertado = IsAlerted false).
        /// Só aplica a EnemyControllers — player não recebe bônus furtivo de si mesmo.
        /// GDD seção 5.1: dano bônus 2.5x ao acertar criatura/inimigo não alertado.
        /// </summary>
        private bool IsStealthHit(Transform target)
        {
            // Sobe na hierarquia para encontrar o EnemyController (pode estar num pai)
            var enemy = target.GetComponentInParent<EnemyController>();
            if (enemy == null) return false;

            // Furtivo = inimigo não alertado E atacante pelas costas
            // "Pelas costas" = dot entre direção do ataque e forward do alvo > 0
            // (forward do inimigo = direção que ele está se movendo / encarando)
            if (enemy.IsAlerted) return false;

            // Verifica ângulo: se a direção do ataque aponta no mesmo sentido do
            // facing do inimigo, o player está atacando pelas costas
            Vector2 enemyFacing = new Vector2(
                enemy.Animator.GetFloat(EnemyController.HashMoveX),
                enemy.Animator.GetFloat(EnemyController.HashMoveY));

            if (enemyFacing.sqrMagnitude < 0.01f) return false;

            float dot = Vector2.Dot(_attackDirection, enemyFacing);
            return dot > stealthFacingThreshold;
        }

        /// <summary>Rotaciona um Vector2 por <paramref name="angleDeg"/> graus.</summary>
        private static Vector2 RotateVector(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!_gizmoVisible || !_isActive) return;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            // Gizmo aproximado (sem rotação para manter simples no editor)
            Gizmos.DrawWireCube(_gizmoCenter, _gizmoSize);
        }

        #endregion
    }
}
