#!/usr/bin/env python3
"""
Monta, a partir dos quadros soltos em Assets/_Project/Art, a pasta de Sprites da entrega final:
uma sprite sheet PNG por personagem e animação (quadros em linha, mesmo tamanho, fundo transparente)
com um .json (tamanho do quadro, quantidade, fps), mais folhas de ícones, a prancha geral (contact sheet)
e as imagens ampliadas usadas pelo manual (Docs/Entrega/img). Ignora Art/_Rejected e Art/_Reference.

Uso: python3 Tools/export_entrega_sprites.py <PASTA_ENTREGA>
Requer: pip install pillow
"""
import glob, json, os, re, shutil, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ART = os.path.join(ROOT, "Assets", "_Project", "Art")
FONT = os.path.join(ART, "UI", "Fonts", "PressStart2P-Regular.ttf")
OUT = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.join(ROOT, "Build", "Entrega")
IMG = os.path.join(ROOT, "Docs", "Entrega", "img")
FPS = 8  # frameRate dos clipes (ArtPipeline.cs)

GROUPS = [  # (pasta de saída, pasta de origem, prefixo de arquivo, nome legível)
    ("Personagens", "Characters/Hero", "pawn", "Peão"),
    ("Inimigos", "Enemies", "slime_green", "Slime Verde"),
    ("Inimigos", "Enemies", "slime_red", "Slime Vermelho"),
    ("Inimigos", "Enemies", "slime_blue", "Slime Azul"),
    ("Inimigos", "Enemies", "ink_pawn", "Slime Mirim (peão de tinta)"),
    ("Chefe", "Bosses", "king_slime", "Rei Slime"),
]
ANIMS = ["idle", "hop", "attack", "summon", "hit", "death"]


def mkdir(p):
    os.makedirs(p, exist_ok=True)
    return p


def frames_of(folder, prefix, anim):
    files = glob.glob(os.path.join(ART, folder, f"{prefix}_{anim}_*.png"))
    files.sort(key=lambda f: int(re.search(r"_(\d+)\.png$", f).group(1)))
    return [Image.open(f).convert("RGBA") for f in files]


def strip(frames):
    w, h = frames[0].size
    sheet = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.paste(f, (i * w, 0))
    return sheet


