# Pânico no Display — versão Unity

Conversão completa do minijogo feito em Godot (pasta `PanicoNoDisplay/` na raiz do projeto) para Unity, rodando **dentro** do jogo principal.

## Como usar

O minijogo abre pela **Central de Minigames** do computador do laboratório
(`IniciarMinigameUnity`, em `Assets/MiniGames/PAC_CODE_Unity_Migracao`): aperte **E** perto do
computador e escolha **PANICO NO LABORATORIO**. Ao sair do minijogo, volta para esse menu.

Também dá para usar um gatilho próprio (cubo amarelo, tecla E):
**Tools → Pânico no Display → Colocar gatilho ao lado do jogador**. Não use os dois no mesmo lugar.

Se alguma arte aparecer borrada ou com tamanho errado: **Tools → Pânico no Display → Reimportar artes do minijogo**.

## Neste projeto (versão independente)

Aperte **Play**: `Assets/Standalone/PNDStandaloneBoot.cs` abre o minijogo sozinho, e o SAIR fecha o jogo.
**Tools → Pânico no Display → Gerar instalador (Windows)** cria o jogo e os instaladores na pasta `Build` (veja o README da raiz).
Para usar o minijogo dentro de outro projeto, copie só a pasta `Assets/PanicoNoDisplay`
(precisa de Input System e do *TMP Essentials*).

## Fluxo (modo infinito)

Menu (JOGAR / COMO JOGAR / SAIR + **TOP 5**) → cutscene (só na primeira vez, sempre pode pular) → corrida sem fim.

- Não há vidas, cronômetro, Display Points nem PC final: a corrida só acaba quando o jogador bate em um obstáculo.
- **Score** = distância percorrida (10 px = 1 ponto). A velocidade cresce com a distância (`StartSpeed`, `MaxSpeed` e `SpeedPerPixel` em `PNDLevel.cs`).
- Os obstáculos são sorteados pelo `PNDSpawner` (em `PNDLevel.cs`): nunca encostam um no outro, o espaço livre cresce junto com a velocidade (`GapMin`/`GapMax`) e o mesmo tipo não sai duas vezes seguidas.
- Ao perder, se o score entrar no TOP 5 o jogador digita o nome (ENTER salva, ESC pula). O nome não fica guardado para a próxima pessoa.
- O ranking fica em `panico_no_display_ranking.json` na pasta de dados do jogo (`Application.persistentDataPath`).
  **Tools → Pânico no Display → Zerar ranking (TOP 5)** apaga tudo; **Abrir pasta do ranking** mostra o arquivo.
- Na tela de fim: **ESPAÇO** joga de novo, **ESC** volta ao menu. **SAIR** no menu ou na pausa volta ao jogo principal, no mesmo lugar.
- Como não existe mais vitória, o minijogo não entrega dinheiro ao `PlayerWallet` (o `RegisterVictory` do launcher continua lá, sem uso).
- Para ver a cutscene de novo: apague a chave `PanicoNoDisplay_CutsceneVista` do PlayerPrefs.
- Dificuldade em escada: a velocidade vai de 200 a 560 e depois sobe devagar até 680; a partir de 1000 pontos os espaços encolhem (até 1,0–1,4 s) e os combos ficam mais frequentes (até 60%) e podem ter 3 obstáculos. Tudo em constantes no topo de `PNDLevel.cs` e do `PNDSpawner`.
- A cada +40 de velocidade aparece "MAIS RÁPIDO!" e a música acelera um pouco (até 12%: `MaxMusicPitch` em `PNDHud.cs`).
- Depois do 12º obstáculo aparecem combos pular + deslizar (`ComboChance` e `ComboGapMin/Max` no `PNDSpawner`).
- Tela de fim parada por 20 s volta sozinha ao menu (`AutoMenuSeconds`); na tela do nome, 45 s sem digitar pula para o ranking.
- Tela de início: arte em `Resources/PanicoNoDisplay/art/menu/`, gerada a partir de `ArtSource/menu/tela_inicio_original.png`
  pelo script `ArtSource/menu/make_menu.py` (recorta os botões, reconstrói o fundo atrás deles e desenha o estado de hover).
  Se mudar a arte, rode o script de novo (Python com Pillow, NumPy e OpenCV) e copie os PNGs para `art/menu/`.
  Os botões são `PNDArtButton` (troca para a moldura verde, cresce 4% e anda para a direita ao passar o mouse).
- Créditos: edite a lista `CREDITOS` no topo de `Scripts/PNDCredits.cs`.
  A foto é `art/menu/credits_photo.png` (gerada por `ArtSource/menu/make_extras.py`).
- Todas as telas (Como Jogar, Créditos, Pause, Fim de jogo, cutscene) usam as molduras 9-slice
  `art/menu/frame_*` (azul normal, verde neon no hover), pelo `PNDUI.FramePanel` e `PNDArtButton.CreateFramed`.
- Passar o mouse em qualquer botão toca `audio/sfx/button_sound.ogg` (`PND.PlayButtonSound`).
- Instagram do autor na tela de início: constante `Instagram` em `Scripts/PNDMainMenu.cs`.
- A música agora é `musica-corrida.ogg` (o MP3 original está em `Backup~/audio/`).
- A pasta `Backup~/` guarda os scripts da versão anterior (com vidas, tempo e PC final). O Unity ignora pastas terminadas em `~`.

## Arquivos

| Godot | Unity |
|---|---|
| `scripts/player/player.gd` | `Scripts/PNDPlayer.cs` |
| `scripts/obstacles/*.gd`, `scripts/environment/*.gd` | `Scripts/PNDObstacles.cs` |
| `scenes/main/main.tscn` e cenas dos obstáculos | `Scripts/PNDLevel.cs` (agora com obstáculos aleatórios: `PNDSpawner`) |
| `ui/hud.gd` + `hud.tscn` | `Scripts/PNDHud.cs` |
| `ui/main_menu.gd` | `Scripts/PNDMainMenu.cs` |
| `ui/how_to_play.gd` | `Scripts/PNDHowToPlay.cs` |
| `scenes/cutscene/intro_cutscene.gd` | `Scripts/PNDCutscene.cs` |
| AnimatedSprite2D | `Scripts/PNDSpriteAnimator.cs` |
| — (ranking TOP 5) | `Scripts/PNDScores.cs` |
| — (integração) | `Scripts/PanicoNoDisplayLauncher.cs`, `Scripts/IniciarPanicoNoDisplay.cs`, `Scripts/PNDGame.cs` |

O minijogo é montado por código numa cena criada na hora, então não há cena nem prefab para configurar.
Escala: 1 unidade = 1 pixel do original (640×360), então todos os números são os mesmos do Godot.
