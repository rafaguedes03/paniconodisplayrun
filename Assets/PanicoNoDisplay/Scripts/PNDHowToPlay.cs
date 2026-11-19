using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Conversão de ui/how_to_play.gd — MANUAL "COMO JOGAR".
    /// Tela aberta pelo menu principal, com as abas OBJETIVO, CONTROLES, OBSTÁCULOS e PONTUAÇÃO.
    /// </summary>
    public class PNDHowToPlay : MonoBehaviour
    {
        public event Action closed;

        const string OBJETIVO_TEXTO = "Você é um discente que, sem querer, derramou café no servidor do laboratório. O curto-circuito se espalhou e o lab virou um caos!\n\nAgora só resta correr: o laboratório não tem fim e a corrida fica cada vez mais rápida.\n\n• Desvie dos obstáculos pulando e deslizando.\n• Quanto mais longe você chegar, maior o seu SCORE.\n• Não há vidas: bateu em um obstáculo, a corrida acaba!\n• Ficou entre os 5 melhores? Digite seu nome e entre para o TOP 5.";

        const string PONTUACAO_TEXTO = "O SCORE é a distância que você correu: quanto mais longe, mais pontos.\n\n• A velocidade aumenta aos poucos durante a corrida, então os pontos também sobem mais rápido... e sobra menos tempo para reagir!\n• O RECORDE atual aparece no canto superior direito durante a corrida.\n• Ao perder, se o seu score entrar no TOP 5, digite seu nome (até 10 letras) e aperte ENTER para salvar.\n• O TOP 5 fica na tela inicial. Tente bater o primeiro lugar!";

        // [tecla, ação]
        static readonly string[,] CONTROLES =
        {
            { "ESPAÇO  ou  SETA ↑", "Pular" },
            { "SETA ↓ (segurar)", "Deslizar (passa por baixo dos fios)" },
            { "P", "Pausar o jogo (ou o botão de pause na tela)" },
            { "M", "Ligar / desligar o som (ou o botão de som na tela)" },
            { "ESPAÇO  ou  ENTER", "Pular a introdução / jogar de novo" },
            { "ENTER", "Salvar seu nome no TOP 5" },
        };
        const string CONTROLES_DICA = "O personagem corre sozinho para a direita: você só precisa pular e deslizar na hora certa!";

        // [ícone, nome, descrição] — o ícone é a mesma imagem que a cena do obstáculo usa
        static readonly string[,] OBSTACULOS =
        {
            { "art/obstacles/a4_papers", "Papéis A4", "Escorregadios! Pule por cima." },
            { "art/obstacles/pencils", "Lápis", "Rolam no chão e fazem escorregar. Pule!" },
            { "art/obstacles/tangled_wires", "Fios embolados", "Fazem você tropeçar. Pule!" },
            { "art/obstacles/notebook_intact", "Notebook no chão", "Tropeço garantido. Pule!" },
            { "art/obstacles/water_cooler_1", "Bebedouro", "Cuidado com a poça d'água: pule ela!" },
            { "art/obstacles/cleaner_sweep_1", "Faxineira", "Dá vassouradas em quem chega perto. Pule por cima!" },
            { "art/obstacles/air_conditioner", "Ar-condicionado", "O vento deixa você mais lento enquanto estiver embaixo dele." },
            { "art/obstacles/hanging_wire_icon", "Fio desencapado", "Solta faíscas na altura da cabeça. Não dá para pular: DESLIZE por baixo!" },
        };

        // Cores do estilo da tela de início
        static readonly Color COR_TITULO = new Color(0.37f, 0.99f, 0.57f);   // verde neon
        static readonly Color COR_TEXTO = new Color(0.9f, 0.94f, 0.99f);
        static readonly Color COR_DESTAQUE = new Color(0.55f, 1f, 0.7f);
        static readonly Color COR_DICA = new Color(0.55f, 0.68f, 0.85f);

        PNDGame game;
        readonly List<GameObject> pages = new List<GameObject>();
        readonly List<PNDArtButton> tabButtons = new List<PNDArtButton>();
        int currentTab;
        PNDArtButton backButton;

        public static PNDHowToPlay Create(Transform parent, PNDGame game)
        {
            var rt = PNDUI.Full(PNDUI.Rect("HowToPlay", parent));
            var h = rt.gameObject.AddComponent<PNDHowToPlay>();
            h.game = game;
            h.Build(rt);
            return h;
        }

        void Build(RectTransform root)
        {
            // Fundo: a arte da tela de início escurecida (também bloqueia o mouse para o menu de trás)
            PNDUI.MenuBackdrop(root);

            var frame = PNDUI.FramePanel("Frame", root, true);
            PNDUI.G(frame.rectTransform, 0, 0, 1, 1, 12, 8, -12, -8);

            // Posições fixas (de cima para baixo): título, abas, conteúdo, rodapé
            var ft = frame.transform;
            var title = PNDUI.Label("Title", ft, "COMO JOGAR", 22, COR_TITULO, TextAlignmentOptions.Center, 5, Color.black);
            PNDUI.G(title.rectTransform, 0, 0, 1, 0, 16, 14, -16, 46);

            // ----- Abas (botões com a moldura do menu) -----
            var header = PNDUI.G(PNDUI.Rect("TabBar", ft), 0, 0, 1, 0, 16, 56, -16, 82);
            PNDUI.HBox(header, 12f, TextAnchor.MiddleCenter);

            var panelImg = PNDUI.FramePanel("Panel", ft, false);
            PNDUI.G(panelImg.rectTransform, 0, 0, 1, 1, 18, 92, -18, -44);

            AddTab(header, panelImg.transform, "OBJETIVO", p => BuildTexto(p, OBJETIVO_TEXTO));
            AddTab(header, panelImg.transform, "CONTROLES", BuildControles);
            AddTab(header, panelImg.transform, "OBSTÁCULOS", p => BuildLista(p, OBSTACULOS, "Bater em qualquer obstáculo encerra a corrida! Só o vento do ar-condicionado não derruba."));
            AddTab(header, panelImg.transform, "PONTUAÇÃO", p => BuildTexto(p, PONTUACAO_TEXTO));

            // ----- Rodapé -----
            var bottom = PNDUI.G(PNDUI.Rect("Bottom", ft), 0, 1, 1, 1, 16, -38, -16, -10);
            PNDUI.HBox(bottom, 16f, TextAnchor.MiddleCenter);

            PNDUI.Label("Hint", bottom, "← → trocar aba", 11, COR_DICA);

            backButton = PNDArtButton.CreateFramed("BackButton", bottom, "VOLTAR", 14);
            PNDUI.Layout(backButton, 110, 26, 110, 26);
            backButton.onClick.AddListener(Close);

            PNDUI.Label("Hint2", bottom, "ESC voltar", 11, COR_DICA);

            SetTab(0);
        }

        void Start()
        {
            // Nada selecionado: o VOLTAR fica azul até o mouse passar por cima
            PNDUI.Select(null);
            game.ConsumeArrows = true;
        }

        void OnDestroy()
        {
            if (game != null) game.ConsumeArrows = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
            {
                Close();
            }
            else if (kb.rightArrowKey.wasPressedThisFrame)
            {
                SetTab((currentTab + 1) % pages.Count);
            }
            else if (kb.leftArrowKey.wasPressedThisFrame)
            {
                SetTab((currentTab - 1 + pages.Count) % pages.Count);
            }
        }

        void Close()
        {
            if (game != null) game.ConsumeArrows = false;
            closed?.Invoke();
            Destroy(gameObject);
        }

        // ---------------- Abas ----------------

        void AddTab(RectTransform header, Transform panel, string title, Action<RectTransform> build)
        {
            int index = pages.Count;

            var page = PNDUI.Full(PNDUI.Rect("Page_" + title, panel));
            var pv = PNDUI.VBox(page, 6f);
            pv.padding = new RectOffset(10, 10, 10, 10);
            build(page);
            pages.Add(page.gameObject);

            var b = PNDArtButton.CreateFramed("Tab_" + title, header, title, 12);
            b.NoKeyboard(); // as abas trocam com ← →
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = b.label.GetPreferredValues(title).x + 30f;
            le.minHeight = le.preferredHeight = 24f;
            b.onClick.AddListener(() => SetTab(index));
            tabButtons.Add(b);
        }

        void SetTab(int i)
        {
            currentTab = i;
            for (int k = 0; k < pages.Count; k++)
            {
                pages[k].SetActive(k == i);
                tabButtons[k].locked = k == i; // a aba aberta fica verde
            }
        }

        void BuildTexto(RectTransform page, string texto)
        {
            var label = PNDUI.Label("Texto", page, texto, 12, COR_TEXTO, TextAlignmentOptions.TopLeft, 0, null, true);
            PNDUI.Layout(label, flexW: 1);
        }

        void BuildControles(RectTransform page)
        {
            var grid = PNDUI.Rect("Grid", page);
            PNDUI.VBox(grid, 6f);

            var chips = new List<RectTransform>();
            float maxW = 0f;
            for (int i = 0; i < CONTROLES.GetLength(0); i++)
            {
                var row = PNDUI.Rect("Row" + i, grid);
                PNDUI.HBox(row, 14f, TextAnchor.MiddleLeft);
                var chip = KeyChip(row, CONTROLES[i, 0], out float w);
                chips.Add(chip);
                maxW = Mathf.Max(maxW, w);
                PNDUI.Label("Action", row, CONTROLES[i, 1], 13, COR_TEXTO);
            }
            // As colunas do GridContainer têm a largura do maior item
            foreach (var c in chips)
            {
                var le = c.GetComponent<LayoutElement>();
                le.minWidth = le.preferredWidth = maxW;
            }

            var dica = PNDUI.Label("Dica", page, "DICA: " + CONTROLES_DICA, 12, COR_DESTAQUE, TextAlignmentOptions.TopLeft, 0, null, true);
            PNDUI.Layout(dica, flexW: 1);
        }

        void BuildLista(RectTransform page, string[,] items, string rodape)
        {
            var grid = PNDUI.Rect("Grid", page);
            PNDUI.VBox(grid, 4f);

            int count = items.GetLength(0);
            for (int i = 0; i < count; i += 2)
            {
                var pair = PNDUI.Rect("Row" + i, grid);
                var ph = PNDUI.HBox(pair, 12f, TextAnchor.UpperLeft);
                ph.childForceExpandWidth = true;
                for (int k = i; k < Mathf.Min(i + 2, count); k++)
                    ItemCell(pair, items[k, 0], items[k, 1], items[k, 2]);
                if (i + 1 >= count)
                {
                    // célula vazia para manter as duas colunas
                    var empty = PNDUI.Rect("Empty", pair);
                    PNDUI.Layout(empty, flexW: 1);
                }
            }

            if (!string.IsNullOrEmpty(rodape))
            {
                var r = PNDUI.Label("Rodape", page, "ATENÇÃO: " + rodape, 12, new Color(1f, 0.45f, 0.45f), TextAlignmentOptions.TopLeft, 0, null, true);
                PNDUI.Layout(r, flexW: 1);
            }
        }

        void ItemCell(RectTransform parent, string iconPath, string name, string desc)
        {
            var row = PNDUI.Rect("Item_" + name, parent);
            PNDUI.HBox(row, 8f, TextAnchor.UpperLeft);
            PNDUI.Layout(row, flexW: 1);

            var iconRt = PNDUI.Rect("Icon", row);
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = PND.Spr(iconPath);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            PNDUI.Layout(icon, 44, 34, 44, 34);

            var textBox = PNDUI.Rect("Text", row);
            var tv = PNDUI.VBox(textBox, 0f);
            PNDUI.Layout(textBox, flexW: 1);
            PNDUI.Label("Name", textBox, name, 13, COR_DESTAQUE);
            var d = PNDUI.Label("Desc", textBox, desc, 11, COR_TEXTO, TextAlignmentOptions.TopLeft, 0, null, true);
            PNDUI.Layout(d, minW: 180, flexW: 1);
        }

        // ---------------- Utilitários ----------------

        static readonly PNDStyle ChipStyle = new PNDStyle
        {
            bg = new Color(0.03f, 0.1f, 0.19f),
            radius = 4,
            border = new Color(0.33f, 0.55f, 0.8f),
            borderL = 1, borderT = 1, borderR = 1, borderB = 3,
        };

        RectTransform KeyChip(RectTransform parent, string text, out float width)
        {
            var box = PNDUI.StyledBox("KeyChip", parent, ChipStyle, false);
            var rt = box.rectTransform;
            var label = PNDUI.Label("Label", rt, text, 12, Color.white, TextAlignmentOptions.Center);
            // content_margin: 8 nas laterais, 2 em cima, 2 + borda de 3 embaixo
            PNDUI.G(label.rectTransform, 0, 0, 1, 1, 8, 2, -8, -4);
            Vector2 pref = label.GetPreferredValues(text);
            width = pref.x + 16f;
            PNDUI.Layout(box, width, pref.y + 6f, width, pref.y + 6f);
            return rt;
        }
    }
}
