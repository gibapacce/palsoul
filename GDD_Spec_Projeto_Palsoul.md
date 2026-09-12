# GAME DESIGN DOCUMENT / SPEC TÉCNICA
## Codinome: "PALSOUL" — Pixel Art Creature-Soulslike (Unity 2D)

**Versão:** 1.0
**Data:** 12/09/2026
**Objetivo do documento:** Servir de especificação de referência para um agente de desenvolvimento (ex: Claude Code) implementar o projeto em Unity de forma incremental, sem ambiguidade sobre escopo, arquitetura e critérios de conclusão.

---

## 1. PITCH (1 parágrafo)

Um ARPG 2D em pixel art, visão top-down, onde o jogador explora um mundo aberto hostil capturando e domesticando criaturas ("Companions") para lutar, trabalhar e sobreviver — como em Palworld — mas todo o combate direto do jogador (e das lutas de chefe) segue a gramática soulslike de Elden Ring: stamina finita, rolls com i-frames, parry/guard-counter, bonfires (checkpoints que respawnam inimigos e curam o jogador), perda de recursos ao morrer com chance de recuperação no local da morte, e chefes de arena com múltiplas fases e "runback". A fusão central é: **captura, base-building e automação (loop Palworld) + combate punitivo e progressão baseada em risco (loop Elden Ring)**.

---

## 2. REFERÊNCIAS PESQUISADAS E O QUE EXTRAIR DE CADA UMA

### 2.1 Palworld (Pocketpair) — pilares de sistema a replicar
- Captura de criaturas ("Pals") via esfera de captura arremessada; taxa de captura sobe conforme o HP da criatura cai e conforme o tier da esfera sobe; ataques pelas costas dão bônus de captura.
- Cada criatura tem tipo elemental com vantagens/desvantagens (STAB) e "work suitability" — aptidões para tarefas específicas de base (minerar, plantar, apagar fogo, gerar eletricidade etc).
- Loop de 3 pilares confirmado nas fontes: **(a) base building + automação de Pals** (Pals designados para tarefas realizam produção sozinhos), **(b) survival crafting** (coleta de recursos → bancadas → armas/estruturas/armaduras), **(c) exploração + combate**.
- Sistema de fome/nutrição tanto do jogador quanto das criaturas; starvation não mata mas trava recuperação de vida.
- Registro tipo "Pokédex" (Palpedia) por espécie capturada, com bônus de XP para as primeiras capturas de cada espécie.
- Progressão de base: estrutura central (Palbox) sobe de nível liberando novos edifícios; Pals com afinidades de construção aceleram isso.
- Multiplayer cooperativo é parte do DNA da referência, mas **fora do escopo do MVP** deste documento (ver seção 12).

### 2.2 Elden Ring / gramática Soulslike — pilares de combate e estrutura a replicar
- Combate stamina-based: todo golpe, esquiva, bloqueio e parry consome stamina; jogador precisa recuar para regenerar — combate deliberado, não button-mash.
- Roll com i-frames (invencibilidade parcial durante o frame de esquiva), parry/guard-counter, e sistema de Poise (armadura pesada resiste hitstun até um limiar).
- Checkpoints tipo bonfire: curam o jogador e recarregam consumíveis, mas **resetam todos os inimigos não-chefe do mapa**.
- Perda de recursos (a "moeda de alma") ao morrer, com um marcador no local da morte que permite recuperar o valor perdido se o jogador chegar lá sem morrer de novo.
- Chefes de arena: múltiplas fases (transição de fase ao atingir % de HP, geralmente com nova moveset/AOE), "runback" (trajeto do checkpoint até a arena, sem monstros triviais no caminho para não ser injusto), telegraphs claros de ataque (windup visual antes do hit).
- Exploração aberta que permite ao jogador recuar de um encontro difícil, farmar/upar em outra área, e voltar mais forte — filosofia de "sempre há outro caminho".
- Jump attacks, guard-counters e stealth attacks (dano bônus por trás/furtivo) são adições da fórmula Elden Ring sobre os Souls anteriores.

