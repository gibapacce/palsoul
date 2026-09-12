# CHANGELOG — PalSoul

Formato: `[MVP-X] Descrição curta — data`

---

## MVP em andamento

### [MVP-3] Sistema de ataque leve/pesado + hitbox/hurtbox — 12/09/2026
- `ElementSO`: ScriptableObject de elemento (placeholder para MVP 5, já referenciado pelo AttackDataSO).
- `AttackDataSO`: define dano, custo de stamina, timing de hitbox, knockback e elemento por ataque.
- `HealthSystem`: HP com `TakeDamage`, `Heal`, `RestoreFull`, `SetMaxHP` e eventos `OnDamaged/OnHealed/OnDeath/OnHPChanged`.
- `HitboxController`: detecção via `Physics2D.OverlapBoxAll` por janela de tempo, hit único por alvo por swing, gizmo de debug.
- `HurtboxController`: recebe hits, respeita i-frames via interface `IInvincible`, aplica knockback, dispara `OnStagger`.
- `PlayerAttackState`: estado compartilhado leve/pesado com combo window, recovery phase e `AttackSpeed` no Animator.
- `PlayerController`: implementa `IInvincible`, callbacks `OnAttackLight`/`OnAttackHeavy`, input buffering (pesado > leve), `ProcessBufferedInputs`, bloqueio de movimento durante ataques, `OnPlayerDeath` hookado ao `HealthSystem`.

---

### [MVP-2] Sistema de Stamina + Dodge Roll com i-frames — 12/09/2026
- `StaminaSO`: parâmetros de stamina (maxStamina, regenRate, regenDelay) e dodge (dodgeCost, dodgeDuration, dodgeSpeed, iFrameStart, iFrameEnd, dodgeCooldown) configuráveis via Inspector.
- `StaminaSystem`: gerencia consumo (`TryConsume`), regeneração com delay, restauração completa (`RestoreFull`) e eventos `OnStaminaChanged/OnStaminaDepleted/OnStaminaRegenStarted` para UI.
- `PlayerDodgeState`: estado Dodging com movimento direcional fixado no Enter, janela de i-frames por timer, cooldown e logs de debug no Editor.
- `PlayerController`: integrado `StaminaSystem`, callback `OnDodge` (valida cooldown + stamina), flag `IsInvincible` pública, feedback visual (flash vermelho) ao tentar dodge sem stamina, label de estado na Scene view.

---

### [MVP-1] Movimento do player top-down + câmera pixel-perfect — 12/09/2026
- Criada estrutura completa de pastas em `Assets/_Project/` (conforme GDD seção 9.3).
- `PlayerMovementSO` (ScriptableObject): parâmetros `moveSpeed` e `deceleration` configuráveis via Inspector.
- `IState` (interface) + `PlayerState` (enum): base da State Machine extensível para todos os sistemas de combate.
- `PlayerController`: State Machine com estados `Idle` e `Moving`; input via Unity Input System (Send Messages); movimento físico em `FixedUpdate` via `Rigidbody2D`.
- `CameraFollow`: segue o player via `LateUpdate` com suavização configurável (`smoothTime`), offset, restrição de bounds e método `SnapToTarget()` para troca de cena.
