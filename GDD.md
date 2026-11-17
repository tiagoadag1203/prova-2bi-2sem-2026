# Xeque de Tinta · GDD v1.1 (versão enxuta)

Autores: Márcio Dias de Lima Filho e Tiago Augusto Dal'Acqua Gonçalves. Base: GDD v0.1 "Tabuleiro Mágico" (outubro de 2026, em `Docs/`). Esta versão define um jogo **curto e completo, com uma fase só**. Se este arquivo e o PDF divergirem, vale este arquivo. A arte está em `ASSETS.md`.

Todos os números são ponto de partida e ficam em ScriptableObjects, para o balanceamento ser ajustado sem mexer em código.

---

## 1. Visão geral

| Item | Valor |
|---|---|
| Gênero | Roguelite de tabuleiro + batalha em turnos com cartas |
| Plataforma | PC (Windows e Mac), Unity 6 2D URP |
| Visual | Pixel art 32x32 (chefe 64x64), câmera fixa, cores vivas sobre fundo roxo escuro. Cenário: um tabuleiro de xadrez gigante manchado de tinta mágica roxa e verde, de madeira, à luz de uma vela |
| Duração da partida | 10 a 15 minutos (1 fase) |
| Referências | Slay the Spire (cartas, intenções), Mario Party (dado e casas) |
| Controles | Mouse. Atalhos: Espaço rola o dado ou encerra o turno, Esc pausa |

**Fase:** "O Tabuleiro Manchado".

**História:** o jogo acontece sobre um tabuleiro de xadrez gigante. Tinta mágica foi derramada sobre ele e "afogou" as peças, que viraram slimes. O Rei Slime é o próprio rei do xadrez tomado pela tinta. O peão, a peça mais fraca, é a única que sobrou para limpar o tabuleiro e percorre as casas até derrotar o Rei Slime. Na primeira vez que o tabuleiro abre, uma caixa curta conta isso antes de liberar o dado; o título mostra a premissa numa linha: "A tinta mágica afogou as peças. Só o peão pode salvar o tabuleiro."

**Final:** ao vencer o Rei Slime, a tinta seca, o peão comemora e aparece a tela de vitória com créditos curtos.

## 2. Fluxo

```
Título → Tabuleiro ⇄ (Batalha / Loja / Descanso / Evento / Baú) → Rei Slime → Vitória
                         morte em qualquer batalha → Derrota → Título
```

**Loop:** rolar o dado e andar → escolher o caminho nas bifurcações → resolver a casa onde parou → recompensa → repetir até o Chefe.

Sem save: cada partida começa do zero. Sem progressão entre partidas.

## 3. Tabuleiro

- 12 casas ligadas em grafo com **2 bifurcações** que voltam a se juntar antes do chefe. O mapa inteiro fica visível desde o início.
- Rola 1d6 e anda essa quantidade. Nas bifurcações, escolhe o caminho clicando na seta.
- Só a casa onde o peão **para** é resolvida. Ao passar pela casa do Chefe, para nela.
- Casa resolvida fica cinza; parar numa casa cinza não faz nada.
- Layout em um asset `BoardLayout` (casas, tipo, posição, ligações).

| Casa | Qtd. | Efeito |
|---|---|---|
| Início | 1 | Nada |
| Batalha | 5 | 1 a 3 slimes |
| Loja | 1 | Ver seção 7 |
| Descanso | 1 | Curar 30% da vida máxima ou melhorar 1 carta |
| Evento | 2 | Ver seção 8 |
| Baú | 1 | 30 moedas e 1 relíquia aleatória |
| Chefe | 1 | Rei Slime |

## 4. Batalha

- Herói à esquerda, até 3 inimigos à direita. Estados: Início, TurnoJogador, TurnoInimigo, Vitória, Derrota.
- **Turno do jogador:** escudo do herói zera, recebe 3 de energia, compra 5 cartas, joga cartas clicando nelas (e no alvo, se precisar). Ao encerrar o turno, a mão vai para o descarte.
- **Baralho vazio:** embaralha o descarte e continua comprando.
- **Turno dos inimigos:** escudo dos inimigos zera, cada inimigo executa a intenção mostrada (da esquerda para a direita) e escolhe a intenção do próximo turno.
- **Intenções (ícone sobre o inimigo):** Ataque (com o número do dano), Defesa, Buff, Debuff, Invocar.
- **Elementos:** Fogo vence Planta, Planta vence Água, Água vence Fogo. Ataque contra elemento fraco causa +50% e mostra "Eficaz!". Neutro não tem vantagem.
- **Cálculo do dano:** base + Força → ×1,5 se eficaz → ×0,75 se atacante Fraco → arredonda para baixo → escudo absorve primeiro.
- **Esgotar:** a carta sai da batalha depois de usada.

