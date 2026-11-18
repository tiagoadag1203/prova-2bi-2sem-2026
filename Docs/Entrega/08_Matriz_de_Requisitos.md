# Xeque de Tinta · 08 Matriz de Requisitos

Cada requisito do professor, onde ele é atendido no projeto e o status. Caminhos relativos à raiz do projeto Unity; "Entrega" é a pasta `Entrega-Xeque-de-Tinta`.

## 1. Requisitos do trabalho

| # | Requisito | Onde está atendido | Status |
|---|---|---|---|
| R1 | **Documentação do sistema** | Documento 02 (arquitetura, cenas, fluxo, máquina de estados, ScriptableObjects, scripts, regras, áudio, testes, como abrir e gerar build); `GDD.md`, `ASSETS.md`, `PROGRESS.md`; Entrega/Documentos/02_Documentacao_Tecnica.pdf | Atendido |
| R2 | **Manual / encarte do jogo** | Documento 03 e `Docs/Entrega/03_Manual_do_Jogo.html` (encarte diagramado); Entrega/Documentos/03_Manual_do_Jogo.pdf | Atendido |
| R2a | Manual: **história** | Manual, seção 1; documento 01, seção 2; caixa de história em `BoardController.IntroRoutine`; cena Title | Atendido |
| R2b | Manual: **personagens** | Manual, seção 2 (Peão); documento 01 | Atendido |
| R2c | Manual: **inimigos** | Manual, seção 3 (4 slimes + Rei Slime, com sprite, vida, elemento e comportamento); `Data/Enemies/*.asset` | Atendido |
| R2d | Manual: **comandos** | Manual, seção 4 (mouse, Espaço, Esc, botão direito) | Atendido |
| R2e | Manual: **ataques e skills** | Manual, seção 8 (12 cartas, versões melhoradas); `Data/Cards/*.asset` | Atendido |
| R3 | **Jogabilidades e movimentações novas** | Documento 01, seção 4.1 e 4.2; documento 07; `Scripts/Board/BoardMover.cs`, `BoardController.cs`, cena Board | Atendido (movimentação por dado, casas e bifurcações; não há movimentação em tempo real) |
| R4 | **Skills inovadoras** | Documento 07 (ciclo de elementos, Atordoar, intenções, Queimadura, área, Fraco, invocação, fúria do chefe); `Scripts/Battle/Effects.cs`, `EnemyEffects.cs` | Atendido |
| R5 | **Sprite sheets originais** | Entrega/Sprites (uma sheet PNG + `.json` por personagem e animação, prancha geral); `Assets/_Project/Art`; documento 04 | Atendido, com a ressalva de que a arte foi **gerada por IA (PixelLab) e selecionada e ajustada pela equipe** (documento 04, seção 7). Quadros soltos no projeto; as sheets foram montadas para a entrega por `Tools/export_entrega_sprites.py` |
| R6 | **Efeitos sonoros próprios integrados** | 15 efeitos + 2 músicas em `Assets/_Project/Audio`, tocados por `AudioManager`/`AudioLibrary`; Entrega/Sons (`.wav` 44,1 kHz) e `LISTA_DE_SONS.md`; documento 05 | Atendido (sintetizados por script Python próprio, CC0; ver documento 05) |
| R7 | **Unity + C#** | Unity 6000.4.10f1, mais de 50 scripts C# em `Assets/_Project/Scripts`; Entrega/Projeto_Unity.zip | Atendido |
| R8 | **Jogabilidade correta** (40%) | Fluxo completo Título → Tabuleiro → Batalha → Recompensa → Chefe → Vitória/Derrota; 176 testes de EditMode e 37 de PlayMode; simulação de 400 partidas; documento 06 | Atendido (com a pendência P1) |
| R9 | **Método de vitória e de derrota** | Documento 01, seção 5; cena End (`EndController`); `GameManager.EndRun` | Atendido |
| R10 | **Builds** | Entrega/Builds/Mac e Entrega/Builds/Windows (`BuildTools`) | Atendido (Windows não executada, ver P3) |
| R11 | **Projeto Unity entregue** | Entrega/Projeto_Unity.zip (sem Library, Temp, Logs, Build e UserSettings) | Atendido |

## 2. Critérios de avaliação

| Critério | Peso | Onde está |
|---|---|---|
| Documentação e manual | 20% | Documentos 01 a 09; manual em PDF com encarte |
| Criatividade | 20% | Documento 07; tema do xadrez tomado pela tinta; tabuleiro com dado + cartas |
| Sprites e sons | 20% | Entrega/Sprites, Entrega/Sons; documentos 04 e 05 |
| Jogabilidade correta | 40% | Build jogável, testes (documento 06), regras no documento 02 |

## 3. Entregáveis da pasta

| Item | Local | Status |
|---|---|---|
| 9 documentos em Markdown e PDF | Entrega/Documentos | Atendido |
| Manual em PDF (encarte) | Entrega/Documentos/03_Manual_do_Jogo.pdf | Atendido |
| Sprite sheets + JSON + prancha | Entrega/Sprites | Atendido |
| Sons em WAV + lista | Entrega/Sons | Atendido |
| Projeto Unity em ZIP | Entrega/Projeto_Unity.zip | Atendido |
| Builds Mac e Windows | Entrega/Builds | Atendido |
| LEIAME | Entrega/LEIAME.md | Atendido |

## Pendências

| # | Pendência | Detalhe |
|---|---|---|
| P1 | **Verificação humana de jogabilidade** | Ritmo, legibilidade, duração real (estimada em 9 min, abaixo dos 10 a 15 do GDD) e dificuldade só podem ser confirmados jogando; lista em `PROGRESS.md` |
| P2 | **Um teste de PlayMode instável em batchmode** | `Musica_TrocaPorCena_E_TransicaoClareiaDepois` falha na execução completa e passa isolado; `FullRunBotTests` segue desativado (`[Explicit]`) |
| P3 | **Build do Windows não foi executada** | Compila sem erros, mas não havia máquina Windows para testar |
| P4 | **Arte gerada por IA** | Requisito de "sprite sheets originais" atendido com arte gerada no PixelLab e curada pela equipe (declarado no documento 04). O professor pode exigir arte desenhada à mão: não atendido nesse caso |
| P5 | **Sons simples, sem Audio Mixer** | Sintetizados por script; volumes fixos em código; escuta humana pendente |
| P6 | **Desempenho não medido com Profiler** | Sem medições formais de FPS e memória |
| P7 | **Layout da tela de Vitória** | No screenshot 1280x720, o botão "Voltar ao título" quebra em duas linhas e o Peão passa por trás dele; é um acabamento visual a revisar (não altera regras) |
| P8 | **Ícones de intenção e das casas** | Ainda não avaliados em tamanho real por um humano (`PROGRESS.md`) |
| P9 | **Erro de Console na tela de Derrota** | `'Peao' AnimationEvent 'OnDeathEnd' on animation 'pawn_death' has no receiver`: o Peão da tela End toca a animação de morte sem o componente `ActorView` que recebe o evento. É só um aviso de log (nada trava), mas o Console não fica 100% limpo nessa tela |
