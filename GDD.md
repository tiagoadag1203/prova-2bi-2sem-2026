# Xeque de Tinta · GDD v1.0 (jogo completo)

Autores: Márcio Dias de Lima Filho e Tiago Augusto Dal'Acqua Gonçalves. Base: GDD v0.1 "Tabuleiro Mágico" (outubro de 2026). Esta versão completa o design para o jogo inteiro e organiza a implementação em marcos para o Claude Code. Se este arquivo e o PDF divergirem, vale este arquivo. A arte de todos os itens está em `ASSETS.md`.

Todos os números são ponto de partida. Ficam em ScriptableObjects para o balanceamento ser ajustado sem mexer em código.

---

## 1. Visão geral

| Item | Valor |
|---|---|
| Gênero | Roguelite de tabuleiro + batalha em turnos com cartas |
| Plataforma | PC (Windows; Mac como bônus), Unity 6 2D |
| Visual | Pixel art 32x32 (chefes 64x64), câmera fixa, cores vivas sobre fundo roxo escuro |
| Duração da partida | 20 a 30 minutos (3 fases) |
| Referências | Slay the Spire (cartas, intenções), Mario Party (dado e casas) |
| Controles | Mouse. Atalhos: Espaço rola o dado / encerra o turno, 1 a 5 escolhem carta, Esc pausa |

**História:** as peças de um jogo de tabuleiro vivo. Um jogador misterioso mexe as peças, e as manchas de tinta mágica derramadas viraram monstros. O peão quer chegar ao fim do tabuleiro e se libertar. O chefe final é o próprio Jogador, a mão que controla o jogo.

**Final:** ao vencer O Jogador, a mão se abre, o peão sai do tabuleiro e aparece uma tela de créditos curta. A tinta ganha vai para a Oficina.

---

## 2. Fluxo do jogo

```
Título → Seleção de herói → [Fase 1 → Fase 2 → Fase 3] → Vitória final / Derrota → Resumo → Título
                                       ↑ cada fase: Tabuleiro ⇄ Batalha / Loja / Descanso / Evento / Baú
```

**Loop da fase:** rolar o dado e andar → escolher caminho nas bifurcações → resolver a casa onde parou → (batalha) → recompensa → repetir até a casa do Chefe. Vencer o chefe: cura 50% da vida que falta, escolhe 1 de 3 relíquias de chefe e vai para a próxima fase.

**Morte:** a partida acaba. Tela de resumo (fase alcançada, inimigos derrotados, cartas, relíquias) e a tinta ganha é guardada.

---

## 3. Tabuleiro

- Grafo de casas com bifurcações que voltam a se juntar antes do chefe. Rola 1d6 e anda essa quantidade. Nas bifurcações, escolhe o caminho clicando na seta.
- Só a casa onde o peão **para** é resolvida. Casas por onde ele só passa não fazem nada.
- Ao passar pela casa do Chefe, o peão para nela.
- Casa resolvida fica cinza. Parar numa casa cinza não faz nada.
- Se não houver mais caminho à frente (só cinza), o peão anda direto até o Chefe.
- O mapa inteiro fica visível desde o início, com o tipo de cada casa à mostra.

### Casas por fase

| Casa | Fase 1 (12) | Fase 2 (14) | Fase 3 (15) |
|---|---|---|---|
| Início | 1 | 1 | 1 |
| Batalha | 4 | 5 | 5 |
| Elite | 1 | 1 | 2 |
| Loja | 1 | 1 | 1 |
| Descanso | 1 | 1 | 2 |
| Evento | 2 | 2 | 2 |
| Baú | 1 | 2 | 1 |
| Chefe | 1 | 1 | 1 |

Layouts ficam em assets `BoardLayout` (lista de casas, tipo, posição na tela e ligações). O Claude Code desenha um layout por fase seguindo a tabela, com 2 bifurcações e a última casa antes do chefe sempre sendo Descanso ou Loja na fase 3.

| Casa | Efeito |
|---|---|
| Batalha | 1 a 3 inimigos comuns da fase |
| Elite | Elite da fase. Recompensa inclui relíquia |
| Loja | Ver seção 7 |
| Descanso | Curar 30% da vida máxima ou melhorar 1 carta |
| Evento | Texto curto com escolha (seção 8) |
| Baú | 60%: 25 a 40 moedas; 40%: relíquia comum |
| Chefe | Batalha final da fase |

