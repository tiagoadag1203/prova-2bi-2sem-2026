# Xeque de Tinta · 02 Documentação Técnica

Unity 6000.4.10f1 (Unity 6), projeto 2D com URP, C#. Pacotes: 2D (Sprite, Tilemap, Animation, Pixel Perfect via URP 2D), Input System, TextMeshPro, Test Framework. Nenhum pacote extra foi instalado para o jogo. Namespace raiz: `XequeDeTinta` (`.Battle`, `.Board`, `.Data`, `.Core`, `.UI`, `.Bot`, `.EditorTools`).

## 1. Arquitetura

Princípio: **lógica separada da apresentação**.

- **Regras do jogo** (deck, energia, dano, escudo, elementos, efeitos, intenções, movimento no tabuleiro, loja, descanso, baú, eventos) são **classes C# puras**, sem `MonoBehaviour`, e por isso testáveis em EditMode.
- **MonoBehaviours** só fazem a ponte com cena, input (Input System), animação, áudio e UI.
- **Dados em ScriptableObjects**: cartas, inimigos, encontros, relíquias, eventos, tabuleiro e a configuração com **todos os números de balanceamento** (`GameConfig`). Conteúdo novo é criado sem mexer no código.
- **Efeitos de carta, de inimigo e de relíquia** são classes pequenas e combináveis, criadas por fábricas (sem `switch` gigante).
- **Aleatoriedade injetável** (`IRng`): `SeededRng` no jogo e `ScriptedRng` nos testes, para testes determinísticos.
- **`GameManager`** persistente (`DontDestroyOnLoad`) guarda o estado da partida (`RunState`).
- Três assemblies de código: `XequeDeTinta.Runtime` (jogo), `XequeDeTinta.Editor` (ferramentas) e os de teste (`Tests.EditMode`, `Tests.PlayMode`).

```
Cenas:   Title → Board ⇄ Battle → End          (+ M0_Test, cena de teste de arte)
Dados:   GameConfig → CardData / EnemyData / EncounterData / RelicData / EventData / BoardLayout
Estado:  GameManager (persistente) → RunState (vida, deck, relíquias, moedas, casas)
Lógica:  BattleSystem, DeckManager, DamageCalc, EnemyBrain, BoardMover, Shop, EventLogic ...
Ponte:   BoardController, BattleController, TitleController, EndController, UI/*
```

## 2. Cenas

| Cena | Arquivo | Função |
|---|---|---|
| Title | `Scenes/Title.unity` | Logo, premissa, **Jogar**, **Sair**, volume; Peão e slimes animados |
| Board | `Scenes/Board.unity` | Tabuleiro de 12 casas, dado, HUD, painéis de loja/descanso/evento/baú/recompensa/deck, caixa de história |
| Battle | `Scenes/Battle.unity` | Duelo de cartas: herói, até 3 inimigos, mão, energia, pilhas, intenções |
| End | `Scenes/End.unity` | Vitória (história e créditos) ou Derrota (Tentar de novo / Título) |
| M0_Test | `Scenes/M0_Test.unity` | Cena de teste com todas as animações (não entra na build) |

As cenas são **montadas por scripts de Editor** (`BoardSceneBuilder`, `BattleSceneBuilder`, `TitleSceneBuilder`, `EndSceneBuilder`, `M0SceneBuilder`, menu `Xeque`), o que torna a reconstrução reproduzível. A interface é criada por código (`UIFactory`) sobre um canvas de referência de 640x360.

## 3. Fluxo do jogo

```
Título ──Jogar──► Tabuleiro (caixa de história na 1ª vez)
                     │ rola o dado, anda, escolhe caminho
                     ▼
        casa onde parou ──► Batalha ──vence──► Recompensa (moedas + 1 de 3 cartas) ─► Tabuleiro
                       ├──► Loja / Descanso / Evento / Baú (painéis)                       │
                       └──► Chefe ──vence──► Vitória ──► Título                              │
        vida = 0 em qualquer batalha ──► Derrota ──► Tentar de novo (Tabuleiro) ou Título
```

