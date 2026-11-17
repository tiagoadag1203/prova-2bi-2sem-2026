# ASSETS.md · Arte e áudio do Xeque de Tinta (versão enxuta)

Toda a arte vem do PixelLab (MCP `pixellab`). Os sprites antigos em `Assets/_Project/Art/_Reference/` são só referência de estilo e **não entram no jogo**.

## Regras de estilo

- Pixel art, contorno escuro de 1 px, sombreamento simples (2 a 3 tons), cores vivas.
- **Vista lateral:** herói virado para a **direita**, inimigos para a **esquerda**.
- Tamanhos: personagens 32x32, chefe 64x64, ícones 16x16. Fundo transparente, PNG.
- Tema: peças de tabuleiro de madeira, feltro e papel; monstros de tinta viva.
- Interface: fundo roxo escuro (#2A1B3D), dourado (#F2C14E), creme (#F4E9D8).
- Cada slime leva um detalhe do elemento (folha, chama, gota), não só a cor.

**Base de prompt (em inglês):** `pixel art, side view, dark 1px outline, vivid saturated colors, simple 3-tone shading, transparent background, board game piece made of [material]`

**Âncora de estilo:** o Peão escolhido vira referência para todos os outros pedidos.

## Economia de gerações

- Personagens: 2 variações, escolher a melhor, guardar a outra em `Art/_Rejected/`.
- Ícones, casas, cartas e relíquias: 1 geração cada, refazer só se sair ilegível.
- Efeitos visuais simples (golpe, fogo, cura, escudo) com o sistema de partículas da Unity, sem gerar sprite.
- Moldura de carta: 1 sprite neutro, tingido por código com a cor do elemento.
- Conferir `get_balance` antes de cada lote e anotar o gasto no PROGRESS.md.

## Animações

| Personagem | Animações |
|---|---|
| Peão | idle (4), hop (4), attack (6, impacto no quadro 4), hit (2), death (6) |
| Slimes | idle (4), attack (5, impacto no 3), hit (2), death (5, se desfaz em tinta) |
| Rei Slime | idle (6), attack (6), summon (5), hit (2), death (8) |

## Lote 1 · M0

| Arquivo | Tamanho | Descrição |
|---|---|---|
| Characters/Hero/pawn_*.png | 32 | wooden chess pawn hero, light beech wood, red scarf, small iron sword, determined dot eyes |
| Enemies/slime_green_*.png | 32 | green ink slime, tall dome, small leaf sprout on top, visible eyes |
| Enemies/slime_red_*.png | 32 | red ink slime, small flame tuft on top, angry eyes |
| Enemies/slime_blue_*.png | 32 | blue ink slime, water drop shape on top, calm eyes |
| Board/dice_face_1..6.png, dice_roll_*.png | 32 | cream wooden die with purple pips, 6 faces + 4 frames rolling |
| Board/tile_[start, battle, shop, rest, event, chest, boss].png | 32 | rounded square tile, colored border, icon: star, crossed swords, coin, heart, question mark, chest, crown |
| Board/tile_*_visited.png | 32 | a mesma casa em cinza (pode ser feita por código, dessaturando) |
| UI/Intents/intent_[attack, defend, buff, debuff, summon].png | 16 | sword, shield, green up arrow, purple down arrow, plus with ink drop |

## Lote 2 · M6

| Arquivo | Tamanho | Descrição |
|---|---|---|
| Bosses/king_slime_*.png | 64 | huge green ink slime with golden cardboard crown and leaf cape |
| UI/Status/status_[shield, burn, weak, strength, stun].png | 16 | ícones simples |
| UI/Elements/element_[fire, plant, water, neutral].png | 16 | chama, folha, gota, círculo |
| UI/Cards/card_frame.png | 48x64 | moldura de carta de papel creme, faixa para nome e custo |
| Cards/card_[golpe, defender, investida, bola_de_fogo, brasa, raiz, casca_grossa, onda, respingo, golpe_duplo, seiva, golpe_pesado].png | 32 | ilustração de cada carta |
| Relics/relic_[dado_viciado, base_de_chumbo, tinta_seca, pincel_afiado, ampulheta_de_bolso].png | 16 | ícone de cada relíquia |
| UI/energy_orb.png, UI/coin.png | 16 a 24 | orbe de tinta dourada; moeda dourada |
| UI/panel_9slice.png, UI/button_9slice_[normal, hover, pressed].png | 9-slice | papel creme com borda de madeira |
| Backgrounds/bg_board.png, bg_battle.png | 640x360 | floresta de feltro verde, árvores de papel recortado, luz suave (o do tabuleiro serve de fundo do título) |
| UI/logo.png | 256x96 | "Xeque de Tinta" em letras de madeira com tinta escorrendo |

## Áudio (M7)

O PixelLab não gera som. Usar bibliotecas livres CC0 (Kenney.nl, OpenGameArt com CC0) ou CC-BY com crédito. Registrar origem e licença em `Assets/_Project/Audio/LICENSES.md`. Lista na seção 10 do GDD.