---

## 4. Batalha

- Herói à esquerda, inimigos à direita (até 3). Estados: Início, TurnoJogador, TurnoInimigo, Vitória, Derrota.
- **Turno do jogador:** escudo do herói zera, recebe 3 de energia, compra 5 cartas, joga quantas quiser. Ao encerrar, a mão vai para o descarte.
- **Baralho vazio:** embaralha o descarte e continua comprando.
- **Turno dos inimigos:** escudo dos inimigos zera, cada inimigo executa a intenção mostrada (da esquerda para a direita) e escolhe a intenção do próximo turno.
- **Intenções (ícones):** Ataque (mostra o dano já calculado com Força e Fraco), Defesa, Buff, Debuff, Especial, Invocar.
- **Elementos:** Fogo vence Planta, Planta vence Água, Água vence Fogo. Ataque contra elemento fraco causa +50%. Neutro não tem vantagem nem fraqueza. Quando o golpe for eficaz, aparece "Eficaz!" sobre o alvo.
- **Ordem do dano:** base + Força → ×1,5 se elemento eficaz → ×1,5 se alvo Exposto → ×0,75 se atacante Fraco → arredonda para baixo → escudo absorve primeiro.
- Ao começar a batalha, o deck é embaralhado. Cartas com **Esgotar** saem da batalha ao serem usadas. **Poder** fica ativo até o fim da batalha.
- Velocidade de animação ajustável nas opções (1x, 1,5x, 2x).

### Status

| Status | Efeito |
|---|---|
| Escudo | Absorve dano. Zera no início do turno do dono |
| Queimadura (N) | No início do turno do alvo, perde 2 de vida. Dura N turnos; reaplicar soma turnos |
| Fraco (N) | Causa 25% menos dano por N turnos |
| Exposto (N) | Recebe 50% mais dano por N turnos |
| Força (N) | +N de dano em cada ataque até o fim da batalha |
| Atordoado | Pula a próxima ação. A intenção mostrada fica para o turno seguinte |
| Espinhos (N) | Quando recebe ataque, causa N ao atacante. Dura até o fim da batalha |
| Regeneração (N) | Cura N no fim do turno e reduz N em 1 |

---

## 5. Heróis

| Herói | Vida | Desbloqueio | Passiva | Carta exclusiva |
|---|---|---|---|---|
| Peão | 50 | Inicial | **Persistente:** ao vencer um chefe, +5 de vida máxima | Investida |
| Torre | 62 | 150 de tinta | **Muralha:** começa toda batalha com 6 de escudo | Bastião |
| Cavalo | 45 | 200 de tinta | **Salto em L:** ao rolar o dado, pode somar ou subtrair 1 | Salto |
| Bispo | 46 | 250 de tinta | **Bênção:** o Descanso cura 50% em vez de 30% | Luz Diagonal |

**Decks iniciais (10 cartas):**
- Peão: 5 Golpe, 4 Defender, 1 Investida.
- Torre: 4 Golpe, 5 Defender, 1 Bastião.
- Cavalo: 5 Golpe, 4 Defender, 1 Salto.
- Bispo: 4 Golpe, 4 Defender, 1 Luz Diagonal, 1 Seiva.

---

## 6. Cartas

Formato: custo · elemento · efeito (versão melhorada). N = Neutro, F = Fogo, P = Planta, A = Água.

### Iniciais e exclusivas

| Carta | Custo | Elem. | Efeito | Melhorada |
|---|---|---|---|---|
| Golpe | 1 | N | 6 de dano | 9 de dano |
| Defender | 1 | N | 5 de escudo | 8 de escudo |
| Investida | 2 | N | 10 de dano e Atordoa | 14 de dano |
| Bastião | 2 | N | 10 de escudo que não zera no próximo turno | 14 |
| Salto | 1 | N | 7 de dano que ignora escudo | 10 |
| Luz Diagonal | 1 | N | Cura 4 e aplica Fraco 1 em todos | Cura 6, Fraco 2 |

### Comuns (recompensa)

