using UnityEngine;
using UnityEngine.InputSystem;
using Palsoul.Core;
using Palsoul.Progression;
using Palsoul.Creatures;
using Palsoul.Combat;
using Palsoul.UI;

namespace Palsoul.Base
{
    public class AnchorpointController : MonoBehaviour
    {
        [SerializeField] private AnchorpointDataSO data;
        private WorldResetSystem world;
        private AnchorpointMenuUI menu;
        private PlayerController player;
        private bool inRange;
        public AnchorpointDataSO Data => data;
        public bool IsDiscovered { get; private set; }
        public CaptureSystem Captures => player != null ? player.GetComponent<CaptureSystem>() : null;
        public SquadController Squad => player != null ? player.GetComponent<SquadController>() : null;
        public EtherWallet Wallet => player != null ? player.GetComponent<EtherWallet>() : null;
        public event System.Action<AnchorpointController> OnPlayerRested;
        public event System.Action<AttributeUpgradeSO, int> OnAttributeUpgraded;
        public event System.Action<AnchorpointController> OnDiscovered;
        private void Start()
        {
            world = FindAnyObjectByType<WorldResetSystem>();
            menu = GetComponentInChildren<AnchorpointMenuUI>();
            player = FindAnyObjectByType<PlayerController>();
        }
        private void Update()
        {
            if (player == null || data == null) return;
            bool nearby = Vector2.Distance(transform.position, player.transform.position) <= data.interactionRadius;
            if (!nearby && inRange) CloseMenu();
            inRange = nearby;
            if (!inRange || player.HealthSystem.IsDead) { menu.HidePrompt(); return; }
            if (!IsDiscovered) { IsDiscovered = true; OnDiscovered?.Invoke(this); }
            menu.ShowPrompt();
            if ((Keyboard.current?.eKey.wasPressedThisFrame ?? false)
                || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false)) OpenMenu();
        }
        public bool CanInteract => inRange && player != null && !player.HealthSystem.IsDead;
        public void OpenMenu()
        {
            if (!CanInteract || !player.CanAct) return;
            player.SetInputBlocked(true);
            menu.Show(this);
        }
        public void CloseMenu()
        {
            menu?.Hide();
            player?.SetInputBlocked(false);
        }
        public void Rest()
        {
            if (!CanInteract) return;
            player.HealthSystem.RestoreFull();
            player.StaminaSystem.RestoreFull();
            Squad?.Rest();
            Captures?.RefillSpheres();
            world?.ResetWorld();
            OnPlayerRested?.Invoke(this);
        }
        public void UpgradeAttribute(int index)
        {
            if (!CanInteract || data?.availableUpgrades == null || index < 0 || index >= data.availableUpgrades.Length) return;
            var upgrade = data.availableUpgrades[index];
            if (player.GetComponent<PlayerProgression>().TryUpgrade(upgrade))
                OnAttributeUpgraded?.Invoke(upgrade, GetAttributeLevel(index));
        }
        public int GetAttributeLevel(int index)
        {
            if (data?.availableUpgrades == null || index < 0 || index >= data.availableUpgrades.Length) return 0;
            return player.GetComponent<PlayerProgression>().GetLevel(data.availableUpgrades[index].attributeType);
        }
        public float GetNextUpgradeCost(int index) => data.availableUpgrades[index].GetCostForLevel(GetAttributeLevel(index));
        public bool Equip(CapturedCreature creature, bool active) => CanInteract && Squad.Equip(creature, active);
        private void OnDisable() { if (menu != null && menu.IsOpen) CloseMenu(); }
    }
}