### 2.3 Híbridos 2D pixel art existentes (prova de conceito de viabilidade)
- Existem asset packs comerciais e projetos indie especificamente rotulados "2D Soulslike" na Unity Asset Store e itch.io (ex.: "2D Soulslike Knight", tilesets "2D soulslike metroidvania", UI de HP/stamina prontas), confirmando que a combinação pixel art + soulslike é um nicho ativo e tecnicamente resolvido em Unity 2D.
- Jogos citados como referência de "soulslike 2D preciso" (ex. Nine Sols) mostram que parry/combate técnico funciona bem em câmera 2D, mesmo sendo side-scroller — usaremos isso como referência de *feel* de combate, adaptando para câmera top-down.
- Conclusão de escopo: **não existe um "Palworld 2D pixel art soulslike" publicado** — é uma combinação de nicho genuína, não uma cópia de um jogo existente.

---

## 3. PILARES DE DESIGN (Design Pillars)

1. **Captura tem peso soulslike.** Capturar uma criatura forte é, na prática, "vencer um miniboss": exige gerenciar stamina, iniciar do jeito certo (ataque furtivo/pelas costas), e arriscar recursos (esferas caras).
2. **A base é seu santuário (bonfire), não uma cidade segura.** A base funciona como checkpoint: cura, guarda progressão, mas também é onde a automação com criaturas acontece. Perder para um chefe te manda de volta pra base mais próxima, não para o início do jogo.
3. **Morte tem custo, não é game over.** Perder recursos (não XP/level) no local da morte, recuperáveis, mantém o "risco" soulslike sem travar a progressão de personagem.
4. **Criaturas são ferramentas de combate E de economia — nunca uma sem a outra.** Toda criatura útil em combate também tem pelo menos 1 aptidão de trabalho de base, e vice-versa.
5. **Pixel art legível > pixel art bonito.** Telegraphs de ataque de chefes precisam ser lidos em poucos frames; silhueta e cor têm prioridade sobre detalhe.

---

## 4. GAMEPLAY LOOP

### 4.1 Loop moment-to-moment (combate)
Explorar → encontrar criatura/inimigo → avaliar (elemento, padrão de ataque) → desgastar via combate soulslike (stamina, dodge, parry) → capturar (se criatura) ou lootar (se inimigo humano/construto) → gerenciar stamina/HP/itens de cura limitados → decidir avançar ou recuar para checkpoint.

### 4.2 Loop de sessão (curto prazo, 15–30 min)
Sair da base → explorar região → lutar/capturar → acumular recursos e novas criaturas → morrer ou retornar → depositar criaturas/recursos na base → atribuir criaturas a tarefas de produção → craftar upgrade → repetir com equipamento/companions melhores.

### 4.3 Loop de progressão (longo prazo)
Desbloquear novas regiões (gated por chefe de área, não por level check duro) → Palpedia/Bestiário completo por bioma → upgrade de base (tier de estrutura central) → builds de equipe de criaturas (elemento/aptidão) → derrotar chefes de área → chefe final de arco.

---

## 5. SISTEMAS CENTRAIS (especificação funcional)