| Carta | Custo | Elem. | Efeito | Melhorada |
|---|---|---|---|---|
| Bola de Fogo | 1 | F | 7 de dano | 10 |
| Faísca | 0 | F | 3 de dano | 5 |
| Brasa | 1 | F | Queimadura 3 | Queimadura 5 |
| Raiz | 1 | P | 4 de dano e 4 de escudo | 6 e 6 |
| Casca Grossa | 1 | P | 8 de escudo | 11 |
| Onda | 2 | A | 5 de dano em todos | 8 em todos |
| Respingo | 1 | A | 4 de dano e Fraco 1 | 6 e Fraco 2 |
| Golpe Duplo | 1 | N | 4 de dano duas vezes | 5 duas vezes |
| Recuar | 1 | N | 6 de escudo e compra 1 | 8 e compra 1 |
| Avanço | 1 | N | 5 de dano e compra 1 | 8 e compra 1 |

### Incomuns

| Carta | Custo | Elem. | Efeito | Melhorada |
|---|---|---|---|---|
| Labareda | 2 | F | 12 de dano e Queimadura 2 | 16 e Queimadura 3 |
| Espinhos | 1 | P | Ganha Espinhos 3 (Poder) | Espinhos 5 |
| Seiva | 1 | P | Cura 5. Esgotar | Cura 8 |
| Muralha de Galhos | 2 | P | 14 de escudo | 18 |
| Maré | 2 | A | 8 de escudo e cura 4 | 11 e 6 |
| Correnteza | 1 | A | Exposto 2 | Exposto 3 |
| Golpe Pesado | 2 | N | 14 de dano | 18 |
| Fôlego | 0 | N | +1 de energia. Esgotar | +2 |
| Rolagem da Sorte | 1 | N | Rola 1d6 e causa 2× o resultado | 3× o resultado |

### Raras

| Carta | Custo | Elem. | Efeito | Melhorada |
|---|---|---|---|---|
| Fênix de Papel | 2 | F | 10 de dano; se matar, +2 de energia | 14 |
| Floresta Viva | 2 | P | Poder: ganha 3 de escudo no início de cada turno | 4 |
| Tsunami | 3 | A | 15 de dano em todos | 20 |
| Tempestade de Tinta | 3 | N | 8 de dano em todos, duas vezes | 10 duas vezes |
| Promoção | 1 | N | Poder: ganha 2 de Força. Esgotar | 3 de Força |

### Maldições (injogáveis, ocupam a mão)

| Carta | Origem | Efeito |
|---|---|---|
| Tinta Borrada | Chefe O Jogador | Injogável. Some da batalha ao ser descartada |
| Fio Preso | Evento "A Mão Estendida" | Injogável. Permanente no deck (pode ser removida na Loja) |

**Recompensa de cartas:** 3 opções do pool do herói (comuns e incomuns e raras de todos os elementos + exclusivas não entram). Chances: batalha 65/30/5 (comum/incomum/rara), elite 40/45/15, chefe só raras.

---

## 7. Moedas, loja e recompensas

- **Moedas por vitória:** comum 10 a 15, elite 25 a 35, chefe 60.
- **Loja:** 5 cartas (preço por raridade 45 / 70 / 110), 2 relíquias comuns (130), cura de 15 de vida (40), remover 1 carta do deck (75, uma vez por loja). Itens comprados somem da prateleira.
- **Recompensa após batalha:** moedas + escolha de 1 entre 3 cartas (ou pular). Elite: + 1 relíquia comum aleatória. Chefe: escolha de 1 entre 3 relíquias de chefe.

### Relíquias

| Relíquia | Raridade | Efeito |
|---|---|---|
| Dado Viciado | Comum | Pode rolar o dado de novo 1 vez por fase |
| Base de Chumbo | Comum | Começa toda batalha com 5 de escudo |
| Tinta Seca | Comum | Ao matar um inimigo, cura 3 |
| Pincel Afiado | Comum | Começa toda batalha com 1 de Força |
| Saquinho de Feltro | Comum | +25% de moedas |
| Ampulheta de Bolso | Comum | No primeiro turno de cada batalha, +1 de energia |
| Carta Marcada | Comum | No primeiro turno de cada batalha, compra 2 cartas a mais |
| Lasca de Brasa | Comum | Suas Queimaduras duram 1 turno a mais |
| Musgo Macio | Comum | Ao fim de cada batalha, cura 4 |
| Gota de Orvalho | Comum | Cartas de Água causam +2 de dano |
| Pedra de Afiar | Comum | Ao chegar numa Loja, melhora 1 carta aleatória |
| Coroa de Tinta | Chefe | +1 de energia por turno |
| Tabuleiro de Mestre | Chefe | Compra 6 cartas por turno |
| Peça Extra | Chefe | Uma vez por partida, ao morrer, volta com 30% da vida |
| Pena do Escriba | Chefe | Cartas de custo 0 também compram 1 carta |
| Verniz Antigo | Chefe | Começa toda batalha com Espinhos 3 |