### Status

| Status | Efeito |
|---|---|
| Escudo | Absorve dano. Zera no início do turno do dono |
| Queimadura (N) | No início do turno do alvo, perde 2 de vida. Dura N turnos; reaplicar soma turnos |
| Fraco (N) | Causa 25% menos dano por N turnos |
| Força (N) | +N de dano em cada ataque até o fim da batalha |
| Atordoado | Pula a próxima ação; a intenção fica para o turno seguinte |

## 5. Herói e cartas

**Peão:** 50 de vida. Deck inicial: 5 Golpe, 4 Defender, 1 Investida.

| Carta | Custo | Elem. | Efeito | Melhorada (Descanso) |
|---|---|---|---|---|
| Golpe | 1 | Neutro | 6 de dano | 9 |
| Defender | 1 | Neutro | 5 de escudo | 8 |
| Investida | 2 | Neutro | 10 de dano e Atordoa | 14 |
| Bola de Fogo | 1 | Fogo | 7 de dano | 10 |
| Brasa | 1 | Fogo | Queimadura 3 | Queimadura 5 |
| Raiz | 1 | Planta | 4 de dano e 4 de escudo | 6 e 6 |
| Casca Grossa | 1 | Planta | 8 de escudo | 11 |
| Onda | 2 | Água | 5 de dano em todos | 8 em todos |
| Respingo | 1 | Água | 4 de dano e Fraco 1 | 6 e Fraco 2 |
| Golpe Duplo | 1 | Neutro | 4 de dano duas vezes | 5 duas vezes |
| Seiva | 1 | Planta | Cura 6. Esgotar | Cura 9 |
| Golpe Pesado | 2 | Neutro | 14 de dano | 18 |

As 9 últimas formam o pool de recompensa (todas com a mesma chance).

## 6. Inimigos

| Inimigo | Elem. | Vida | Comportamento |
|---|---|---|---|
| Slime Verde (carrega uma torre afundada) | Planta | 18 | Aleatório: ataca 5 ou ganha 4 de escudo (nunca escudo duas vezes seguidas) |
| Slime Vermelho (carrega um cavalo) | Fogo | 14 | Ataca 7. A cada 3 turnos, em vez de atacar, aplica Queimadura 2 |
| Slime Azul (carrega um bispo) | Água | 22 | Ataca 4 e cura 3 do aliado mais ferido (ou a si mesmo) |
| **Rei Slime** (chefe: o rei do xadrez tomado pela tinta, com a coroa afundada na gosma) | Planta | 70 | Ciclo: Esmagar (12 de dano) → Dividir (invoca 2 Slimes Mirins, máx. 2 vivos) → Coroa Pesada (8 de escudo + 6 de dano). Abaixo de 50% de vida, ganha 2 de Força uma vez |
| Slime Mirim ("peão de tinta": slime verde em forma de peão derretido) | Planta | 8 | Ataca 3. Arte própria (`ink_pawn`), escala 0,75 |

**Grupos das 5 batalhas** (sorteados sem repetir): [Verde], [Vermelho], [Verde, Azul], [Vermelho, Verde], [Azul, Vermelho, Verde].

## 7. Moedas, recompensas, loja e relíquias

- **Depois de cada batalha:** 10 a 15 moedas + escolha de 1 entre 3 cartas do pool (ou pular).
- **Loja:** 3 cartas (50 cada), 1 relíquia (100), cura de 15 de vida (35), remover 1 carta do deck (60). Itens comprados somem.

| Relíquia | Efeito |
|---|---|
| Dado Viciado | Pode rolar o dado de novo 1 vez |
| Base de Chumbo | Começa toda batalha com 5 de escudo |
| Tinta Seca | Ao matar um inimigo, cura 3 |
| Pincel Afiado | Começa toda batalha com 1 de Força |
| Ampulheta de Bolso | No primeiro turno de cada batalha, +1 de energia |

## 8. Eventos

Painel com título, 2 a 3 linhas de texto e 2 botões. As 2 casas de Evento sorteiam 2 dos 3 eventos, sem repetir.

