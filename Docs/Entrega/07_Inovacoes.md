# Xeque de Tinta · 07 Inovações

Texto pensado para a **apresentação**. O jogo mistura dois gêneros que raramente andam juntos e dá a cada peça um papel na história.

## 1. Tabuleiro com dado + batalha de cartas

A maior novidade é a **combinação de dois jogos em um**: um jogo de tabuleiro com **dado** (como *Mario Party*) que decide *onde* o jogador vai, e um **duelo de cartas em turnos** (como *Slay the Spire*) que decide *o que acontece* ali. O dado deixa de ser sorte pura: o mapa é visível desde o início, tem **bifurcações** em que o jogador escolhe o caminho (Baú ou batalha extra; Descanso ou batalha), e o Peão é **obrigado a parar no Chefe**. O jogador planeja a rota para chegar ao Rei Slime com o baralho e a vida certos.

- Movimentação nova para um jogo de cartas: **peão que pula casa a casa**, com escolha de caminho por clique.
- Casas **resolvidas ficam cinzas**: o tabuleiro vira um registro visual da partida.
- O dado de **1 a 3** foi calibrado por simulação (400 partidas) para a partida ter batalhas, lojas e eventos suficientes.

## 2. O ciclo de elementos como habilidade central

**Fogo vence Planta, Planta vence Água, Água vence Fogo**, com **+50% de dano** e o aviso dourado **"Eficaz!"**. Cada slime tem um elemento e o desenho mostra isso (folha, chama, gota), então o jogador decide *antes* de jogar qual carta usar. As 12 cartas têm **habilidades variadas**: ataque único, ataque em **área** (Onda), **dano duplo**, **dano e escudo juntos** (Raiz), **dano e Fraco** (Respingo), **Queimadura** que ignora escudo (Brasa), **cura que esgota** (Seiva) e **Atordoar** (Investida).

## 3. Intenções dos inimigos

Cada inimigo **mostra o que vai fazer** no próximo turno (ícone e número de dano). O jogador não adivinha: **calcula**. Isso transforma a batalha em um quebra-cabeça tático (atacar ou se proteger? atordoar quem?).

## 4. Atordoar como skill de controle

A **Investida** atordoa o alvo: ele **perde a ação** e **guarda a intenção** para o turno seguinte. É controle, não só dano, e funciona contra o golpe de 12 do Rei Slime.

## 5. Chefe com fases e invocação

O **Rei Slime** tem um **ciclo de três ações** legível (Esmagar → Dividir → Coroa Pesada), **invoca** Mirins (limitados a 2) e se **enfurece** abaixo de 50% da vida (+2 de Força). Como é um ciclo previsível mostrado pela intenção, a luta é um desafio de planejamento, não de sorte.

## 6. Relíquias que mudam as regras

Cinco relíquias alteram regras de forma diferente: uma no **tabuleiro** (Dado Viciado rola de novo), três no **início da batalha** (escudo, Força, energia extra) e uma na **morte de inimigos** (cura). Combinam com os baralhos (Pincel Afiado + Golpe Duplo, por exemplo).

## 7. O tema: um tabuleiro de xadrez tomado pela tinta

A identidade visual e a narrativa nascem do próprio tabuleiro: **a tinta mágica afogou as peças**. Torre, cavalo e bispo viraram os slimes Verde, Vermelho e Azul; o rei virou o chefe com a coroa afundada na gosma; o **peão**, a peça mais fraca, é o herói. A "tinta" também aparece no nome do jogo (*Xeque de Tinta*), nos efeitos (mortes que se desfazem em tinta) e nos fundos manchados de roxo e verde.

## 8. Engenharia pensada para evoluir

- **Conteúdo por dados** (ScriptableObjects): cartas, inimigos, relíquias e eventos novos sem tocar em código.
- **Efeitos como classes pequenas e combináveis** (`DealDamage`, `ApplyBurn`, `Stun`...).
- **Lógica separada da apresentação** e 214 testes automáticos; um **robô** joga partidas inteiras para balancear.
- **Dano sincronizado com a animação** por Animation Event no quadro de impacto.
- **Áudio 100% original**, sintetizado por script, com licença CC0.

## 9. Pontos fortes para destacar na apresentação

1. Dois gêneros num só, com movimentação por dado e escolha de caminho.
2. Ciclo de elementos legível, com feedback visual ("Eficaz!") e sonoro.
3. Intenções claras: decisão tática, não adivinhação.
4. Identidade visual forte e coerente (xadrez + tinta).
5. Partida curta e completa, com início, meio e fim, e vitória e derrota.
