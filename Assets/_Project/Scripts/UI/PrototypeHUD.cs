using Palsoul.Combat;
using Palsoul.Creatures;
using UnityEngine;

namespace Palsoul.UI
{
    public class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        private CaptureSystem capture;
        private string message = "Explore à direita. Enfraqueça duas criaturas e capture com Q.";
        private void Start()
        {
            capture = player.GetComponent<CaptureSystem>();
            capture.OnCaptureSuccess += Captured;
            capture.OnCaptureFailure += Failed;
            capture.OnNoSpheresLeft += Empty;
        }
        private void Captured(Core.CreatureDefinitionSO species) => message = $"{species.creatureName} capturada! Volte ao Ancoradouro para equipar.";
        private void Failed(float chance) => message = $"Captura falhou ({chance:P0}). A criatura entrou em fúria!";
        private void Empty() => message = "Sem esferas. Descanse no Ancoradouro.";
        private void OnDestroy()
        {
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
            if (player.HealthSystem.IsDead)
                GUI.Box(new Rect(Screen.width / 2 - 220, Screen.height / 2 - 45, 440, 90),
                    "Você morreu.\nPare e reinicie o Play para tentar novamente.\nRespawn e Eco serão implementados no MVP 8.");
        }
    }
}