---

## 8. Eventos

Cada evento: título, 2 a 3 linhas de texto, 2 escolhas com resultado visível. Cada evento aparece no máximo uma vez por partida.

| Evento | Fases | Escolha A | Escolha B |
|---|---|---|---|
| Poça de Tinta | 1, 2, 3 | Beber: perde 6 de vida, ganha 1 carta rara aleatória | Seguir em frente |
| Dado Gigante | 1, 2, 3 | Rolar: 1-2 perde 8 de vida; 3-4 ganha 35 moedas; 5-6 ganha relíquia comum | Não rolar |
| Fonte de Feltro | 1, 2 | Cura 12 | Remove 1 carta do deck |
| Peça Caída | 1, 2 | Ajudar: perde 5 de vida, ganha relíquia comum | Roubar: +40 moedas, próxima batalha começa com Fraco 2 |
| Mercador de Sombras | 2, 3 | Trocar 1 carta por outra aleatória de raridade acima | Ignorar |
| Ampulheta Rachada | 2 | Pagar 30 moedas: melhora 2 cartas aleatórias | Ignorar |
| Espelho de Verniz | 2, 3 | Duplicar 1 carta do deck, perde 8 de vida | Ignorar |
| A Mão Estendida | 3 | Aceitar: ganha 1 relíquia de chefe e a maldição Fio Preso | Recusar: cura 8 |

---

## 9. Inimigos

Comportamento descrito como padrão de intenções. "Aleatório" usa o RNG da partida com semente.

### Fase 1 · Floresta de Feltro (fundo: feltro verde, árvores de papel)

| Inimigo | Elem. | Vida | Comportamento |
|---|---|---|---|
| Slime Verde | P | 18 | Aleatório: ataca 5 ou ganha 4 de escudo (nunca escudo duas vezes seguidas) |
| Slime Vermelho | F | 14 | Ataca 7. A cada 3 turnos, em vez de atacar, aplica Queimadura 2 |
| Slime Azul | A | 22 | Ataca 4 e cura 3 do aliado mais ferido (ou a si mesmo) |
| **Elite:** Slime Tinteiro | N | 46 | Alterna: Jato de Tinta (8 de dano + Fraco 1) e Absorver (ganha 2 de Força) |
| **Chefe:** Rei Slime | P | 75 | Ciclo: Esmagar (12) → Dividir (invoca 2 Slimes Mirins, 8 de vida, ataque 3; máx. 2 vivos) → Coroa Pesada (10 de escudo + 6 de dano). Abaixo de 50% de vida, ganha 2 de Força uma vez |

Grupos de batalha da fase 1: [Verde], [Vermelho], [Verde, Azul], [Vermelho, Verde], [Azul, Vermelho], [Verde, Verde, Vermelho].

### Fase 2 · Deserto de Ampulheta (fundo: dunas de areia de ampulheta, céu ocre)

| Inimigo | Elem. | Vida | Comportamento |
|---|---|---|---|
| Dama Preta | F | 26 | Ataca 8. Se estiver sozinha, ganha 2 de Força em vez de atacar 1 vez |
| Dama Branca | A | 30 | Alterna: dá 6 de escudo a todos os inimigos / ataca 6 |
| Dado Vivo | N | 24 | No início do turno rola 1d6 (mostrado no ícone): ataca 2× o valor. Se tirar 6, ataca e ganha 5 de escudo |
| **Elite:** Dama Coroada | P | 72 | Alterna: Pulo Duplo (7 duas vezes) e Coroação (cura 10 + 2 de Força) |
| **Chefe:** Ampulheta Gigante | N | 130 | Contador visível de 3 turnos. No fim do contador: Areia do Tempo (25 de dano) e o contador reinicia. Nos outros turnos alterna: Areia nos Olhos (6 de dano + Fraco 1) e Grão a Grão (invoca 1 Dado Vivo, máx. 2). Se receber 30 ou mais de dano em um turno do jogador, o contador volta para 3 |

