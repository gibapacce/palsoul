# Palsoul

**ARPG 2D pixel art em Unity — captura e automação de criaturas (estilo Palworld) fundidas com combate soulslike punitivo (stamina, i-frames, parry, bonfires, chefes em fases — estilo Elden Ring).**

> ⚠️ Projeto em desenvolvimento inicial (pré-alpha). Nada aqui é final.

---

## O que é

Você explora um mundo hostil em pixel art top-down, captura criaturas selvagens para lutar ao seu lado e trabalhar na sua base, e enfrenta chefes de arena que exigem leitura de padrão, gerenciamento de stamina e paciência — não button-mash.

A fusão central:
- **Loop Palworld:** captura por esfera, aptidões de trabalho, automação de base, elementos.
- **Loop Elden Ring:** stamina em toda ação, dodge roll com i-frames, parry/guard-counter, checkpoints tipo bonfire que curam mas resetam o mundo, morte com perda/recuperação de recurso, chefes em múltiplas fases.

Especificação completa de design e arquitetura técnica: [`GDD_Spec_Projeto_Palsoul.md`](./GDD_Spec_Projeto_Palsoul.md).

---

## Stack

- Unity 2022 LTS+ · URP 2D
- C# · Input System (novo)
- ScriptableObjects para todo conteúdo de design (criaturas, ataques, itens, edifícios)
- Save/Load em JSON local

---

## Status do MVP

Ordem de implementação definida na seção 12.1 do GDD. Progresso atual:

- [ ] Movimento do player top-down + câmera pixel-perfect
- [ ] Stamina + Dodge Roll com i-frames
- [ ] Ataque leve/pesado + hitbox/hurtbox
- [ ] Inimigo básico com State Machine (Idle/Patrol/Chase/Attack/Stagger/Death)
- [ ] Sistema de captura funcional (1 criatura de teste)
- [ ] Ancoradouro funcional (cura, reset de mundo, gasto de atributo)
- [ ] Sistema de morte + recurso perdido/recuperável
- [ ] 1 chefe de arena com 2 fases + runback
- [ ] Base mínima: 1 edifício + 1 criatura alocada + produção
- [ ] Save/Load cobrindo tudo acima

Changelog detalhado em [`CHANGELOG.md`](./CHANGELOG.md).

---

## Estrutura do projeto

```
Assets/_Project/
  Art/          # sprites, tilesets, UI, VFX
  Audio/
  Prefabs/
  ScriptableObjects/  # criaturas, ataques, elementos, itens, edifícios
  Scenes/
  Scripts/
    Core/         # regras puras: stamina, HP, matriz elemental, fórmula de captura
    Combat/       # state machines, hitboxes, parry
    Creatures/    # controller, IA, alocação de trabalho
    Base/         # construção, produção, checkpoint
    Progression/  # atributos, moeda de progressão, marcador de morte
    Save/
    UI/
```

Detalhes completos de arquitetura, convenções de código e critérios de aceitação por sistema: ver seções 9 e 13 do GDD.

---

## Rodando localmente

1. Clone o repositório.
2. Abra a pasta com Unity Hub (versão indicada no `ProjectSettings`).
3. Abra a cena `_Boot` em `Assets/_Project/Scenes/`.
4. Play.

---

## Contribuindo / fluxo de desenvolvimento

O desenvolvimento segue estritamente a ordem do MVP (seção 12.1 do GDD), um item por vez, com critérios de aceitação definidos na seção 13. Não implemente itens fora de ordem ou mecânicas não descritas no GDD sem discutir antes.

---

## Licença

_A definir._

## Nota de originalidade

Palsoul é um projeto original inspirado em *padrões de mecânica* (captura de criaturas, automação, combate soulslike), não uma cópia de nenhum jogo específico. Nenhum nome, personagem, arte ou texto de Palworld, Elden Ring ou qualquer outro título é reproduzido neste projeto.
