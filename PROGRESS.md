# PROGRESS.md · Xeque de Tinta

Diário de construção mantido pelo Claude Code. Leia ao começar cada sessão e atualize ao fim de cada marco.

## Marco atual
M5 (Casas e relíquias), a iniciar. Escopo: versão enxuta, M0 a M8.

## Concluídos
- **M0 · Base e arte (lote 1).** Pastas do projeto, câmera Pixel Perfect (640x360, PPU 32), pipeline de importação de arte, lote 1 gerado e importado, clipes e Animators criados, cena `Scenes/M0_Test.unity` com tudo animando. 2 testes de EditMode passam.
- **M1 · Dados.** ScriptableObjects `CardData`, `EnemyData`, `EncounterData`, `RelicData`, `EventData`, `BoardLayout` e `GameConfig` (todos os números de balanceamento) em `Scripts/Data`. Assets criados pelo menu `Xeque > M1 > Criar assets de dados` (idempotente): 12 cartas, 5 inimigos, 6 encontros, 5 relíquias, 3 eventos, 1 tabuleiro e o `GameConfig`. 21 testes de EditMode passam (19 de dados, conferindo cada número contra o GDD).

- **M2 · Tabuleiro.** Cena `Scenes/Board.unity` (já no Build Settings) com as 12 casas, ligações pontilhadas, as 2 bifurcações, Peão que pula casa a casa, dado que rola, casas resolvidas em cinza, parada no Chefe e HUD (vida, moedas, "Ver deck", botão de rolar, Espaço rola). Lógica pura em `BoardGraph`, `BoardMover`, `BoardProgress`, `DiceRoller`; apresentação em `BoardController`. 14 testes de EditMode (lógica) e 4 de PlayMode (fluxo rolar, bifurcar, escolher, resolver, casa cinza, chefe). Screenshot em `Docs/screenshots/m2_board.png`.

- **M3 · Batalha.** `BattleSystem` (Início, TurnoJogador, TurnoInimigo, Vitória, Derrota), `DeckManager` (compra, reembaralho, descarte, esgotar), `DamageCalc`/`ElementRules`, efeitos de carta e de inimigo como classes pequenas (`DealDamage`, `GainShield`, `ApplyBurn`, `ApplyWeak`, `Heal`, `Stun`; `EnemyAttack`, `EnemyShield`, `EnemyHealAlly`, `EnemySummon`...), IA em `EnemyBrain`. Cena `Scenes/Battle.unity` com `BattleController` (atores animados, barras de vida, status, intenções com ícone e dano, mão de cartas com hover, alvo por clique, energia, contadores, números flutuantes "Eficaz!", painel de vitória/derrota). Animation Events `OnImpact` (quadro 4 dos ataques) e `OnDeathEnd`. 63 testes de EditMode de regra + 5 de PlayMode de fluxo (inclui "o dano só entra no quadro de impacto"). Screenshot em `Docs/screenshots/m3_battle.png`.

- **M4 · Partida.** `RunState` (vida, deck, relíquias, moedas, casa atual e visitadas, filas de encontros e eventos sorteadas pela semente), `GameManager` persistente (`DontDestroyOnLoad`) e a troca Board ⇄ Battle: pisar numa casa de Batalha ou no Chefe lança a batalha pelo `BattleLauncher`; ao vencer volta ao tabuleiro com o painel de recompensa (10 a 15 moedas e 1 entre 3 cartas, ou pular); só então a casa fica cinza. Vida persiste entre batalhas. Derrota (ou vitória no Chefe, por ora) leva ao título. Painéis `Reward` e `Deck` ("Ver deck", com rolagem) em `UI/Panels.cs`. Cena `Title` (logo em texto, Jogar, Sair, volume, Peão e slimes animados). 11 testes de EditMode (RunState) e 14 de PlayMode (inclui os fluxos da partida). Screenshot em `Docs/screenshots/m4_recompensa.png`.

