using Palsoul.Combat;
using Palsoul.Creatures;
using Palsoul.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Palsoul.UI
{
    public class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        private CaptureSystem capture;
        private PlayerDeathSystem death;
        [SerializeField, Min(1)] private float ecoMapRange = 30;
        [SerializeField] private Sprite humanPortrait;
        private string message = "Explore à direita. Enfraqueça duas criaturas e capture com Q.";
        private void Start()
        {
            capture = player.GetComponent<CaptureSystem>();
            death = player.GetComponent<PlayerDeathSystem>();
            death.OnDied += Died;
            death.OnRespawned += Respawned;
            death.OnEcoRecovered += Recovered;
            capture.OnCaptureSuccess += Captured;
            capture.OnCaptureFailure += Failed;
            capture.OnNoSpheresLeft += Empty;
        }
        private void Captured(Core.CreatureDefinitionSO species) => message = $"{species.creatureName} capturada! Volte ao Ancoradouro para equipar.";
        private void Failed(float chance) => message = $"Captura falhou ({chance:P0}). A criatura entrou em fúria!";
        private void Empty() => message = "Sem esferas. Descanse no Ancoradouro.";
        private void Died(float amount) => message = $"Você perdeu {amount:0.##} Éter. O Eco anterior foi perdido.";
        private void Respawned() => message = "De volta ao Ancoradouro. Alcance o Eco para recuperar seu Éter.";
        private void Recovered(float amount) => message = $"Eco recuperado: +{amount:0.##} Éter.";
        private void Update()
        {
            if (death != null && death.AwaitingRespawn &&
                ((Keyboard.current?.enterKey.wasPressedThisFrame ?? false)
                || (Gamepad.current?.startButton.wasPressedThisFrame ?? false))) death.Respawn();
        }
        private void OnDestroy()
        {
            if (death != null)
            {
                death.OnDied -= Died;
                death.OnRespawned -= Respawned;
                death.OnEcoRecovered -= Recovered;
            }
            if (capture == null) return;
            capture.OnCaptureSuccess -= Captured;
            capture.OnCaptureFailure -= Failed;
            capture.OnNoSpheresLeft -= Empty;
        }
        private void OnGUI()
        {
            if (player == null || capture == null) return;
            var squad = player.GetComponent<SquadController>();
            GUI.Label(new Rect(12, 65, 500, 25), $"Forma: {squad.ActiveFormDefinition?.creatureName ?? "Humano"} | Esferas: {capture.SphereCount}");
            GUI.Label(new Rect(12, 85, 500, 25), $"Companheiro: {squad.PassiveFormDefinition?.creatureName ?? "Nenhum"}");
            GUI.Box(new Rect(10, Screen.height - 115, Screen.width - 20, 55),
                "WASD/setas: mover | J/K: leve/pesado | Espaço: esquiva | Q: capturar\nTab: alternar criatura | E: Ancoradouro | Esc: fechar menu");
            GUI.Label(new Rect(12, 125, Screen.width - 24, 45), message);
            if (death == null) return;
            DrawEcoMap();
            if (death.AwaitingRespawn)
            {
                float x = Screen.width / 2 - 220, y = Screen.height / 2 - 80;
                GUI.Box(new Rect(x, y, 440, 160), $"Você morreu.\nÉter perdido: {death.LastLostEther:0.##}");
                if (humanPortrait != null)
                    GUI.DrawTexture(new Rect(x + 12, y + 45, 48, 48), humanPortrait.texture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(x + 70, y + 50, 350, 45),
                    "Retorne ao Eco para recuperar o Éter.\nOutra morte apaga o Eco anterior.");
                if (GUI.Button(new Rect(x + 70, y + 110, 300, 30), "Renascer — Enter / Start")) death.Respawn();
            }
        }
        private void DrawEcoMap()
        {
            if (death.Marker == null || !death.Marker.IsAvailable) return;
            var area = new Rect(Screen.width - 180, 10, 170, 180);
            GUI.Box(area, $"Eco: {death.Marker.Amount:0.##} Éter");
            Vector2 delta = death.Marker.transform.position - player.transform.position;
            Vector2 point = Vector2.ClampMagnitude(delta / ecoMapRange, 1) * 55;
            Vector2 center = new(area.center.x, area.y + 90);
            GUI.Label(new Rect(center.x - 5, center.y - 10, 20, 20), "P");
            GUI.color = Color.cyan;
            GUI.Label(new Rect(center.x + point.x - 5, center.y - point.y - 10, 20, 20), "E");
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 10, area.y + 140, 150, 35), $"N ↑ | Distância: {delta.magnitude:0.0}\nP: você  E: Eco");
        }
    }
}
