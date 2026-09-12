using UnityEngine;

namespace Palsoul.Core
{
    /// <summary>
    /// Fórmula de captura de criaturas — classe estática pura (sem MonoBehaviour).
    /// GDD seção 5.2:
    ///   chance = baseChance(tier) × hpModifier(hpNormalized) × stealthModifier × statusModifier
    ///
    /// Por ser estática e sem dependências de Unity scene, pode ser testada diretamente
    /// no Console do Editor via CaptureFormula.RunTests().
    ///
    /// Critério de aceitação (seção 13): "taxa de sucesso responde corretamente a %HP,
    /// tier de esfera e ataque furtivo (testável com valores fixos gerando probabilidade
    /// esperada, cobertura por teste unitário simples de CaptureFormula)."
    /// </summary>
    public static class CaptureFormula
    {
        // ── Constantes de balanceamento ────────────────────────────────────────
        // Todas configuráveis via SO no futuro; aqui como constantes para o MVP.

        /// <summary>Multiplicador de captura furtiva (ataque pelas costas, não alertado).</summary>
        public const float StealthMultiplier = 1.5f;

        /// <summary>HP normalizado abaixo do qual o modificador de HP começa a escalar.</summary>
        public const float HPScaleThreshold = 0.50f;

        /// <summary>Multiplicador de status "Atordoado".</summary>
        public const float StunModifier = 1.4f;

        /// <summary>Multiplicador de status "Queimando".</summary>
        public const float BurnModifier = 1.2f;

        /// <summary>Multiplicador de status "Envenenado".</summary>
        public const float PoisonModifier = 1.15f;

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Calcula a chance de captura (0–1) para um arremesso.
        /// </summary>
        /// <param name="sphereData">Esfera usada (define baseChance).</param>
        /// <param name="hpNormalized">HP atual da criatura / HP máximo (0–1).</param>
        /// <param name="isStealth">True se foi ataque furtivo (pelas costas, não alertado).</param>
        /// <param name="statusEffects">Flags de status ativos na criatura.</param>
        /// <returns>Chance final de captura, clampeada entre 0 e 1.</returns>
        public static float Calculate(
            CaptureSphereDataSO sphereData,
            float               hpNormalized,
            bool                isStealth,
            CaptureStatus       statusEffects)
        {
            if (sphereData == null) return 0f;

            // ── 1. Chance base do tier da esfera ──────────────────────────────
            float chance = sphereData.baseChance;

            // ── 2. Modificador de HP ──────────────────────────────────────────
            // HP cheio = 0.5× base chance; HP crítico (0%) = 2× base chance
            // Curva linear entre [HPScaleThreshold..0] → [1.0..2.0]
            float hpMod = HPModifier(hpNormalized);
            chance *= hpMod;

            // ── 3. Modificador furtivo ────────────────────────────────────────
            if (isStealth)
                chance *= StealthMultiplier;

            // ── 4. Modificador de status ──────────────────────────────────────
            chance *= StatusModifier(statusEffects);

            // ── 5. Clamp final ────────────────────────────────────────────────
            return Mathf.Clamp01(chance);
        }

        // ─────────────────────────────────────────────────────────────────────
        #region Modificadores

        /// <summary>
        /// Modificador de HP:
        ///   HP >= HPScaleThreshold → 0.5 (chance reduzida enquanto criatura está saudável)
        ///   HP == 0                → 2.0 (chance dobrada no limite)
        ///   Interpolação linear entre threshold e 0.
        /// </summary>
        public static float HPModifier(float hpNormalized)
        {
            hpNormalized = Mathf.Clamp01(hpNormalized);

            if (hpNormalized >= HPScaleThreshold)
                return 0.5f;

            // Lerp de 0.5 (em HPScaleThreshold) até 2.0 (em HP=0)
            float t = 1f - (hpNormalized / HPScaleThreshold);
            return Mathf.Lerp(0.5f, 2.0f, t);
        }