def up(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def write_json(path, **d):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(d, f, ensure_ascii=False, indent=2)


def label_font(size):
    try:
        return ImageFont.truetype(FONT, size)
    except Exception:
        return ImageFont.load_default()


def characters():
    rows = []
    for out, folder, prefix, nice in GROUPS:
        d = mkdir(os.path.join(OUT, "Sprites", out))
        for anim in ANIMS:
            fr = frames_of(folder, prefix, anim)
            if not fr:
                continue
            name = f"{prefix}_{anim}"
            strip(fr).save(os.path.join(d, name + ".png"))
            k = 3 if fr[0].width <= 44 else 2
            up(strip(fr), k).save(os.path.join(mkdir(IMG), f"strip_{name}.png"))
            info = dict(character=nice, animation=anim, sheet=name + ".png", frameWidth=fr[0].width,
                        frameHeight=fr[0].height, frameCount=len(fr), fps=FPS, loop=(anim == "idle"),
                        pixelsPerUnit=32, pivot="bottom-center")
            if anim == "attack":
                info["impactFrameIndex"] = 3  # Animation Event OnImpact
            write_json(os.path.join(d, name + ".json"), **info)
            rows.append((nice, anim, fr))
        up(frames_of(folder, prefix, "idle")[0], 6).save(os.path.join(mkdir(IMG), f"{prefix}_idle0_x6.png"))
    return rows


def contact_sheet(rows):
    k = 3
    f = label_font(10)
    lab_w = 360
    cw = max(r[2][0].width for r in rows) * k
    ch = max(r[2][0].height for r in rows) * k
    maxn = max(len(r[2]) for r in rows)
    W = lab_w + maxn * (cw + 6) + 10
    H = len(rows) * (ch + 8) + 40
    sheet = Image.new("RGBA", (W, H), (42, 27, 61, 255))
    dr = ImageDraw.Draw(sheet)
    dr.text((10, 10), "Xeque de Tinta - prancha geral (8 fps, ampliada 3x)", font=f, fill=(242, 193, 78))
    y = 30
    for nice, anim, fr in rows:
        dr.text((10, y + ch // 2 - 6), f"{nice.split(' (')[0]} / {anim} ({len(fr)})", font=f, fill=(244, 233, 216))
        for i, im in enumerate(fr):
            sheet.alpha_composite(up(im, k), (lab_w + i * (cw + 6), y + (ch - im.height * k)))
        y += ch + 8
    sheet.save(os.path.join(mkdir(os.path.join(OUT, "Sprites")), "prancha_geral.png"))
    sheet.convert("RGB").resize((sheet.width // 2, sheet.height // 2), Image.NEAREST).save(os.path.join(IMG, "prancha_geral.png"))
    return sheet.size


def static_sheet(sub, relpaths, name, size, items, up_k=6, desc=""):
    d = mkdir(os.path.join(OUT, "Sprites", sub))
    imgs = [Image.open(os.path.join(ART, p)).convert("RGBA") for p in relpaths]
    w, h = size
    sheet = Image.new("RGBA", (w * len(imgs), h), (0, 0, 0, 0))
    for i, im in enumerate(imgs):
        sheet.paste(im, (i * w, 0))
    sheet.save(os.path.join(d, name + ".png"))
    write_json(os.path.join(d, name + ".json"), sheet=name + ".png", description=desc, frameWidth=w,
               frameHeight=h, frameCount=len(imgs), fps=0, order=items, pixelsPerUnit=32)
    for it, im in zip(items, imgs):
        up(im, up_k).save(os.path.join(mkdir(IMG), f"{it}_x{up_k}.png"))


def statics():
    tiles = ("start", "battle", "shop", "rest", "event", "chest", "boss")
    static_sheet("Tabuleiro", [f"Board/tile_{t}.png" for t in tiles], "tiles", (32, 32), [f"tile_{t}" for t in tiles],
                 desc="Casas do tabuleiro: Início, Batalha, Loja, Descanso, Evento, Baú, Chefe")
    static_sheet("Tabuleiro", [f"Board/dice_face_{i}.png" for i in range(1, 7)], "dice_faces", (32, 32),
                 [f"dice_face_{i}" for i in range(1, 7)], desc="Faces do dado (o tabuleiro sorteia de 1 a 3)")
    it = ("attack", "defend", "buff", "debuff", "summon")
    static_sheet("Interface", [f"UI/Intents/intent_{n}.png" for n in it], "intents", (16, 16), [f"intent_{n}" for n in it],
                 up_k=8, desc="Ícones de intenção dos inimigos")
    st = ("shield", "burn", "weak", "strength", "stun")
    static_sheet("Interface", [f"UI/Status/status_{n}.png" for n in st], "status", (16, 16), [f"status_{n}" for n in st],
                 up_k=8, desc="Ícones de status: Escudo, Queimadura, Fraco, Força, Atordoado")
    el = ("fire", "plant", "water", "neutral")
    static_sheet("Interface", [f"UI/Elements/element_{n}.png" for n in el], "elements", (16, 16), [f"element_{n}" for n in el],
                 up_k=8, desc="Ícones de elemento")
    static_sheet("Interface", ["UI/coin.png"], "coin", (16, 16), ["coin"], up_k=8, desc="Moeda")
    static_sheet("Interface", ["UI/energy_orb.png"], "energy_orb", (20, 20), ["energy_orb"], up_k=8, desc="Orbe de energia")
    logo = Image.open(os.path.join(ART, "UI", "logo.png")).convert("RGBA")
    di = mkdir(os.path.join(OUT, "Sprites", "Interface"))
    logo.save(os.path.join(di, "logo.png"))
    write_json(os.path.join(di, "logo.json"), sheet="logo.png", frameWidth=256, frameHeight=96, frameCount=1, fps=0)
    up(logo, 3).save(os.path.join(IMG, "logo_x3.png"))
    cards = ["golpe", "defender", "investida", "bola_de_fogo", "brasa", "raiz", "casca_grossa", "onda", "respingo",
             "golpe_duplo", "seiva", "golpe_pesado"]
    static_sheet("Cartas", [f"Cards/card_{c}.png" for c in cards], "card_art", (32, 32), [f"card_{c}" for c in cards],
                 desc="Ilustrações das 12 cartas (a moldura da carta é desenhada por código)")
    rel = ["dado_viciado", "base_de_chumbo", "tinta_seca", "pincel_afiado", "ampulheta_de_bolso"]
    static_sheet("Reliquias", [f"Relics/relic_{r}.png" for r in rel], "relics", (16, 16), [f"relic_{r}" for r in rel],
                 up_k=8, desc="Ícones das 5 relíquias")
    d = mkdir(os.path.join(OUT, "Sprites", "Fundos"))
    for n in ("bg_board", "bg_battle"):
        shutil.copy(os.path.join(ART, "Backgrounds", n + ".png"), d)
        shutil.copy(os.path.join(ART, "Backgrounds", n + ".png"), IMG)
        write_json(os.path.join(d, n + ".json"), sheet=n + ".png", frameWidth=640, frameHeight=360, frameCount=1, fps=0,
                   usage="fundo de tela")


if __name__ == "__main__":
    mkdir(IMG)
    rows = characters()
    print("prancha", contact_sheet(rows))
    statics()
    print("ok", OUT)
