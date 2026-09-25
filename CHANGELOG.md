# CHANGELOG — PalSoul

Formato: `[MVP-X] Descrição curta — data`

---

## MVP em andamento

### [MVP-1–7] Cena gerada e integração validada — 25/09/2026
- Licença Unity funcional nesta sessão; gerados `_Boot`, prefabs, controles, parâmetros e assets de teste.
- Corrigida ordem de criação da cena para preservar referências dos ScriptableObjects nos prefabs.
- 21/21 testes Unity aprovados, incluindo captura, combo, fúria, squad, Vigor, descanso, bloqueios de ações, movimento por teclado e i-frames da esquiva. Também passaram 15 verificações independentes de captura e a compilação das três assemblies.
- Testes em Editor avançam explicitamente o tempo de Play Mode e sincronizam teleporte com a física.
- README, GDD, prompt e setup atualizados. Avaliação visual e sensação do combate continuam pendentes do roteiro manual. MVP 8 não iniciado.

### [Docs] Catálogo, evolução e elementos — 25/09/2026
- Definida meta de 151 entradas no bestiário, incluindo formas iniciais e evoluídas.
- Definidas linhagens com duas ou três evoluções além da forma inicial, liberando novos poderes. Níveis variam por criatura; 23, 40 e 56 são exemplos.
- Pelo menos 30 criaturas terão duas opções na terceira evolução, escolhidas pelo jogador, podendo trocar elementos ou adicionar um segundo elemento. Todas as alternativas contam no catálogo.
- Distribuição das 151 entradas ainda precisa ser fechada: 30 linhagens independentes com bifurcação somam 150 entradas. Não foram presumidas exceções ou formas compartilhadas para acomodar a entrada restante e linhagens menores.
- Fixados dez elementos: Água, Fogo, Eletricidade, Terra, Fantasma, Escuridão, Luz, Grama, Gelo e Dragão. Criaturas podem ter um ou dois elementos distintos, com impacto em ataques, defesas e magias.
- Atualizados GDD, README e prompt; documentados dados futuros, critérios de aceitação e decisões de balanceamento pendentes. Nenhuma implementação de evolução ou combate elemental nesta alteração.
- Retomada: branch `feat/mvp-7-consolidacao` com alterações locais sem commit; compilação externa registrada como aprovada, mas `_Boot` e relatório de testes Unity ausentes. A tentativa inicial falhou por licença; houve abertura posterior do Editor, cujo último registro indica uma cena sem salvar. Validação dos MVPs 1–7 continua pendente.

---

### [MVP-1–7] Consolidação do protótipo — 21/09/2026
- Fixados Unity 6000.6.0f1, Input System 1.19.0, URP 17.6.0 e Test Framework 1.8.0; `Packages` deixa de ser ignorado pelo Git.
- Adicionado gerador de cena `_Boot`, prefabs, duas espécies, sprites de teste, câmera pixel-perfect, Input Actions, HUD e parâmetros editáveis. Geração ocorre na primeira abertura ou pelo menu Palsoul; preserva cena já existente.
- Capturas agora têm identidade e HP individuais; bestiário é clonado por sessão. Troca do squad preserva vida/posição, inicializa a espécie correta e mantém um companheiro autônomo, sem curar nem consumir stamina.
- Ancoradouro permite escolher forma e companheiro entre capturas, cura o squad, repõe esferas e reseta selvagens. Vigor pertence ao jogador e permanece entre formas. Outros upgrades não são cobrados enquanto não implementados.
- Corrigidos combo leve, bloqueio de ações na morte/menu, stagger do player, acerto duplicado por múltiplos colliders e aplicação da fúria ao dano. Arremesso usa uma representação visual reutilizável.
- Fórmula de captura separada em regras puras e configuração por ScriptableObject; adicionados resultados esperados independentes e testes de integração.
- Validação: 15 verificações das regras executadas com sucesso. Compilação externa contra bibliotecas locais do Unity disponível em `Tools/verify_compile.py`. **Editor/Play Mode e geração real de assets ainda não executados: licença Unity inativa (código 198).**
- README, setup e prompt alinhados às 11 etapas e ao squad de duas criaturas. MVPs 8–11 permanecem pendentes.

As entradas anteriores registram código criado; não representam comprovação de validação em Play Mode.

---

### [MVP-7] Ancoradouro funcional — 12/09/2026
- `EtherWallet`: `Add`, `TrySpend`, `HasEnough`, `TakeDeath` (retorna Éter perdido), `RestoreFromEco`; eventos `OnEtherChanged/OnEtherGained/OnEtherSpent`.
- `AttributeUpgradeSO`: define 1 atributo upgradeável com `AnimationCurve` de custo por nível e `AttributeType` enum.
- `AnchorpointDataSO`: nome, `regionID`, lista de upgrades disponíveis, raio de interação.
- `WorldResetSystem`: `SpawnEntry` list (prefab + posição), `SpawnAll` no Start, `ResetWorld` destrói e reinstancia todos os inimigos não-chefe, gizmos de spawn.
- `AnchorpointController`: `Rest` (cura HP+Stamina+reset mundo), `UpgradeAttribute` (consome Éter+aplica efeito), `SwapActiveForm` (chama SquadController), detecção por trigger, eventos `OnPlayerRested/OnAttributeUpgraded/OnDiscovered`.
- `AnchorpointMenuUI`: menu OnGUI arrastável com Descansar, lista de upgrades com custo/nível/cor de affordance, trocar Forma Ativa, fechar com ESC, prompt de interação.

---

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
