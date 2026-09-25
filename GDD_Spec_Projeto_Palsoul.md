# GAME DESIGN DOCUMENT / SPEC TÉCNICA
## Codinome: "PALSOUL" — Pixel Art Creature-Soulslike (Unity 2D)

**Versão:** 1.2 — catálogo, evolução por nível e elementos
**Data:** 25/09/2026 (criação: 12/09/2026)
**Objetivo do documento:** Servir de especificação de referência para um agente de desenvolvimento (ex: Claude Code) implementar o projeto em Unity de forma incremental, sem ambiguidade sobre escopo, arquitetura e critérios de conclusão.

---

## 1. PITCH (1 parágrafo)

Um ARPG 2D em pixel art, visão top-down, onde o jogador explora um mundo aberto hostil capturando criaturas ("Companions") — e ao capturá-las, **torna-se elas**: o personagem humano desaparece e o jogador passa a controlar a criatura capturada, usando seu moveset, elemento e poderes únicos. Duas criaturas compõem o squad ativo; o jogador alterna o controle direto entre elas em tempo real, enquanto a outra luta de forma autônoma. A Forma Ativa (qual criatura o jogador "é") é escolhida no Ancoradouro. Todo o combate segue a gramática soulslike de Elden Ring: stamina finita, rolls com i-frames, parry/guard-counter, bonfires (checkpoints que respawnam inimigos e curam o jogador), perda de recursos ao morrer com chance de recuperação no local da morte, e chefes de arena com múltiplas fases e "runback". A fusão central é: **transformação em criaturas capturadas (identidade de combate fluida) + combate punitivo e progressão baseada em risco (loop Elden Ring) + base-building e automação (loop Palworld)**.

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

1. **Capturar é tornar-se.** Capturar uma criatura não é só "recrutá-la" — é adquirir uma nova identidade de combate. O jogador literalmente se transforma na criatura e joga com seu moveset, elemento e poderes. Cada captura expande o repertório de "quem você pode ser".
2. **Captura tem peso soulslike.** Capturar uma criatura forte é, na prática, "vencer um miniboss": exige gerenciar stamina, iniciar do jeito certo (ataque furtivo/pelas costas), e arriscar recursos (esferas caras).
3. **A base é seu santuário (bonfire), não uma cidade segura.** A base funciona como checkpoint: cura, guarda progressão, permite trocar a Forma Ativa, mas também é onde a automação com criaturas acontece. Perder para um chefe te manda de volta pra base mais próxima, não para o início do jogo.
4. **Morte tem custo, não é game over.** Perder recursos (não XP/level) no local da morte, recuperáveis, mantém o "risco" soulslike sem travar a progressão de personagem.
5. **Criaturas são identidade de combate E economia — nunca uma sem a outra.** Toda criatura útil em combate também tem pelo menos 1 aptidão de trabalho de base, e vice-versa.
6. **Pixel art legível > pixel art bonito.** Telegraphs de ataque de chefes precisam ser lidos em poucos frames; silhueta e cor têm prioridade sobre detalhe.

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
- O jogo terá **10 elementos de criaturas**: **Água, Fogo, Eletricidade, Terra, Fantasma, Escuridão, Luz, Grama, Gelo e Dragão**. Esta lista substitui a proposta anterior de 6 a 8 elementos.
- Cada criatura terá **um ou dois elementos**. Nas criaturas de dois elementos, eles devem ser distintos. **Fantasma e Escuridão são elementos separados.**
- Os elementos devem diferenciar o repertório de **ataques, defesas e magias** de cada criatura, influenciando tanto sua identidade de combate quanto suas vantagens e vulnerabilidades.
- **Ataques e magias:** cada poder deve declarar sua afinidade elemental, comportamento, dano, custo, cooldown e efeitos aplicáveis. Compartilhar um elemento não obriga criaturas diferentes a terem o mesmo moveset.
- **Defesas:** uma matriz elemental deve definir as resistências e vulnerabilidades contra cada elemento atacante. O cálculo precisa considerar ambos os elementos de um defensor de tipo duplo.
- **Balanceamento a definir:** relações de vantagem entre os dez elementos, multiplicadores, combinação das duas afinidades defensivas, eventuais imunidades e bônus por usar poderes do próprio elemento. Não assumir soma ou multiplicação dos modificadores de tipo duplo antes dessa definição.
- A vantagem elemental deve complementar leitura de padrões, stamina e esquiva, preservando o combate soulslike. Os números ficam em ScriptableObjects/Inspector; não fixar os exemplos antigos de multiplicador como valores finais.
- Físico/Neutro, Sombra e Veneno não integram a lista de dez elementos de criaturas. Dano físico e efeitos como envenenamento podem ser tratados separadamente; a taxonomia de dano/status será definida no balanceamento.

