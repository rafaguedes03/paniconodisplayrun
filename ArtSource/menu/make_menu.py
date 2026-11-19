"""
Gera as artes do menu a partir da tela de início (src.png, 1672x941):
  - menu_background.png : a tela sem os botões (área dos botões reconstruída)
  - btn_<nome>_normal.png / btn_<nome>_hover.png : botões isolados, com transparência
  - layout.txt : posição de cada botão na imagem (para o Unity)
"""
import json
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFilter

SRC = "src.png"
src = np.asarray(Image.open(SRC).convert("RGB"))
H_IMG, W_IMG = src.shape[:2]

# ---------------------------------------------------------------
# Botões na arte original (borda externa medida na imagem)
# ---------------------------------------------------------------
ORIG = {
    "jogar":     (67, 433, 536, 531),
    "comojogar": (68, 546, 535, 628),
    "creditos":  (68, 639, 535, 721),
    "sair":      (68, 730, 535, 811),
}
ORDER = ["jogar", "comojogar", "creditos", "sair"]

# Novo layout: 4 botões do mesmo tamanho, espaçamento igual, mesma coluna
BX, BW, BH, GAP = 68, 468, 82, 13
TOP = 440
NEW = {}
for i, n in enumerate(ORDER):
    y = TOP + i * (BH + GAP)
    NEW[n] = (BX, y, BX + BW, y + BH)

CL, CR = 14, 20   # chanfro esquerdo / direito (igual à arte)
PAD = 26          # margem transparente em volta (espaço para o brilho)
SS = 4            # supersampling para bordas suaves


def shape_poly(x0, y0, x1, y1, s=1.0):
    return [((x0 + CL) * s, y0 * s), ((x1 - CR) * s, y0 * s), (x1 * s, (y0 + CR) * s),
            (x1 * s, (y1 - CR) * s), ((x1 - CR) * s, y1 * s), ((x0 + CL) * s, y1 * s),
            (x0 * s, (y1 - CL) * s), (x0 * s, (y0 + CL) * s)]


