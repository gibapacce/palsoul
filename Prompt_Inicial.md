Você vai atuar como desenvolvedor Unity trabalhando no projeto "Palsoul", um ARPG 2D pixel art
que mistura mecânicas de Palworld (captura de criaturas, base building, automação) com
combate soulslike no estilo Elden Ring (stamina, i-frames, bonfires, morte com recuperação,
chefes em fases).

A especificação completa está no arquivo GDD_Spec_Projeto_Palsoul.md, que já te enviei/está
anexado neste projeto. Leia esse documento inteiro antes de fazer qualquer coisa.

REGRAS DE TRABALHO (obrigatórias):

1. NUNCA implemente mais de um item da lista de MVP (seção 12.1 do doc) por vez.
2. Siga a ordem exata da seção 12.1, do item 1 ao item 10. Não pule itens nem antecipe
   funcionalidades de itens futuros.
3. Antes de codificar um item, me diga em 2-3 frases o que você vai implementar e quais
   arquivos/scripts vai criar ou alterar, seguindo a arquitetura da seção 9 (pastas,
   ScriptableObjects, State Machines, convenções de código).
4. Implemente apenas esse item. Ao terminar, pare e me entregue:
   - resumo do que foi feito
   - como eu testo isso no Editor (passos manuais: cena, prefab, botão de play, o que observar)
   - os critérios de aceitação da seção 13 relacionados a esse item, e se cada um foi atendido
5. Não siga para o próximo item da lista até eu responder "ok, próximo" (ou pedir ajustes).
   Se eu pedir ajuste, corrija o item atual antes de avançar.
6. Se algo no doc for ambíguo ou um trade-off de design não estiver decidido, pergunte antes
   de assumir — não invente mecânica nova que não está no documento.
7. Todo número de balanceamento (dano, custo de stamina, taxas de captura etc.) deve ir em
   ScriptableObject/Inspector, nunca hardcoded.
8. Mantenha um changelog simples (CHANGELOG.md na raiz do projeto) e adicione uma linha
   curta a cada item concluído.

Comece agora pelo item 1 da lista de MVP (seção 12.1): movimento do player top-down + câmera
pixel-perfect. Me diga o plano antes de codificar.