## Decisões tomadas
- **Pixel Perfect Camera** vem embutida no URP 17 (`UnityEngine.Rendering.Universal.PixelPerfectCamera`). Nenhum pacote novo foi instalado; `Packages/` e `ProjectSettings/` não foram alterados.
- **Quadros soltos em vez de sheets fatiadas:** cada quadro é um PNG (`nome_anim_N.png`), importado como Sprite Single. O resultado é o mesmo da fatia por grade e dispensa ferramenta de montagem de sheet. `Scripts/Editor/ArtPipeline.cs` aplica as regras de pixel art (PPU 32, Point, sem compressão, sem mipmaps, pivô embaixo para personagens) e gera clipes e Animators pelo menu `Xeque > Gerar animações`.
- **Slimes como objetos, não personagens:** o modo de personagem do PixelLab só desenha humanoides (a primeira tentativa saiu com braços e pernas). Os slimes foram gerados com `create_1_direction_object` (64 candidatos por pedido, escolhidos à mão) e animados com `animate_object`. Os humanoides descartados não foram salvos.
- **Contagem de quadros (limite do gerador v3: pares, mínimo 4):** Peão idle 4, hop 4, attack 6, hit **4** (ASSETS dizia 2), death 6. Slimes idle 4, attack **6** (dizia 5), hit **4** (dizia 2), death **6** (dizia 5). O quadro de impacto do ataque do Peão ficou no índice 3 (quadro 4); o dos slimes é o índice 2 a 3, a conferir ao ligar o Animation Event no M3.
- **Quadros de "dado rolando":** não foram gerados. O dado vai girar por código (alternando faces com tremor), o que poupa gerações. As 6 faces vieram como pedidos separados e variam um pouco de ângulo e tamanho.
- **Casas "visitadas":** serão o mesmo sprite dessaturado por código no M2.
- Peão de referência de estilo: variação A (v3, 8 direções, só a leste foi animada). Variação B (humanoide de camisa vermelha) em `Art/_Rejected/`.

- **Assemblies:** `Scripts/XequeDeTinta.Runtime.asmdef` (código do jogo), `Scripts/Editor/XequeDeTinta.Editor.asmdef` e `Tests/EditMode/XequeDeTinta.Tests.EditMode.asmdef`, necessários para os testes enxergarem o código. Isso não mexe em pacotes nem em `ProjectSettings/`. O `PixelPerfectCamera` está no assembly `Unity.RenderPipelines.Universal.2D.Runtime`.
- **Efeitos de carta como dados:** `CardData` guarda uma lista de `EffectSpec` (tipo, valor, repetições). No M3 cada tipo vira uma classe pequena (`DealDamage`, `GainShield`, `ApplyBurn`...) criada por uma fábrica. Nos status (Queimadura, Fraco) `value` são os turnos.
- **IA dos inimigos como dados:** `EnemyData` tem `EnemyBehaviorKind` (Simple, RandomNoRepeatDefend, EveryNthSpecial, Cycle) mais a lista de ações; o Rei Slime usa Cycle, `enrage*` (Força abaixo de 50%) e `summonedEnemy`/`summonMaxAlive`.
- **Layout do tabuleiro (provisório):** linha reta de 12 casas com bifurcações nas casas 2 (Batalha ou Descanso) e 6 (Batalha ou Baú), que se juntam nas casas 5 e 9. Posições em unidades de mundo, ajustáveis no asset.
- **Textos dos eventos** escritos por mim (provisório), com as escolhas do GDD.
- **Aleatoriedade injetável:** `IRng` (`SeededRng` no jogo, `ScriptedRng` nos testes). Todo sorteio (dado, embaralhar, IA, recompensas) deve usar `IRng`.
- **Interface por código:** `UIFactory` monta Canvas (640x360 de referência), textos TMP, painéis e botões; a fonte é Press Start 2P (OFL, licença em `Art/UI/Fonts/OFL.txt`), asset TMP em `Resources/Fonts`. Até o lote 2 (M6) painéis e botões são retângulos creme/dourado/roxo.
- **TextMeshPro Essentials** importados (pasta `Assets/TextMesh Pro`), necessários para a fonte; nenhum pacote novo.
- **Ganchos do tabuleiro:** `BoardController.Resolver(tile, done)` é onde o M4/M5 ligam batalha, loja etc.; `RollFinished` e `DeckRequested` são eventos; `PlaceAt` posiciona o peão sem animar.
- **Peão escalado 2x** no tabuleiro (a figura ocupa uns 20 px do quadro de 44).
- **Resolução em duas fases:** `BattleSystem.PlayCard` gasta energia e tira a carta da mão; os efeitos só valem em `CardPlay.Resolve()`, chamado pela apresentação no Animation Event de impacto. Idem para inimigos (`NextEnemyAction()` -> `EnemyPlay.Resolve()`). Nos testes basta chamar `Resolve()` direto.
- **Regras que o GDD deixou em aberto (provisório):** Queimadura tira 2 no início do turno do dono, ignora escudo, e gasta 1 turno; Fraco gasta 1 turno ao fim da ação do dono; escudo do herói zera no início do turno dele e o dos inimigos no início do turno deles; atordoado pula a ação, perde o atordoamento e mantém a mesma intenção; Slime Vermelho queima nos turnos 3, 6...; Slime Azul cura o aliado com mais vida faltando (ele mesmo se estiver sozinho ou todos cheios, sem efeito); "abaixo de 50%" é estritamente menor; o chefe só invoca até 2 Mirins vivos; Slime Mirim invocado só age no turno seguinte.
- **Ganchos para relíquias (M5):** `IBattleHook` (OnBattleStart, OnPlayerTurnStart, OnEnemyKilled) em `BattleSystem.Hooks`.
- **Entrega da batalha ao GameManager (M4):** `BattleLauncher.Setup` (entrada) e `BattleLauncher.OnFinished` (saída com `BattleResult`). Sem eles, a cena monta uma batalha de teste (config.battleEncounters[4]). `BattleLauncher.SpeedOverride` acelera a apresentação.
- **Animator Controllers atualizados no lugar** (`ArtPipeline`), para manter o GUID. Se algum dia reaparecer referência quebrada, rode `Xeque > M1 > Criar assets de dados`, `M2 > Construir cena Board` e `M3 > Construir cena Battle`.
- **Escala em batalha:** herói 3x, slimes 2x (Mirim 1,2x). Status são texto colorido ("Queim 3", "Fraco 1", "Forç 2", "Atord", "Esc 5"); os ícones de status chegam no lote 2 (M6). Cartas sem ilustração até o M6.
- **GameManager não cria partida sozinho:** `Ensure(config)` só garante o objeto; `EnsureRun()` cria a partida (o Tabuleiro aberto direto no Editor faz isso). O Título nunca cria partida.
- **Fim de partida (provisório até o M6):** `GameManager.EndRun(won)` carrega a cena `End` se existir; senão, o `Title`. `LastRunWon` guarda o resultado.
- **Ouvinte de testes:** `Scripts/Editor/TestFailureLogger.cs` registra no Console `[TESTFAIL]` com a mensagem; o relatório completo fica em `~/Library/Application Support/DefaultCompany/prova-2bi-2sem-2026/TestResults.xml`.
- **Testes de PlayMode:** o `GameManager` sobrevive às cenas; cada teste destrói o dele no início. Para vencer rápido, os testes põem 1 de vida nos inimigos.
- Sprites de cartas, relíquias e do Rei Slime ficam nulos até o M6 (lote 2).

