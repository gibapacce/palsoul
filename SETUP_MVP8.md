# MVP 8 — Morte, Éter e Eco

Abra `Assets/_Project/Scenes/_Boot.unity` no Unity 6000.6.0f1 e pressione Play. Para atualizar uma cópia antiga da cena, salve-a e use **Palsoul → Upgrade Prototype to MVP 8**. A migração acrescenta componentes e referências, preservando a região e os dados existentes.

## Comportamento

- Derrotar inimigos concede `EnemyDataSO.etherDrop` uma única vez. Capturar e resetar o mundo não concedem Éter.
- A morte transfere todo o saldo para um Eco ciano no local da morte. Uma nova morte substitui esse Eco; se o saldo atual for zero, o Eco anterior desaparece sem criar um Eco vazio.
- A tela de morte mostra o valor perdido, a representação humana e um minimapa relativo com o Eco. Clique em **Renascer**, pressione **Enter** ou **Start** no controle.
- O jogador retorna ao último Ancoradouro em cujo raio entrou. Antes da primeira visita, usa o Ancoradouro inicial configurado na cena; cenas sem essa referência usam a posição inicial do jogador.
- O retorno restaura HP e stamina do jogador e HP das duas criaturas equipadas, preservando suas identidades, capturas e Vigor. O companheiro reaparece junto ao jogador. Esferas só são repostas ao descansar.
- Inimigos mortos, feridos ou capturados permanecem como estavam. Apenas descansar reseta o mundo. Descansar não apaga o Eco.
- Aproximar-se do Eco recupera seu valor uma única vez, somando ao saldo atual. O raio é editável em `Prototype/Eco.prefab`; o deslocamento de respawn fica no `AnchorpointController`.
- A morte cancela ataques e esferas em voo e fecha o menu do Ancoradouro. Uma esfera já lançada continua consumida.

## Validação manual

1. Derrote um inimigo com J/K e observe o aumento de Éter. Capture outro e confirme que a captura não concede essa recompensa.
2. Afaste-se do Ancoradouro e deixe o inimigo restante matar você. Confira o saldo zerado, o valor perdido e o Eco no minimapa.
3. Pressione Enter. Confira retorno ao Ancoradouro, HP/stamina cheios e controles funcionando. O inimigo derrotado não deve reaparecer.
4. Retorne ao Eco: ele deve desaparecer e devolver exatamente o valor perdido. Voltar ao mesmo ponto não concede mais Éter.
5. Morra com Éter, renasça e morra novamente antes de recuperá-lo. O primeiro valor deve ser perdido definitivamente. Repita a segunda morte com saldo zero.
6. Equipe duas capturas, compre Vigor e repita o ciclo. Confira formas, HP máximo, companheiro junto ao jogador e troca com Tab.
7. Deixe um Eco distante e descanse no Ancoradouro: inimigos retornam, esferas são repostas e o Eco permanece.

## Verificações automatizadas

`Tools/run_unity_checks.ps1` atualiza a cena e executa a suíte Unity. Os novos testes cobrem saldo fracionário exato, recuperação por proximidade sem duplicação, segunda morte com/sem saldo, revisita de checkpoints, preservação do mundo/squad/upgrades, loot sem duplicação e cancelamento de captura/menu.

Critérios da seção 13 do GDD: **atendidos nos testes automatizados** — perda igual ao saldo carregado e perda definitiva do Eco anterior na segunda morte. Suíte Unity: **27/27 aprovados** (12 casos de captura e 15 de integração); 15 verificações independentes de captura e compilação externa também aprovadas. Aparência, legibilidade e sensação do retorno ainda exigem o roteiro manual. Persistência entre sessões pertence ao MVP 11.