        /// <summary>
        /// Modificador de status: multiplica os modificadores de cada status ativo.
        /// Se nenhum status ativo, retorna 1.0.
        /// </summary>
        public static float StatusModifier(CaptureStatus status)
        {
            float mod = 1f;
            if (status.HasFlag(CaptureStatus.Stunned)   ) mod *= StunModifier;
            if (status.HasFlag(CaptureStatus.Burning)   ) mod *= BurnModifier;
            if (status.HasFlag(CaptureStatus.Poisoned)  ) mod *= PoisonModifier;
            return mod;
        }

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region Testes (chamados via Menu Unity no Editor)

#if UNITY_EDITOR
        /// <summary>
        /// Roda uma bateria de testes com valores fixos e loga os resultados.
        /// Chame via: CaptureFormula.RunTests() ou pelo menu Palsoul > Run Capture Formula Tests.
        /// Critério: valores conhecidos devem gerar probabilidades esperadas.
        /// </summary>
        [UnityEditor.MenuItem("Palsoul/Run Capture Formula Tests")]
        public static void RunTests()
        {
            // Cria uma esfera de teste inline (sem precisar de um asset)
            var sphere = ScriptableObject.CreateInstance<CaptureSphereDataSO>();
            sphere.baseChance = 0.30f;  // Esfera Comum

            Debug.Log("=== CaptureFormula Tests ===");

            // Teste 1: HP cheio, sem furtivo, sem status → ~0.15 (30% * 0.5)
            float t1 = Calculate(sphere, 1.0f, false, CaptureStatus.None);
            LogTest("T1 HP=100% no stealth no status", t1, 0.15f);

            // Teste 2: HP 20%, sem furtivo, sem status → ~0.90 (30% * 1.8)
            float t2 = Calculate(sphere, 0.20f, false, CaptureStatus.None);
            float expected2 = sphere.baseChance * HPModifier(0.20f);
            LogTest("T2 HP=20%  no stealth no status", t2, expected2);

            // Teste 3: HP 0%, sem furtivo, sem status → ~0.60 (30% * 2.0)
            float t3 = Calculate(sphere, 0.0f, false, CaptureStatus.None);
            LogTest("T3 HP=0%   no stealth no status", t3, 0.60f);

            // Teste 4: HP 20%, furtivo, sem status → t2 * 1.5
            float t4 = Calculate(sphere, 0.20f, true, CaptureStatus.None);
            LogTest("T4 HP=20%  stealth    no status", t4, expected2 * StealthMultiplier);

            // Teste 5: HP 20%, furtivo, atordoado → t4 * 1.4
            float t5 = Calculate(sphere, 0.20f, true, CaptureStatus.Stunned);
            LogTest("T5 HP=20%  stealth    stunned  ", t5,
                    expected2 * StealthMultiplier * StunModifier);

            // Teste 6: HP cheio, sem furtivo, queimando → ~0.18 (30% * 0.5 * 1.2)
            float t6 = Calculate(sphere, 1.0f, false, CaptureStatus.Burning);
            LogTest("T6 HP=100% no stealth burning  ", t6, 0.15f * BurnModifier);

            // Limpa o SO temporário
            Object.DestroyImmediate(sphere);
            Debug.Log("=== Tests Complete ===");
        }

        private static void LogTest(string name, float result, float expected)
        {
            bool pass = Mathf.Abs(result - expected) < 0.001f;
            string status = pass ? "<color=green>PASS</color>" : "<color=red>FAIL</color>";
            Debug.Log($"[{status}] {name} → result={result:F4}  expected={expected:F4}");
        }
#endif

        #endregion
    }

    // ── Status de captura (flags) ──────────────────────────────────────────────
    /// <summary>
    /// Flags de status ativo numa criatura no momento do arremesso.
    /// Expandível sem quebrar a fórmula.
    /// </summary>
    [System.Flags]
    public enum CaptureStatus
    {
        None     = 0,
        Stunned  = 1 << 0,
        Burning  = 1 << 1,
        Poisoned = 1 << 2,
        Frozen   = 1 << 3,
    }
}