### 5.1 Combate do jogador (Soulslike)
| Elemento | Especificação |
|---|---|
| Stamina | Barra separada de HP. Consumida por: ataque leve, ataque pesado, dodge roll, bloqueio (dano reduzido drena stamina proporcional ao dano bloqueado), parry (custo fixo). Regenera após ~1s sem gastar, taxa configurável via ScriptableObject. |
| Dodge Roll | Direcional (8 direções), tem janela de i-frames configurável (ex.: frames 3–9 de uma animação de 18 frames). Diagonal e mesmo tempo de execução em todas direções. |
| Parry | Input de bloqueio cronometrado no frame de impacto do inimigo; sucesso = inimigo entra em "stagger" (guard-break), abre janela de execução (crítico com dano alto ou animação de "riposte"). |
| Poise/Stagger do jogador | Jogador tem um "poise" pequeno (quase 0) — hits normalmente interrompem ação, reforçando o incentivo a evitar tomar dano em vez de "tankar". |
| Ataques furtivos | Dano bônus (ex.: 2.5x) ao acertar pelas costas de inimigo/criatura não alertado. Também aumenta chance de captura (ver 5.2). |
| Ciclo de risco/recompensa de item de cura | Item de cura consumível com carga limitada, recarregada apenas em checkpoints (bonfire-like). Sem cura infinita. |

### 5.2 Captura de criaturas
- Ferramenta: "Esfera de Contenção" (item consumível, múltiplos tiers: Comum/Aprimorada/Rara/Lendária).
- Fórmula de captura (baseada no modelo Palworld): `chance_base(tier_esfera) × modificador(%HP restante da criatura) × modificador(furtivo/pelas_costas) × modificador(status: atordoada/queimando/etc)`.
- HP crítico (abaixo de um limiar, ex. 20%) é obrigatório para chances viáveis em criaturas de tier alto — força o jogador a "lutar como um soulslike" antes de capturar, não apenas spammar esferas.
- Falha na captura: a esfera é perdida e a criatura entra em fúria (buff de dano temporário) — pune spam de tentativas.
- Toda criatura capturada é registrada num "Bestiário" com stats, elemento, aptidões de trabalho e lore curta.

### 5.3 Elementos e combate
- 6 a 8 elementos no MVP (ex.: Físico/Neutro, Fogo, Água, Terra, Sombra, Luz, Veneno) com matriz de vantagem simples (triângulo estendido), aplicável tanto a ataques de criaturas quanto (parcialmente) a armas elementais do jogador.
- Vantagem elemental = multiplicador de dano fixo (ex.: 1.3x / 0.75x), nunca "one-shot" — mantém a tensão soulslike de que técnica > vantagem numérica pura.

### 5.4 Criaturas ("Companions")
- Cada espécie definida via `CreatureDefinitionSO` (ScriptableObject) contendo: stats base, elemento, moveset de combate (lista de `AttackDataSO`), aptidões de trabalho (enum flags: Mineração, Corte, Colheita, Fundição, Geração de Energia, Transporte), tabela de loot/drop, raridade, sprite sheet e animator controller reference.
- Estados de uma criatura: Selvagem (mundo) → Em combate → Capturada → (Alocada: Squad ativo | Base: Ociosa | Base: Trabalhando).
- Squad ativo do jogador: limite inicial de 2–3 criaturas acompanhando simultaneamente (expansível via progressão), lutando semi-autonomamente com IA simples orientada a comandos do jogador (Atacar / Focar alvo / Recuar).

### 5.5 Base building & automação
- Grid de construção livre (não estritamente tile-locked) ao redor de uma estrutura central ("Ancoradouro", equivalente ao Palbox).
- Estrutura central = também o checkpoint tipo bonfire da região: cura o jogador, restaura itens de cura e recarrega esferas ao descansar nela; descansar respawna inimigos não-chefe do mundo (regra soulslike explícita).
- Edifícios de produção (serraria, forja, fazenda, mina automatizada) recebem criaturas alocadas cuja aptidão de trabalho corresponde; produção ocorre em tempo real/offline (definir no MVP se offline-progress entra ou não — recomendação: **não** entra no MVP, mantém simplicidade).
- Fome/energia: criaturas na base consomem ração do celeiro; sem ração, produção para (sem "morte" — consistente com a mecânica de starvation da referência).

