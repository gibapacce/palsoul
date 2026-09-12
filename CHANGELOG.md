# CHANGELOG — PalSoul

Formato: `[MVP-X] Descrição curta — data`

---

## MVP em andamento

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
