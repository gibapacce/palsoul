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

        // ── Estado ─────────────────────────────────────────────────────────────
        private bool _isThrowing;

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
            if (!value.isPressed || _isThrowing) return;

            if (!HasSpheres)
            {
                OnNoSpheresLeft?.Invoke();
                Debug.Log("[CaptureSystem] Sem esferas de captura!");
                return;
            }

            // Busca a criatura capturável mais próxima no raio
            CreatureController target = FindNearestCapturable();
            if (target == null)
            {
                Debug.Log("[CaptureSystem] Nenhuma criatura capturável no raio.");
                return;
            }

            StartCoroutine(ThrowSphere(target));
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
            yield return new WaitForSeconds(throwDuration);

            // ── Verifica se o alvo ainda é válido (pode ter morrido durante o voo) ──
            if (target == null || !target.IsCapturable)
            {
                Debug.Log("[CaptureSystem] Alvo não é mais capturável (morreu durante o arremesso).");
                _isThrowing = false;
                yield break;
            }

            // ── Aplica fórmula ─────────────────────────────────────────────────
            float chance = CaptureFormula.Calculate(
                activeSphere,
                target.HPNormalized,
                !target.IsAlerted,            // furtivo = inimigo não alertado
                target.ActiveStatus
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
                bestiary?.RegisterCapture(target.Definition);
                OnCaptureSuccess?.Invoke(target.Definition);
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