### 5.4 Criaturas ("Companions") e Sistema de Transformação

#### Definição de espécie
Cada espécie será definida via `CreatureDefinitionSO` (ScriptableObject) contendo: identificador estável, stats base, um ou dois elementos, moveset de combate (lista de `AttackDataSO`), dados das evoluções e poderes desbloqueados, aptidões de trabalho (enum flags: Mineração, Corte, Colheita, Fundição, Geração de Energia, Transporte), tabela de loot/drop, raridade, sprite sheet e animator controller reference. Esta é a especificação de destino; o código atual ainda precisa ser adaptado para evolução e dois elementos.

#### Catálogo de criaturas
- **Meta do jogo completo: 151 entradas no bestiário, incluindo as formas iniciais e todas as evoluções.** O catálogo de nomes, visuais, distribuição elemental e poderes ainda será elaborado.
- Cada forma inicial ou evoluída conta como uma entrada; capturar vários indivíduos da mesma forma não aumenta o total do catálogo.
- As linhagens podem ter **duas ou três evoluções**, além da forma inicial. Todas as alternativas de uma evolução ramificada contam como entradas próprias nas 151.
- **Distribuição a fechar antes de produzir o catálogo:** pelo menos 30 criaturas devem oferecer duas opções na terceira evolução. Considerando linhagens independentes (inicial → evolução 1 → evolução 2 → evolução 3A ou 3B), cada uma ocupa cinco entradas no catálogo; 30 linhagens assim ocupam 150. Resta definir como acomodar a entrada restante e as linhagens com duas evoluções. Não presumir formas compartilhadas, fusão de linhagens ou uma criatura sem evolução sem decisão explícita.
- A meta de 151 criaturas não altera a quantidade de duas criaturas do squad e não implica produzir todo o catálogo durante a consolidação dos MVPs 1–7.

#### Evolução por nível e desbloqueio de poderes
- Cada linhagem terá **duas ou três evoluções definidas pelo nível da criatura**, além da forma inicial. Cada evolução libera novos poderes próprios daquela criatura; as formas evoluídas são etapas da mesma linhagem, não o início de outra sequência de evoluções.
- A evolução passa a ser um requisito do jogo completo, substituindo a menção anterior a evolução opcional.
- **Exemplo fornecido para os marcos de evolução:**

| Marco | Nível de exemplo | Requisito de progressão |
|---|---|---|
| Primeira evolução | 23 | Desbloquear novos poderes definidos para a criatura |
| Segunda evolução | 40 | Desbloquear novos poderes da próxima evolução |
| Terceira evolução | 56 | Desbloquear novos poderes da última evolução |

- Os níveis **23, 40 e 56 são apenas exemplos**. Os níveis exigidos **variam por criatura/linhagem** e devem ser configuráveis, positivos e ordenados. Os valores finais serão definidos na ficha de cada linhagem.
- **Linhagem com duas evoluções:** inicial → evolução 1 → evolução 2, totalizando três formas.
- **Linhagem com três evoluções sem bifurcação:** inicial → evolução 1 → evolução 2 → evolução 3, totalizando quatro formas.
- **Pelo menos 30 criaturas terão duas opções na terceira evolução:** inicial → evolução 1 → evolução 2 → **evolução 3A ou evolução 3B**. O jogador escolhe o caminho; cada caminho percorrido tem quatro formas, mas a linhagem completa ocupa cinco entradas no catálogo, pois ambas as alternativas contam.
- As duas alternativas devem ter poderes próprios. A escolha pode **trocar a composição elemental** da criatura ou **acrescentar um segundo elemento**, respeitando o limite de dois elementos distintos da seção 5.3. Uma criatura que já tenha dois elementos não pode receber um terceiro; nesse caso a alternativa deve definir explicitamente a substituição de elementos.
- Cada etapa deve declarar explicitamente quais ataques, magias ou habilidades defensivas libera. A evolução precisa mudar as opções de jogo, além de possíveis mudanças visuais e de atributos.
- A progressão pertence ao indivíduo capturado: registrar nível, experiência, estágio evolutivo e poderes desbloqueados. Alternar Forma Ativa ou atribuir a criatura ao squad/base não deve apagar essa progressão.
- Os poderes desbloqueados devem estar disponíveis ao controlar a criatura; a IA do companheiro também precisará reconhecer seu estágio e repertório. A seleção de poderes nos slots de combate será definida antes da implementação.
- **Outras decisões pendentes:** evolução automática ou confirmada nas etapas sem bifurcação; possibilidade de adiamento; reversibilidade da escolha na terceira evolução; preservação/substituição de poderes anteriores; níveis e composição elemental de cada alternativa; tratamento de níveis que atravessam vários marcos de uma vez. A terceira evolução bifurcada exige escolha do jogador. Não inventar as demais regras durante a implementação.