`GameManager.StartBattle(tile)` guarda a casa pendente, monta o `BattleSetup` (vida, deck, relíquias, encontro, semente) e carrega `Battle`. Ao fim, `BattleLauncher.OnFinished(result)` devolve a vida; vitória leva à recompensa no `Board`; derrota ou vitória no Chefe chamam `EndRun`. Trocas de cena usam `ScreenFader` (fade preto).

## 4. Máquina de estados da batalha

`BattleSystem` (C# puro) tem `BattlePhase { Start, PlayerTurn, EnemyTurn, Victory, Defeat }`.

```
Start ──Begin()──► PlayerTurn ──EndPlayerTurn()──► EnemyTurn ──(todos agiram)──► PlayerTurn
                      │                               │
                      └── inimigos 0 ──► Victory      └── herói 0 ──► Defeat
```

- **Início do turno do jogador:** escudo do herói zera, Queimadura do herói (se houver), ganchos de relíquia (`IBattleHook.OnPlayerTurnStart`), energia = 3 (+1 no primeiro turno com a Ampulheta), compra 5 cartas (reembaralha o descarte quando o baralho acaba).
- **`PlayCard(handIndex, target)`** gasta a energia e tira a carta da mão; os efeitos só valem em `CardPlay.Resolve()`, chamado pela apresentação no **Animation Event de impacto** (dano sincronizado com a animação).
- **Turno dos inimigos:** `NextEnemyAction()` devolve cada `EnemyPlay` da esquerda para a direita; escudo do inimigo zera, Queimadura e atordoamento são resolvidos (`PreEvents`) e `Resolve()` aplica a ação; depois `EnemyBrain.ChooseIntent` escolhe a próxima.
- **Eventos de batalha (`BattleEvent`)**: a lógica devolve eventos (dano, escudo, cura, status, morte, invocação) que `BattleController` transforma em animação, som e números flutuantes.

## 5. ScriptableObjects (`Scripts/Data`, assets em `Data/`)

| Tipo | Campos principais | Assets |
|---|---|---|
| `CardData` | id, nome, custo, elemento, alvo (`Self`/`SingleEnemy`/`AllEnemies`), lista de `EffectSpec` (tipo, valor, repetições), efeitos melhorados, esgotar, arte | 12 |
| `EnemyData` | nome, elemento, vida, `EnemyBehaviorKind`, ações (`EnemyAction`), animator, escala, `flipX`, invocação, fúria | 5 |
| `EncounterData` | lista de inimigos, `isBoss` | 6 |
| `RelicData` | `RelicKind`, valor, ícone | 5 |
| `EventData` | título, texto, duas `EventChoice` com `EventOutcome` | 3 |
| `BoardLayout` | casas (`BoardTile`: id, tipo, posição, ligações), casa inicial | 1 |
| `GameConfig` | vida do herói, deck inicial, energia, mão, multiplicadores, dado, preços, recompensas, pool de cartas, conteúdo | 1 |

Os assets são criados e atualizados de forma **idempotente** pelo menu `Xeque > M1 > Criar assets de dados` (`DataAssetBuilder`).

## 6. Principais scripts

### Core
| Script | O que faz |
|---|---|
| `GameManager` | Singleton persistente: config, `RunState`, trocas de cena, `StartBattle`, `EndRun` |
| `RunState` | Vida, deck, relíquias, moedas, casas visitadas, filas de encontros e eventos sorteadas pela semente, recompensa pendente |
| `Rng` | `IRng`, `SeededRng`, `ScriptedRng` |
| `TileLogic` | `RestLogic`, `ChestLogic`, `Shop`, `EventLogic`: regras de descanso, baú, loja e eventos |
| `AudioManager` / `AudioLibrary` | Pool de 8 `AudioSource` para efeitos; música em loop por cena; biblioteca de clipes |
| `ScreenFader` | Fade preto entre cenas |

### Battle
| Script | O que faz |
|---|---|
| `BattleSystem` | Máquina de estados, energia, jogadas, turno dos inimigos, Queimadura, fúria do chefe |
| `DeckManager` | Compra, descarte, reembaralho, esgotar |
| `DamageCalc`, `ElementRules`, `BattleRules` | Fórmula de dano e ciclo de elementos |
| `Effects`, `EnemyEffects` | `DealDamage`, `GainShield`, `ApplyBurn`, `ApplyWeak`, `Heal`, `Stun`; `EnemyAttack`, `EnemyShield`, `EnemyBurnHero`, `EnemyHealAlly`, `EnemyGainStrength`, `EnemySummon` e suas fábricas |
| `EnemyBrain` | Escolhe a intenção do próximo turno por `EnemyBehaviorKind` |
| `RelicHooks` | Relíquias como ganchos `IBattleHook` |
| `Combatant` (`Hero`, `Enemy`) | Vida, escudo, status |
| `BattleController`, `ActorView`, `CardView` | Apresentação: atores animados, mão de cartas, alvo por clique, intenções, números flutuantes, tremor, flash |

### Board
| Script | O que faz |
|---|---|
| `BoardGraph`, `BoardMover`, `BoardProgress` | Grafo de casas, movimento passo a passo com bifurcação e parada no Chefe, casas visitadas |
| `BoardController` | Casas, Peão que pula, dado (giro e número grande), HUD, caixa de história, resolução das casas |

### UI e Bot
| Script | O que faz |
|---|---|
| `UIFactory`, `CanvasMatch` | Canvas, textos TMP, painéis e botões; zoom inteiro igual ao da Pixel Perfect Camera |
| `Panels`, `TilePanels`, `RelicBar`, `PauseMenu`, `TitleController`, `EndController` | Painéis, relíquias com tooltip, pausa, título e tela final |
| `BotPolicy`, `RunSimulator` | Robô que joga partidas inteiras em lógica pura (playtest automático e balanceamento) |

### Editor (`Scripts/Editor`)
`ArtPipeline` (regras de importação de pixel art e geração de clipes e Animators), `DataAssetBuilder`, construtores de cena, `DiceFaceBuilder`, `UIArtBuilder`, `AudioLibraryBuilder`, `FontSetup`, `TestFailureLogger`, `BuildTools` (builds de Mac e Windows).

## 7. Regras

### 7.1 Dano
`dano = (base + Força) × 1,5 se eficaz × 0,75 se o atacante está Fraco`, arredondado para baixo; o **escudo** absorve primeiro e o restante tira vida. Valores (`GameConfig`): multiplicador eficaz 1,5, fraqueza 0,75, Queimadura 2.

### 7.2 Elementos
`ElementRules.Beats`: Fogo vence Planta, Planta vence Água, Água vence Fogo; Neutro não tem vantagem nem fraqueza.

### 7.3 Status
Escudo zera no início do turno do dono. **Queimadura**: 2 de vida no início do turno do dono, ignora escudo, gasta 1 turno; reaplicar soma. **Fraco** gasta 1 turno ao fim da ação do dono. **Força** dura a batalha. **Atordoado** faz pular a ação, consome o atordoamento e mantém a intenção.

### 7.4 IA dos inimigos (`EnemyBrain`, `EnemyBehaviorKind`)
| Comportamento | Regra | Quem usa |
|---|---|---|
| `Simple` | Repete a ação 0 | Slime Azul, Slime Mirim |
| `RandomNoRepeatDefend` | Sorteia a ação; escudo nunca duas vezes seguidas | Slime Verde |
| `EveryNthSpecial` | Ação 0; a ação 1 a cada N turnos | Slime Vermelho (N = 3) |
| `Cycle` | Percorre as ações em ordem | Rei Slime: Esmagar → Dividir → Coroa Pesada |

Rei Slime: invoca Mirins até 2 vivos (`summonMaxAlive`); abaixo de 50% de vida (`enrageBelowHpFraction`, estritamente menor) ganha 2 de Força uma vez. A intenção do próximo turno é decidida ao fim do turno atual e mostrada com o dano calculado (`PreviewDamage`).

### 7.5 Movimento no tabuleiro (`BoardMover`)
`Begin(steps)` define os passos; `Advance()` devolve `Moved`, `ChoiceNeeded` (bifurcação: o jogador chama `Choose(id)`) ou `Stopped`. Entrar na casa do Chefe zera os passos restantes. Só a casa de parada é resolvida; `BoardProgress` marca as visitadas.

### 7.6 Outras regras (decisões do projeto)
Descanso cura 30% da vida máxima ou melhora 1 carta; Baú dá 30 moedas e 1 relíquia (se não faltar nenhuma, vira 30 moedas); Loja e eventos conforme `GameConfig`; Dado Viciado rola de novo 1 vez por rolagem; perda de vida em evento deixa no mínimo 1.

## 8. Animação e Animation Events

Cada animação tem quadros `nome_anim_N.png` importados como sprites; `ArtPipeline` gera os clipes (8 fps) e os Animator Controllers. O clipe `attack` tem um **Animation Event `OnImpact`** no quadro 4 (índice 3) e o `death` tem `OnDeathEnd` no último quadro. `ActorView` recebe `OnImpact`: aplica o efeito (`Resolve()`) e dispara a animação `hit` do alvo, sincronizando o dano com o golpe.

## 9. Sistema de áudio

`AudioManager` (singleton persistente) tem 1 `AudioSource` de música em loop (a faixa muda por cena: `Battle` toca a de batalha; `Title`, `Board` e `End` tocam a do tabuleiro) e um **pool de 8 `AudioSource`** para efeitos (`PlayOneShot`). `AudioLibrary` (`Resources/AudioLibrary.asset`) reúne os 15 clipes de efeito e as 2 músicas; `Sfx` é o enum de efeitos. Volume: música 0,45, efeitos 0,85 e **volume geral** em `AudioListener.volume` (controle deslizante no título). Detalhes e origem dos sons no documento 05.

## 10. Testes automatizados

`Tests/EditMode` (lógica) e `Tests/PlayMode` (fluxos de cena) com NUnit e Unity Test Framework. Resultados e cobertura no documento 06: **176 testes de EditMode e 37 de PlayMode** passando na última execução (mais 1 teste de PlayMode marcado `[Explicit]`).

## 11. Como abrir, rodar e gerar build

**Abrir:** Unity Hub → *Add project from disk* → pasta `Projeto_Unity` (ou o repositório) → abrir com **Unity 6000.4.10f1** (a primeira abertura reimporta a pasta `Library/`, alguns minutos). Cena inicial: `Assets/_Project/Scenes/Title.unity`.

**Rodar:** abrir `Title.unity` e apertar Play. Para jogar direto no tabuleiro, abra `Board.unity` (o `GameManager` cria uma partida sozinho).

**Testes:** *Window > General > Test Runner* (EditMode e PlayMode) ou, em linha de comando:
```
Unity -batchmode -projectPath <pasta> -runTests -testPlatform EditMode -testResults edit.xml
Unity -batchmode -projectPath <pasta> -runTests -testPlatform PlayMode -testResults play.xml
```

**Build:** menu `Xeque > Build > Mac` ou `Xeque > Build > Windows` (`BuildTools`), que gera `Build/Mac/XequeDeTinta.app` e `Build/Windows/XequeDeTinta.exe` com as cenas do Build Settings (Title, Board, Battle, End). Requer os módulos *Mac Build Support (Mono)* e *Windows Build Support (Mono)* do Editor.

**Regenerar conteúdo:** `Xeque > M1 > Criar assets de dados`, `Xeque > Gerar animações`, menus de construção das cenas, e `python3 Tools/gen_audio.py` para os sons.

## 12. Estrutura de pastas

```
Assets/_Project/
  Art/ (Characters, Enemies, Bosses, Board, Cards, Relics, UI, Backgrounds; _Reference e _Rejected fora do jogo)
  Animations/  Audio/ (Sfx, Music, LICENSES.md)  Data/  Resources/
  Scenes/  Scripts/ (Core, Board, Battle, Data, UI, Bot, Editor)  Tests/ (EditMode, PlayMode)
Tools/ (gen_audio.py, export_entrega_sprites.py, build_docs.py)   Docs/ (GDD v0.1, Entrega)
GDD.md  ASSETS.md  PROGRESS.md  CLAUDE.md  README.md
```
