using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Tela de CRÉDITOS, aberta pelo botão "Créditos" da tela de início.
    /// Para mudar os nomes, edite a lista CREDITOS logo abaixo.
    /// </summary>
    public class PNDCredits : MonoBehaviour
    {
        public event Action closed;

        // ===== EDITE AQUI =====
        // [função, nomes] — use \n para colocar mais de um nome na mesma função
        static readonly string[,] CREDITOS =
        {
            { "DESENVOLVIMENTO", "Rafael Lima Guedes" },
            { "ARTE E PERSONAGEM", "Rafael Lima Guedes / IA" },
            { "EFEITOS SONOROS", "Kenney (kenney.nl) — licença CC0" },
            { "FEITO COM", "Unity 6" },
        };
        const string SUBTITULO = "Pânico no Display — Corrida Infinita";
        const string RODAPE = "DISPLAY — Laboratório de Novas Tecnologias e Jogos\nObrigado por jogar!";
        const string FOTO = "art/menu/credits_photo"; // foto redonda no centro (troque o PNG para mudar)
        // ======================

        static readonly Color Verde = new Color(0.37f, 0.99f, 0.57f);
        static readonly Color Texto = new Color(0.9f, 0.94f, 0.99f);
        static readonly Color Funcao = new Color(0.55f, 0.75f, 0.95f);

        PNDArtButton backButton;

        public static PNDCredits Create(Transform parent)
        {
            var rt = PNDUI.Full(PNDUI.Rect("Credits", parent));
            var c = rt.gameObject.AddComponent<PNDCredits>();
            c.Build(rt);
            return c;
        }

        void Build(RectTransform root)
        {
            // Escurece a tela de início e bloqueia o mouse para os botões de trás
            PNDUI.ColorRect("Dimmer", root, new Color(0f, 0.01f, 0.04f, 0.72f));

            // Painel com a moldura verde do menu
            var panel = PNDUI.FramePanel("Panel", root, true);
            var panelRt = panel.rectTransform;
            PNDUI.Art(panelRt, 356f, 40f, 960f, 860f);

            var box = PNDUI.G(PNDUI.Rect("VBox", panelRt), 0, 0, 1, 1, 24, 10, -24, -44);
            PNDUI.VBox(box, 4f, TextAnchor.UpperCenter);

            PNDUI.Label("Title", box, "CRÉDITOS", 24, Verde, TextAlignmentOptions.Center, 5, Color.black);
            PNDUI.Label("Subtitle", box, SUBTITULO, 12, Texto, TextAlignmentOptions.Center);

            // Foto no centro
            var photoRt = PNDUI.Rect("Photo", box);
            var photo = photoRt.gameObject.AddComponent<Image>();
            photo.sprite = PND.Spr(FOTO);
            photo.preserveAspect = true;
            photo.raycastTarget = false;
            PNDUI.Layout(photo, -1, 100, -1, 100);

            // Funções em duas colunas
            int count = CREDITOS.GetLength(0);
            for (int i = 0; i < count; i += 2)
            {
                var row = PNDUI.Rect("Row" + i, box);
                var h = PNDUI.HBox(row, 12f, TextAnchor.UpperCenter);
                h.childForceExpandWidth = true;
                for (int k = i; k < i + 2; k++)
                {
                    var cell = PNDUI.Rect("Cell" + k, row);
                    PNDUI.VBox(cell, 0f, TextAnchor.UpperCenter);
                    PNDUI.Layout(cell, 0, -1, 0, -1, 1); // colunas sempre do mesmo tamanho
                    if (k >= count) continue; // célula vazia mantém as duas colunas
                    PNDUI.Label("Role", cell, CREDITOS[k, 0], 9, Funcao, TextAlignmentOptions.Center);
                    PNDUI.Label("Names", cell, CREDITOS[k, 1], 12, Texto, TextAlignmentOptions.Center, 0, null, true);
                }
            }

            PNDUI.Label("Footer", box, RODAPE, 10, new Color(0.7f, 0.78f, 0.88f), TextAlignmentOptions.Center);

            // Botão VOLTAR: azul, fica verde ao passar o mouse (igual ao menu)
            backButton = PNDArtButton.CreateFramed("BackButton", panelRt, "VOLTAR", 14);
            backButton.NoKeyboard();
            var brt = (RectTransform)backButton.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.sizeDelta = new Vector2(110f, 26f);
            brt.anchoredPosition = new Vector2(0f, 12f);
            backButton.onClick.AddListener(Close);
        }

        void Start()
        {
            // Nada selecionado: o VOLTAR começa azul
            PNDUI.Select(null);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
                || kb.numpadEnterKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
                Close();
        }

        void Close()
        {
            closed?.Invoke();
            Destroy(gameObject);
        }
    }
}