#### Estados de uma criatura
Selvagem (mundo) → Em combate → Capturada → (Forma Ativa | Squad Autônomo | Base: Ociosa | Base: Trabalhando).

#### Sistema de Transformação (mecânica central)
- **Forma Ativa:** a criatura que o jogador **é** em campo. O personagem humano desaparece — o jogador controla diretamente a criatura capturada, usando seu moveset, elemento e poderes únicos.
- **Squad ativo:** 2 criaturas no total. Uma é a Forma Ativa (controle direto do jogador); a outra luta de forma **autônoma** com IA simples ao lado do jogador.
- **Troca de controle em campo:** o jogador pode alternar o controle direto entre as duas criaturas do squad com um botão dedicado (ex.: `Tab` / `LB`). Ao trocar, a criatura que perde o controle passa a operar de forma autônoma; a que recebe o controle passa a ser controlada pelo jogador. Não há custo de stamina na troca — o custo é de decisão tática.
- **Troca de Forma Ativa:** qual criatura ocupa o slot principal do squad só pode ser alterada no **Ancoradouro**, escolhendo entre todas as criaturas capturadas. Isso mantém o peso da decisão de "qual identidade de combate levar" para cada área.
- **Personagem humano:** existe como entidade de progressão (atributos, Éter, equipamento), mas não tem presença visual em combate enquanto há uma Forma Ativa viva no squad. Reaparece apenas na tela de morte / tela do Ancoradouro.

#### Moveset por criatura
- Cada `CreatureDefinitionSO` define os ataques disponíveis quando o jogador está no controle daquela forma.
- Ataque leve, ataque pesado e habilidade especial (cooldown) são os slots padrão — configuráveis por criatura via `AttackDataSO`.
- O dodge roll e o sistema de stamina são universais (não mudam por criatura) — cada forma usa os mesmos controles base com movesets diferentes.

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
- Progressão de criaturas: nível por batalha/uso, duas ou três evoluções por linhagem com desbloqueio de novos poderes (seção 5.4). Pelo menos 30 criaturas oferecem duas alternativas na terceira evolução, escolhidas pelo jogador, podendo trocar ou adicionar elementos dentro do limite de dois. Níveis variáveis, com 23, 40 e 56 apenas como exemplos. Sem gacha/paywall (fora de escopo monetização neste doc).

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

### 9.5 Dados planejados para catálogo, evolução e elementos
- Separar definição de espécie/forma (conteúdo de design) do estado de cada indivíduo capturado.
- Definições de conteúdo deverão registrar até dois elementos distintos, duas ou três etapas evolutivas, níveis exigidos, referências visuais e poderes liberados por etapa. Representar alternativas na terceira evolução como destinos distintos, cada um com seus elementos e poderes. Usar identificadores estáveis para relacionar linhagem, forma e habilidade; cada forma e cada alternativa contam para as 151 entradas. Fechar a distribuição antes de produzir o catálogo.
- A matriz de ataque/defesa deverá cobrir os dez elementos e parametrizar a regra de combinação defensiva de tipos duplos quando aprovada.
- O salvamento futuro deverá preservar identidade do indivíduo, nível/experiência, estágio evolutivo, alternativa escolhida, poderes desbloqueados e alocação no squad/base. A forma escolhida deve restaurar a composição elemental correta. Planejar versão do formato e migração quando esses sistemas forem incorporados.
- Estas mudanças são requisitos documentados, ainda não implementados nos scripts atuais.

