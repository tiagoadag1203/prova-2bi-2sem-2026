# CLAUDE.md · Xeque de Tinta

Roguelite de tabuleiro com batalhas de cartas em turnos, feito em Unity 2D. O design completo está em `GDD.md`: leia antes de qualquer tarefa.

## Ambiente

- Unity 6 (LTS), projeto 2D com URP. Desenvolvimento no macOS; o alvo final é Windows (build pelo módulo Windows Build Support).
- O Editor fica aberto durante as sessões e é controlado pelo MCP da Unity (`unity-editor-mcp`). A arte nova vem do MCP do PixelLab (`pixellab`).
- Pacotes esperados: 2D (Sprite, Tilemap, Animation, Pixel Perfect), Input System, TextMeshPro, Test Framework. Peça autorização antes de instalar qualquer outro pacote.

## Como trabalhar (modo autônomo)

O objetivo é construir o jogo inteiro, do M0 ao M8, **sem parar para pedir aprovação**.

1. Siga os marcos da seção 12 do `GDD.md` em ordem, um depois do outro, até o fim. Não adiante funcionalidades de marcos futuros.
2. **Não pare para perguntar.** Quando faltar uma regra ou houver dúvida de design, escolha a opção mais simples e coerente com o GDD, registre no GDD com a marca "(provisório)" e siga.
3. **Ao começar cada sessão, leia `PROGRESS.md`** e continue de onde parou. Ao terminar cada marco, atualize o `PROGRESS.md` (o que foi feito, decisões tomadas, o que o humano precisa testar jogando, pendências) e marque `[x]` no GDD.
4. **Commit e push ao fim de cada marco:** `git add -A && git commit -m "M<n>: <resumo>" && git push`. Se o push falhar, faça só o commit e anote no PROGRESS.md.
5. Se algo falhar 3 vezes seguidas (erro que não resolve, ferramenta fora do ar, geração ruim), use uma solução provisória (placeholder, versão simplificada), anote em "Pendências" no PROGRESS.md e siga para o próximo item.
6. **Só pare de verdade** se: o saldo do PixelLab acabar, algo exigir pagamento, conta ou senha, ou o projeto ficar sem compilar e você não conseguir recuperar nem voltando ao último commit.
7. Uma sessão do Claude por vez neste projeto. Não altere pacotes ou `ProjectSettings/` sem anotar o motivo no PROGRESS.md.
8. Para não estourar o contexto: não peça o dump completo de cenas grandes; consulte só o objeto que precisa.

## Definição de pronto

Um marco só está pronto quando:
- o projeto compila sem erros e o Console não tem erros nem warnings novos;
- os testes de EditMode e PlayMode do marco passam;
- você tirou um screenshot da cena mostrando o resultado;
- o PROGRESS.md lista o que o humano precisa testar jogando (o que você não consegue sentir: ritmo, legibilidade, feedback).

Nunca marque um marco como pronto sem cumprir esses itens.

## Arquitetura

- **Lógica separada da apresentação.** Regras de jogo (deck, energia, dano, escudo, elementos, efeitos, intenções, movimento no tabuleiro) em classes C# puras, sem `MonoBehaviour`, testáveis no EditMode. `MonoBehaviour`s só fazem a ponte com cena, input, animação e UI.
- **Dados em ScriptableObjects:** os da seção 11 do GDD (`CardData`, `EnemyData`, `EncounterData`, `RelicData`, `EventData`, `BoardLayout`). Conteúdo novo deve ser possível sem mexer em código. Nada de números de balanceamento fixos no código.
- **Batalha:** máquina de estados com Início, TurnoJogador, TurnoInimigo, Vitória e Derrota.
- **GameManager** persistente (`DontDestroyOnLoad`) guarda o estado da partida: vida, deck, relíquias, moedas, casa atual e casas visitadas. Cenas: `Title`, `Board`, `Battle`, `End`.
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
    Scenes/                   Title, Board, Battle, End
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
- Fundo roxo escuro, cores vivas. Chefe 64x64 com PPU 32.

## Arte e áudio

- **Toda a arte do jogo é gerada no PixelLab**, seguindo `ASSETS.md` (regras de estilo, lotes, nomes de arquivo). Os sprites antigos em `Art/_Reference/` são só referência de estilo e não entram no jogo.
- Gere **um lote por vez**, no marco indicado. Para personagens: gere 2 variações, **escolha você mesmo** a que melhor segue as regras de estilo do ASSETS.md (silhueta legível em 32 px, contorno, paleta, vista lateral, direção certa) e anime só a escolhida. Salve as rejeitadas em `Art/_Rejected/` para eu poder trocar depois.
- O Peão escolhido é a âncora de estilo de todo o resto: use-o como referência nos pedidos seguintes.
- Antes de cada lote, confira o saldo (`get_balance`) e anote no PROGRESS.md quanto foi gasto. Economize: não gere variações extras de itens pequenos (ícones, cartas, relíquias).
- Se o PixelLab falhar ou o resultado ficar fora do estilo, use um placeholder simples (retângulo colorido com o nome) e continue o código; anote o que ficou pendente.
- Áudio: siga a seção "Áudio" do `ASSETS.md` e registre a licença de cada arquivo em `Audio/LICENSES.md`. Nunca use música ou som sem licença clara.

## Testes

- EditMode para toda regra de jogo (custos, dano, escudo, fraqueza elemental +50%, compra e reembaralho, Queimadura, comportamento dos inimigos, movimento no tabuleiro, parada no Chefe).
- PlayMode para fluxos: entrar numa batalha a partir do tabuleiro, vencer, receber recompensa e voltar.
- Use uma semente de aleatoriedade injetável (dado, embaralhamento, IA dos inimigos) para os testes serem determinísticos.

## Git

- Commit e push ao fim de cada marco (ver "Como trabalhar"). Nunca reescreva o histórico nem use `push --force`.
- `.gitignore` padrão da Unity (`Library/`, `Temp/`, `Logs/`, `obj/`, `Build/`, `UserSettings/`).
