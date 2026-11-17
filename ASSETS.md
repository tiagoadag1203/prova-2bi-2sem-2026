# ASSETS.md · Arte e áudio do Xeque de Tinta

Lista de tudo que precisa ser gerado. A arte vem do PixelLab (MCP `pixellab`). Os sprites antigos ficam em `Assets/_Project/Art/_Reference/` só como referência de estilo e **não entram no jogo**.

## Regras de estilo (valem para todo pedido no PixelLab)

- Pixel art, contorno escuro de 1 px, sombreamento simples (2 a 3 tons por cor), cores vivas e saturadas.
- Personagens em **vista lateral**: herói virado para a **direita**, inimigos virados para a **esquerda**.
- Tamanhos: personagens 32x32, chefes 64x64 (O Jogador 96x96), ícones 16x16, cartas 48x64.
- Fundo transparente. Exportar PNG.
- Tema: peças de jogo de tabuleiro feitas de madeira, feltro, papel e tinta. Monstros são manchas de tinta viva.
- Paleta da interface: fundo roxo escuro (#2A1B3D), destaques em dourado (#F2C14E), creme (#F4E9D8).
- Elementos: Fogo vermelho/laranja, Planta verde, Água azul, Neutro creme/cinza. Cada inimigo elemental leva um detalhe com o símbolo do elemento (chama, folha, gota), não só a cor.

### Âncora de estilo
O primeiro asset aprovado (o Peão) vira referência de estilo para todos os outros pedidos. Se o PixelLab aceitar imagem de referência, usar sempre o PNG aprovado do Peão; se não, repetir as regras acima no prompt.

**Base de prompt (em inglês, o PixelLab responde melhor):**
`pixel art, side view, dark 1px outline, vivid saturated colors, simple 3-tone shading, transparent background, board game piece made of [material], cute but heroic`

## Fluxo de geração

1. Gerar um lote por vez, na ordem dos marcos.
2. Para cada personagem: gerar 3 variações → mostrar para aprovação → animar só o aprovado.
3. Conferir `get_balance` antes de cada lote e informar quantas gerações o lote vai gastar.
4. Salvar em `Assets/_Project/Art/...` com os nomes desta lista.

## Animações padrão

| Tipo | Animações (quadros sugeridos) |
|---|---|
| Herói | idle (4), hop no tabuleiro (4), attack (6, impacto no quadro 4), cast (4), hit (2, flash branco), death (6), victory (4) |
| Inimigo comum/elite | idle (4), attack (5, impacto no 3), special (4), hit (2), death (5, se desfaz em tinta) |
| Chefe | idle (6), 2 a 3 ataques (6), summon (5), hit (2), death (8) |

---

## Lote 1 · M0 (fase 1 base)

| Arquivo | Tamanho | Descrição para o prompt |
|---|---|---|
| Characters/Hero/pawn_*.png | 32 | wooden chess pawn hero, light beech wood, red scarf, small iron sword, determined dot eyes |
| Enemies/Phase1/slime_green_*.png | 32 | green ink slime, taller dome, small leaf sprout on top, visible eyes |
| Enemies/Phase1/slime_red_*.png | 32 | red ink slime, small flame tuft on top, angry eyes |
| Enemies/Phase1/slime_blue_*.png | 32 | blue ink slime, water drop shape on top, calm eyes |
| Board/dice_face_1..6.png, dice_roll_*.png | 32 | cream wooden die with purple pips (6 faces + 4 frames rolling) |
| Board/tile_[battle, elite, shop, rest, event, chest, boss, start].png | 32 | rounded square board tile with colored border and icon: crossed swords, skull, coin, heart, question mark, chest, crown, star |
| Board/tile_*_visited.png | 32 | mesma casa em cinza dessaturado |
| UI/Intents/intent_[attack, defend, buff, debuff, special, summon].png | 16 | sword, shield, green up arrow, purple down arrow, yellow exclamation, plus sign with ink drop |

## Lote 2 · M7 a M9 (interface e fim da fase 1)

| Arquivo | Tamanho | Descrição |
|---|---|---|
| Enemies/Phase1/slime_inkwell_*.png | 32 | elite: black ink slime wearing a glass inkwell as a helmet |
| Bosses/king_slime_*.png | 64 | huge green ink slime wearing a golden cardboard crown, leaf cape |
| Enemies/Phase1/slime_mini_*.png | 32 | tiny green slime (16 px de corpo dentro de 32) |
| UI/Status/status_[shield, burn, weak, exposed, strength, stun, thorns, regen].png | 16 | ícones simples e legíveis |
| UI/Elements/element_[fire, plant, water, neutral].png | 16 | chama, folha, gota, círculo |
| UI/Cards/card_frame_[neutral, fire, plant, water].png | 48x64 | moldura de carta de papel com borda da cor do elemento, faixa para nome e custo |
| UI/Cards/card_rarity_[common, uncommon, rare].png | 16 | gema cinza, azul, dourada |
| UI/energy_orb.png, energy_orb_empty.png | 24 | orbe de tinta dourada |
| UI/bar_hp_*.png, bar_shield_*.png | 9-slice | barra de vida vermelha, escudo azul |
| UI/coin.png, ink_coin.png | 16 | moeda dourada; gota de tinta roxa com brilho |
| UI/panel_9slice.png, button_9slice_[normal, hover, pressed].png | 9-slice | painel de papel creme com borda de madeira |
| VFX/vfx_[slash, fire, leaf, water, shield, heal, burn, ink_splash].png | 32 | 5 a 6 quadros cada |
| Backgrounds/bg_phase1_battle.png, bg_phase1_board.png | 640x360 | floresta de feltro verde, árvores de papel recortado, luz suave |
| Cards/Art/card_[nome].png | 32 | ilustração de cada carta das seções "Iniciais" e "Comuns" do GDD |
| Relics/relic_[nome].png | 16 | ícone de cada relíquia comum |

## Lote 3 · M11 (fase 2)

| Arquivo | Tamanho | Descrição |
|---|---|---|
| Enemies/Phase2/checker_black_*.png | 32 | black checkers piece creature with tiny flame on top |
| Enemies/Phase2/checker_white_*.png | 32 | white checkers piece creature with water drop on top |
| Enemies/Phase2/living_die_*.png | 32 | angry living red die with tiny legs; animação de rolar mostrando o número |
| Enemies/Phase2/crowned_checker_*.png | 32 | elite: stacked checkers king with leaf crown |
| Bosses/giant_hourglass_*.png | 64 | giant wooden hourglass with glowing sand eyes, sand pouring; animação de contagem |
| Backgrounds/bg_phase2_*.png | 640x360 | deserto de areia de ampulheta, céu ocre, ampulhetas quebradas |

## Lote 4 · M12 (fase 3)

| Arquivo | Tamanho | Descrição |
|---|---|---|
| Enemies/Phase3/shadow_pawn_*.png | 32 | dark ebony chess pawn with purple glowing eyes |
| Enemies/Phase3/shadow_knight_*.png | 32 | dark chess knight with flame mane |
| Enemies/Phase3/shadow_bishop_*.png | 32 | dark chess bishop with water halo |
| Enemies/Phase3/shadow_rook_*.png | 32 | elite: dark chess rook with vines |
| Bosses/the_player_*.png | 96 | giant pale hand in a dark purple sleeve, ink dripping from fingertips; etapa 2 com olho no centro da palma e aura da cor do elemento atual |
| Cards/Art/card_curse_[ink_smear, stuck_thread].png | 32 | mancha borrada; fio preso |
| Backgrounds/bg_phase3_*.png | 640x360 | salão de mármore xadrez, vitrais roxos |

## Lote 5 · M13 a M15 (heróis, cartas, relíquias, menus)

| Arquivo | Tamanho | Descrição |
|---|---|---|
| Characters/Rook/rook_*.png | 32 | wooden chess rook hero, blue banner, tower shield |
| Characters/Knight/knight_*.png | 32 | wooden chess knight hero, orange mane, lance |
| Characters/Bishop/bishop_*.png | 32 | wooden chess bishop hero, green sash, glowing staff |
| Characters/*/portrait_*.png | 64 | retratos para a seleção de herói |
| Cards/Art/card_[nome].png | 32 | cartas incomuns, raras e exclusivas restantes |
| Relics/relic_[nome].png | 16 | relíquias de chefe |
| UI/logo.png | 256x96 | logo "Xeque de Tinta" em letras de madeira com tinta escorrendo |
| Backgrounds/bg_title.png | 640x360 | tabuleiro visto de cima com peças paradas e manchas de tinta |
| UI/Font | | fonte pixel gerada no PixelLab ou fonte livre (Press Start 2P, m5x7) |

## Áudio (M16)

O PixelLab não gera som. Opções, em ordem de preferência:
1. **Bibliotecas livres CC0** (Kenney.nl, OpenGameArt com licença CC0): efeitos de UI, dado, moedas, golpes.
2. **ElevenLabs** (MCP oficial, plano pago para uso comercial): efeitos específicos (tinta respingando, ampulheta, mão gigante).
3. **Música:** faixas CC0/CC-BY (creditar nos créditos) ou gerador com plano comercial. Registrar a licença de cada arquivo em `Assets/_Project/Audio/LICENSES.md`.

Lista de faixas e efeitos: seção 13 do GDD.
