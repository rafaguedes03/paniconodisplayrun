"""Ícone do jogo (exe, atalho e instalador): rosto do personagem da tela de início."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 1024
src = Image.open("tela_inicio_original.png" if __import__("os").path.exists("tela_inicio_original.png") else "src.png").convert("RGB")
cx, cy, h = 1088, 345, 268
face = src.crop((cx - h, cy - h, cx + h, cy + h)).resize((S, S), Image.LANCZOS)

# Leve escurecida nas bordas para o rosto saltar
yy, xx = np.mgrid[0:S, 0:S]
d = np.sqrt((xx - S * 0.5) ** 2 + (yy - S * 0.45) ** 2) / (S * 0.72)
vig = np.clip(1.15 - d * 0.55, 0.55, 1.0)[..., None]
face = Image.fromarray(np.clip(np.asarray(face).astype(np.float32) * vig, 0, 255).astype(np.uint8))

R = 190      # raio dos cantos
M = 40       # margem transparente (espaço do brilho)
ss = 2
mask = Image.new("L", (S * ss, S * ss), 0)
ImageDraw.Draw(mask).rounded_rectangle((M * ss, M * ss, (S - M) * ss, (S - M) * ss), radius=R * ss, fill=255)
mask = mask.resize((S, S), Image.LANCZOS)

ring = Image.new("L", (S * ss, S * ss), 0)
ImageDraw.Draw(ring).rounded_rectangle((M * ss, M * ss, (S - M) * ss, (S - M) * ss), radius=R * ss,
                                       outline=255, width=22 * ss)
ring = ring.resize((S, S), Image.LANCZOS)
glow = ring.filter(ImageFilter.GaussianBlur(18))

green = (95, 252, 145)
out = Image.new("RGBA", (S, S), (0, 0, 0, 0))
g = Image.new("RGBA", (S, S), green + (0,))
g.putalpha(Image.fromarray(np.clip(np.asarray(glow).astype(np.float32) * 1.6, 0, 255).astype(np.uint8)))
out.alpha_composite(g)
f = face.convert("RGBA")
f.putalpha(mask)
out.alpha_composite(f)
r = Image.new("RGBA", (S, S), green + (0,))
r.putalpha(ring)
out.alpha_composite(r)

out.save("icone_jogo.png", optimize=True)
# .ico em BMP (sem PNG dentro): o Inno Setup recusa ícones PNG-comprimidos
from write_ico import write_ico
write_ico(out, "icone_jogo.ico")
print("ok")
