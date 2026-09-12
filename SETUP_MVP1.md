# Setup MVP 1 — Movimento do Player + Câmera Pixel-Perfect

Siga os passos abaixo no Unity Editor para montar e testar o Item 1 do MVP.

---

## Pré-requisitos (packages necessários)

Abra **Window → Package Manager** e confirme que estão instalados:

| Package | Versão mínima |
|---|---|
| **Input System** | 1.7+ |
| **2D Pixel Perfect** | 5.0+ |
| **Universal RP** (URP) | incluído no template URP 2D |

> Se o Input System não estiver instalado, instale pelo Package Manager e **reinicie o Editor** quando solicitado. Na janela que pergunta sobre o backend de input, escolha **"Both"** ou **"New Input System"**.

---

## 1. Criar o projeto Unity (se ainda não criado)

1. Unity Hub → **New Project**
2. Template: **2D (URP)**
3. Nome: `PalSoul`
4. Copie (ou clone) a pasta `Assets/_Project` gerada pelos scripts para dentro do projeto.

---

## 2. Criar o ScriptableObject de Movimento

1. No **Project window**, navegue até `Assets/_Project/ScriptableObjects/Player/`
2. Clique com botão direito → **Create → Palsoul → Player Movement Data**
3. Nomeie como `PlayerMovementData`
4. Valores recomendados para teste:
   - **Move Speed:** `5`
   - **Deceleration:** `0.85`

---

## 3. Criar o Input Actions Asset

1. No **Project window**, navegue até `Assets/_Project/Settings/`
2. Clique com botão direito → **Create → Input Actions**
3. Nomeie como `InputActions`
4. Dê duplo clique para abrir o editor de Input Actions
5. Adicione um **Action Map** chamado `Player`
6. Dentro de `Player`, adicione uma **Action** chamada `Move`:
   - Action Type: **Value**
   - Control Type: **Vector 2**
7. Clique em `+` em Bindings e adicione:
   - **2D Vector Composite** (para teclado WASD/setas)
     - Up: `W` / `Arrow Up`
     - Down: `S` / `Arrow Down`
     - Left: `A` / `Arrow Left`
     - Right: `D` / `Arrow Right`
   - **Left Stick** (gamepad)
8. Clique em **Save Asset** (canto superior esquerdo do editor)

---

## 4. Criar o Animator Controller do Player

1. Em `Assets/_Project/Art/Characters/`, clique com botão direito → **Create → Animator Controller**
2. Nomeie como `PlayerAnimator`
3. Abra o Animator (duplo clique)
4. Crie dois estados: **Idle** e **Walk** (podem ser clips vazios por agora)
5. Adicione os parâmetros:
   - `IsMoving` — **Bool**
   - `MoveX` — **Float**
   - `MoveY` — **Float**
6. Adicione transição **Idle → Walk** com condição `IsMoving = true` (Has Exit Time: off)
7. Adicione transição **Walk → Idle** com condição `IsMoving = false` (Has Exit Time: off)

---

## 5. Montar o Prefab do Player

1. Crie um **GameObject vazio** na cena e nomeie `Player`
2. Adicione os componentes abaixo:

| Componente | Configuração |
|---|---|
| **Sprite Renderer** | Qualquer sprite de placeholder (ex.: quadrado branco) |
| **Rigidbody2D** | Body Type: Dynamic · Gravity Scale: **0** · Collision Detection: Continuous · Freeze Rotation Z: ✓ |
| **CapsuleCollider2D** | Ajuste ao tamanho do sprite |
| **Animator** | Controller: `PlayerAnimator` |
| **Player Input** | Actions: `InputActions` · Behavior: **Send Messages** |
| **PlayerController** | Movement Data: `PlayerMovementData` (arraste o SO) |

3. Salve como prefab em `Assets/_Project/Prefabs/Player/Player.prefab`

---

## 6. Configurar a câmera pixel-perfect

1. Selecione a **Main Camera** na cena
2. Adicione o componente **Pixel Perfect Camera** (do package 2D Pixel Perfect):

| Campo | Valor |
|---|---|
| Assets Per Unit | `16` |
| Reference Resolution X | `320` |
| Reference Resolution Y | `180` |
| Crop Frame | Both |
| Grid Snapping | **Upscale Render Texture** |

3. Adicione o script **CameraFollow**:
   - **Target:** arraste o Transform do `Player`
   - **Smooth Time:** `0.08`
   - **Offset:** `(0, 0)`

---

## 7. Criar a cena de teste

1. **File → New Scene** (template: Basic 2D)
2. Salve em `Assets/_Project/Scenes/Regions/Region_01.unity`
3. Adicione o prefab `Player` na cena
4. Crie um **Tilemap** simples (ou alguns sprites de chão) para ter referência visual de movimento
5. Confirme que a Main Camera tem `CameraFollow` e `PixelPerfectCamera` configurados

---

## 8. Testar no Editor

1. Pressione **Play**
2. Use **WASD** ou **setas** para mover o player

### O que observar:
- ✅ Player se move nas 8 direções (diagonal inclusa)
- ✅ Ao soltar as teclas, o player desacelera suavemente (não para abruptamente)
- ✅ A câmera segue o player sem trepidação
- ✅ Sprites ficam em pixel-grid (sem sub-pixel blurring)
- ✅ No **Animator** (Window → Animation → Animator), o parâmetro `IsMoving` muda entre `true`/`false` ao mover/parar
- ✅ No **Inspector** do player enquanto em play, o gizmo ciano na Scene view aponta a direção de movimento

### Verificação de console:
- Se aparecer o erro `[PlayerController] PlayerMovementSO não atribuído!`, volte ao passo 5 e arraste o SO no Inspector.

---

## Critérios de Aceitação (seção 13 do GDD) — Item 1

| Critério | Como verificar | Status |
|---|---|---|
| Player se move nas 8 direções | WASD/diagonal | A verificar em play |
| Velocidade configurável via Inspector (não hardcoded) | Alterar `moveSpeed` no SO e observar diferença | A verificar em play |
| Câmera pixel-perfect sem shimmer | Mover pela cena e observar grid dos tiles | A verificar em play |
| State Machine transiciona Idle ↔ Moving | Parâmetro `IsMoving` no Animator | A verificar em play |
