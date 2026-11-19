"""Foto dos créditos (recorte redondo com aro neon) e ícone do Instagram."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 320          # tamanho final
R = 140          # raio da foto
GREEN = (95, 252, 145)

photo = Image.open("foto_original.png").convert("RGB")
# Rosto centralizado (quadrado em volta do rosto)
cx, cy, half = 296, 335, 215
photo = photo.crop((cx - half, cy - half, cx + half, cy + half)).resize((2 * R, 2 * R), Image.LANCZOS)

ss = 4
mask = Image.new("L", (S * ss, S * ss), 0)
ImageDraw.Draw(mask).ellipse(((S / 2 - R) * ss, (S / 2 - R) * ss, (S / 2 + R) * ss, (S / 2 + R) * ss), fill=255)
mask = mask.resize((S, S), Image.LANCZOS)

out = Image.new("RGBA", (S, S), (0, 0, 0, 0))
# brilho verde em volta
ring = Image.new("L", (S * ss, S * ss), 0)
ImageDraw.Draw(ring).ellipse(((S / 2 - R - 3) * ss, (S / 2 - R - 3) * ss, (S / 2 + R + 3) * ss, (S / 2 + R + 3) * ss),
                             outline=255, width=6 * ss)
ring = ring.resize((S, S), Image.LANCZOS)
glow = ring.filter(ImageFilter.GaussianBlur(9))
g = np.clip(np.asarray(glow).astype(np.float32) * 2.0, 0, 255).astype(np.uint8)
out.alpha_composite(Image.merge("RGBA", (Image.new("L", (S, S), GREEN[0]), Image.new("L", (S, S), GREEN[1]),
                                         Image.new("L", (S, S), GREEN[2]), Image.fromarray(g))))
ph = Image.new("RGBA", (S, S), (0, 0, 0, 0))
ph.paste(photo, (S // 2 - R, S // 2 - R))
ph.putalpha(mask)
out.alpha_composite(ph)
out.alpha_composite(Image.merge("RGBA", (Image.new("L", (S, S), GREEN[0]), Image.new("L", (S, S), GREEN[1]),
                                         Image.new("L", (S, S), GREEN[2]), ring)))
out.save("credits_photo.png", optimize=True)

Image.open("instagram_original.png").convert("RGBA").resize((128, 128), Image.LANCZOS).save("instagram.png", optimize=True)