def mask_of(rect, w, h, grow=0):
    x0, y0, x1, y1 = rect
    m = Image.new("L", (w * SS, h * SS), 0)
    ImageDraw.Draw(m).polygon(shape_poly(x0 - grow, y0 - grow, x1 + grow, y1 + grow, SS), fill=255)
    return np.asarray(m.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255.0


# ---------------------------------------------------------------
# 1) Fundo sem os botões (inpainting só dentro da área dos botões)
# ---------------------------------------------------------------
hole = np.zeros((H_IMG, W_IMG), np.uint8)
for n, r in ORIG.items():
    g = 12 if n == "jogar" else 3   # o "Jogar" tem brilho verde em volta
    hole = np.maximum(hole, (mask_of(r, W_IMG, H_IMG, g) > 0.01).astype(np.uint8) * 255)
# Tira também os raios verdes de "glitch" que vazavam do brilho do Jogar
bgr = cv2.cvtColor(src, cv2.COLOR_RGB2BGR)
clean = cv2.inpaint(bgr, hole, 9, cv2.INPAINT_TELEA)
# Suaviza só a área reconstruída (o fundo ali já é desfocado)
blur = cv2.GaussianBlur(clean, (0, 0), 6)
soft = cv2.GaussianBlur(hole.astype(np.float32) / 255.0, (0, 0), 3)[..., None]
clean = (clean * (1 - soft) + blur * soft).astype(np.uint8)
Image.fromarray(cv2.cvtColor(clean, cv2.COLOR_BGR2RGB)).save("menu_background.png", optimize=True)

# ---------------------------------------------------------------
# 2) Ícone + texto de cada botão (extraídos da arte, viram máscara alfa)
# ---------------------------------------------------------------
def glyph(n):
    x0, y0, x1, y1 = ORIG[n]
    crop = src[y0:y1, x0:x1].astype(np.float32)
    inner = mask_of((13, 13, x1 - x0 - 13, y1 - y0 - 13), x1 - x0, y1 - y0)
    m = crop.max(axis=2)
    a = np.clip((m - 105.0) / (215.0 - 105.0), 0, 1) * (inner > 0.99)
    # Só a faixa do ícone + texto (à direita aparecem luzes do fundo através do painel)
    a[:, 400:] = 0
    ys, xs = np.where(a > 0.05)
    a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return a, xs.min()   # alfa recortado + deslocamento x a partir da borda esquerda


# ---------------------------------------------------------------
# 3) Desenho dos botões (normal e hover) no mesmo tamanho
# ---------------------------------------------------------------
CW, CH = BW + 2 * PAD, BH + 2 * PAD
BODY = mask_of((PAD, PAD, PAD + BW - 1, PAD + BH - 1), CW, CH)
INNER = mask_of((PAD + 2, PAD + 2, PAD + BW - 3, PAD + BH - 3), CW, CH)
INNER3 = mask_of((PAD + 3, PAD + 3, PAD + BW - 4, PAD + BH - 4), CW, CH)
BORDER2 = np.clip(BODY - INNER, 0, 1)
BORDER3 = np.clip(BODY - INNER3, 0, 1)

TEXT_NORMAL = np.array([222, 236, 250], np.float32)
TEXT_HOVER = np.array([255, 255, 255], np.float32)
ICON_HOVER = np.array([80, 245, 132], np.float32)
FILL_NORMAL = (np.array([2, 18, 34], np.float32), 0.88)
FILL_HOVER = (np.array([2, 36, 30], np.float32), 0.90)
EDGE_NORMAL = np.array([64, 120, 172], np.float32)
EDGE_HOVER = np.array([95, 252, 145], np.float32)
ICON_SPLIT = 125  # px a partir da borda: à esquerda é ícone, à direita é texto


def over(dst_rgb, dst_a, rgb, a):
    """Composição 'over' com alfa não pré-multiplicado."""
    a = a[..., None]
    out_a = a + dst_a[..., None] * (1 - a)
    out_rgb = np.where(out_a > 0, (rgb * a + dst_rgb * dst_a[..., None] * (1 - a)) / np.maximum(out_a, 1e-6), 0)
    return out_rgb, out_a[..., 0]


def render(n, hover):
    rgb = np.zeros((CH, CW, 3), np.float32)
    al = np.zeros((CH, CW), np.float32)

    if hover:
        # Brilho neon em volta
        ring = Image.fromarray((BORDER3 * 255).astype(np.uint8))
        glow = np.asarray(ring.filter(ImageFilter.GaussianBlur(9))).astype(np.float32) / 255.0
        glow = np.clip(glow * 2.2, 0, 1) * 0.75
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_HOVER, rgb.shape), glow)

    fill, fa = FILL_HOVER if hover else FILL_NORMAL
    rgb, al = over(rgb, al, np.broadcast_to(fill, rgb.shape), INNER * fa)

    if hover:
        # Leve luz verde vindo da borda para dentro
        inner_glow = np.asarray(Image.fromarray((BORDER3 * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(7))).astype(np.float32) / 255.0 * INNER * 0.55
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_HOVER, rgb.shape), inner_glow)
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_HOVER, rgb.shape), BORDER3)
    else:
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_NORMAL, rgb.shape), BORDER2)

    # Ícone e texto
    ga, gx = glyph(n)
    gh, gw = ga.shape
    layer = np.zeros((CH, CW), np.float32)
    oy = PAD + (BH - gh) // 2
    ox = PAD + gx
    layer[oy:oy + gh, ox:ox + gw] = ga
    split = PAD + ICON_SPLIT
    col = np.empty((CH, CW, 3), np.float32)
    col[:] = TEXT_HOVER if hover else TEXT_NORMAL
    if hover:
        col[:, :split] = ICON_HOVER
    if hover:
        # Contorno escuro bem leve para destacar do fundo verde
        sh = np.asarray(Image.fromarray((layer * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(1.2))).astype(np.float32) / 255.0 * 0.6
        rgb, al = over(rgb, al, np.zeros_like(rgb), sh * INNER)
    rgb, al = over(rgb, al, col, layer)

    out = np.dstack([np.clip(rgb, 0, 255), np.clip(al * 255, 0, 255)]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


layout = {"image": [W_IMG, H_IMG], "pad": PAD, "buttons": {}}
for n in ORDER:
    render(n, False).save(f"btn_{n}_normal.png", optimize=True)
    render(n, True).save(f"btn_{n}_hover.png", optimize=True)
    x0, y0, x1, y1 = NEW[n]
    layout["buttons"][n] = [x0 - PAD, y0 - PAD, CW, CH]
json.dump(layout, open("layout.json", "w"), indent=1)

# ---------------------------------------------------------------
# 3b) Molduras sem texto (painel do ranking, painel dos créditos, botão pequeno)
# ---------------------------------------------------------------
def frame(w, h, hover, pad, fill_alpha=None, edge=None):
    cw, ch = w + 2 * pad, h + 2 * pad
    body = mask_of((pad, pad, pad + w - 1, pad + h - 1), cw, ch)
    inner = mask_of((pad + 2, pad + 2, pad + w - 3, pad + h - 3), cw, ch)
    inner3 = mask_of((pad + 3, pad + 3, pad + w - 4, pad + h - 4), cw, ch)
    b2 = np.clip(body - inner, 0, 1)
    b3 = np.clip(body - inner3, 0, 1)
    rgb = np.zeros((ch, cw, 3), np.float32)
    al = np.zeros((ch, cw), np.float32)
    if hover:
        glow = np.asarray(Image.fromarray((b3 * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(9))).astype(np.float32) / 255.0
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_HOVER, rgb.shape), np.clip(glow * 2.2, 0, 1) * 0.75)
    fill, fa = FILL_HOVER if hover else FILL_NORMAL
    if fill_alpha is not None:
        fa = fill_alpha
    rgb, al = over(rgb, al, np.broadcast_to(fill, rgb.shape), inner * fa)
    if hover:
        ig = np.asarray(Image.fromarray((b3 * 255).astype(np.uint8)).filter(
            ImageFilter.GaussianBlur(7))).astype(np.float32) / 255.0 * inner * 0.55
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_HOVER, rgb.shape), ig)
        rgb, al = over(rgb, al, np.broadcast_to(EDGE_HOVER, rgb.shape), b3)
    else:
        e = EDGE_NORMAL if edge is None else np.array(edge, np.float32)
        rgb, al = over(rgb, al, np.broadcast_to(e, rgb.shape), b2)
    out = np.dstack([np.clip(rgb, 0, 255), np.clip(al * 255, 0, 255)]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


frame(310, 330, False, 0, fill_alpha=0.84).save("panel_ranking.png", optimize=True)
frame(940, 620, False, 0, fill_alpha=0.92, edge=EDGE_HOVER).save("panel_large.png", optimize=True)
frame(260, 70, False, PAD).save("btn_small_normal.png", optimize=True)
frame(260, 70, True, PAD).save("btn_small_hover.png", optimize=True)

# ---------------------------------------------------------------
# 4) Prévia: menu com o "Jogar" em hover
# ---------------------------------------------------------------
prev = Image.open("menu_background.png").convert("RGBA")
for n in ORDER:
    st = "hover" if n == "jogar" else "normal"
    b = Image.open(f"btn_{n}_{st}.png")
    x, y, w, h = layout["buttons"][n]
    prev.alpha_composite(b, (x, y))
prev.alpha_composite(Image.open("panel_ranking.png"), (1345, 465))
prev.convert("RGB").save("preview.png")
prev2 = Image.open("menu_background.png").convert("RGBA")
for n in ORDER:
    st = "hover" if n == "creditos" else "normal"
    b = Image.open(f"btn_{n}_{st}.png")
    x, y, w, h = layout["buttons"][n]
    prev2.alpha_composite(b, (x, y))
prev2.convert("RGB").save("preview2.png")
print(json.dumps(layout))

# ---------------------------------------------------------------
# 5) Molduras "9-slice" para as outras telas (pause, fim de jogo, como jogar...)
#    O Unity estica só o miolo; os cantos chanfrados ficam iguais aos do menu.
#    Bordas usadas no código: botão = 50 px, painel = 26 px.
# ---------------------------------------------------------------
frame(200, 84, False, PAD).save("frame_btn_normal.png", optimize=True)
frame(200, 84, True, PAD).save("frame_btn_hover.png", optimize=True)
frame(200, 200, False, 0, fill_alpha=0.9).save("frame_panel.png", optimize=True)
frame(200, 200, False, 0, fill_alpha=0.92, edge=EDGE_HOVER).save("frame_panel_green.png", optimize=True)
