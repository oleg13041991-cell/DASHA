#!/usr/bin/env python3
"""Автоматичне збирання кліпу «Самотній привид» з готових кадрів через ffmpeg.

Поклади відібрані кадри в папку frames/ під іменами kadr_01.png ... kadr_21.png,
поруч поклади трек із Suno і запусти:

    python make_video.py --audio track.mp3

Скрипт сам розставить кадри по таймінгах із frames.json, додасть рух Ken Burns
(зум/панорама з розкадровки), плавні переходи, фінальний fade to black,
легкий teal-грейд із віньєткою та зерном і підкладе трек. На виході —
lonely_ghost.mp4 у 1920x1080. Потрібні лише Python 3 та встановлений ffmpeg.
"""

import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Збирання кліпу з кадрів і треку через ffmpeg")
    parser.add_argument("--audio", required=True, help="Шлях до аудіотреку (mp3/wav/m4a)")
    parser.add_argument("--frames-dir", default=str(SCRIPT_DIR / "frames"), help="Папка з kadr_01.png ... kadr_21.png")
    parser.add_argument("--json", default=str(SCRIPT_DIR / "frames.json"), help="Шлях до frames.json із таймінгами")
    parser.add_argument("--out", default=str(SCRIPT_DIR / "lonely_ghost.mp4"), help="Вихідний файл")
    parser.add_argument("--fps", type=int, default=30, help="Частота кадрів відео")
    parser.add_argument("--transition", type=float, default=0.8, help="Тривалість cross dissolve у секундах")
    parser.add_argument("--no-grade", action="store_true", help="Вимкнути грейд, віньєтку та зерно")
    return parser.parse_args()


def ken_burns(motion: str, frame_count: int) -> tuple[str, str, str]:
    """Повертає вирази (zoom, x, y) фільтра zoompan для заданого руху."""
    center_x = "(iw-iw/zoom)/2"
    center_y = "(ih-ih/zoom)/2"
    motions = {
        "zoom_in": (f"1+0.06*on/{frame_count}", center_x, center_y),
        "zoom_out": (f"1.06-0.06*on/{frame_count}", center_x, center_y),
        "pan_right": ("1.08", f"(iw-iw/zoom)*on/{frame_count}", center_y),
        "pan_left": ("1.08", f"(iw-iw/zoom)*(1-on/{frame_count})", center_y),
        "pan_down": ("1.08", center_x, f"(ih-ih/zoom)*on/{frame_count}"),
        "pan_up": ("1.08", center_x, f"(ih-ih/zoom)*(1-on/{frame_count})"),
    }
    if motion not in motions:
        raise ValueError(f"Невідомий рух «{motion}» у frames.json")
    return motions[motion]


def main() -> int:
    args = parse_args()

    if shutil.which("ffmpeg") is None:
        print("Не знайдено ffmpeg. Встанови його і переконайся, що він у PATH.", file=sys.stderr)
        return 1

    audio_path = Path(args.audio)
    if not audio_path.exists():
        print(f"Не знайдено аудіофайл: {audio_path}", file=sys.stderr)
        return 1

    config = json.loads(Path(args.json).read_text(encoding="utf-8"))
    frames = config["frames"]
    frames_dir = Path(args.frames_dir)

    images = [frames_dir / f"kadr_{frame['id']:02d}.png" for frame in frames]
    missing = [image.name for image in images if not image.exists()]
    if missing:
        print(f"У папці {frames_dir} бракує кадрів: {', '.join(missing)}", file=sys.stderr)
        print("Відбери варіанти з variants/ і поклади їх сюди під цими іменами.", file=sys.stderr)
        return 1

    durations = [frame["end"] - frame["start"] for frame in frames]
    total = frames[-1]["end"]
    transition = args.transition
    last = len(frames) - 1

    filters = []
    for index, frame in enumerate(frames):
        # Кожен кліп, крім останнього, подовжується на час переходу,
        # щоб межі кадрів після xfade припадали рівно на таймінги розкадровки.
        clip_seconds = durations[index] + (transition if index < last else 0)
        frame_count = round(clip_seconds * args.fps)
        zoom, x, y = ken_burns(frame["motion"], frame_count)
        filters.append(
            f"[{index}:v]scale=3840:2160,setsar=1,"
            f"zoompan=z='{zoom}':x='{x}':y='{y}':d={frame_count}:s=1920x1080:fps={args.fps},"
            f"settb=AVTB,format=yuv420p[v{index}]"
        )

    current = "v0"
    offset = 0.0
    for index in range(1, len(frames)):
        offset += durations[index - 1]
        merged = f"x{index}"
        filters.append(
            f"[{current}][v{index}]xfade=transition=fade:duration={transition}:offset={offset:.3f}[{merged}]"
        )
        current = merged

    finishing = [f"fade=t=out:st={total - 2:.3f}:d=2"]
    if not args.no_grade:
        finishing += [
            "colorbalance=bs=0.05:bm=0.02",
            "eq=contrast=1.05:saturation=0.95",
            "vignette=PI/5",
            "noise=alls=5:allf=t",
        ]
    finishing.append("format=yuv420p")
    filters.append(f"[{current}]{','.join(finishing)}[vout]")

    audio_index = len(frames)
    filters.append(f"[{audio_index}:a]afade=t=out:st={total - 2:.3f}:d=2[aout]")

    filter_script = Path(args.out).with_suffix(".filters.txt")
    filter_script.write_text(";\n".join(filters), encoding="utf-8")

    command = ["ffmpeg", "-y"]
    for image in images:
        command += ["-i", str(image)]
    command += ["-i", str(audio_path)]
    command += [
        "-filter_complex_script", str(filter_script),
        "-map", "[vout]",
        "-map", "[aout]",
        "-c:v", "libx264",
        "-crf", "18",
        "-preset", "medium",
        "-c:a", "aac",
        "-b:a", "192k",
        "-t", f"{total:.3f}",
        str(args.out),
    ]

    print("Запускаю ffmpeg — рендер займе кілька хвилин...")
    result = subprocess.run(command)
    if result.returncode != 0:
        print("ffmpeg завершився з помилкою — дивись повідомлення вище.", file=sys.stderr)
        return result.returncode

    filter_script.unlink(missing_ok=True)
    print(f"Готово: {args.out} ({total:.0f} сек, 1920x1080, {args.fps} fps)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
