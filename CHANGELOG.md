# CHANGELOG — PalSoul

Formato: `[MVP-X] Descrição curta — data`

---

## MVP em andamento

### [MVP-6] Sistema de Transformação (Forma Ativa) — 12/09/2026
- `TransformationSystem`: `SetActiveForm()` troca AnimatorController, AttackDataSO leve/pesado, HP máximo e escala do sprite; salva estado humano para restauração.
- `SquadMemberAI`: IA autônoma da criatura do squad — segue player em distância configurável, ataca inimigos próximos com `lightAttack`, para ao `IsPlayerControlled=true`.
- `SquadController`: gerencia 2 slots, `OnSwap` callback (Tab/LB), `SetSquad`/`SetActiveFormOnly`/`SetPassiveSlot` API, instancia `SquadMemberAI` prefab em runtime com cooldown de troca.
- `PlayerController`: `SetAttackData()` troca AttackDataSOs e reconstrói estados de ataque; `SetMoveSpeedOverride()` permite que a Forma Ativa tenha velocidade diferente do SO; `LightAttackData`/`HeavyAttackData` expostos como propriedades públicas.

---

### [MVP-5] Sistema de captura funcional com fórmula completa — 12/09/2026
- `WorkAffinity`: enum flags com 8 aptidões de trabalho de base.
- `CreatureDefinitionSO`: define espécie (stats, elemento, moveset, WorkAffinity, loot, animatorController, captureHPThreshold).
- `CaptureSphereDataSO`: parâmetros por tier de esfera (baseChance, cor, tier).
- `CaptureFormula`: classe estática pura com `Calculate()`, `HPModifier()`, `StatusModifier()` e `RunTests()` via menu Unity (Palsoul > Run Capture Formula Tests).
- `CaptureStatus`: enum flags de status (Stunned, Burning, Poisoned, Frozen).
- `BestiaryData`: ScriptableObject com `RegisterCapture`, `HasCaptured`, `GetCaptureCount` e eventos `OnNewSpeciesCaptured`/`OnCaptureCounted`.
- `CreatureController`: HP%, IsAlerted proxy, status effects, fúria pós-falha (buff de dano + flash vermelho), evento `OnCaptured`.
- `CaptureSystem`: arremesso via coroutine, detecção de alvo por `OverlapCircleAll`, fórmula completa, log detalhado de cada tentativa, `OnCaptureSuccess`/`OnCaptureFailure` eventos.

---

### [MVP-4] Inimigo básico com State Machine completa — 12/09/2026
- `EnemyDataSO`: HP, speeds, raios de IA, AttackData, patrol, stagger, death delay, etherDrop.
- `EnemyController`: hub da State Machine com helpers `MoveTowards`/`StopMovement`/`DistanceToPlayer`, flag `IsAlerted`, gizmos de raios, assinatura de `OnDeath` e `OnStagger`.
- `EnemyIdleState`: detecta player no `detectionRadius`, transiciona para Patrol após delay.
- `EnemyPatrolState`: waypoints aleatórios dentro de `patrolRadius`, `patrolWaitTime` em cada ponto.
- `EnemyChaseState`: persegue com `chaseSpeed`, perde aggro em `loseAggroRadius`, ataca em `attackRange`.
- `EnemyAttackState`: para movimento (telegraph), ativa `HitboxController`, seta cooldown ao sair.
- `EnemyStaggerState`: imobiliza por `staggerDuration`, retorna para Chase ou Idle.
- `EnemyDeathState`: desliga física e colliders, fade out, destrói GO após `deathDestroyDelay`.
- `HitboxController`: `IsStealthHit` implementado — detecta `EnemyController.IsAlerted` e ângulo de ataque.

---

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
