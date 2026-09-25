# PalSoul

ARPG 2D de captura e transformação em criaturas, com combate baseado em stamina e esquiva. Projeto em pré-alpha.

A especificação está no [GDD](GDD_Spec_Projeto_Palsoul.md). A entrega atual consolida os **MVPs 1–7**; não inclui morte/Eco, chefe, produção ou salvamento.

## Visão do jogo completo — atualização de 25/09/2026

- **151 entradas no bestiário**, contando formas iniciais e evoluídas.
- **Duas ou três evoluções por linhagem**, além da forma inicial, cada uma desbloqueando novos poderes. Os níveis variam por criatura; 23, 40 e 56 são exemplos, não valores universais.
- **Pelo menos 30 criaturas com duas opções na terceira evolução**, escolhidas pelo jogador. As alternativas podem trocar elementos ou acrescentar um segundo elemento e contam separadamente nas 151 entradas.
- **Dez elementos:** Água, Fogo, Eletricidade, Terra, Fantasma, Escuridão, Luz, Grama, Gelo e Dragão.
- Cada criatura terá um ou dois elementos distintos, diferenciando ataques, defesas e magias. Matriz de vantagens e cálculo da defesa dupla ainda serão definidos.
- **Distribuição de catálogo pendente:** 30 linhagens independentes com essa bifurcação já somam 150 entradas. Definir como acomodar a entrada restante e as linhagens de duas evoluções antes de produzir o catálogo; não presumir exceções ou formas compartilhadas.

Esses requisitos estão documentados nas seções 5.3, 5.4, 9.5 e 12.3 do GDD e ainda não estão implementados.

## Abrir e jogar

1. Ative sua licença no Unity Hub e abra esta pasta com **Unity 6000.6.0f1**.
2. Aguarde a importação dos pacotes e a compilação.
3. Na primeira abertura, o editor prepara a cena e os assets do protótipo. Se a geração não iniciar, use **Palsoul → Create Missing Prototype Assets**.
4. Abra `Assets/_Project/Scenes/_Boot.unity` e pressione Play.

O gerador cria uma região pequena, três criaturas selvagens de duas espécies, jogador, companheiro, Ancoradouro, HUD, câmera pixel-perfect, Input Actions e ScriptableObjects editáveis em `Assets/_Project/Prototype/`. Uma cena existente não é sobrescrita.

**Ponto de retomada — 25/09/2026:** cena `_Boot` e assets gerados no Unity 6000.6.0f1, na branch `feat/mvp-7-consolidacao`. A licença funcionou nesta sessão e a suíte automatizada passou. Consulte `SETUP_MVP1.md` para cobertura e limites. Avaliação visual da câmera, controle físico e sensação do combate permanece manual. O próximo item funcional é o MVP 8, após aceite desta consolidação.

Veja o [roteiro completo de validação](SETUP_MVP1.md).

## Controles

| Ação | Teclado | Controle |
|---|---|---|
| Movimento | WASD / setas | Analógico esquerdo |
| Ataque leve / pesado | J / K | RB / RT |
| Esquiva | Espaço | B |
| Captura | Q | Y |
| Alternar controle do squad | Tab | LB |
| Abrir Ancoradouro | E | A |
| Fechar menu | Esc | B |

O menu de protótipo usa mouse para selecionar criaturas e upgrades. O squad tem duas criaturas. A captura preserva o HP restante; descansar cura as criaturas capturadas. Trocar o controle preserva a vida e a posição de cada indivíduo, sem custo de stamina. Ao morrer, reinicie o Play; respawn e Eco pertencem ao MVP 8.

## Status por etapa

| MVP | Entrega | Situação |
|---|---|---|
| 1 | Movimento e câmera | Movimento axial/diagonal testado; avaliação visual da câmera pendente |
| 2 | Stamina e dodge | Custo da esquiva e janela de i-frames testados; avaliar sensação manualmente |
| 3 | Ataques e hitboxes | Combo, dano e bloqueios verificados por testes |
| 4 | Inimigo com estados | Duas espécies na cena; fúria e reset verificados |
| 5 | Captura | Fórmula e captura verificadas em testes Unity |
| 6 | Transformação e squad | HP, posição, espécie e troca verificados |
| 7 | Ancoradouro | Equipamento do squad, descanso e Vigor verificados |
| 8 | Morte, Éter e Eco | Pendente |
| 9 | Chefe de duas fases | Pendente |
| 10 | Produção de base | Pendente |
| 11 | Save/Load | Pendente |

Parry, matriz elemental, habilidade especial e consumível de cura não foram implementados nesta consolidação; a posição deles no cronograma precisa ser definida antes de considerar o combate completo. Os sprites e telegraphs desta cena são representações de teste.

## Verificações

- `dotnet run --project Tools/RuleChecks/RuleChecks.csproj`: executa 15 casos sobre o código real de probabilidade, sem depender de licença Unity. Requer .NET SDK 10.
- `python Tools/verify_compile.py`: compila runtime, editor e testes separadamente contra as bibliotecas do Unity instalado. Não executa o motor.
- `powershell -ExecutionPolicy Bypass -File Tools/run_unity_checks.ps1`: gera a cena e executa testes do Unity. Requer licença ativa. Resultados em `Logs/test-results.xml`.
- No Editor: **Window → General → Test Runner → EditMode → Run All**. A suíte de integração entra e sai do Play Mode automaticamente.

## Organização e fluxo

- `Assets/_Project/Scripts`: gameplay, dados e UI.
- `Assets/_Project/Editor`: gerador do protótipo.
- `Assets/_Project/Tests`: probabilidades e regressões de integração.
- `ProjectSettings` e `Packages/manifest.json`: versão e configuração do projeto.
- [CHANGELOG](CHANGELOG.md): histórico, distinguindo implementação de validação.

Trabalhar em uma branch por item; seguir a ordem do GDD. Merge via PR pelo usuário. Licença do código: a definir. Nomes, arte e conteúdo final devem ser originais.
