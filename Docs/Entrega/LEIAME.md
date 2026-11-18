# Xeque de Tinta · Entrega final

Roguelite de tabuleiro com batalhas de cartas em turnos. Unity 6 (6000.4.10f1), C#. Autores: Márcio Dias de Lima Filho e Tiago Augusto Dal'Acqua Gonçalves.

> A tinta mágica afogou as peças. Só o peão pode salvar o tabuleiro.

## Conteúdo da pasta

| Pasta / arquivo | O que tem |
|---|---|
| `Documentos/` | 9 documentos em `.md` e `.pdf`: 01 Planejamento, 02 Documentação Técnica, **03 Manual do Jogo (encarte)**, 04 Design Visual, 05 Design Sonoro, 06 Testes e Ajustes, 07 Inovações, 08 Matriz de Requisitos, 09 Roteiro de Apresentação |
| `Sprites/` | Sprite sheets PNG (uma por personagem e animação, quadros em linha, fundo transparente) com `.json` (tamanho do quadro, quantidade, fps). Subpastas `Personagens/`, `Inimigos/`, `Chefe/`, `Tabuleiro/`, `Interface/`, `Cartas/`, `Reliquias/`, `Fundos/`, e a **`prancha_geral.png`** com tudo lado a lado |
| `Sons/` | `Efeitos/` (15 `.wav`) e `Musicas/` (2 `.wav`), 44,1 kHz, 16 bits, mais `LISTA_DE_SONS.md` |
| `Builds/Mac/XequeDeTinta.app` | Build de macOS |
| `Builds/Windows/XequeDeTinta.exe` | Build de Windows (a pasta inteira é necessária) |
| `Projeto_Unity.zip` | Projeto Unity completo (sem `Library/`, `Temp/`, `Logs/`, `Build/` e `UserSettings/`) |

## Como jogar

- **Mac:** abra `Builds/Mac/XequeDeTinta.app` (se o macOS bloquear por ser de fonte não identificada: botão direito → Abrir).
- **Windows:** execute `Builds/Windows/XequeDeTinta.exe` mantendo a pasta `XequeDeTinta_Data` ao lado.
- **Controles:** mouse; **Espaço** rola o dado e encerra o turno; **Esc** pausa. O manual completo está em `Documentos/03_Manual_do_Jogo.pdf`.

## Como abrir o projeto

1. Descompacte `Projeto_Unity.zip`.
2. No Unity Hub: *Add project from disk* e escolha a pasta. Use o **Unity 6000.4.10f1**.
3. Abra `Assets/_Project/Scenes/Title.unity` e aperte Play.
4. Testes: *Window > General > Test Runner* (EditMode: 176, PlayMode: 37).
5. Builds: menu *Xeque > Build > Mac* ou *Windows*.

## Observações

- A arte foi **gerada no PixelLab (IA)** e selecionada e ajustada pela equipe (documento 04, seção 7).
- Os sons foram **sintetizados por script Python próprio** (`Tools/gen_audio.py`), licença CC0 (documento 05).
- A build do Windows compila sem erros, mas **não foi executada** (sem máquina Windows).
- Pendências e a matriz de requisitos: `Documentos/08_Matriz_de_Requisitos.pdf`.
