"""Grava um .ico "clássico" (todas as imagens em BMP 32 bits + máscara), aceito por qualquer programa,
inclusive o Inno Setup (que recusa ícones com PNG dentro)."""
import struct
from PIL import Image


def write_ico(img, path, sizes=(16, 24, 32, 48, 64, 128, 256)):
    blobs = []
    for s in sizes:
        im = img.resize((s, s), Image.LANCZOS).convert("RGBA")
        px = im.tobytes("raw", "BGRA")
        rows = [px[y * s * 4:(y + 1) * s * 4] for y in range(s)][::-1]  # de baixo para cima
        xor = b"".join(rows)
        mask_row = ((s + 31) // 32) * 4
        and_mask = b"\x00" * (mask_row * s)
        header = struct.pack("<IiiHHIIiiII", 40, s, s * 2, 1, 32, 0, len(xor) + len(and_mask), 0, 0, 0, 0)
        blobs.append((s, header + xor + and_mask))
    out = struct.pack("<HHH", 0, 1, len(blobs))
    offset = 6 + 16 * len(blobs)
    entries, data = b"", b""
    for s, b in blobs:
        entries += struct.pack("<BBBBHHII", s % 256, s % 256, 0, 0, 1, 32, len(b), offset + len(data))
        data += b
    open(path, "wb").write(out + entries + data)


if __name__ == "__main__":
    write_ico(Image.open("icone_jogo.png"), "icone_jogo.ico")
    print("ok")
