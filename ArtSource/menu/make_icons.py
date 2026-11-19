"""Ícones do HUD (desenhados aqui para não depender de emoji na fonte)."""
from PIL import Image, ImageDraw

S, SS = 64, 8
W = (255, 255, 255, 255)


def canvas():
    im = Image.new("RGBA", (S * SS, S * SS), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def save(im, name):
    im.resize((S, S), Image.LANCZOS).save(name, optimize=True)


def p(*pts):
    return [(x * SS, y * SS) for x, y in pts]


def speaker(d):
    d.rectangle(p((8, 24), (20, 40)), fill=W)
    d.polygon(p((18, 24), (32, 12), (32, 52), (18, 40)), fill=W)


# Som ligado
im, d = canvas()
speaker(d)
for r, w in ((10, 4.5), (19, 4.5)):
    d.arc([(32 - r) * SS, (32 - r) * SS, (32 + r) * SS, (32 + r) * SS], -50, 50, fill=W, width=int(w * SS))
save(im, "icon_sound_on.png")

# Som desligado
im, d = canvas()
speaker(d)
d.line(p((40, 22), (56, 42)), fill=W, width=5 * SS)
d.line(p((56, 22), (40, 42)), fill=W, width=5 * SS)
save(im, "icon_sound_off.png")

# Pause
im, d = canvas()
d.rounded_rectangle(p((16, 12), (27, 52)), radius=2 * SS, fill=W)
d.rounded_rectangle(p((37, 12), (48, 52)), radius=2 * SS, fill=W)
save(im, "icon_pause.png")

# Troféu (dourado)
G = (255, 205, 60, 255)
GD = (200, 140, 20, 255)
im, d = canvas()
d.arc([10 * SS, 14 * SS, 26 * SS, 32 * SS], 90, 270, fill=G, width=4 * SS)    # alça esquerda
d.arc([38 * SS, 14 * SS, 54 * SS, 32 * SS], -90, 90, fill=G, width=4 * SS)    # alça direita
d.pieslice([16 * SS, -8 * SS, 48 * SS, 38 * SS], 0, 180, fill=G)             # taça
d.rectangle(p((16, 8), (48, 15)), fill=G)
d.rectangle(p((29, 36), (35, 46)), fill=G)                                   # haste
d.rounded_rectangle(p((20, 46), (44, 54)), radius=2 * SS, fill=GD)          # base
d.polygon(p((26, 14), (30, 14), (27, 28)), fill=(255, 240, 170, 255))        # brilho
save(im, "icon_trophy.png")
