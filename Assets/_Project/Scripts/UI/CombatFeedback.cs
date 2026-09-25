using UnityEngine;
using Palsoul.Combat;
using Palsoul.Core;
using Palsoul.Creatures;

namespace Palsoul.UI
{
    /// <summary>Readable prototype telegraphs and health bars without final animation art.</summary>
    public class CombatFeedback : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private HitboxController hitbox;
        private HealthSystem health;
        private EnemyController enemy;
        private CreatureController creature;
        private PlayerController player;
        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            hitbox = GetComponent<HitboxController>();
            health = GetComponent<HealthSystem>();
            enemy = GetComponent<EnemyController>();
            creature = GetComponent<CreatureController>();
            player = GetComponent<PlayerController>();
        }
        private void LateUpdate()
        {
            if (health.IsDead) return;
            sprite.color = player != null && player.IsInvincible ? Color.cyan
                : hitbox.IsActive ? Color.yellow
                : creature != null && creature.IsEnraged ? new Color(1, .25f, .25f) : Color.white;
        }
        private void OnGUI()
        {
            if (player != null || Camera.main == null || health == null || health.IsDead) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * .7f);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(point.x - 28, Screen.height - point.y, 56, 6), Texture2D.whiteTexture);
            GUI.color = enemy != null ? Color.red : Color.green;
            GUI.DrawTexture(new Rect(point.x - 28, Screen.height - point.y, 56 * health.NormalizedHP, 6), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