| Evento | Escolha A | Escolha B |
|---|---|---|
| Poça de Tinta | Beber: perde 6 de vida, ganha 1 carta aleatória melhorada | Seguir em frente |
| Dado Gigante | Rolar: 1-2 perde 8 de vida; 3-4 ganha 30 moedas; 5-6 ganha 1 relíquia | Não rolar |
| Fonte de Feltro | Cura 12 | Remove 1 carta do deck |

## 9. Telas e interface

- **Título:** logo, Jogar, Sair, e um controle de volume.
- **Tabuleiro (HUD):** vida, moedas, relíquias (ícones com tooltip), botão "Ver deck", botão de rolar o dado.
- **Batalha (HUD):** vida e escudo sobre cada personagem, status com ícone e número, orbe de energia, mão de cartas (hover aumenta), contador de compra e descarte, botão Encerrar Turno, intenção sobre cada inimigo, números de dano flutuando.
- **Painéis sobre o tabuleiro:** recompensa, loja, descanso, evento, baú, ver deck.
- **Pausa (Esc):** Continuar, Voltar ao título.
- **Vitória** (com créditos curtos) e **Derrota** (com "Tentar de novo").
- Fonte pixel livre (Press Start 2P ou m5x7, licença OFL/CC0).

## 10. Áudio

- **Músicas:** Tabuleiro/título e Batalha (uma serve ao chefe). CC0 ou CC-BY com crédito.
- **Efeitos:** dado, passo do peão, jogar carta, golpe, golpe eficaz, escudo, cura, queimadura, inimigo morre, herói recebe dano, moedas, botão de UI, vitória, derrota.
- Origem e licença de cada arquivo em `Assets/_Project/Audio/LICENSES.md`.

## 11. Arquitetura

- ScriptableObjects: `CardData`, `EnemyData`, `EncounterData`, `RelicData`, `EventData`, `BoardLayout`.
- Regras em C# puro (testável no EditMode); MonoBehaviours só ligam cena, input, animação e UI.
- `GameManager` persistente com o `RunState` (vida, deck, relíquias, moedas, casa atual, casas visitadas).
- Batalha como máquina de estados. Animation Event no quadro de impacto aplica o dano e dispara a animação de dano do alvo.
- Cenas: `Title`, `Board`, `Battle`, `End`.

## 12. Marcos de implementação

Um marco por vez, na ordem, seguindo a "definição de pronto" do CLAUDE.md. Marque `[x]` ao terminar.

- [x] **M0 · Base e arte.** Estrutura de pastas, pacotes, Pixel Perfect Camera. Gerar o **lote 1** do ASSETS.md, importar, fatiar, criar Animators e uma cena de teste com tudo animando.
- [x] **M1 · Dados.** ScriptableObjects da seção 11 e os assets de todas as cartas, inimigos, encontros, relíquias, eventos e o layout do tabuleiro. Testes de EditMode.
- [x] **M2 · Tabuleiro.** Cena Board: casas, dado, movimento, bifurcações, parada no chefe, casas cinza.
- [x] **M3 · Batalha.** Máquina de estados, energia, compra/descarte/reembaralho, cartas, dano, escudo, elementos, status, IA dos 3 slimes, intenções, Animation Events. Lógica com testes de EditMode.
- [x] **M4 · Partida.** GameManager e RunState, troca Board ⇄ Battle, recompensa de cartas e moedas, derrota volta ao título.
- [x] **M5 · Casas e relíquias.** Loja, Descanso, Evento, Baú e o efeito das 5 relíquias.
- [x] **M6 · Chefe e telas.** Gerar o **lote 2**. Rei Slime, Slime Mirim, telas de título, pausa, vitória e derrota.
- [x] **M7 · Áudio e acabamento.** Sons e músicas, números de dano, tremor de tela leve, flash de dano, transições.
- [x] **M8 · Fechamento.** Playtest automático da partida inteira, ajustes de balanceamento, builds para Mac e Windows, README atualizado.

## 13. Expansão (fora do escopo, só se sobrar tempo)

Ideias guardadas para uma versão maior: elite Slime Tinteiro, mais 2 fases (Deserto de Ampulheta com a Ampulheta Gigante, Castelo de Xadrez com O Jogador), heróis Torre, Cavalo e Bispo, save e Oficina com desbloqueios.
