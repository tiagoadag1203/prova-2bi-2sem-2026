# Licenças de áudio · Xeque de Tinta

Todo o áudio do jogo é **original**, sintetizado (chiptune) pelo script `Tools/gen_audio.py`, que usa só a biblioteca padrão do Python. Nenhum arquivo de terceiros é usado.

| Arquivo | Origem | Licença |
|---|---|---|
| `Sfx/sfx_*.wav` (15 efeitos: dado, passo, carta, golpe, golpe eficaz, escudo, cura, queimadura, inimigo morre, herói ferido, moedas, clique, vitória, derrota, invocação) | Gerados por `Tools/gen_audio.py` | CC0 (domínio público), obra dos autores do projeto |
| `Music/music_board.wav` (título, tabuleiro e tela final) | Gerado por `Tools/gen_audio.py` | CC0, obra dos autores do projeto |
| `Music/music_battle.wav` (batalhas e Rei Slime) | Gerado por `Tools/gen_audio.py` | CC0, obra dos autores do projeto |

Por que síntese própria: o PixelLab só gera som e música no plano nível 2 ou superior (o projeto usa o nível 1), e o `ASSETS.md` pede nunca usar áudio sem licença clara. Para regenerar ou ajustar: `python3 Tools/gen_audio.py` e depois `Xeque > M7 > Preencher AudioLibrary` no Unity.

## Fonte da interface

`Assets/_Project/Art/UI/Fonts/PressStart2P-Regular.ttf` · Press Start 2P, © 2012 The Press Start 2P Project Authors · SIL Open Font License 1.1 (texto em `Art/UI/Fonts/OFL.txt`).
