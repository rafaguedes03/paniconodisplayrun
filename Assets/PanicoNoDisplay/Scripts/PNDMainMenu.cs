using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Tela de início: arte de fundo (art/menu/menu_background), os 4 botões recortados da arte
    /// (Jogar / Como Jogar / Créditos / Sair) com efeito ao passar o mouse e o quadro do TOP 5.
    ///
    /// As posições estão em pixels da arte original (1672 x 941), então batem com a imagem
    /// em qualquer resolução.
    /// </summary>
    public class PNDMainMenu : MonoBehaviour
    {
        const string Art = "art/menu/";

        // Coluna dos botões (pixels da arte) — igual ao script ArtSource/menu/make_menu.py
        const float ButtonX = 68f, ButtonW = 468f, ButtonH = 82f, ButtonGap = 13f, ButtonTop = 440f;
        const float ButtonPad = 26f; // margem transparente das imagens (espaço do brilho)

        // Instagram (embaixo dos botões)
        const string Instagram = "@rafaguedes.js";

        // Quadro do TOP 5 (acima do logo, no canto inferior direito)
        const float RankX = 1345f, RankY = 465f, RankW = 310f, RankH = 330f;

        static readonly PNDStyle InstaBgStyle = PNDStyle.Flat(new Color(0f, 0f, 0f, 0.6f), 6);

        public PNDGame game;
        PNDArtButton playButton, howToButton, creditsButton, quitButton;
        GameObject buttons, ranking;

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
            // ---------- Fundo (a arte, sem os botões) ----------
            var bg = PNDUI.Full(PNDUI.Rect("Background", root)).gameObject.AddComponent<Image>();
            bg.sprite = PND.Spr(Art + "menu_background");
            bg.raycastTarget = true;

            // ---------- Botões ----------
            var col = PNDUI.Full(PNDUI.Rect("Buttons", root));
            buttons = col.gameObject;
            playButton = MakeButton(col, 0, "jogar");
            howToButton = MakeButton(col, 1, "comojogar");
            creditsButton = MakeButton(col, 2, "creditos");
            quitButton = MakeButton(col, 3, "sair");

            // Setas ↑ ↓ circulam pelos botões
            var list = new Selectable[] { playButton, howToButton, creditsButton, quitButton };
            for (int i = 0; i < list.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = list[(i - 1 + list.Length) % list.Length],
                    selectOnDown = list[(i + 1) % list.Length],
                };
                list[i].navigation = nav;
            }

            playButton.onClick.AddListener(OnPlayPressed);
            howToButton.onClick.AddListener(OnHowToPressed);
            creditsButton.onClick.AddListener(OnCreditsPressed);
            quitButton.onClick.AddListener(OnQuitPressed);

            // ---------- Instagram ----------
            var insta = PNDUI.Rect("Instagram", root);
            // Preso pela esquerda; a largura acompanha o texto
            insta.anchorMin = insta.anchorMax = new Vector2(74f / PNDUI.ArtW, 1f - 869f / PNDUI.ArtH);
            insta.pivot = new Vector2(0f, 0.5f);
            insta.anchoredPosition = Vector2.zero;
            // Fundo preto semitransparente para facilitar a leitura
            var instaBg = insta.gameObject.AddComponent<Image>();
            instaBg.sprite = InstaBgStyle.Sprite;
            instaBg.type = Image.Type.Sliced;
            instaBg.raycastTarget = false;
            var ih = PNDUI.HBox(insta, 8f, TextAnchor.MiddleLeft);
            ih.padding = new RectOffset(8, 12, 4, 4);
            var fit = insta.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var iconRt = PNDUI.Rect("Icon", insta);
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = PND.Spr(Art + "instagram");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PNDUI.Layout(icon, 20, 20, 20, 20);
            PNDUI.Label("Handle", insta, Instagram, 14, Color.white, TextAlignmentOptions.MidlineLeft, 4, Color.black);

            // ---------- TOP 5 (lido do arquivo toda vez que o menu abre) ----------
            var board = PNDScores.BuildMenuBoard(root);
            PNDUI.Art(board, RankX, RankY, RankW, RankH);
            ranking = board.gameObject;
        }

        PNDArtButton MakeButton(RectTransform parent, int index, string id)
        {
            var b = PNDArtButton.Create("Btn_" + id, parent, Art + "btn_" + id + "_normal", Art + "btn_" + id + "_hover",
                ButtonW, ButtonH, ButtonPad);
            float y = ButtonTop + index * (ButtonH + ButtonGap);
            PNDUI.Art((RectTransform)b.transform, ButtonX, y, ButtonW, ButtonH);
            return b;
        }

        void Start()
        {
            // O "Jogar" começa destacado (igual à arte) e o teclado já funciona.
            // Se o mouse tirar o destaque, a primeira seta volta a selecionar o "Jogar".
            PNDUI.Select(playButton);
            game.DefaultSelectable = playButton;
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
            SetMenuVisible(false);
            var manual = PNDHowToPlay.Create(transform, game);
            manual.closed += () => { SetMenuVisible(true); PNDUI.Select(null); };
        }

        void OnCreditsPressed()
        {
            // Os créditos abrem por cima da tela de início (os botões ficam escurecidos atrás)
            SetButtonsInteractable(false);
            var credits = PNDCredits.Create(transform);
            credits.closed += () => { SetButtonsInteractable(true); PNDUI.Select(null); };
        }

        void OnQuitPressed()
        {
            // Integrado ao jogo principal: volta ao laboratório. Na versão independente: fecha o jogo.
            game.ExitToMainGame();
        }

        void SetMenuVisible(bool visible)
        {
            buttons.SetActive(visible);
            ranking.SetActive(visible);
        }

        void SetButtonsInteractable(bool on)
        {
            playButton.interactable = howToButton.interactable = creditsButton.interactable = quitButton.interactable = on;
        }
    }
}
