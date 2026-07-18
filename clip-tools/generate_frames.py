#!/usr/bin/env python3
"""Генерація всіх кадрів кліпу «Самотній привид» через API Automatic1111 / Forge / SD.Next.

Перед запуском увімкни API у webui (прапорець запуску --api) і вибери модель
DreamShaper XL у самому webui. Далі:

    python generate_frames.py --variants 4

Скрипт прожене всі 21 кадр із frames.json, згенерує по 4 варіанти на кадр
і складе їх у папку variants/ як kadr_01_v1.png ... kadr_21_v4.png.
Уже згенеровані файли пропускаються, тож перерваний запуск можна просто повторити.

Потрібен лише Python 3 — сторонніх бібліотек немає.
"""

import argparse
import base64
import json
import sys
import urllib.error
import urllib.request
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Генерація кадрів кліпу через API Stable Diffusion webui")
    parser.add_argument("--url", default="http://127.0.0.1:7860", help="Адреса webui (типово http://127.0.0.1:7860)")
    parser.add_argument("--frames", default=str(SCRIPT_DIR / "frames.json"), help="Шлях до frames.json")
    parser.add_argument("--out", default=str(SCRIPT_DIR / "variants"), help="Папка для згенерованих варіантів")
    parser.add_argument("--variants", type=int, default=4, help="Скільки варіантів генерувати на кадр")
    parser.add_argument("--steps", type=int, default=30, help="Кроки семплера")
    parser.add_argument("--cfg", type=float, default=6.0, help="CFG scale")
    parser.add_argument("--sampler", default="DPM++ 2M Karras", help="Назва семплера")
    parser.add_argument("--only", default="", help="Генерувати лише вказані кадри, напр. --only 3,5,9")
    return parser.parse_args()


def txt2img(url: str, payload: dict) -> list[str]:
    request = urllib.request.Request(
        url.rstrip("/") + "/sdapi/v1/txt2img",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=600) as response:
        return json.loads(response.read())["images"]


def main() -> int:
    args = parse_args()
    config = json.loads(Path(args.frames).read_text(encoding="utf-8"))
    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    only_ids = {int(x) for x in args.only.split(",") if x.strip()} if args.only else None
    frames = [f for f in config["frames"] if only_ids is None or f["id"] in only_ids]

    total = len(frames) * args.variants
    done = 0
    for frame in frames:
        for variant in range(1, args.variants + 1):
            done += 1
            target = out_dir / f"kadr_{frame['id']:02d}_v{variant}.png"
            if target.exists():
                print(f"[{done}/{total}] {target.name} вже є — пропускаю")
                continue

            payload = {
                "prompt": f"{frame['prompt']}, {config['base_prompt']}",
                "negative_prompt": config["negative_prompt"],
                "width": config["width"],
                "height": config["height"],
                "steps": args.steps,
                "cfg_scale": args.cfg,
                "sampler_name": args.sampler,
                "seed": -1,
                "batch_size": 1,
                "n_iter": 1,
            }
            print(f"[{done}/{total}] Кадр {frame['id']:02d} «{frame['title']}», варіант {variant}...")
            try:
                images = txt2img(args.url, payload)
            except urllib.error.URLError as error:
                print(f"Не вдалося звернутись до webui за адресою {args.url}: {error}", file=sys.stderr)
                print("Перевір, що webui запущено з прапорцем --api.", file=sys.stderr)
                return 1
            target.write_bytes(base64.b64decode(images[0]))
            print(f"    збережено {target}")

    print()
    print("Готово. Тепер відбери по одному найкращому варіанту на кадр і скопіюй його")
    print(f"в папку {SCRIPT_DIR / 'frames'} під іменем kadr_01.png ... kadr_21.png,")
    print("після чого запусти make_video.py.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
