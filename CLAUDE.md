# CLAUDE.md · Xeque de Tinta

Roguelite de tabuleiro com batalhas de cartas em turnos, feito em Unity 2D. O design completo está em `GDD.md`: leia antes de qualquer tarefa.

## Ambiente

- Unity 6 (LTS), projeto 2D com URP. Desenvolvimento no macOS; o alvo final é Windows (build pelo módulo Windows Build Support).
- O Editor fica aberto durante as sessões e é controlado pelo MCP da Unity (`unity-editor-mcp`). A arte nova vem do MCP do PixelLab (`pixellab`).
- Pacotes esperados: 2D (Sprite, Tilemap, Animation, Pixel Perfect), Input System, TextMeshPro, Test Framework. Peça autorização antes de instalar qualquer outro pacote.

## Como trabalhar

1. Trabalhe em **um marco por vez**, na ordem da seção 15 do `GDD.md`. Não adiante funcionalidades de marcos futuros.
2. Antes de editar, faça um plano curto e **liste os arquivos** que vai criar ou alterar.
3. Uma sessão do Claude por vez neste projeto.
4. Não altere `ProjectSettings/`, pacotes ou configurações de build sem avisar o motivo.
5. Quando faltar uma regra no GDD, **não invente em silêncio**: escolha algo simples, registre no GDD com a marca "(provisório)" e me avise no resumo.
6. Ao terminar um marco, marque `[x]` no GDD e escreva um resumo curto: o que foi feito, o que testar à mão e as decisões tomadas.

## Definição de pronto

Um marco só está pronto quando:
- o projeto compila sem erros e o Console não tem erros nem warnings novos;
- os testes de EditMode e PlayMode do marco passam;
- você tirou um screenshot da cena mostrando o resultado;
- você listou para mim o que preciso testar jogando (o que você não consegue sentir: ritmo, legibilidade, feedback).

Nunca diga que terminou sem cumprir esses itens.

## Arquitetura

- **Lógica separada da apresentação.** Regras de jogo (deck, energia, dano, escudo, elementos, efeitos, intenções, movimento no tabuleiro) em classes C# puras, sem `MonoBehaviour`, testáveis no EditMode. `MonoBehaviour`s só fazem a ponte com cena, input, animação e UI.
- **Dados em ScriptableObjects:** os da seção 14 do GDD (`HeroData`, `CardData`, `EnemyData`, `EncounterData`, `RelicData`, `EventData`, `BoardLayout`, `PhaseData`, `UnlockData`). Conteúdo novo deve ser possível sem mexer em código. Nada de números de balanceamento fixos no código.
- **Batalha:** máquina de estados com Início, TurnoJogador, TurnoInimigo, Vitória e Derrota.
- **GameManager** persistente (`DontDestroyOnLoad`) guarda o estado da partida: vida, deck, relíquias, moedas, casa atual e casas visitadas. Cenas: `Boot`, `Title`, `Board`, `Battle`, `Credits`.
- **Dano sincronizado com a animação:** Animation Event no quadro de impacto do ataque dispara a aplicação do dano e a animação de "dano" do alvo.
- **Efeitos de carta e de relíquia** como classes pequenas e combináveis (por exemplo `DealDamage`, `GainShield`, `ApplyBurn`), não um `switch` gigante.
- Namespace raiz `XequeDeTinta` (`XequeDeTinta.Battle`, `XequeDeTinta.Board`, `XequeDeTinta.Data`, `XequeDeTinta.UI`).
- Input pelo Input System, interação por clique/mouse.

## Pastas

```
Assets/
  _Project/
    Art/
      _Reference/             sprites antigos (só referência, fora do jogo)
      Characters/  Enemies/  Bosses/  Board/  Cards/  Relics/
      UI/  VFX/  Backgrounds/  (nomes conforme ASSETS.md)
    Animations/               clips e Animator Controllers
    Audio/
    Data/                     assets de ScriptableObject (Cards, Enemies, Relics, Tiles, Boards)
    Prefabs/
    Scenes/                   Boot, Board, Battle
    Scripts/
      Core/  Board/  Battle/  Data/  UI/
    Tests/
      EditMode/  PlayMode/
```

Nomes de arquivo em inglês e `snake_case` para arte (`slime_red_attack.png`), `PascalCase` para scripts e assets de dados (`Card_Strike.asset`). Textos exibidos ao jogador em português.

## Pixel art

- Sprites de 32x32. Em toda textura: Pixels Per Unit 32, Filter Mode Point (no filter), Compression None, Generate Mip Maps desligado.
- Sprite sheets fatiadas por grade (Sprite Mode Multiple, Grid by Cell Size 32x32, salvo se a sheet indicar outro tamanho). Pivot no centro inferior para personagens.
- Câmera fixa ortográfica com Pixel Perfect Camera, resolução de referência 640x360, PPU 32.
- Fundo roxo escuro, cores vivas. Chefes 64x64 (O Jogador 96x96) com PPU 32.

## Arte e áudio

- **Toda a arte do jogo é gerada no PixelLab**, seguindo `ASSETS.md` (regras de estilo, lotes, nomes de arquivo). Os sprites antigos em `Art/_Reference/` são só referência de estilo e não entram no jogo.
- Gere **um lote por vez**, no marco indicado. Para personagens: 3 variações, **pare e me mostre** para eu aprovar, e só então anime o escolhido.
- O Peão aprovado é a âncora de estilo de todo o resto.
- Antes de cada lote, confira o saldo (`get_balance`) e diga quantas gerações o lote vai gastar.
- Se o PixelLab falhar ou o resultado ficar fora do estilo, use um placeholder simples (retângulo colorido com o nome) e continue o código; anote o que ficou pendente.
- Áudio: siga a seção "Áudio" do `ASSETS.md` e registre a licença de cada arquivo em `Audio/LICENSES.md`. Nunca use música ou som sem licença clara.

## Testes

- EditMode para toda regra de jogo (custos, dano, escudo, fraqueza elemental +50%, compra e reembaralho, Queimadura, comportamento dos inimigos, movimento no tabuleiro, parada no Chefe).
- PlayMode para fluxos: entrar numa batalha a partir do tabuleiro, vencer, receber recompensa e voltar.
- Use uma semente de aleatoriedade injetável (dado, embaralhamento, IA dos inimigos) para os testes serem determinísticos.

## Git

- Eu faço commit antes de cada sessão. Não reescreva o histórico.
- `.gitignore` padrão da Unity (`Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `UserSettings/`).