### 5.6 Morte e penalidade (regra soulslike)
- Ao morrer: jogador perde um recurso de progressão secundário chamado "Éter" (moeda usada para upgrades de atributo, não XP de nível) e reaparece no último Ancoradouro visitado.
- Um marcador visual ("Eco") aparece no local da morte com o Éter perdido; alcançar o marcador sem morrer novamente recupera o valor. Morrer antes de chegar lá destrói o Éter perdido (consistente com Souls).
- Todos os inimigos não-chefe do mapa da região são resetados ao descansar no Ancoradouro (não ao morrer, para evitar punição dupla).

### 5.7 Chefes
- Todo chefe de área vive numa arena fechada (sem fuga lateral), acessível por um "runback" curto e limpo (sem trash mobs no caminho, seguindo a boa prática identificada nas referências).
- Estrutura mínima por chefe: 2 fases (troca de moveset/padrão ao cruzar limiar de HP, ex. 50%), todo ataque com telegraph de no mínimo N frames antes do hit (N definido por dificuldade), pelo menos 1 janela de punição clara por combo.
- Chefes só podem ser **derrotados**, nunca capturados — mantém o "peso soulslike" da vitória e evita trivializar a progressão de captura.

---

## 6. PROGRESSÃO DE PERSONAGEM

- Atributos manuais (estilo Souls): Vigor (HP), Vigor de Ação (Stamina), Força/Destreza (dano de arma), Afinidade Elemental (dano/resistência elemental).
- Pontos de atributo comprados no Ancoradouro gastando "Éter" (ver 5.6) — reforça o loop de risco: Éter é ganho matando inimigos/explorando e perdido ao morrer.
- Equipamento: armas com movesets distintos (não só stats), armaduras com trade-off peso/poise/velocidade de rolagem (peso alto = rolagem mais lenta, referência direta ao sistema de equip load de Souls).
- Progressão de criaturas: nível por batalha/uso, evolução opcional de espécie (transformação visual + realoque de stats), sem gacha/paywall (fora de escopo monetização neste doc).

---

## 7. ESTRUTURA DE MUNDO

- Mundo dividido em **regiões conectadas** (não world aberto contínuo no MVP — recomendação de escopo para viabilidade de produção), cada região com: 1 bioma visual, 1 Ancoradouro central, 1 a 2 chefes de área, população de criaturas selvagens temáticas do bioma.
- Gate de progressão entre regiões: barreira de dificuldade (chefe) ou de item-chave (habilidade de traversal, ex. "criatura voadora" desbloqueia acesso a região elevada) — nunca level-gate numérico puro.
- Câmera: top-down 2D com leve profundidade (pseudo-perspectiva, ao estilo Zelda/Tunic), suporte a diferença de elevação simples (pontes, penhascos) via camadas de colisão e sorting layers, não verdadeiro 3D.

---

## 8. ESPECIFICAÇÃO DE ARTE (Pixel Art)

| Item | Especificação recomendada |
|---|---|
| Resolução de referência de câmera | 320×180 (16:9 nativo) com pixel-perfect camera do Unity 2D. |
| Tile size | 16×16 px base (grid de tilemap), personagens em múltiplos de 16 (ex. 16×24 ou 32×32 para chefes). |
| Paleta | Paleta limitada por bioma (16–32 cores por bioma) para manter leitura e coesão; alto contraste entre inimigo/telegraph e fundo. |
| Animação de combate | Mínimo por entidade combatente: Idle, Walk, Attack leve, Attack pesado, Hit-react, Death, Dodge/Roll (se aplicável), Telegraph/Windup (chefes). |
| Legibilidade de telegraph | Uso de flash de cor / partícula de aviso nos frames de windup de ataques de chefe — não depender só da animação. |
| Ferramentas sugeridas | Aseprite (fonte de sprites), Unity 2D Animation package + Sprite Atlas, Tilemap + Rule Tiles para biomas. |

---

## 9. ARQUITETURA TÉCNICA UNITY (para o agente implementar)

