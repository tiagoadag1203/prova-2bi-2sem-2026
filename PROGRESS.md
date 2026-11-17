# PROGRESS.md · Xeque de Tinta

Diário de construção mantido pelo Claude Code. Leia ao começar cada sessão e atualize ao fim de cada marco.

## Marco atual
M1 (Dados), a iniciar. Escopo: versão enxuta, M0 a M8.

## Concluídos
- **M0 · Base e arte (lote 1).** Pastas do projeto, câmera Pixel Perfect (640x360, PPU 32), pipeline de importação de arte, lote 1 gerado e importado, clipes e Animators criados, cena `Scenes/M0_Test.unity` com tudo animando. 2 testes de EditMode passam.

## Decisões tomadas
- **Pixel Perfect Camera** vem embutida no URP 17 (`UnityEngine.Rendering.Universal.PixelPerfectCamera`). Nenhum pacote novo foi instalado; `Packages/` e `ProjectSettings/` não foram alterados.
- **Quadros soltos em vez de sheets fatiadas:** cada quadro é um PNG (`nome_anim_N.png`), importado como Sprite Single. O resultado é o mesmo da fatia por grade e dispensa ferramenta de montagem de sheet. `Scripts/Editor/ArtPipeline.cs` aplica as regras de pixel art (PPU 32, Point, sem compressão, sem mipmaps, pivô embaixo para personagens) e gera clipes e Animators pelo menu `Xeque > Gerar animações`.
- **Slimes como objetos, não personagens:** o modo de personagem do PixelLab só desenha humanoides (a primeira tentativa saiu com braços e pernas). Os slimes foram gerados com `create_1_direction_object` (64 candidatos por pedido, escolhidos à mão) e animados com `animate_object`. Os humanoides descartados não foram salvos.
- **Contagem de quadros (limite do gerador v3: pares, mínimo 4):** Peão idle 4, hop 4, attack 6, hit **4** (ASSETS dizia 2), death 6. Slimes idle 4, attack **6** (dizia 5), hit **4** (dizia 2), death **6** (dizia 5). O quadro de impacto do ataque do Peão ficou no índice 3 (quadro 4); o dos slimes é o índice 2 a 3, a conferir ao ligar o Animation Event no M3.
- **Quadros de "dado rolando":** não foram gerados. O dado vai girar por código (alternando faces com tremor), o que poupa gerações. As 6 faces vieram como pedidos separados e variam um pouco de ângulo e tamanho.
- **Casas "visitadas":** serão o mesmo sprite dessaturado por código no M2.
- Peão de referência de estilo: variação A (v3, 8 direções, só a leste foi animada). Variação B (humanoide de camisa vermelha) em `Art/_Rejected/`.

## Para o humano testar jogando
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