## Para o humano testar jogando
- **Partida (M4):** abrir `Scenes/Title.unity` e jogar do início: Jogar, rolar o dado, pisar numa Batalha, vencer, escolher a carta e voltar. Conferir se a troca de cena não parece brusca, se o painel de recompensa é claro, se "Ver deck" mostra o deck certo e se a derrota volta ao título. Ainda não há loja, descanso, evento, baú, relíquias nem a tela de vitória (M5/M6).
- **Batalha (M3):** abrir `Scenes/Battle.unity`, dar Play e jogar uma batalha de 3 inimigos: ritmo das animações e do dano (o golpe acerta no quadro certo do ataque?), se a intenção sobre o inimigo é clara, se os números flutuantes e "Eficaz!" aparecem a tempo de ler, se o hover/enlargement das cartas atrapalha clicar nos inimigos atrás delas, se a mão cobre demais os inimigos, se 3 de energia e 5 cartas dão um turno interessante.
- A mão ocupa a parte de baixo e as barras de vida dos inimigos ficam logo acima dela; ajustar se parecer apertado.
- **Tabuleiro (M2):** abrir `Scenes/Board.unity` e jogar: ritmo do dado e do pulo do Peão, se a escolha de caminho (seta dourada, clicar na casa) é clara, se o cinza das casas resolvidas fica legível, se a mensagem do topo ajuda. Hoje a casa só mostra uma mensagem ("Batalha!"); a batalha de verdade entra no M3/M4.
- O Peão sobre a casa tampa um pouco do ícone dela; avaliar se vale subir o Peão.
- Abrir `Scenes/M0_Test.unity` e dar Play: conferir se Peão e slimes têm silhueta legível em tamanho real e se o ritmo das animações (8 quadros/s) está bom.
- O Peão ficou menor e menos "cheio" que os slimes (a figura ocupa uns 20 px do quadro de 44). Avaliar se precisa ampliar.
- O Slime Vermelho ficou escuro; a chama no topo é pequena. Avaliar se a leitura "Fogo" está clara ou se vale regenerar.
- As faces do dado estão inconsistentes entre si; avaliar se incomoda.

## Pendências
- Ícones de intenção (16x16) e ícones das casas ainda não foram avaliados em tamanho real dentro do jogo.
- Quadro de impacto exato das animações (Animation Event) a ligar no M3.
- Aviso na Game view "Rendering at an odd-numbered resolution" aparece só por causa do tamanho da janela do Editor, não do jogo.
- Warning antigo no Console ("Account API did not become accessible"), vem do pacote `com.unity.ai.assistant`, anterior ao projeto de jogo. Não é novo.

## Gasto no PixelLab
| Lote | Gerações | Saldo depois |
|---|---|---|
| Lote 1 (M0) | 73 | 1927 de 2000 |
