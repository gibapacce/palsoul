using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;

namespace Palsoul.Creatures
{
    /// <summary>
    /// Gerencia o arremesso de esferas de captura e o resultado (sucesso/falha).
    ///
    /// Fluxo:
    ///   1. Jogador pressiona a action "Capture" (Input System).
    ///   2. CaptureSystem verifica se há esferas no inventário e um alvo válido no raio.
    ///   3. Anima o arremesso (coroutine de projétil simples).
    ///   4. Chama CaptureFormula.Calculate() com os dados da criatura alvo.
    ///   5. Sucesso: CreatureController.ApplyCapture() + BestiaryData.RegisterCapture().
    ///      Falha:  CreatureController.ApplyCaptureFailed() (fúria).
    ///
    /// Setup no prefab do Player:
    ///   - Adicione este componente ao mesmo GO do PlayerController.
    ///   - Atribua o CaptureSphereDataSO ativo e o BestiaryData no Inspector.
    ///   - Adicione a action "Capture" no InputActions asset (ex.: tecla E / botão B).
    ///
    /// GDD seção 5.2: "Falha na captura: a esfera é perdida e a criatura entra em fúria."
    /// </summary>
    public class CaptureSystem : MonoBehaviour
    {
        // ── Dados de design ────────────────────────────────────────────────────
        [Header("Esfera Ativa")]
        [Tooltip("Esfera de captura atualmente equipada pelo jogador.")]
        [SerializeField] private CaptureSphereDataSO activeSphere;

        [Header("Bestiário")]
        [Tooltip("Asset de BestiaryData — fonte da verdade do progresso do jogador.")]
        [SerializeField] private BestiaryData bestiary;

        [Header("Inventário")]
        [Tooltip("Quantidade inicial de esferas do tipo ativo.")]
        [Min(0)]
        [SerializeField] private int sphereCount = 5;

        [Header("Raio de detecção de alvo")]
        [Tooltip("Raio em que o sistema busca a criatura mais próxima para capturar.")]
        [Min(0f)]
        [SerializeField] private float captureRadius = 5f;

        [Tooltip("LayerMask da layer 'Creature' (criaturas capturáveis).")]
        [SerializeField] private LayerMask creatureLayer;

        [Header("Arremesso (visual)")]
        [Tooltip("Duração da animação de arremesso em segundos (tempo até a esfera 'chegar').")]
        [Min(0.05f)]
        [SerializeField] private float throwDuration = 0.3f;
        [SerializeField] private SpriteRenderer projectileView;
        [SerializeField, Range(-1, 1)] private float stealthFacingThreshold = .3f;

        // ── Estado ─────────────────────────────────────────────────────────────
        private bool _isThrowing;
        private readonly System.Collections.Generic.List<CapturedCreature> captured = new();
        public System.Collections.Generic.IReadOnlyList<CapturedCreature> Captured => captured;
        public bool Owns(CapturedCreature creature) => captured.Contains(creature);
        public BestiaryData Bestiary => bestiary;
        private int initialSphereCount;
        private void Awake()
        {
            initialSphereCount = sphereCount;
            bestiary = bestiary != null ? Instantiate(bestiary) : ScriptableObject.CreateInstance<BestiaryData>();
            bestiary.Clear();
        }
        private void OnDestroy() { if (bestiary != null) Destroy(bestiary); }
        private void OnDisable()
        {
            CancelThrow();
        }
        public void CancelThrow()
        {
            StopAllCoroutines();
            _isThrowing = false;
            if (projectileView != null) projectileView.gameObject.SetActive(false);
        }
        public void RefillSpheres() => sphereCount = initialSphereCount;
        public CapturedCreature RegisterCapture(CreatureDefinitionSO definition, float healthRatio)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            var individual = new CapturedCreature(definition, healthRatio);
            captured.Add(individual);
            bestiary.RegisterCapture(definition);
            OnCaptureSuccess?.Invoke(definition);
            return individual;
        }

        // ── Eventos ────────────────────────────────────────────────────────────
        /// <summary>Disparado ao tentar capturar sem esferas.</summary>
        public event System.Action OnNoSpheresLeft;

        /// <summary>Disparado ao capturar com sucesso. Parâmetro: definição da espécie.</summary>
        public event System.Action<CreatureDefinitionSO> OnCaptureSuccess;

        /// <summary>Disparado quando a captura falha. Parâmetro: chance que foi calculada.</summary>
        public event System.Action<float> OnCaptureFailure;

        // ── Propriedades públicas ──────────────────────────────────────────────
        public int  SphereCount  => sphereCount;
        public bool HasSpheres   => sphereCount > 0;

        // ─────────────────────────────────────────────────────────────────────
        #region Input Callback (Send Messages via PlayerInput)

