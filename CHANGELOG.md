# CHANGELOG — PalSoul

Formato: `[MVP-X] Descrição curta — data`

---

## MVP em andamento

### [MVP-1] Movimento do player top-down + câmera pixel-perfect — 12/09/2026
- Criada estrutura completa de pastas em `Assets/_Project/` (conforme GDD seção 9.3).
- `PlayerMovementSO` (ScriptableObject): parâmetros `moveSpeed` e `deceleration` configuráveis via Inspector.
- `IState` (interface) + `PlayerState` (enum): base da State Machine extensível para todos os sistemas de combate.
- `PlayerController`: State Machine com estados `Idle` e `Moving`; input via Unity Input System (Send Messages); movimento físico em `FixedUpdate` via `Rigidbody2D`.
- `CameraFollow`: segue o player via `LateUpdate` com suavização configurável (`smoothTime`), offset, restrição de bounds e método `SnapToTarget()` para troca de cena.