---

## 10. UI/UX (telas mínimas)

1. HUD de combate: barra de HP, barra de Stamina, ícones de squad ativo (2 criaturas com mini-HP), contador de item de cura, contador de esferas de captura.
2. Bestiário (Palpedia-like): catálogo com meta de 151 entradas, contando formas iniciais, evoluções e alternativas, exibindo stats, aptidões, um ou dois elementos, estágio evolutivo, níveis exigidos e poderes de cada evolução. Na escolha da terceira evolução, apresentar as duas formas possíveis, seus elementos e poderes antes da confirmação. Diferenciar a ficha de conteúdo da progressão individual de cada criatura capturada.
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
6. **Sistema de Transformação:** ao capturar uma criatura, o jogador pode ativá-la como Forma Ativa no Ancoradouro — o sprite/animator/moveset do jogador são substituídos pelos da criatura. Troca de controle entre Forma Ativa e criatura autônoma do squad via botão dedicado em campo.
7. 1 Ancoradouro funcional: cura, reset de mundo, gasto de Éter em 1 atributo, troca de Forma Ativa.
8. Sistema de morte + Éter + Eco (marcador recuperável).
9. 1 chefe de arena com 2 fases e runback curto.
10. Base mínima: 1 tipo de edifício de produção + alocação de 1 criatura + 1 recurso sendo gerado.
11. Save/Load em JSON cobrindo todo o exposto acima.

### 12.1.1 Acompanhamento da consolidação (25/09/2026)

A consolidação atual cobre implementação dos itens 1–7, cena e assets gerados e testes automatizados aprovados no Unity, incluindo integração em Play Mode. Consulte README.md e SETUP_MVP1.md para cobertura e limites. Avaliação visual da câmera, controle físico e sensação do combate ainda dependem do roteiro manual; o aceite da seção 13 não é integral, pois inclui sistemas futuros, como parry.

O protótipo usa dois indivíduos capturados distintos no squad. Cada um conserva seu HP nas trocas; descansar cura as criaturas capturadas. Vigor pertence ao jogador e soma ao HP base de cada forma. A cena começa com Éter de teste para validar o item 7 sem antecipar o fluxo de loot/morte do item 8.

Pendência de planejamento: parry, matriz elemental, habilidade especial e consumível de cura aparecem na visão funcional, mas não possuem etapa própria na lista incremental. Não estão entregues nesta consolidação; definir sua posição antes de considerar concluídos os critérios completos de combate da seção 13.

### 12.2 Fora do MVP (backlog pós-MVP)
- Multiplayer cooperativo.
- Produção do catálogo completo de 151 entradas e sistema de duas ou três evoluções por nível, com novos poderes por etapa e duas alternativas na terceira evolução de pelo menos 30 criaturas; etapa de implementação a planejar após fechar a distribuição do catálogo e definir os marcos de cada linhagem. Requisito do jogo completo, ainda fora da consolidação atual.
- Breeding (separado do sistema obrigatório de evolução).
- Progressão offline da base (produção enquanto o jogo está fechado).
- Sistema de armas com múltiplos movesets por classe de arma.
- Ciclo dia/noite com IA de criaturas variando por horário.
- Monetização/itens cosméticos.

### 12.3 Atualização de escopo documental (25/09/2026)

Catálogo de 151 criaturas, evolução por nível, dez elementos e criaturas de tipo duplo são requisitos do jogo completo. A implementação atual permanece na consolidação dos MVPs 1–7. A posição da matriz elemental, dos tipos duplos e da evolução no cronograma precisa ser definida; esta atualização não autoriza tratá-los como já implementados nem altera automaticamente a ordem das 11 etapas.

Próximos passos de design: distribuir as 151 entradas entre linhagens com duas/três evoluções e as alternativas de pelo menos 30 criaturas; resolver a restrição de contagem indicada na seção 5.4; definir níveis, elementos e poderes de cada etapa/alternativa; elaborar matriz elemental e regra de defesa dupla; então planejar a produção do catálogo.

---

