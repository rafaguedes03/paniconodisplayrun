using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PanicoNoDisplay
{
    /// <summary>
    /// Botão desenhado com duas imagens recortadas da arte da tela de início:
    /// "normal" (moldura azul) e "hover" (moldura verde neon com brilho).
    ///
    /// Ao passar o mouse (ou chegar pelo teclado) ele troca suavemente para o hover,
    /// cresce um pouco e anda alguns pixels para a direita. Ao clicar, "afunda" de leve.
    /// O foco acompanha o mouse, então só um botão fica destacado por vez.
    /// </summary>
    public class PNDArtButton : Button
    {
        public Image normalImage;
        public Image hoverImage;
        public RectTransform visual;      // parte que cresce/anda (as duas imagens + o texto)
        public TextMeshProUGUI label;     // opcional (botões sem o texto desenhado na arte)
        public Color labelNormal = new Color(0.87f, 0.925f, 0.98f);
        public Color labelHover = Color.white;

        public bool selectOnHover = true; // o mouse em cima também move o foco do teclado
        public bool locked;               // fica sempre destacado (ex.: aba aberta)
        public bool playSound = true;     // som ao passar o mouse

        public float hoverScale = 1.04f;  // quanto cresce
        public float hoverShift = 3f;     // quanto anda para a direita (unidades do Canvas)
        public float fadeTime = 0.12f;    // duração da transição

        bool pointerInside, pointerDown;
        float t; // 0 = normal, 1 = destacado
        float createdAt;

        protected override void Awake()
        {
            base.Awake();
            createdAt = Time.unscaledTime;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            pointerInside = true;
            if (!IsInteractable()) return;
            if (playSound) PND.PlayButtonSound();
            // O foco acompanha o mouse (teclado e mouse nunca destacam dois botões)
            if (selectOnHover && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            // Chegou pelo teclado (setas): também toca o som. Não toca ao abrir a tela.
            if (!pointerInside && playSound && IsInteractable() && Time.unscaledTime - createdAt > 0.25f)
                PND.PlayButtonSound();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            pointerInside = false;
            // Mouse saiu: volta a ficar azul (solta o foco que o hover tinha pegado)
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject == gameObject)
                es.SetSelectedGameObject(null);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left) pointerDown = true;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button == PointerEventData.InputButton.Left) pointerDown = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            pointerInside = pointerDown = false;
            t = 0f;
            Apply(0f);
        }

        bool Highlighted
        {
            get
            {
                if (locked) return true;
                if (!IsInteractable()) return false;
                var es = EventSystem.current;
                return pointerInside || (es != null && es.currentSelectedGameObject == gameObject);
            }
        }

        void LateUpdate()
        {
            float target = Highlighted ? 1f : 0f;
            // Tempo real: funciona mesmo com o jogo pausado (Time.timeScale = 0)
            t = Mathf.MoveTowards(t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            Apply(Mathf.SmoothStep(0f, 1f, t));
        }

        void Apply(float e)
        {
            if (hoverImage != null)
            {
                var c = hoverImage.color;
                c.a = e;
                hoverImage.color = c;
                hoverImage.enabled = e > 0.001f;
            }
            if (normalImage != null)
            {
                var c = normalImage.color;
                c.a = 1f - e;
                normalImage.color = c;
                normalImage.enabled = e < 0.999f;
            }
            if (visual != null)
            {
                float press = pointerDown && pointerInside ? 0.97f : 1f;
                float s = Mathf.Lerp(1f, hoverScale, e) * press;
                visual.localScale = new Vector3(s, s, 1f);
                visual.anchoredPosition = new Vector2(hoverShift * e, 0f);
            }
            if (label != null)
                label.color = Color.Lerp(labelNormal, labelHover, e);
        }

        /// <summary>
        /// Cria o botão. 'body' é a área clicável (a moldura); as imagens têm 'padPx' pixels
        /// de margem transparente em volta (espaço do brilho), na mesma escala da imagem.
        /// </summary>
        public static PNDArtButton Create(string name, Transform parent, string normalSprite, string hoverSprite,
            float bodyWpx, float bodyHpx, float padPx)
        {
            var rt = PNDUI.Rect(name, parent);

            // Área clicável = só a moldura (sem o brilho)
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            var visual = PNDUI.Full(PNDUI.Rect("Visual", rt));

            // As imagens passam um pouco da área do botão (a margem do brilho)
            Vector2 aMin = new Vector2(-padPx / bodyWpx, -padPx / bodyHpx);
            Vector2 aMax = new Vector2(1f + padPx / bodyWpx, 1f + padPx / bodyHpx);

            Image MakeImage(string n, string sprite)
            {
                var irt = PNDUI.Rect(n, visual);
                irt.anchorMin = aMin;
                irt.anchorMax = aMax;
                irt.offsetMin = irt.offsetMax = Vector2.zero;
                var img = irt.gameObject.AddComponent<Image>();
                img.sprite = PND.Spr(sprite);
                img.raycastTarget = false;
                return img;
            }

            var normal = MakeImage("Normal", normalSprite);
            var hover = MakeImage("Hover", hoverSprite);

            var b = rt.gameObject.AddComponent<PNDArtButton>();
            b.transition = Transition.None;
            b.targetGraphic = hit;
            b.normalImage = normal;
            b.hoverImage = hover;
            b.visual = visual;
            b.Apply(0f);
            return b;
        }

        /// <summary>
        /// Botão com texto usando a moldura 9-slice do menu (azul → verde neon no hover).
        /// Serve para qualquer tamanho: Voltar, Continuar, abas do Como Jogar, etc.
        /// O tamanho vem de um LayoutElement (ajuste com PNDUI.Layout) ou do RectTransform.
        /// </summary>
        public static PNDArtButton CreateFramed(string name, Transform parent, string text, float fontSize)
        {
            var rt = PNDUI.Rect(name, parent);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;

            var visual = PNDUI.Full(PNDUI.Rect("Visual", rt));
            float pad = 26f * PNDUI.ArtUnit; // margem do brilho

            Image MakeImage(string n, string path)
            {
                var irt = PNDUI.Rect(n, visual);
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(-pad, -pad);
                irt.offsetMax = new Vector2(pad, pad);
                var img = irt.gameObject.AddComponent<Image>();
                PNDUI.Frame(img, path, 50f);
                img.raycastTarget = false;
                return img;
            }

            var b = rt.gameObject.AddComponent<PNDArtButton>();
            b.transition = Transition.None;
            b.targetGraphic = hit;
            b.normalImage = MakeImage("Normal", "art/menu/frame_btn_normal");
            b.hoverImage = MakeImage("Hover", "art/menu/frame_btn_hover");
            b.visual = visual;
            b.hoverShift = 0f;      // centralizado: só cresce
            b.hoverScale = 1.05f;
            b.AddLabel(text, fontSize);

            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = Mathf.Ceil(fontSize * 1.4f) + 10f;
            le.preferredWidth = b.label.GetPreferredValues(text).x + 34f;
            return b;
        }

        /// <summary>Ícone (imagem) no centro do botão, no lugar de texto.</summary>
        public Image SetIcon(string spritePath, float size)
        {
            if (icon == null)
            {
                var irt = PNDUI.Rect("Icon", visual);
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = new Vector2(size, size);
                icon = irt.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            icon.sprite = PND.Spr(spritePath);
            return icon;
        }

        public Image icon;

        /// <summary>Tira o botão da navegação por setas (ele só funciona com o mouse).</summary>
        public void NoKeyboard()
        {
            var nav = navigation;
            nav.mode = Navigation.Mode.None;
            navigation = nav;
            selectOnHover = false;
        }

        /// <summary>Texto por cima do botão (para molduras sem texto desenhado).</summary>
        public void AddLabel(string text, float size)
        {
            label = PNDUI.Label("Label", visual, text, size, labelNormal, TextAlignmentOptions.Center, 4, Color.black);
            PNDUI.Full(label.rectTransform);
            Apply(0f);
        }
    }
}
