# Setup e validação — consolidação dos MVPs 1–7

Este roteiro substitui a montagem manual antiga, que cobria somente movimento.

## Preparação

1. Unity Hub: ativar licença e abrir a raiz deste repositório com **6000.6.0f1**.
2. Aguardar importação e compilação. Não copiar apenas `Assets`: `Packages` e `ProjectSettings` fazem parte do projeto.
3. O gerador da primeira abertura cria os assets ausentes. Alternativa: **Palsoul → Create Missing Prototype Assets**.
4. Abrir `Assets/_Project/Scenes/_Boot.unity` e Play.

O gerador não sobrescreve `_Boot` existente. Ele grava prefabs, sprites de teste, animações Idle/Walk, controles, parâmetros e pipeline URP 2D em `Assets/_Project/Prototype`. Versionar os assets gerados, seus `.meta` e `Packages/packages-lock.json` depois de validar.

## Roteiro manual

1. Mover com WASD/setas nas oito direções. Confirmar mesma velocidade axial e diagonal e ausência de tremulação na câmera.
2. Pressionar Espaço para esquivar. Observar o indicador ciano de invencibilidade, consumo de stamina e intervalo até regenerar.
3. Encontrar uma criatura à direita. Atacar com J/K, acompanhar HP e telegraph amarelo. Usar J novamente ao fim do primeiro golpe para verificar combo e consumo por golpe.
4. Enfraquecer a criatura e usar Q a até 2,5 unidades. Observar esfera, consumo do inventário e mensagem de sucesso/falha. Falha deixa a criatura vermelha e aumenta seu dano temporariamente.
5. Capturar duas criaturas. Retornar ao Ancoradouro à esquerda, E para abrir. Usar o mouse para selecionar **Usar forma** em uma e **Companheiro** na outra.
6. Fechar com Esc. Conferir sprite e ataques de cada espécie. Pressionar Tab: controle muda para a posição do companheiro e a criatura anterior passa a agir autonomamente. A vida de cada uma deve ser preservada, sem gasto de stamina.
7. Receber dano, voltar ao Ancoradouro e comprar Vigor. O protótipo inicia com **100 Éter de teste**, independente do futuro sistema de loot. Conferir o bônus de 10 HP nas duas formas; mudar de forma não pode apagar o upgrade.
8. Descansar: curar jogador e criaturas capturadas, repor as 12 esferas iniciais e restaurar os três inimigos selvagens. Capturas e atributos permanecem.
9. Abrir o menu: movimento, ataques, captura e troca de controle ficam bloqueados. Fechar: controles voltam a funcionar.
10. Morrer: movimento, ataques e troca ficam bloqueados. Reiniciar Play para tentar novamente; respawn/Eco ficam para o MVP 8.

## Testes automáticos

Executar `Tools/run_unity_checks.ps1`, ou usar **Window → General → Test Runner → EditMode → Run All**. Os testes de integração carregam `_Boot`, entram no Play Mode e voltam ao Editor ao terminar.

Cobertura executada: resultados fixos de captura, tiers, status e raridade; consumo de esfera; isolamento do bestiário por sessão; vida/posição/moveset do squad; custo de stamina na troca; persistência de Vigor entre formas; rejeição de upgrade não implementado; descanso/reset; bloqueio de captura/troca ao morrer ou abrir menu.

A aparência, sensação do combate, movimentação por controle e ausência de shimmer exigem também o roteiro manual.

## Evidência desta sessão — 25/09/2026

- 15 verificações de `CaptureRules.cs` executadas com .NET: passaram.
- Compilação externa das assemblies de runtime, editor e testes: ver `Logs/Compile/result.txt`.
- Cena `_Boot`, prefabs, parâmetros e controles gerados no Unity 6000.6.0f1, com referências verificadas.
- **21/21 testes Unity aprovados**: 12 casos de captura e 9 testes de integração entrando em Play Mode. Relatório em `Logs/test-results.xml` (pasta local ignorada pelo Git), execução de 25/09/2026 às 09:58 BRT.
- Teclado simulado confirma movimento axial/diagonal com mesma velocidade, custo de stamina da esquiva e janela de invencibilidade. O teste configura temporariamente o foco do Input System para execução sem janela e restaura as opções ao terminar.
- Todos os assets possuem `.meta`; nenhum GUID duplicado encontrado.
- Cobertura adicional: combo com cobrança por golpe, fúria sem dano duplicado por colliders, invencibilidade e rejeição de gasto inválido de stamina.
- Corrigido o gerador para criar a cena antes dos ScriptableObjects, evitando descarregar referências durante a geração.
- Corrigida a preparação dos testes: avanço explícito do tempo do jogo e sincronização de teleporte com física.
- O erro de licença de 21/09 foi superado nesta sessão.

Avaliação visual, câmera sem shimmer, controle físico e sensação do combate continuam pendentes do roteiro manual. Testes em batch sem gráficos não comprovam esses critérios.

As novas regras de 151 entradas, linhagens com duas ou três evoluções, duas alternativas na terceira evolução de pelo menos 30 criaturas e dez elementos (incluindo tipos duplos) estão no GDD. Ainda não são comportamentos implementados nem fazem parte deste roteiro de validação dos MVPs 1–7.

A aprovação automatizada não substitui o aceite visual do protótipo. Parry, matriz elemental, habilidade especial, consumível de cura, arte final e salvamento não fazem parte desta entrega.
