#!/usr/bin/env python3
"""
Converte os documentos de Docs/Entrega/*.md em HTML e PDF (Chrome headless).
- O manual (03_Manual_do_Jogo) vira um encarte diagramado: Docs/Entrega/03_Manual_do_Jogo.html (fica versionado).
- Os demais HTML são temporários (pasta de trabalho) e só o PDF vai para a pasta de entrega.

Uso: python3 Tools/build_docs.py <PASTA_ENTREGA> [<pasta_temporaria>]
Requer: pip install markdown ; Google Chrome instalado.
"""
import glob, os, re, subprocess, sys, shutil
import markdown

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DOCS = os.path.join(ROOT, "Docs", "Entrega")
DEST = os.path.abspath(sys.argv[1])
TMP = os.path.abspath(sys.argv[2]) if len(sys.argv) > 2 else os.path.join(ROOT, "Build", "docs_tmp")
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"

TECH_CSS = """
@page { size: A4; margin: 16mm 14mm; }
* { box-sizing: border-box; }
body { font-family: -apple-system, 'Helvetica Neue', Arial, sans-serif; font-size: 10.5pt; line-height: 1.45; color: #222; }
h1 { color: #2A1B3D; border-bottom: 4px solid #F2C14E; padding-bottom: 6px; font-size: 22pt; }
h2 { color: #2A1B3D; border-bottom: 1px solid #ccc; padding-bottom: 3px; margin-top: 22px; font-size: 15pt; page-break-after: avoid; }
h3 { color: #5b3a8c; font-size: 12pt; page-break-after: avoid; }
table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; font-size: 9.2pt; page-break-inside: auto; }
tr { page-break-inside: avoid; }
th { background: #2A1B3D; color: #F4E9D8; text-align: left; padding: 4px 6px; }
td { border: 1px solid #d6d0dd; padding: 4px 6px; vertical-align: top; }
tr:nth-child(even) td { background: #f6f3fa; }
code { background: #efeaf5; padding: 1px 4px; border-radius: 3px; font-size: 9pt; font-family: Menlo, Consolas, monospace; }
pre { background: #f3f0f7; border-left: 4px solid #5b3a8c; padding: 8px 10px; overflow-wrap: anywhere; white-space: pre-wrap; font-size: 8.6pt; page-break-inside: avoid; }
pre code { background: none; padding: 0; }
img { max-width: 100%; image-rendering: pixelated; }
blockquote { border-left: 4px solid #F2C14E; margin: 8px 0; padding: 2px 12px; background: #fbf6e6; }
.shot img { border: 1px solid #ccc; }
"""

MANUAL_CSS = """
@font-face { font-family: 'PS2P'; src: url('img/PressStart2P-Regular.ttf'); }
@page { size: A4; margin: 0; }
@page :first { margin: 0; }
* { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
html { background: #2A1B3D; }
body { margin: 0; background: #2A1B3D; color: #F4E9D8; font-family: 'Trebuchet MS', 'Helvetica Neue', Arial, sans-serif; font-size: 10.5pt; line-height: 1.5; }
.cover { height: 296mm; page-break-after: always; position: relative; text-align: center; overflow: hidden;
  background: linear-gradient(rgba(42,27,61,.55), rgba(42,27,61,.92)), url('img/bg_board.png') center/cover; image-rendering: pixelated; }
.cover img.logo { width: 170mm; margin-top: 34mm; image-rendering: pixelated; filter: drop-shadow(0 6px 0 rgba(0,0,0,.5)); }
.cover .tag { font-family: 'PS2P'; color: #F2C14E; font-size: 12pt; margin: 14mm 18mm 0; line-height: 1.7; }
.cover .sub { font-family: 'PS2P'; font-size: 9pt; color: #F4E9D8; margin-top: 12mm; line-height: 1.9; }
.cover .heroes { position: absolute; bottom: 30mm; left: 0; right: 0; }
.cover .heroes img { height: 46mm; margin: 0 4mm; image-rendering: pixelated; }
.cover .foot { position: absolute; bottom: 10mm; left: 0; right: 0; font-family: 'PS2P'; font-size: 7pt; color: #F4E9D8; opacity: .85; line-height: 1.8; }
section.page { padding: 14mm 14mm 10mm; page-break-before: always; min-height: 296mm; }
section.page:first-of-type { page-break-before: auto; }
h1 { display: none; }
h2 { font-family: 'PS2P'; font-size: 13pt; color: #F2C14E; margin: 0 0 8mm; padding-bottom: 4mm; border-bottom: 3px solid #F2C14E; line-height: 1.5; }
h3 { font-family: 'PS2P'; font-size: 9.5pt; color: #8fe39a; margin: 7mm 0 2mm; line-height: 1.6; page-break-after: avoid; }
p { margin: 2mm 0 3mm; }
strong { color: #F2C14E; }
em { color: #c9b8ec; }
.lead { font-size: 13pt; color: #F4E9D8; border-left: 5px solid #F2C14E; padding-left: 5mm; margin: 4mm 0; }
.meta { font-size: 8.5pt; color: #bfb0d8; margin-bottom: 8mm; }
.callout { font-family: 'PS2P'; font-size: 8.5pt; line-height: 1.9; text-align: center; background: #F2C14E; color: #2A1B3D; padding: 5mm; margin: 6mm 0; border-radius: 3mm; }
table { border-collapse: separate; border-spacing: 0; width: 100%; margin: 3mm 0 5mm; font-size: 9pt; background: #F4E9D8; color: #2A1B3D; border-radius: 2mm; overflow: hidden; page-break-inside: auto; }
tr { page-break-inside: avoid; }
th { background: #F2C14E; color: #2A1B3D; text-align: left; padding: 2mm 3mm; font-family: 'PS2P'; font-size: 7pt; }
td { padding: 2mm 3mm; border-top: 1px solid #d9c9a8; vertical-align: middle; }
td strong { color: #2A1B3D; }
td img { height: 11mm; vertical-align: middle; margin-right: 2mm; image-rendering: pixelated; }
tr:nth-child(even) td { background: #efe0c6; }
section.page > p > img { image-rendering: pixelated; }
p > img[src*="_x6"], p > img[src*="_x8"] { height: 30mm; display: block; margin: 2mm 0; }
p > img[src*="screenshots"] { width: 100%; border: 3px solid #F2C14E; border-radius: 2mm; margin: 2mm 0; image-rendering: auto; }
ul, ol { padding-left: 6mm; } li { margin-bottom: 1.5mm; }
code { background: rgba(0,0,0,.3); padding: 0 2mm; border-radius: 1mm; }
"""


