# Xeque de Tinta · 05 Design Sonoro

## 1. Como os sons foram produzidos

Todos os efeitos e músicas são **originais e sintetizados pela equipe** (estilo chiptune), pelo script **`Tools/gen_audio.py`**, escrito em **Python** com apenas a biblioteca padrão. **Não são sintetizados em tempo de execução nem em C#**: o script gera arquivos `.wav` que o Unity importa como `AudioClip`s e que o `AudioManager` toca. Nenhum arquivo de terceiros é usado; a obra é dedicada ao domínio público (CC0) (`Assets/_Project/Audio/LICENSES.md`).

A síntese usa osciladores (quadrada, pulso 25%, triangular, dente de serra, seno e ruído), envelope de ataque e decaimento, glissandos (variação de frequência), ruído filtrado (passa-baixa) e mistura de várias camadas. A semente do ruído é fixa (`random.seed(7)`), portanto o resultado é **reproduzível**.

Por que síntese própria: o PixelLab só gera som e música em plano pago de nível 2 (o projeto usa o nível 1) e a regra do projeto é nunca usar áudio sem licença clara.

**Formatos:** o jogo usa os `.wav` em **22,05 kHz, 16 bits, mono** (`Assets/_Project/Audio`). Para a entrega, o mesmo script renderizou tudo em **44,1 kHz, 16 bits, mono** (`XDT_SR=44100 XDT_OUT=<pasta> python3 Tools/gen_audio.py`), em `Sons/Efeitos` e `Sons/Musicas`.

## 2. Efeitos sonoros (15)

| # | Arquivo (entrega) | Enum `Sfx` | Quando toca | Duração | Síntese |
|---|---|---|---|---|---|
| 1 | `01_dado_rolando.wav` | `Dice` | Ao rolar o dado no tabuleiro | 0,79 s | 9 estalos de madeira (ruído + triangular) que se espaçam |
| 2 | `02_passo_do_peao.wav` | `Step` | A cada pulo do Peão | 0,09 s | Quadrada descendente (180 → 90 Hz) |
| 3 | `03_jogar_carta.wav` | `Card` | Ao jogar uma carta | 0,11 s | Dois blips ascendentes |
| 4 | `04_golpe.wav` | `Hit` | Dano num inimigo | 0,16 s | Ruído + triangular grave |
| 5 | `05_golpe_eficaz.wav` | `HitEffective` | Dano com vantagem elemental ("Eficaz!") | 0,38 s | Golpe + arpejo brilhante (988, 1319, 1760 Hz) |
| 6 | `06_escudo.wav` | `Shield` | Ganhar escudo ou bloquear dano | 0,18 s | Duas senoides metálicas + estalo |
| 7 | `07_cura.wav` | `Heal` | Cura (Seiva, Tinta Seca, descanso) | 0,35 s | Arpejo ascendente em senoide (C-E-G-C) |
| 8 | `08_queimadura.wav` | `Burn` | Queimadura aplicada ou causando dano | 0,40 s | Ruído filtrado + serra ascendente |
| 9 | `09_inimigo_morre.wav` | `Die` | Um inimigo cai | 0,40 s | Quadrada descendente + ruído |
| 10 | `10_heroi_ferido.wav` | `Hurt` | O herói recebe dano | 0,24 s | Serra descendente + ruído |
| 11 | `11_moedas.wav` | `Coin` | Receber moedas (recompensa, baú) | 0,27 s | Dois tons agudos (1568 e 2093 Hz) |
| 12 | `12_botao_ui.wav` | `Click` | Qualquer botão da interface | 0,04 s | Quadrada curta (880 Hz) |
| 13 | `13_vitoria.wav` | `Win` | Vitória na batalha e tela de Vitória | 1,35 s | Arpejo C maior + acorde |
| 14 | `14_derrota.wav` | `Lose` | Derrota (herói cai, tela de Derrota) | 1,46 s | Quatro notas descendentes em triangular |
| 15 | `15_invocacao.wav` | `Summon` | O Rei Slime invoca os Mirins | 0,55 s | Serra e quadrada ascendentes |

## 3. Músicas (2, em loop)

| Arquivo | Toca em | Duração | Andamento e tom | Camadas |
|---|---|---|---|---|
| `musica_tabuleiro.wav` | Título, Tabuleiro e tela final | 40 s | 96 bpm, dó maior, 16 compassos (C Am F G) | Baixo triangular, melodia em pulso, arpejo suave |
| `musica_batalha.wav` | Batalha e Rei Slime | 27 s | 140 bpm, lá menor, 16 compassos (Am F G Em) | Baixo em colcheias, riff em pulso, percussão de ruído |

A música troca por cena: `AudioManager.TrackFor(scene)` devolve `battle` para `Battle` e `board` para as demais; se a faixa já toca, não reinicia.

## 4. Integração e mixagem

- **`AudioLibrary`** (`Resources/AudioLibrary.asset`) guarda os 15 clipes e as 2 músicas; o menu `Xeque > M7 > Preencher AudioLibrary` os liga.
- **`AudioManager`** é persistente: **1 fonte de música** em loop e um **pool de 8 fontes de efeitos** (`PlayOneShot`), que permite vários sons ao mesmo tempo sem cortar.
- **Volumes:** música **0,45**, efeitos **0,85**, fixos em código (`MusicVolume`, `SfxVolume`). Os `.wav` têm pico normalizado a 0,9 (efeitos) e 0,8 (músicas).
- **Mixer:** o projeto **não usa um `AudioMixer` do Unity**: a mixagem é feita por esses volumes relativos e pelo controle geral.
- **Controle de volume do jogador:** controle deslizante na tela de título, que ajusta `AudioListener.volume` (vale para tudo).
- Os botões de toda a interface tocam `Click` automaticamente (`UIFactory.CreateButton`).

## 5. Pendências sonoras

Os sons são simples, de síntese; o volume relativo entre música e efeitos e o cansaço dos efeitos repetidos (dado, golpe, clique) dependem de **escuta humana** e podem ser ajustados trocando os `.wav` ou os dois volumes em `AudioManager`.
