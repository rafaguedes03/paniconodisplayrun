using TMPro;
using UnityEngine;

namespace PanicoNoDisplay
{
    /// <summary>Menu principal: JOGAR / COMO JOGAR / SAIR e o quadro com o TOP 5.</summary>
    public class PNDMainMenu : MonoBehaviour
    {
        public PNDGame game;
        PNDButton playButton, howToButton, quitButton;
        GameObject menuBox, title, ranking;

        public static PNDMainMenu Create(Transform canvasRoot, PNDGame game)
        {
            var rt = PNDUI.Full(PNDUI.Rect("MainMenu", canvasRoot));
            var m = rt.gameObject.AddComponent<PNDMainMenu>();
            m.game = game;
            m.Build(rt);
            return m;
        }

        void Build(RectTransform root)
        {
            PNDUI.ColorRect("ColorRect", root, new Color(0.12549f, 0.164706f, 0.282353f, 1f));

            var t = PNDUI.Label("Title", root, "PÂNICO NO LABORATÓRIO", 36, new Color(1f, 0.8f, 0.2f),
                TextAlignmentOptions.Center, 6, Color.black);
            PNDUI.G(t.rectTransform, 0.5f, 0, 0.5f, 0, -250, 35, 250, 95);
            title = t.gameObject;

            // Botões à esquerda
            var box = PNDUI.G(PNDUI.Rect("VBoxContainer", root), 0, 0, 0, 0, 70, 135, 270, 305);
            PNDUI.VBox(box, 16f);
            menuBox = box.gameObject;

            playButton = PNDUI.Button("PlayButton", box, "JOGAR", 24);
            howToButton = PNDUI.Button("HowToButton", box, "COMO JOGAR", 24);
            quitButton = PNDUI.Button("QuitButton", box, "SAIR", 24);

            // TOP 5 à direita (lido do arquivo toda vez que o menu abre)
            var board = PNDScores.BuildBoard(root);
            PNDUI.G(board, 0, 0, 0, 0, 350, 125, 590, 275);
            ranking = board.gameObject;

            // Conecta os cliques dos botões às funções
            playButton.onClick.AddListener(OnPlayPressed);
            quitButton.onClick.AddListener(OnQuitPressed);
            howToButton.onClick.AddListener(OnHowToPressed);
        }

        void Start()
        {
            // Dá o foco automático para o botão "JOGAR", permitindo usar o teclado
            PNDUI.Select(playButton);
        }

        void OnPlayPressed()
        {
            // Ao apertar Jogar, inicia a cutscene de contexto antes da fase
            // (só na primeira vez; depois vai direto para a fase)
            game.OnPlayPressed();
        }

        void OnHowToPressed()
        {
            // Abre o manual "COMO JOGAR" por cima do menu
            menuBox.SetActive(false);
            title.SetActive(false); // Fundo só com a cor sólida, sem o nome do jogo
            ranking.SetActive(false);
            var manual = PNDHowToPlay.Create(transform, game);
            manual.closed += OnHowToClosed;
        }

        void OnHowToClosed()
        {
            menuBox.SetActive(true);
            title.SetActive(true);
            ranking.SetActive(true);
            PNDUI.Select(howToButton);
        }

        void OnQuitPressed()
        {
            // No Godot fechava o jogo; integrado, volta para o jogo principal
            game.ExitToMainGame();
        }
    }
}