### 9.1 Stack
- **Engine:** Unity 2022 LTS ou superior, pipeline URP 2D (Renderer 2D).
- **Linguagem:** C#, .NET padrão do Unity.
- **Input:** Unity Input System (novo), não o Input Manager legado.
- **Dados de conteúdo:** ScriptableObjects para todo conteúdo de design (criaturas, ataques, itens, elementos) — nunca hardcoded em MonoBehaviours.
- **Save/Load:** JSON serializado (Newtonsoft.Json ou System.Text.Json) em arquivo local; sem dependência de backend no MVP.

### 9.2 Padrões de arquitetura obrigatórios
- **State Machine explícita** para Player e para cada Inimigo/Chefe (ex.: enum + interface `IState` com `Enter/Tick/Exit`, ou classe base `StateMachineBehaviour`). Nada de combate implementado via `if/else` soltos em `Update()`.
- **Command/Event bus leve** (C# events ou um `EventBus` estático simples) para desacoplar UI, áudio e gameplay (ex.: evento `OnPlayerDamaged`, `OnCreatureCaptured`, `OnBossPhaseChanged`).
- **ScriptableObject-driven design:** `CreatureDefinitionSO`, `AttackDataSO`, `ElementSO`, `ItemDefinitionSO`, `BuildingDefinitionSO` — permitem ao designer/agente criar conteúdo sem tocar em código depois que os sistemas existirem.
- **Object pooling** obrigatório para projéteis, partículas de dano e VFX de telegraph (evitar `Instantiate/Destroy` em loop de combate).
- **Separação de camadas:** Core (regras de combate/stamina/captura) não deve referenciar diretamente UI ou Áudio — comunicação via eventos.

### 9.3 Estrutura de pastas sugerida
```
Assets/
  _Project/
    Art/
      Characters/
      Creatures/
      Tilesets/
      UI/
      VFX/
    Audio/
      SFX/
      Music/
    Prefabs/
      Player/
      Creatures/
      Bosses/
      Buildings/
      Projectiles/
    ScriptableObjects/
      Creatures/
      Attacks/
      Elements/
      Items/
      Buildings/
    Scenes/
      Regions/
      _Boot.unity
    Scripts/
      Core/          (regras puras: Stamina, HealthSystem, ElementMatrix, CaptureFormula)
      Combat/        (StateMachines de player/inimigo, HitboxSystem, ParrySystem)
      Creatures/      (CreatureController, CreatureAI, WorkAssignment)
      Base/          (BuildingPlacement, ProductionSystem, AnchorpointCheckpoint)
      Progression/   (StatSystem, EtherWallet, DeathMarker)
      Save/
      UI/
      Utils/
    Settings/ (URP assets, Input Actions asset)
```

### 9.4 Convenções de código
- Namespaces por pasta (`Palsoul.Core`, `Palsoul.Combat`, `Palsoul.Creatures`, etc.).
- Sem singletons "God object"; usar injeção simples via referência de cena ou `ServiceLocator` minimalista se necessário.
- Toda constante de balanceamento (dano, custo de stamina, taxas de captura) exposta em ScriptableObject/Inspector, nunca "magic number" no código.

---

## 10. UI/UX (telas mínimas)

1. HUD de combate: barra de HP, barra de Stamina, ícones de squad ativo (até 3 criaturas com mini-HP), contador de item de cura, contador de esferas de captura.
2. Bestiário (Palpedia-like): grid de espécies descobertas/capturadas com stats e aptidões.
3. Tela de Base: overlay de construção (grid + lista de edifícios) e tela de alocação de criaturas por edifício.
4. Tela de Ancoradouro (checkpoint): opções de descansar (cura+reset de mundo), gastar Éter em atributos, viajar rápido entre Ancoradouros descobertos.
5. Tela de morte: mostra Éter perdido e ponto do "Eco" no minimapa.
6. Tela de chefe: barra de HP do chefe com marcador de fase, sem barra de stamina do chefe (mantém mistério, como em Elden Ring).

---

## 11. ÁUDIO (diretriz, não obrigatório para MVP funcional)

- SFX de combate reativo a i-frames (som distinto de "esquiva bem-sucedida" vs "hit recebido").
- Música dinâmica: silêncio/ambiente em exploração, tema de combate ao entrar em encontro, tema de chefe dedicado por chefe de área (referência direta à estrutura Souls).

---

## 12. ESCOPO — MVP vs. VISÃO COMPLETA

### 12.1 MVP (o que o agente deve construir primeiro, nesta ordem)
1. Movimento do player top-down + câmera pixel-perfect.
2. Sistema de Stamina + Dodge Roll com i-frames.
3. Sistema de ataque leve/pesado do player + hitbox/hurtbox.
4. 1 inimigo básico com State Machine (Idle/Patrol/Chase/Attack/Stagger/Death).
5. Sistema de captura funcional com 1 criatura de teste (fórmula completa da seção 5.2).
6. 1 Ancoradouro funcional: cura, reset de mundo, gasto de Éter em 1 atributo.
7. Sistema de morte + Éter + Eco (marcador recuperável).
8. 1 chefe de arena com 2 fases e runback curto.
9. Base mínima: 1 tipo de edifício de produção + alocação de 1 criatura + 1 recurso sendo gerado.
10. Save/Load em JSON cobrindo todo o exposto acima.

### 12.2 Fora do MVP (backlog pós-MVP)
- Multiplayer cooperativo.
- Evolução visual de criaturas / breeding.
- Progressão offline da base (produção enquanto o jogo está fechado).
- Sistema de armas com múltiplos movesets por classe de arma.
- Ciclo dia/noite com IA de criaturas variando por horário.
- Monetização/itens cosméticos.

---

## 13. CRITÉRIOS DE ACEITAÇÃO POR SISTEMA (Definition of Done)

- **Combate do player:** roll tem i-frames mensuráveis (testável via log/gizmo), stamina impede spam infinito de ações, parry tem janela configurável e gera stagger visível no inimigo.
- **Captura:** taxa de sucesso responde corretamente a %HP, tier de esfera e ataque furtivo (testável com valores fixos gerando probabilidade esperada, cobertura por teste unitário simples de `CaptureFormula`).
- **Morte/Éter:** valor perdido é exatamente o Éter carregado no momento da morte; Eco desaparece permanentemente se o jogador morrer antes de alcançá-lo.
- **Chefe:** transição de fase é determinística por %HP (não por tempo), todo ataque tem telegraph mínimo de X frames antes do hitbox ativar.
- **Base/Produção:** criatura alocada só aceita tarefas compatíveis com suas `WorkAffinity` flags; produção gera recurso a uma taxa configurável por ScriptableObject.
- **Save/Load:** fechar e reabrir o jogo restaura posição, atributos, squad, bestiário e estado da base sem perda de dados.

---

## 14. GLOSSÁRIO RÁPIDO

- **Ancoradouro:** equivalente a bonfire (Souls) + Palbox (Palworld). Checkpoint e hub de base.
- **Éter:** moeda de progressão perdida/recuperável ao morrer (equivalente a Souls/Runas).
- **Eco:** marcador do local de morte com o Éter perdido.
- **Companion:** criatura capturável, equivalente a "Pal".
- **Work Affinity:** aptidão de trabalho de uma criatura para tarefas de base.

---

## 15. NOTA SOBRE ORIGINALIDADE

Este documento descreve um sistema de jogo **original**, inspirado nos *padrões de mecânica* de Palworld (captura, base-building, automação) e da fórmula soulslike consagrada por Elden Ring/FromSoftware (stamina, i-frames, bonfires, morte com recuperação, chefes em fases). Nenhum nome próprio, personagem, criatura, arte ou texto desses jogos deve ser reproduzido — todos os nomes acima ("Ancoradouro", "Éter", "Eco", "Companion") são propositalmente autorais para uso livre em produção.