        /// <summary>
        /// Chamado pelo PlayerInput quando a action "Capture" é pressionada.
        /// </summary>
        private void OnCapture(InputValue value)
        {
            if (value.isPressed) TryCaptureNearest();
        }

        public bool TryCaptureNearest()
        {
            if (_isThrowing || activeSphere == null || activeSphere.balance == null) return false;
            if (!GetComponent<Combat.PlayerController>().CanAct) return false;

            if (!HasSpheres)
            {
                OnNoSpheresLeft?.Invoke();
                Debug.Log("[CaptureSystem] Sem esferas de captura!");
                return false;
            }

            // Busca a criatura capturável mais próxima no raio
            CreatureController target = FindNearestCapturable();
            if (target == null)
            {
                Debug.Log("[CaptureSystem] Nenhuma criatura capturável no raio.");
                return false;
            }

            StartCoroutine(ThrowSphere(target));
            return true;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Arremesso

        private IEnumerator ThrowSphere(CreatureController target)
        {
            _isThrowing = true;
            sphereCount--;

            // ── Visual de arremesso (placeholder: debug ray) ───────────────────
            // O MVP usa um simples delay; substituir por animação de projétil depois.
#if UNITY_EDITOR
            Debug.DrawLine(transform.position, target.transform.position,
                           activeSphere != null ? activeSphere.sphereColor : Color.white,
                           throwDuration);
#endif
            Vector3 origin = transform.position;
            if (projectileView != null)
            {
                projectileView.sprite = activeSphere.icon;
                projectileView.color = activeSphere.sphereColor;
                projectileView.gameObject.SetActive(true);
            }
            float elapsed = 0;
            while (elapsed < throwDuration && target != null && target.IsCapturable)
            {
                elapsed += Time.deltaTime;
                if (projectileView != null)
                    projectileView.transform.position = Vector3.Lerp(origin, target.transform.position, elapsed / throwDuration);
                yield return null;
            }
            if (projectileView != null) projectileView.gameObject.SetActive(false);

            // ── Verifica se o alvo ainda é válido (pode ter morrido durante o voo) ──
            if (target == null || !target.IsCapturable || GetComponent<HealthSystem>().IsDead)
            {
                Debug.Log("[CaptureSystem] Alvo não é mais capturável (morreu durante o arremesso).");
                _isThrowing = false;
                yield break;
            }

            // ── Aplica fórmula ─────────────────────────────────────────────────
            float chance = CaptureFormula.Calculate(
                activeSphere,
                target.HPNormalized,
                IsStealthTarget(target),
                target.ActiveStatus,
                target.Definition
            );

            float roll = Random.value;        // [0, 1)
            bool  success = roll < chance;

#if UNITY_EDITOR
            Debug.Log($"[CaptureSystem] {target.Definition?.creatureName} | " +
                      $"HP={target.HPNormalized:P0} | " +
                      $"Alerted={target.IsAlerted} | " +
                      $"Status={target.ActiveStatus} | " +
                      $"Chance={chance:P2} | Roll={roll:F4} | " +
                      $"Resultado={( success ? "CAPTURADO" : "FALHOU")}");
#endif

            if (success)
            {
                target.ApplyCapture();
                RegisterCapture(target.Definition, target.HPNormalized);
            }
            else
            {
                target.ApplyCaptureFailed();
                OnCaptureFailure?.Invoke(chance);
            }

            _isThrowing = false;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Detecção de Alvo

        /// <summary>
        /// Retorna a criatura capturável mais próxima dentro de captureRadius.
        /// Prioriza a de HP mais baixo em caso de empate de distância.
        /// </summary>
        private CreatureController FindNearestCapturable()
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(
                transform.position, captureRadius, creatureLayer);

            CreatureController best     = null;
            float              bestDist = float.MaxValue;

            foreach (var col in cols)
            {
                var cc = col.GetComponentInParent<CreatureController>();
                if (cc == null || !cc.IsCapturable) continue;

                float dist = Vector2.Distance(transform.position, cc.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = cc;
                }
            }

            return best;
        }

        private bool IsStealthTarget(CreatureController target)
        {
            if (target.IsAlerted) return false;
            var animator = target.GetComponent<Animator>();
            Vector2 facing = new(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
            Vector2 direction = target.transform.position - transform.position;
            return facing.sqrMagnitude > .01f && Vector2.Dot(direction.normalized, facing.normalized) > stealthFacingThreshold;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        /// <summary>Adiciona esferas ao inventário (ex.: ao pegar um item).</summary>
        public void AddSpheres(int count) => sphereCount += count;

        /// <summary>Troca a esfera equipada (ex.: ao selecionar tier diferente).</summary>
        public void SetActiveSphere(CaptureSphereDataSO sphere) => activeSphere = sphere;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, captureRadius);
        }

        #endregion
    }
}
