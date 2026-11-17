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

        const string OBJETIVO_TEXTO = "Você é um discente que, sem querer, derramou café no servidor do laboratório. O curto-circuito se espalhou e o lab virou um caos!\n\nAgora só resta correr: o laboratório não tem fim e a corrida fica cada vez mais rápida.\n\n➜ Desvie dos obstáculos pulando e deslizando.\n➜ Quanto mais longe você chegar, maior o seu SCORE.\n➜ Não há vidas: bateu em um obstáculo, a corrida acaba!\n➜ Ficou entre os 5 melhores? Digite seu nome e entre para o TOP 5.";

        const string PONTUACAO_TEXTO = "O SCORE é a distância que você correu: quanto mais longe, mais pontos.\n\n➜ A velocidade aumenta aos poucos durante a corrida, então os pontos também sobem mais rápido... e sobra menos tempo para reagir!\n➜ O RECORDE atual aparece no canto superior direito durante a corrida.\n➜ Ao perder, se o seu score entrar no TOP 5, digite seu nome (até 10 letras) e aperte ENTER para salvar.\n➜ O TOP 5 fica na tela inicial. Tente bater o primeiro lugar!";

        // [tecla, ação]
        static readonly string[,] CONTROLES =
        {
            { "ESPAÇO  ou  SETA ↑", "Pular" },
            { "SETA ↓ (segurar)", "Deslizar (passa por baixo dos fios)" },
            { "P  ou  botão ⏸", "Pausar o jogo" },
            { "M  ou  botão 🔊", "Ligar / desligar o som" },
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

        static readonly Color COR_FUNDO = new Color(0.12549f, 0.164706f, 0.282353f, 1f); // mesma cor sólida do menu
        static readonly Color COR_TITULO = new Color(1f, 0.8f, 0.2f);
        static readonly Color COR_TEXTO = new Color(0.92f, 0.92f, 0.95f);
        static readonly Color COR_DESTAQUE = new Color(1f, 0.85f, 0.35f);

        static readonly PNDStyle TabSelected = new PNDStyle { bg = new Color(0.1f, 0.1f, 0.1f, 0.6f), radius = 3, radiusBottom = 0 };
        static readonly PNDStyle TabUnselected = new PNDStyle { bg = new Color(0.1f, 0.1f, 0.1f, 0.3f), radius = 3, radiusBottom = 0 };
        static readonly PNDStyle TabHover = new PNDStyle { bg = new Color(0.225f, 0.225f, 0.225f, 0.45f), radius = 3, radiusBottom = 0 };

        PNDGame game;
        readonly List<GameObject> pages = new List<GameObject>();
        readonly List<PNDButton> tabButtons = new List<PNDButton>();
        int currentTab;
        PNDButton backButton;

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
            // Bloqueia o mouse para o menu de trás
            PNDUI.ColorRect("Background", root, COR_FUNDO);

            var vbox = PNDUI.G(PNDUI.Rect("VBox", root), 0, 0, 1, 1, 14, 14, -14, -14);
            PNDUI.VBox(vbox, 6f);

            PNDUI.Label("Title", vbox, "COMO JOGAR", 22, COR_TITULO, TextAlignmentOptions.Center, 5, Color.black);

            // ----- TabContainer -----
            var tabs = PNDUI.Rect("TabContainer", vbox);
            PNDUI.VBox(tabs, 0f);
            PNDUI.Layout(tabs, flexW: 1, flexH: 1);

            var header = PNDUI.Rect("TabBar", tabs);
            PNDUI.HBox(header, 0f, TextAnchor.LowerLeft);
            header.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

            var panelImg = PNDUI.StyledBox("Panel", tabs, PNDUI.GodotPanel, false);
            PNDUI.Layout(panelImg, flexH: 1);

            AddTab(header, panelImg.transform, "OBJETIVO", p => BuildTexto(p, OBJETIVO_TEXTO));
            AddTab(header, panelImg.transform, "CONTROLES", BuildControles);
            AddTab(header, panelImg.transform, "OBSTÁCULOS", p => BuildLista(p, OBSTACULOS, "Bater em qualquer obstáculo encerra a corrida! Só o vento do ar-condicionado não derruba."));
            AddTab(header, panelImg.transform, "PONTUAÇÃO", p => BuildTexto(p, PONTUACAO_TEXTO));

            // ----- Rodapé -----
            var bottom = PNDUI.Rect("Bottom", vbox);
            PNDUI.HBox(bottom, 14f, TextAnchor.MiddleCenter);

            PNDUI.Label("Hint", bottom, "◀ ▶ trocar aba", 11, new Color(0.7f, 0.7f, 0.75f));

            backButton = PNDUI.Button("BackButton", bottom, "VOLTAR", 16);
            PNDUI.Layout(backButton, 120, 30, 120, 30);
            backButton.onClick.AddListener(Close);

            PNDUI.Label("Hint2", bottom, "ESC voltar", 11, new Color(0.7f, 0.7f, 0.75f));

            SetTab(0);
        }

        void Start()
        {
            PNDUI.Select(backButton);
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
            if (kb.escapeKey.wasPressedThisFrame)
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

            var b = PNDUI.Button("Tab_" + title, header, title, 13, TabUnselected, TabHover, TabSelected, focusable: false);
            var le = b.GetComponent<LayoutElement>();
            le.preferredWidth = b.label.GetPreferredValues(title).x + 20f;
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
                var b = tabButtons[k];
                b.styleNormal = k == i ? TabSelected : TabUnselected;
                b.fontNormal = k == i ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.7f, 0.7f, 0.7f);
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

            var dica = PNDUI.Label("Dica", page, "💡 " + CONTROLES_DICA, 12, COR_DESTAQUE, TextAlignmentOptions.TopLeft, 0, null, true);
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
                var r = PNDUI.Label("Rodape", page, "⚠ " + rodape, 12, new Color(1f, 0.45f, 0.45f), TextAlignmentOptions.TopLeft, 0, null, true);
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
            bg = new Color(0.9f, 0.9f, 0.92f),
            radius = 4,
            border = new Color(0.55f, 0.55f, 0.6f),
            borderL = 1, borderT = 1, borderR = 1, borderB = 3,
        };

        RectTransform KeyChip(RectTransform parent, string text, out float width)
        {
            var box = PNDUI.StyledBox("KeyChip", parent, ChipStyle, false);
            var rt = box.rectTransform;
            var label = PNDUI.Label("Label", rt, text, 12, new Color(0.1f, 0.1f, 0.12f), TextAlignmentOptions.Center);
            // content_margin: 8 nas laterais, 2 em cima, 2 + borda de 3 embaixo
            PNDUI.G(label.rectTransform, 0, 0, 1, 1, 8, 2, -8, -4);
            Vector2 pref = label.GetPreferredValues(text);
            width = pref.x + 16f;
            PNDUI.Layout(box, width, pref.y + 6f, width, pref.y + 6f);
            return rt;
        }
    }
}
