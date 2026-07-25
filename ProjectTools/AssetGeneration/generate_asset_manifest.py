#!/usr/bin/env python3
"""Build a machine-readable inventory and validate generated Unity assets."""

from __future__ import annotations

import hashlib
import json
import wave
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
PACK = ROOT / "Assets" / "SpiderProjection"
OUTPUT = PACK / "Documentation" / "asset_manifest.json"


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def inspect_png(path: Path) -> dict:
    with Image.open(path) as image:
        return {
            "kind": "image",
            "size_px": list(image.size),
            "mode": image.mode,
            "has_alpha": "A" in image.getbands(),
        }


def inspect_gif(path: Path) -> dict:
    with Image.open(path) as image:
        return {
            "kind": "preview",
            "size_px": list(image.size),
            "frames": getattr(image, "n_frames", 1),
            "runtime_asset": False,
        }


def inspect_wav(path: Path) -> dict:
    with wave.open(str(path), "rb") as audio:
        frames = audio.getnframes()
        rate = audio.getframerate()
        channels = audio.getnchannels()
        return {
            "kind": "audio",
            "duration_seconds": round(frames / rate, 4),
            "sample_rate_hz": rate,
            "channels": channels,
            "sample_width_bytes": audio.getsampwidth(),
        }


def inspect_json(path: Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    return {
        "kind": "data",
        "schema": data.get("schema"),
    }


def inspect(path: Path) -> dict:
    suffix = path.suffix.lower()
    if suffix == ".png":
        details = inspect_png(path)
    elif suffix == ".gif":
        details = inspect_gif(path)
    elif suffix == ".wav":
        details = inspect_wav(path)
    elif suffix == ".json":
        details = inspect_json(path)
    else:
        details = {"kind": "document" if suffix in {".md", ".csv"} else "other"}
    details.update(
        {
            "path": path.relative_to(ROOT).as_posix(),
            "bytes": path.stat().st_size,
            "sha256": sha256(path),
        }
    )
    return details


def main() -> None:
    assets = []
    for path in sorted(PACK.rglob("*")):
        if not path.is_file() or path == OUTPUT or path.suffix == ".meta":
            continue
        assets.append(inspect(path))

    audio = [asset for asset in assets if asset["kind"] == "audio"]
    images = [asset for asset in assets if asset["kind"] == "image"]
    invalid_audio = [
        asset
        for asset in audio
        if asset["sample_rate_hz"] != 44100 or asset["channels"] != 2 or asset["bytes"] <= 44
    ]
    empty_images = [
        asset
        for asset in images
        if asset["size_px"][0] <= 0 or asset["size_px"][1] <= 0 or asset["bytes"] == 0
    ]
    if invalid_audio:
        raise RuntimeError(f"Invalid audio outputs: {invalid_audio}")
    if empty_images:
        raise RuntimeError(f"Invalid image outputs: {empty_images}")

    manifest = {
        "schema": "spider-projection.asset-pack.v1",
        "root": "Assets/SpiderProjection",
        "summary": {
            "files": len(assets),
            "runtime_images": len(images),
            "audio_files": len(audio),
            "music_files": len([asset for asset in audio if "/Music/" in asset["path"]]),
            "sfx_files": len([asset for asset in audio if "/SFX/" in asset["path"]]),
        },
        "import_defaults": {
            "pixel_art": {
                "texture_type": "Sprite (2D and UI)",
                "sprite_mode": "Multiple for atlases and strips; Single for standalone props/VFX",
                "pixels_per_unit": 32,
                "filter_mode": "Point",
                "compression": "None",
                "generate_mip_maps": False,
                "wrap_mode": "Clamp",
                "sRGB": True,
            },
            "audio": {
                "load_type_sfx": "Decompress On Load",
                "load_type_music": "Streaming",
                "compression_format_music": "Vorbis",
                "force_to_mono": False,
            },
        },
        "provenance": {
            "runtime_pixel_art": "Deterministic local Pillow generator; original shapes and palette; no source image pixels copied.",
            "audio": "Local LS-CLAD algorithmic synthesis; no samples, datasets, or Unity AI credits.",
            "third_party_reference_files_included": False,
            "character_note": "Crimson Crawler is an original runtime stand-in for a private fan prototype.",
        },
        "assets": assets,
    }
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest["summary"], indent=2))


if __name__ == "__main__":
    main()
