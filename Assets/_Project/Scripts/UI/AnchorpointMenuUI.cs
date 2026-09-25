using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Base;

namespace Palsoul.UI
{
    public class AnchorpointMenuUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        private bool prompt;
        private AnchorpointController controller;
        private Vector2 scroll;
        public void Show(AnchorpointController anchor) { controller = anchor; IsOpen = true; prompt = false; }
        public void Hide() { IsOpen = false; prompt = false; }
        public void ShowPrompt() => prompt = true;
        public void HidePrompt() => prompt = false;
        private void Update()
        {
            if (IsOpen && ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
                || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false))) controller.CloseMenu();
        }
        private void OnGUI()
        {
            if (prompt && !IsOpen) GUI.Box(new Rect(Screen.width / 2 - 160, Screen.height - 50, 320, 30), "E / A: abrir Ancoradouro");
            if (!IsOpen || controller == null) return;
            float width = Mathf.Min(520, Screen.width - 20);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 15, width, Screen.height - 30), GUI.skin.box);
            GUILayout.Label(controller.Data.anchorpointName);
            GUILayout.Label($"Éter: {controller.Wallet.CurrentEther:0}");
            if (GUILayout.Button("Descansar — curar squad, repor esferas e resetar inimigos")) controller.Rest();
            var upgrades = controller.Data.availableUpgrades;
            if (upgrades != null) for (int i = 0; i < upgrades.Length; i++)
            {
                var upgrade = upgrades[i];
                int level = controller.GetAttributeLevel(i);
                GUI.enabled = level < upgrade.maxLevel && controller.Wallet.HasEnough(controller.GetNextUpgradeCost(i));
                if (GUILayout.Button($"{upgrade.attributeName} {level}/{upgrade.maxLevel} — {controller.GetNextUpgradeCost(i):0} Éter"))
                    controller.UpgradeAttribute(i);
                GUI.enabled = true;
            }
            GUILayout.Space(12);
            GUILayout.Label("Criaturas capturadas — escolha uma forma e um companheiro");
            scroll = GUILayout.BeginScrollView(scroll);
            controller.Squad.SyncHealth();
            foreach (var creature in controller.Captures.Captured)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{creature.definition.creatureName} — HP {creature.healthRatio:P0}");
                bool active = controller.Squad.ActiveMember == creature;
                bool passive = controller.Squad.PassiveMember == creature;
                GUI.enabled = !active && !passive;
                if (GUILayout.Button(active ? "Ativa" : "Usar forma", GUILayout.Width(95))) controller.Equip(creature, true);
                if (GUILayout.Button(passive ? "No squad" : "Companheiro", GUILayout.Width(105))) controller.Equip(creature, false);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            if (controller.Captures.Captured.Count == 0) GUILayout.Label("Capture criaturas com Q antes de escolher sua forma.");
            GUILayout.EndScrollView();
            if (GUILayout.Button("Fechar — Esc / B")) controller.CloseMenu();
            GUILayout.EndArea();
        }
    }
}