def render(md_path):
    with open(md_path, encoding="utf-8") as f:
        text = f.read()
    return markdown.markdown(text, extensions=["tables", "fenced_code", "md_in_html", "sane_lists"])


def run_chrome(html, pdf):
    cmd = [CHROME, "--headless=new", "--disable-gpu", "--no-pdf-header-footer", "--virtual-time-budget=4000",
           f"--print-to-pdf={pdf}", "file://" + html]
    subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=180)
    if not os.path.exists(pdf) or os.path.getsize(pdf) < 1000:
        raise RuntimeError("falha ao gerar " + pdf)


def build_manual():
    body = render(os.path.join(DOCS, "03_Manual_do_Jogo.md"))
    parts = re.split(r"(?=<h2>)", body)
    head = parts[0]
    pages = "".join(f'<section class="page">{p}</section>' for p in parts[1:])
    # a abertura (h1, lead, meta) vira o começo da primeira seção
    if head.strip():
        pages = pages.replace('<section class="page">', '<section class="page">' + head, 1)
    cover = f"""<div class="cover">
<img class="logo" src="img/logo_x3.png" alt="Xeque de Tinta">
<div class="tag">A tinta mágica afogou as peças.<br>Só o peão pode salvar o tabuleiro.</div>
<div class="sub">MANUAL DO JOGO</div>
<div class="heroes"><img src="img/pawn_idle0_x6.png"><img src="img/king_slime_idle0_x6.png" style="height:60mm"><img src="img/slime_green_idle0_x6.png"><img src="img/slime_red_idle0_x6.png"></div>
<div class="foot">Roguelite de tabuleiro com batalhas de cartas · Unity 6 · PC<br>Márcio Dias de Lima Filho · Tiago Augusto Dal'Acqua Gonçalves</div>
</div>"""
    html = f"""<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><title>Xeque de Tinta · Manual do Jogo</title>
<style>{MANUAL_CSS}</style></head><body>{cover}{pages}</body></html>"""
    out = os.path.join(DOCS, "03_Manual_do_Jogo.html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(html)
    return out


def build_tech(md_path):
    name = os.path.splitext(os.path.basename(md_path))[0]
    html = f"""<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><base href="file://{DOCS}/"><title>{name}</title>
<style>{TECH_CSS}</style></head><body>{render(md_path)}</body></html>"""
    os.makedirs(TMP, exist_ok=True)
    out = os.path.join(TMP, name + ".html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(html)
    return out


def check_images(html_path, base):
    with open(html_path, encoding="utf-8") as f:
        h = f.read()
    missing = [s for s in re.findall(r'src="([^"]+)"', h) if not s.startswith("http") and not os.path.exists(os.path.join(base, s))]
    return missing


if __name__ == "__main__":
    out_docs = os.path.join(DEST, "Documentos")
    os.makedirs(out_docs, exist_ok=True)
    problems = []
    for md in sorted(glob.glob(os.path.join(DOCS, "0*.md"))):
        name = os.path.splitext(os.path.basename(md))[0]
        if name.startswith("03_"):
            html = build_manual()
        else:
            html = build_tech(md)
        miss = check_images(html, DOCS)
        if miss:
            problems.append((name, miss))
        run_chrome(html, os.path.join(out_docs, name + ".pdf"))
        shutil.copy(md, os.path.join(out_docs, name + ".md"))
        print("ok", name)
    for p in problems:
        print("IMAGENS FALTANDO em", p[0], p[1])