Grupos: [Dama Preta, Dama Branca], [Dado Vivo, Dado Vivo], [Dama Branca, Dado Vivo], [Dama Preta, Dado Vivo, Dama Branca], [Dama Preta].

### Fase 3 · Castelo de Xadrez (fundo: salão de mármore xadrez, vitrais roxos)

| Inimigo | Elem. | Vida | Comportamento |
|---|---|---|---|
| Peão Sombrio | N | 30 | Ataca 9 e ganha 1 de Força todo turno |
| Cavalo Sombrio | F | 34 | Alterna: Salto (12 de dano que ignora escudo) / 8 de escudo |
| Bispo Sombrio | A | 32 | Alterna: cura 8 do aliado mais ferido / aplica Exposto 2 no herói |
| **Elite:** Torre Sombria | P | 92 | Alterna: Fortificar (16 de escudo) e Avalanche (18 de dano) |
| **Chefe:** O Jogador (a Mão) | N | 190 | **Etapa 1 (até 50%):** ciclo Peteleco (14) → Mover Peça (invoca 1 Peão Sombrio, máx. 2) → Embaralhar (põe 2 Tinta Borrada no descarte do herói). **Etapa 2 (abaixo de 50%):** remove os escudos, a cada turno troca de elemento (mostrado acima dela), ataca 9 duas vezes; a cada 4 turnos, Xeque-mate (30 de dano, intenção avisada 1 turno antes) |

Grupos: [Peão Sombrio, Peão Sombrio], [Cavalo Sombrio, Bispo Sombrio], [Peão Sombrio, Cavalo Sombrio], [Bispo Sombrio, Peão Sombrio, Peão Sombrio], [Cavalo Sombrio, Cavalo Sombrio].

---

## 10. Progressão entre partidas (Oficina)

- **Tinta ganha por partida:** 2 por batalha vencida, 6 por elite, 20 por chefe, +30 se vencer o jogo.
- **Oficina** (menu): gasta tinta em desbloqueios permanentes.

| Desbloqueio | Custo |
|---|---|
| Herói Torre | 150 |
| Herói Cavalo | 200 |
| Herói Bispo | 250 |
| Pacote de cartas "Chamas" (Fênix de Papel, Labareda entram no pool) | 60 |
| Pacote de cartas "Marés" (Tsunami, Maré entram no pool) | 60 |
| Pacote de cartas "Bosque" (Floresta Viva, Espinhos entram no pool) | 60 |
| Relíquias avançadas (Pedra de Afiar, Verniz Antigo, Pena do Escriba entram no pool) | 100 |

Cartas e relíquias de pacotes bloqueados não aparecem até serem compradas.

---

## 11. Telas e interface

- **Título:** logo, Jogar, Oficina, Opções, Sair. Fundo animado com peças paradas no tabuleiro.
- **Seleção de herói:** retrato, vida, passiva, deck inicial. Bloqueados aparecem em silhueta com o custo.
- **Tabuleiro (HUD):** vida, moedas, número da fase, relíquias (com tooltip), botão de ver deck, botão de rolar o dado.
- **Batalha (HUD):** vida e escudo sobre cada personagem, status com ícone e número (tooltip), energia, mão de cartas em leque (hover aumenta, tooltip de palavras-chave), pilha de compra e descarte com contador, botão Encerrar Turno, intenção sobre cada inimigo.
- **Recompensa, Loja, Descanso, Evento, Baú:** painéis sobre o tabuleiro.
- **Ver deck:** grade com todas as cartas.
- **Pausa:** Continuar, Opções, Desistir da partida.
- **Opções:** volume de música, volume de efeitos, tela cheia, resolução, velocidade de animação.
- **Fim de partida:** vitória ou derrota, resumo e tinta ganha.
- **Créditos:** após a vitória final e acessíveis pelo título.
- Fonte pixel legível (gerar no PixelLab ou usar fonte livre como "Press Start 2P" / "m5x7").

## 12. Save

- **Progresso permanente** (tinta, desbloqueios, opções): JSON em `Application.persistentDataPath`.
- **Partida em andamento:** salva automaticamente ao entrar em cada casa do tabuleiro (não no meio da batalha). Ao abrir o jogo com partida salva, o botão Jogar vira "Continuar" e "Nova partida".
- Semente da partida salva junto, para o RNG continuar igual.

## 13. Áudio

