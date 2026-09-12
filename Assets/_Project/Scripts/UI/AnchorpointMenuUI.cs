using UnityEngine;
using Palsoul.Progression;

namespace Palsoul.UI
{
    /// <summary>
    /// UI do menu do Ancoradouro — implementada via OnGUI para o MVP
    /// (sem dependência de Canvas/prefabs de UI, testável imediatamente no Editor).
    ///
    /// Quando a UI definitiva (Canvas) for implementada, este componente pode ser
    /// substituído ou ter o OnGUI removido mantendo a lógica de estado.
    ///
    /// Ações disponíveis:
    ///   [1] Descansar — cura + reset de mundo
    ///   [2..N] Upgrade de atributo (listado dinamicamente do AnchorpointDataSO)
    ///   [F] Trocar Forma Ativa (placeholder — lista de criaturas capturadas virá do Bestiário)
    ///   [ESC] Fechar menu
    /// </summary>
    public class AnchorpointMenuUI : MonoBehaviour
    {
        // ── Estado ─────────────────────────────────────────────────────────────
        private bool _isOpen;
        private bool _showPrompt;
        private Base.AnchorpointController _controller;

        // ── Layout ─────────────────────────────────────────────────────────────
        private Rect _windowRect = new Rect(
            Screen.width / 2f - 200f,
            Screen.height / 2f - 220f,
            400f, 440f);

        private int _windowID = 100;

        // ─────────────────────────────────────────────────────────────────────
        #region API Pública

        public void Show(Base.AnchorpointController controller)
        {
            _controller = controller;
            _isOpen     = true;
            _showPrompt = false;
        }

        public void Hide()
        {
            _isOpen     = false;
            _showPrompt = false;
        }

        public void ShowPrompt() => _showPrompt = true;
        public void HidePrompt() => _showPrompt = false;

        #endregion

        // ─────────────────────────────────────────────────────────────────────
        #region OnGUI

        private void OnGUI()
        {
            // Prompt de interação
            if (_showPrompt && !_isOpen)
            {
                GUI.color = Color.white;
                var promptRect = new Rect(Screen.width / 2f - 100f, Screen.height - 80f, 200f, 30f);
                GUI.Box(promptRect, "Pressione E para interagir");
            }

            if (!_isOpen || _controller == null) return;

            // Fecha com ESC
            if (Event.current.type == EventType.KeyDown
             && Event.current.keyCode == KeyCode.Escape)
            {
                _controller.CloseMenu();
                return;
            }

            // Janela do menu
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.95f);
            _windowRect = GUI.Window(_windowID, _windowRect, DrawWindow,
                $"⚓  {_controller.Data?.anchorpointName ?? "Ancoradouro"}");
        }

        private void DrawWindow(int id)
        {
            var data       = _controller.Data;
            var etherWallet = FindFirstObjectByType<EtherWallet>();
            float ether    = etherWallet != null ? etherWallet.CurrentEther : 0f;

            GUILayout.Space(8f);

            // ── Éter atual ────────────────────────────────────────────────────
            GUI.color = new Color(0.8f, 0.6f, 1f);
            GUILayout.Label($"Éter disponível:  {ether:F0}");
            GUI.color = Color.white;

            GUILayout.Space(8f);
            DrawSeparator();

            // ── Descansar ─────────────────────────────────────────────────────
            GUI.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
            if (GUILayout.Button("  [1]  Descansar  (cura + reseta mundo)", GUILayout.Height(36f)))
                _controller.Rest();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6f);
            DrawSeparator();

            // ── Upgrades de atributo ──────────────────────────────────────────
            if (data?.availableUpgrades != null)
            {
                GUILayout.Label("  Atributos:");
                GUILayout.Space(4f);

                for (int i = 0; i < data.availableUpgrades.Length; i++)
                {
                    var upgrade  = data.availableUpgrades[i];
                    int level    = _controller.GetAttributeLevel(i);
                    float cost   = _controller.GetNextUpgradeCost(i);
                    bool maxed   = level >= upgrade.maxLevel;
                    bool canAfford = etherWallet != null && etherWallet.HasEnough(cost);

                    GUI.color = maxed ? Color.gray
                              : canAfford ? Color.white
                              : new Color(1f, 0.4f, 0.4f);

                    string label = maxed
                        ? $"  [{i + 2}]  {upgrade.attributeName}  Lv.{level}/{upgrade.maxLevel}  [MAX]"
                        : $"  [{i + 2}]  {upgrade.attributeName}  Lv.{level}/{upgrade.maxLevel}  —  {cost:F0} Éter";

                    GUI.backgroundColor = canAfford && !maxed
                        ? new Color(0.2f, 0.3f, 0.5f)
                        : new Color(0.15f, 0.15f, 0.2f);

                    if (GUILayout.Button(label, GUILayout.Height(30f)) && !maxed)
                        _controller.UpgradeAttribute(i);

                    GUI.color           = Color.white;
                    GUI.backgroundColor = Color.white;
                }
            }

            GUILayout.Space(6f);
            DrawSeparator();

            // ── Trocar Forma Ativa (placeholder) ──────────────────────────────
            GUI.backgroundColor = new Color(0.3f, 0.2f, 0.5f);
            if (GUILayout.Button("  [F]  Trocar Forma Ativa  (lista de criaturas capturadas)", GUILayout.Height(30f)))
                Debug.Log("[AnchorpointMenuUI] Trocar Forma Ativa — lista completa no MVP 11 (Bestiário).");
            GUI.backgroundColor = Color.white;

            GUILayout.Space(6f);
            DrawSeparator();

            // ── Fechar ────────────────────────────────────────────────────────
            GUI.backgroundColor = new Color(0.4f, 0.1f, 0.1f);
            if (GUILayout.Button("  [ESC]  Fechar", GUILayout.Height(28f)))
                _controller.CloseMenu();
            GUI.backgroundColor = Color.white;

            // Torna a janela arrastável
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20f));
        }

        private static void DrawSeparator()
        {
            GUI.color = new Color(0.5f, 0.5f, 0.7f, 0.5f);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            GUI.color = Color.white;
        }

        #endregion
    }
}
