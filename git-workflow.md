---
inclusion: always
---

# Git Workflow — PalSoul

## Regra obrigatória: branch por item do MVP

Antes de implementar qualquer item do MVP, SEMPRE:

1. Criar uma branch de desenvolvimento com o padrão:
   ```
   feat/mvp-<numero>-<descricao-curta>
   ```
   Exemplo: `feat/mvp-3-attack-hitbox`

2. Commitar o trabalho nessa branch (nunca direto no `main`).

3. Push da branch com tracking:
   ```
   git push -u origin feat/mvp-<numero>-<descricao-curta>
   ```

4. O merge para `main` é feito pelo usuário via Pull Request no GitHub.

## Branches já no main (histórico)
- `feat/mvp-1` e `feat/mvp-2` foram commitados direto no main antes desta regra ser definida.
- A partir do MVP-3, seguir o fluxo acima sem exceções.

## Nomenclatura de commits
Seguir Conventional Commits:
- `feat(mvp-X): descrição` — nova funcionalidade
- `fix(mvp-X): descrição` — correção de bug
- `refactor(mvp-X): descrição` — refatoração sem mudança de comportamento