- **Músicas (loop):** Título, Fase 1, Fase 2, Fase 3, Chefe, Chefe final, Vitória (curta), Derrota (curta).
- **Efeitos:** dado rolando, dado parando, passo do peão, casa resolvida, comprar carta, jogar carta, sem energia, golpe, golpe eficaz, escudo, cura, queimadura, buff, debuff, inimigo morre, herói recebe dano, moedas, relíquia, abrir baú, botão de UI, vitória da batalha, invocar.
- Mixer com grupos Música e Efeitos ligados às opções.

## 14. Arquitetura (resumo)

- ScriptableObjects: `HeroData`, `CardData`, `EnemyData`, `EncounterData`, `RelicData`, `EventData`, `BoardLayout`, `PhaseData`, `UnlockData`.
- Lógica de regras em C# puro (testável), apresentação em MonoBehaviours.
- `RunState` (estado da partida) guardado pelo `GameManager` persistente. `MetaState` (tinta e desbloqueios) separado.
- Batalha como máquina de estados. Animation Event no quadro de impacto aplica o dano e dispara a animação de "dano" do alvo.
- Cenas: `Boot`, `Title`, `Board`, `Battle`, `Credits`.

---

## 15. Marcos de implementação

Um marco por vez, na ordem. Um marco só termina com a "definição de pronto" do CLAUDE.md cumprida. Marque `[x]` ao terminar.

### Bloco A · Fase 1 jogável
- [ ] **M0 · Base e arte de referência.** Estrutura de pastas, pacotes, Pixel Perfect Camera. Gerar no PixelLab o **lote 1** do ASSETS.md (peão + 3 slimes + dado + casas + ícones de intenção) e parar para aprovação. Depois: importar, fatiar, Animators, cena de teste com todos animando.
- [ ] **M1 · Dados.** ScriptableObjects da seção 14 e assets de toda a fase 1 (cartas comuns, iniciais, 3 slimes, relíquias comuns). Testes de EditMode.
- [ ] **M2 · Tabuleiro.** Cena Board, layout da fase 1, dado, movimento, bifurcações, parada no Chefe, casas cinza.
- [ ] **M3 · Estado da partida.** GameManager + RunState, troca Board ⇄ Battle, morte reinicia.
- [ ] **M4 · Núcleo da batalha.** Máquina de estados, energia, compra/descarte/reembaralho, cartas, escudo, ordem do dano, elementos, status. Lógica com testes de EditMode.
- [ ] **M5 · Inimigos e intenções.** IA dos 3 slimes, intenções, Atordoado, Animation Events.
- [ ] **M6 · Recompensas.** Moedas, 1 de 3 cartas, relíquias aplicadas na batalha e no tabuleiro.
- [ ] **M7 · HUD da batalha e do tabuleiro.** Seção 11, tooltips, ver deck, telas de vitória e derrota da batalha.
- [ ] **M8 · Casas restantes.** Elite (Slime Tinteiro), Loja, Descanso (com melhoria de carta), Evento (os de fase 1), Baú.
- [ ] **M9 · Rei Slime.** Arte (lote 2), IA, recompensa de chefe, transição para a fase 2 (por enquanto uma tela "Fase 2 em breve").
- [ ] **M10 · Playtest da fase 1.** Ajustes de balanceamento. Build de teste para Mac.

### Bloco B · Jogo completo
- [ ] **M11 · Fase 2.** Arte (lote 3), inimigos, elite, Ampulheta Gigante, layout, eventos, fundo.
- [ ] **M12 · Fase 3.** Arte (lote 4), inimigos, elite, O Jogador em 2 etapas, maldições, layout, eventos, fundo, final e créditos.
- [ ] **M13 · Pool completo.** Todas as cartas incomuns e raras e todas as relíquias de chefe.
- [ ] **M14 · Menus e save.** Título, seleção de herói, pausa, opções, fim de partida, save permanente e de partida.
- [ ] **M15 · Oficina e heróis.** Tinta, desbloqueios, Torre, Cavalo e Bispo (arte lote 5) com passivas e cartas.
- [ ] **M16 · Áudio.** Mixer, músicas e efeitos da seção 13.
- [ ] **M17 · Polimento.** Juice (tremor de tela, números de dano, flash, partículas), transições, legibilidade, acessibilidade (cores + ícones de elemento).
- [ ] **M18 · Fechamento.** Balanceamento das 3 fases, build Windows e Mac, playtest completo do início ao fim.