## 13. CRITÉRIOS DE ACEITAÇÃO POR SISTEMA (Definition of Done)

- **Combate do player:** roll tem i-frames mensuráveis (testável via log/gizmo), stamina impede spam infinito de ações, parry tem janela configurável e gera stagger visível no inimigo.
- **Captura:** taxa de sucesso responde corretamente a %HP, tier de esfera e ataque furtivo (testável com valores fixos gerando probabilidade esperada, cobertura por teste unitário simples de `CaptureFormula`).
- **Morte/Éter:** valor perdido é exatamente o Éter carregado no momento da morte; Eco desaparece permanentemente se o jogador morrer antes de alcançá-lo.
- **Chefe:** transição de fase é determinística por %HP (não por tempo), todo ataque tem telegraph mínimo de X frames antes do hitbox ativar.
- **Base/Produção:** criatura alocada só aceita tarefas compatíveis com suas `WorkAffinity` flags; produção gera recurso a uma taxa configurável por ScriptableObject.
- **Transformação / Forma Ativa:** ao ativar uma criatura como Forma Ativa no Ancoradouro, o sprite e moveset do jogador são substituídos pelos da criatura; troca de controle entre as duas criaturas do squad funciona em campo sem custo; a criatura que perde o controle opera com IA autônoma.
- **Save/Load:** fechar e reabrir o jogo restaura posição, atributos, squad (incluindo Forma Ativa), bestiário e estado da base sem perda de dados.

Critérios adicionais para a entrega futura do catálogo/evolução/elementos:
- **Catálogo:** total de 151 entradas, incluindo evoluções e ambas as alternativas das bifurcações, condicionado ao fechamento da distribuição. Sem identificadores duplicados e com vínculos evolutivos válidos.
- **Evolução:** cada linhagem possui uma forma inicial e duas ou três evoluções; testar os limites imediatamente antes, no nível exigido e depois de cada marco. Cada evolução desbloqueia os poderes especificados, sem duplicar recompensas.
- **Escolha evolutiva:** pelo menos 30 criaturas oferecem exatamente duas alternativas na terceira evolução; o jogador escolhe o destino, que aplica os poderes e elementos definidos. Testar adição de segundo elemento e substituição de elementos, garantindo que nenhuma forma tenha mais de dois elementos ou elementos duplicados.
- **Elementos:** aceitar somente os dez elementos definidos; cada criatura tem um ou dois elementos distintos. Testar resistências/vulnerabilidades para defensores simples e duplos conforme a regra aprovada.
- **Poderes:** ataques, magias e habilidades defensivas respeitam elemento, estágio e desbloqueios do indivíduo, tanto em controle direto quanto na IA.
- **Persistência da progressão:** trocar o controle e salvar/carregar preservam nível, experiência, evolução, alternativa escolhida, composição elemental e poderes desbloqueados.

---

## 14. GLOSSÁRIO RÁPIDO

- **Ancoradouro:** equivalente a bonfire (Souls) + Palbox (Palworld). Checkpoint, hub de base e local de troca de Forma Ativa.
- **Éter:** moeda de progressão perdida/recuperável ao morrer (equivalente a Souls/Runas).
- **Eco:** marcador do local de morte com o Éter perdido.
- **Companion:** criatura capturável, equivalente a "Pal".
- **Forma Ativa:** a criatura que o jogador controla diretamente em campo (o jogador "é" ela).
- **Squad Autônomo:** a segunda criatura do squad, que luta com IA ao lado da Forma Ativa. O jogador pode assumir controle direto dela a qualquer momento, tornando-a a nova Forma Ativa temporária em campo.
- **Work Affinity:** aptidão de trabalho de uma criatura para tarefas de base.

---

## 15. NOTA SOBRE ORIGINALIDADE

Este documento descreve um sistema de jogo **original**, inspirado nos *padrões de mecânica* de Palworld (captura, base-building, automação) e da fórmula soulslike consagrada por Elden Ring/FromSoftware (stamina, i-frames, bonfires, morte com recuperação, chefes em fases). Nenhum nome próprio, personagem, criatura, arte ou texto desses jogos deve ser reproduzido — todos os nomes acima ("Ancoradouro", "Éter", "Eco", "Companion") são propositalmente autorais para uso livre em produção.
