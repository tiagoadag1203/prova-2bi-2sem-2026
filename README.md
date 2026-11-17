# Xeque de Tinta

Roguelite de tabuleiro com batalhas de cartas em turnos, em pixel art. Tinta mágica foi derramada sobre um tabuleiro de xadrez gigante e "afogou" as peças, que viraram slimes. O peão, a peça mais fraca, é o único que sobrou: ele atravessa O Tabuleiro Manchado casa por casa até derrotar o Rei Slime, o rei do xadrez tomado pela tinta.

- **Engine:** Unity 6 (6000.4), 2D URP
- **Plataformas:** Windows e macOS
- **Autores:** Márcio Dias de Lima Filho e Tiago Augusto Dal'Acqua Gonçalves
- **Projeto:** disciplina de Desenvolvimento de Jogos, 2º bimestre, 2º semestre de 2026

## Como jogar

Mouse para tudo. Atalhos: **Espaço** rola o dado ou encerra o turno, **Esc** pausa (ou cancela a escolha de alvo).

1. No **tabuleiro**, role o dado (1 a 3) e ande. Nas bifurcações, clique na casa para escolher o caminho. Só a casa onde o peão **para** é resolvida.
2. **Batalha:** 3 de energia e 5 cartas por turno. Clique na carta (e no inimigo, se pedir alvo). Acima de cada inimigo há a intenção do turno. Fogo vence Planta, Planta vence Água, Água vence Fogo (+50% de dano, "Eficaz!").
3. Vença batalhas para ganhar moedas e cartas; use Loja, Descanso, Eventos e Baús; junte relíquias.
4. Chegue ao Chefe e derrote o **Rei Slime**.

## Como abrir o projeto

1. Instale o Unity 6000.4.10f1 pelo Unity Hub (com o módulo Windows Build Support, se quiser compilar para Windows).
2. Clone o repositório e abra a pasta no Unity Hub (Add > Add project from disk).
3. Abra `Assets/_Project/Scenes/Title.unity` e aperte Play.

## Testes

`Window > General > Test Runner`: EditMode (regras, dados, balanceamento por simulação) e PlayMode (fluxos de cena e playtest automático da partida inteira). Para refazer assets: menu **Xeque** (dados, cenas, animações, UIArt, AudioLibrary).

## Documentos

| Arquivo | Conteúdo |
|---|---|
| [GDD.md](GDD.md) | Design do jogo e marcos de implementação |
| [ASSETS.md](ASSETS.md) | Lista de arte (PixelLab) e áudio |
| [PROGRESS.md](PROGRESS.md) | Diário de construção, decisões, pendências e o que testar jogando |
| [CLAUDE.md](CLAUDE.md) | Regras de desenvolvimento para o Claude Code |
| `Docs/screenshots/` | Capturas de cada marco |

## Créditos

Arte gerada no PixelLab. Áudio sintetizado por `Tools/gen_audio.py` (CC0). Fonte Press Start 2P (SIL OFL 1.1). Licenças em `Assets/_Project/Audio/LICENSES.md`.
