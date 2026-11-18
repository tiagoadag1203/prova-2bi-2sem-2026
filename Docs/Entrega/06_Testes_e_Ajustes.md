# Xeque de Tinta · 06 Testes e Ajustes

## 1. Testes automatizados

Executados com o Unity Test Framework (NUnit) em **18/11/2026**, Unity 6000.4.10f1, em linha de comando (`-batchmode -runTests`).

| Modo | Testes | Resultado |
|---|---|---|
| **EditMode** (lógica pura) | **176** | **176 passaram**, 0 falhas (3,7 s) |
| **PlayMode** (fluxos nas cenas) | **38** | 36 passaram, 1 ignorado (`[Explicit]`) e 1 falhou **só na execução completa em batchmode** (ver 1.3) |

### 1.1 EditMode: o que cobre

| Classe de teste | Testes | O que cobre |
|---|---|---|
| `BattleTests` | 61 | Custos de energia, compra e reembaralho, descarte, Esgotar, dano base, Força, escudo, **fraqueza elemental +50%**, Fraco −25%, arredondamento, **Queimadura**, Atordoar, intenções, comportamento de cada inimigo (Verde sem escudo repetido, Vermelho a cada 3 turnos, Azul cura o aliado, Rei Slime em ciclo, invocação limitada a 2, fúria abaixo de 50%), relíquias na batalha, vitória e derrota |
| `TileAndRelicTests` | 35 | Descanso (cura e melhoria), Baú, Loja (preços, compra, itens que somem, cura e remoção), eventos e seus resultados, as 5 relíquias |
| `DataAssetTests` | 19 | Cada número dos assets (cartas, inimigos, encontros, relíquias, eventos, tabuleiro, config) conferido contra o GDD |
| `ArtCompletenessTests` | 17 | Todos os sprites, animações e ícones esperados existem e estão ligados aos dados |
| `BoardLogicTests` | 14 | Movimento no tabuleiro, **bifurcação e escolha**, **parada no Chefe**, casas visitadas, dado |
| `RunStateTests` | 11 | Vida, moedas, deck, relíquias, filas de encontros sem repetição, recompensas |
| `BalanceTests` | 7 | **Simulação de 400 partidas** (ver seção 2) |
| `AudioTests` | 5 | Biblioteca de áudio completa, faixa por cena |
| `DiceFaceTests` | 5 | Faces do dado (pontos, identidade, simetria) |
| `ArtImportTests` | 2 | Regras de importação de pixel art (PPU 32, Point, sem compressão) |

### 1.2 PlayMode: o que cobre

| Classe | Testes | Fluxo |
|---|---|---|
| `TileFlowTests` | 10 | Clicar nos botões reais de Descanso, Loja, Evento, Baú e relíquias no tabuleiro |
| `EndFlowTests` | 7 | Rei Slime até a vitória, derrota, tela final, pausa, título, música por cena e fade |
| `BattleFlowTests` | 5 | Cena de batalha, o dano só entra no quadro de impacto, carta e alvo por clique |
| `RunFlowTests` | 5 | Do tabuleiro à batalha, vencer, receber a recompensa e voltar |
| `StoryFlowTests` | 5 | Caixa de história que bloqueia o dado até "Começar" |
| `BoardFlowTests` | 4 | Rolar, bifurcar, escolher, resolver, casa cinza, Chefe |
| `DiceFlowTests` | 1 | Faces sorteadas da rolagem e número grande |
| `FullRunBotTests` | 1 (ignorado) | Robô clicando as cenas: **desativado** porque congela o Editor após o primeiro dado; a partida inteira é coberta pela simulação em lógica pura |

### 1.3 Falha e pendências de teste

`EndFlowTests.Musica_TrocaPorCena_E_TransicaoClareiaDepois` falhou ("tempo esgotado esperando a condição") na execução **completa** de PlayMode em batchmode, mas **passa quando executado sozinho** (re-executado em seguida). É uma dependência de ordem/tempo do fade entre cenas em batchmode, não uma regra de jogo; fica registrada como pendência para investigar. O teste `FullRunBotTests` continua desativado (seção 1.2).

## 2. Playtest automático e balanceamento

`Scripts/Bot/BotPolicy.cs` e `RunSimulator.cs` jogam partidas inteiras em **lógica pura** (um jogador razoável, não perfeito), com semente injetável. `BalanceTests` simula **400 partidas** e exige:

- taxa de vitória entre **40% e 85%**;
- duração estimada entre **8 e 16 minutos** (40 s por turno + 25 s por casa);
- nenhuma batalha passa de **60 turnos** (o robô nunca trava);
- as batalhas comuns são vencíveis (pelo menos 60% de vitória em cada encontro);
- a simulação é determinística para a mesma semente.

