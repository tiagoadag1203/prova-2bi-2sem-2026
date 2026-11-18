# Xeque de Tinta · 09 Roteiro de Apresentação

**Duração:** 8 a 10 minutos · **Formato:** slides curtos intercalados com demonstração ao vivo. **Preparação:** abra a build (ou o Editor na cena `Title`) com o volume audível e uma janela em 16:9. Deixe um segundo jogador de apoio avisando o tempo.

| # | Slide | Tempo | Fala (resumo) | Mostrar ao vivo |
|---|---|---|---|---|
| 1 | **Capa** | 0:20 | Nome, autores, disciplina, "Unity 6 + C#" | Tela de título do jogo |
| 2 | **Conceito e história** | 0:50 | "Um tabuleiro de xadrez gigante, tinta mágica afogou as peças. O Peão é o único que sobrou." Roguelite de tabuleiro + cartas | Título: premissa na tela; clique em **Jogar** e leia a caixa de história |
| 3 | **Personagens** | 0:50 | Peão, 3 slimes (torre, cavalo, bispo), peão de tinta e Rei Slime | Prancha de sprites (`Sprites/prancha_geral.png`) |
| 4 | **Jogabilidade 1: o tabuleiro** | 1:30 | 12 casas, dado de 1 a 3, bifurcações, só a casa onde para, parada no Chefe | **Rolar o dado** (Espaço), ver o Peão pular, **escolher um caminho** na bifurcação, casa ficando cinza |
| 5 | **Jogabilidade 2: a batalha** | 2:00 | Energia 3, mão de 5, intenções, elementos e "Eficaz!", escudo, descarte | Parar numa **Batalha**: ler a intenção, jogar um **Golpe**, um **Defender**, mostrar **Encerrar turno**; usar uma carta de Fogo no Slime Verde para o **"Eficaz!"** |
| 6 | **Skills (cartas) e status** | 1:00 | 12 cartas: área, duplo, Queimadura, Fraco, Atordoar, cura | **Ver deck** e, se possível, uma **Investida** atordoando; um ícone de status |
| 7 | **Casas, relíquias e eventos** | 0:50 | Loja, Descanso, Evento, Baú, 5 relíquias | Painéis de **Loja** e **Evento** (ou screenshots de reserva, `screenshots/tela_*.png`) |
| 8 | **Chefe** | 0:50 | Rei Slime: ciclo de 3 ações, invoca Mirins, fúria abaixo de 50% | Cena do Chefe (jogar até lá ou usar screenshot `m10_chefe.png`); mostrar o ícone de **Invocar** |
| 9 | **Arte e som** | 0:50 | Pixel art 32x32, paleta, animações, 15 efeitos e 2 músicas sintetizados por script, **transparência sobre o uso do PixelLab** | Sprite sheets em `Sprites/`; tocar 2 ou 3 `.wav` de `Sons/Efeitos` |
| 10 | **Engenharia e testes** | 0:50 | Lógica separada, ScriptableObjects, efeitos como classes, **176 + 37 testes**, robô que simula 400 partidas, ajuste d6 → d3 | Test Runner com os testes verdes (ou o documento 06) |
| 11 | **Inovações** | 0:40 | Dado + cartas, elementos, intenções, tema do xadrez | Slide de resumo (documento 07) |
| 12 | **Vitória, derrota e encerramento** | 0:30 | Como se ganha e como se perde; o que ficou pendente; perguntas | Tela de **Vitória** (ou de Derrota) e **Voltar ao título** |

**Total: cerca de 10 minutos.** Se estiver apertado, funda os slides 6 e 7 e corte o slide 11.

## Plano B (se algo falhar ao vivo)

- Se o Editor travar: use a **build** (`Builds/Mac` ou `Builds/Windows`).
- Se uma batalha demorar: mostre as telas prontas em `Documentos/` (manual em PDF) e `screenshots/`.
- Para chegar rápido ao Chefe na demonstração: use o Editor, abra `Board.unity` e jogue a batalha do Chefe pela casa final, ou mostre o screenshot `m10_chefe.png`.

## Dicas de fala

- Comece pela **ideia** ("o peão é a peça mais fraca e salva o tabuleiro"), depois mostre o jogo, depois a técnica.
- Ao mostrar o dado, diga por quê: *"testamos 400 partidas simuladas e mudamos de d6 para d3 para ter mais batalhas"*.
- Seja franco sobre a arte: *"gerada com IA no PixelLab, e nós escolhemos, ajustamos e montamos"*.