Resultado com o dado de 1 a 3: vitória do robô **cerca de 77%**, chega ao Chefe em quase todas as partidas, **cerca de 3 batalhas por partida**, e **todas as mortes ocorrem no Chefe**.

### 2.1 Ajuste do dado: d6 → d3

Com 12 casas e o dado de 6 faces, o Peão cruzava o mapa em poucas rolagens e resolvia só **cerca de 1,9 batalha** por partida: poucas batalhas, poucas lojas e eventos, partida curta e sem graça. Reduzir o dado para **1 a 3** (`diceSides = 3`) **dobrou as paradas**, e a partida passou a ter cerca de 3 batalhas e duração estimada de **cerca de 9 minutos**. O Dado Gigante (evento) continua usando 6 faces (`eventDiceSides = 6`). O valor é um número do `GameConfig`, ajustável sem mexer em código; se a partida parecer curta, `diceSides = 2` leva a ~3,5 batalhas.

### 2.2 Observações de balanceamento

As batalhas comuns são fáceis para o robô com o deck inicial; o desafio está no **Chefe**. A duração real (9 min) fica abaixo da meta do GDD (10 a 15 min); isso depende de **jogo humano** e está listado como pendência.

## 3. Bugs encontrados e corrigidos

| Bug | Como apareceu | Correção |
|---|---|---|
| **Jogo mudo** e Console inundado de "There are no audio listeners" | Playtest automático / Console | `SceneKit.CreateCamera` passou a adicionar `AudioListener`; as 4 cenas foram reconstruídas |
| Interface **desalinhada em janela larga** | Screenshots 2,3:1 | A Pixel Perfect Camera usa zoom **inteiro** e o canvas escalava de forma contínua; `CanvasMatch` aplica o mesmo zoom inteiro e o HUD ancora num quadro 640x360 |
| Dado 3D com **faces inconsistentes** | Avaliação visual | Faces chapadas geradas por código (`DiceFaceBuilder`), idênticas entre si; versões antigas em `_Rejected` |
| Peão **ilegível** e Slime Mirim **parecendo de costas** | Avaliação visual | Peão de tinta refeito com rosto; Peão com rosto |
| Texto das cartas e botões estourando | Screenshots | Texto das cartas em 7 px máx., botão Encerrar turno 124 px, caixas de Descarte/Compra mais largas |
| Setas de escolha de caminho **desalinhadas** | Screenshot | Alinhadas às casas |
| Rei Slime **de costas** para o Peão | Screenshot | `EnemyData.flipX` |
| Arquivo de imagem truncado (`bg_battle.png`) | Download longo do PixelLab | Verificação do final do PNG (`IEND`) e arquivo refeito |
| Testes de derrota **dependiam de sorte** | Falha intermitente | O Slime Verde pode escolher escudo no 1º turno: testes reescritos para não depender disso |
| `AudioLibrary` não era encontrada pelo Unity | Script de ScriptableObject em arquivo com nome diferente | Classe movida para `AudioLibrary.cs` |

## 4. Desempenho

O jogo é 2D simples (poucos sprites, UI por Canvas, sem física, sem iluminação dinâmica). Não foi feita uma medição formal de quadros por segundo com profiler nesta entrega; as observações são: o Editor roda a taxa de quadros do monitor, a transição entre cenas é imediata com fade de 0,25 s, as builds abrem e rodam fora do Editor (Mac verificada), e o pool de 8 `AudioSource` evita criar objetos de áudio durante a partida. **Pendência:** medir FPS e memória nas builds com o Profiler.

## 5. Resoluções testadas

| Resolução | Zoom inteiro | Resultado |
|---|---|---|
| 1280x720 (16:9) | 2x | Referência dos screenshots; tudo alinhado |
| 1920x1080 (16:9) | 3x | Funciona |
| 1656x720 (≈ 2,3:1, janela larga) | 2x | HUD no quadro central, faixas roxas nas laterais |
| Janelas pequenas ou muito largas | 1x possível | Tudo menor (comportamento esperado do pixel perfect) |

A câmera é fixa em 640x360; em proporções diferentes de 16:9 aparecem faixas roxas nas laterais.

## 6. Verificações da entrega

Compilação sem erros, **Console sem erros novos** (o warning antigo "Account API did not become accessible" vem de um pacote do assistente de IA da Unity anterior ao jogo), build de **Windows regenerada em 18/11/2026** (0 erros, 130 MB) e build de **Mac** de 17/11/2026 (0 erros). A build do Windows **não foi executada** (não havia máquina Windows).

**Erro conhecido de Console:** ao abrir a tela de Derrota, o Unity registra `'Peao' AnimationEvent 'OnDeathEnd' on animation 'pawn_death' has no receiver` (o Peão da tela final toca a morte sem receptor do evento). Não afeta o jogo; pendência P9 da matriz de requisitos.
