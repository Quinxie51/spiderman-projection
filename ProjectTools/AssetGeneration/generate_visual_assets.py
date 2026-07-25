#!/usr/bin/env python3
"""Generate the portable pixel-art pack for Spider Projection.

The output is deliberately deterministic and uses only opaque pixel clusters:
no antialiasing, external fonts, source artwork, or engine-specific materials.
"""

from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "SpiderProjection" / "Art"
CHARACTER = ART / "Character"
ENVIRONMENT = ART / "Environment"
VFX = ART / "VFX"
UI = ART / "UI"
PROJECTION = ART / "Projection"
PREVIEWS = ROOT / "Assets" / "SpiderProjection" / "Documentation" / "Previews"

FRAME = 64
PPU = 32
TRANSPARENT = (0, 0, 0, 0)

INK = (16, 16, 25, 255)
CRIMSON_DARK = (137, 18, 38, 255)
CRIMSON = (221, 30, 52, 255)
CRIMSON_LIGHT = (255, 67, 68, 255)
NAVY_DARK = (16, 34, 82, 255)
NAVY = (28, 76, 161, 255)
NAVY_LIGHT = (47, 121, 231, 255)
IVORY = (245, 248, 255, 255)
WEB = (211, 222, 238, 255)
GRAY = (101, 111, 133, 255)
GOLD_DARK = (116, 70, 25, 255)
GOLD = (213, 147, 52, 255)
GOLD_LIGHT = (255, 210, 96, 255)
WOOD_DARK = (73, 42, 28, 255)
WOOD = (137, 79, 43, 255)
WOOD_LIGHT = (195, 123, 61, 255)

Point = Tuple[int, int]
Pose = Dict[str, Point | int | float | bool]


def clamp_int(value: float) -> int:
    return int(round(value))


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


def pt(x: float, y: float) -> Point:
    return clamp_int(x), clamp_int(y)


def shifted(p: Point, dx: float = 0, dy: float = 0) -> Point:
    return pt(p[0] + dx, p[1] + dy)


def mix_point(a: Point, b: Point, t: float) -> Point:
    return pt(lerp(a[0], b[0], t), lerp(a[1], b[1], t))


def line(draw: ImageDraw.ImageDraw, points: Sequence[Point], color, width: int) -> None:
    draw.line(points, fill=color, width=width, joint="curve")


def outlined_limb(
    draw: ImageDraw.ImageDraw,
    a: Point,
    b: Point,
    c: Point,
    upper_color,
    lower_color,
    upper_width: int = 7,
    lower_width: int = 6,
) -> None:
    line(draw, [a, b], INK, upper_width + 4)
    line(draw, [b, c], INK, lower_width + 4)
    line(draw, [a, b], upper_color, upper_width)
    line(draw, [b, c], lower_color, lower_width)
    r = max(2, lower_width // 2)
    draw.ellipse((c[0] - r - 1, c[1] - r - 1, c[0] + r + 1, c[1] + r + 1), fill=INK)
    draw.ellipse((c[0] - r, c[1] - r, c[0] + r, c[1] + r), fill=lower_color)


def base_pose() -> Pose:
    return {
        "head": (33, 14),
        "neck": (32, 21),
        "shoulder_back": (27, 24),
        "shoulder_front": (37, 24),
        "elbow_back": (25, 31),
        "elbow_front": (40, 31),
        "hand_back": (26, 39),
        "hand_front": (39, 39),
        "hip_back": (29, 39),
        "hip_front": (35, 39),
        "knee_back": (29, 48),
        "knee_front": (36, 48),
        "foot_back": (27, 58),
        "foot_front": (39, 58),
        "torso_lean": 0,
        "eye_squint": 0,
        "web_hand": False,
    }


def set_points(pose: Pose, **kwargs) -> Pose:
    result = dict(pose)
    result.update(kwargs)
    return result


def move_pose(pose: Pose, dx: float = 0, dy: float = 0) -> Pose:
    result: Pose = {}
    for key, value in pose.items():
        if isinstance(value, tuple):
            result[key] = shifted(value, dx, dy)
        else:
            result[key] = value
    return result


def grounded_pose(kind: str, t: float, index: int) -> Pose:
    p = base_pose()
    phase = 2 * math.pi * t

    if kind == "idle":
        bob = 1 if index in (2, 3) else 0
        p = move_pose(p, 0, bob)
        p["hand_front"] = pt(40 + math.sin(phase) * 1.5, 38)
        p["hand_back"] = pt(25 - math.sin(phase) * 1.5, 39)
        p["eye_squint"] = 1 if index == 4 else 0
    elif kind == "run":
        bob = -abs(math.sin(phase)) * 2
        p = move_pose(p, 0, bob)
        stride = math.sin(phase) * 12
        arm = math.sin(phase) * 10
        p.update(
            {
                "head": pt(34, 15 + bob),
                "neck": pt(33, 22 + bob),
                "shoulder_back": pt(28, 25 + bob),
                "shoulder_front": pt(38, 24 + bob),
                "elbow_back": pt(27 - arm * 0.45, 31 + bob),
                "hand_back": pt(25 - arm, 36 + bob),
                "elbow_front": pt(40 + arm * 0.45, 31 + bob),
                "hand_front": pt(42 + arm, 36 + bob),
                "hip_back": pt(30, 39 + bob),
                "hip_front": pt(36, 39 + bob),
                "knee_back": pt(30 - stride * 0.45, 47 + bob - max(0, -stride) * 0.15),
                "foot_back": pt(28 - stride, 58),
                "knee_front": pt(36 + stride * 0.45, 47 + bob - max(0, stride) * 0.15),
                "foot_front": pt(38 + stride, 58),
                "torso_lean": 2,
            }
        )
    elif kind == "crouch":
        down = 6 + index
        p.update(
            {
                "head": pt(35, 18 + down),
                "neck": pt(34, 24 + down),
                "shoulder_back": pt(29, 26 + down),
                "shoulder_front": pt(39, 26 + down),
                "elbow_back": pt(27, 35 + down),
                "hand_back": pt(31, 41 + down),
                "elbow_front": pt(43, 34 + down),
                "hand_front": pt(45, 41 + down),
                "hip_back": pt(29, 40 + down),
                "hip_front": pt(35, 40 + down),
                "knee_back": pt(23, 49 + down * 0.2),
                "foot_back": pt(20, 58),
                "knee_front": pt(40, 49 + down * 0.2),
                "foot_front": pt(46, 58),
                "torso_lean": 3,
            }
        )
    elif kind in {"jump_start", "land"}:
        if kind == "jump_start":
            squash = [4, 7, 2][index]
            arm_raise = [0, 4, 10][index]
        else:
            squash = [1, 8, 5, 1][index]
            arm_raise = [10, 0, -3, 0][index]
        p.update(
            {
                "head": pt(34, 15 + squash),
                "neck": pt(33, 22 + squash),
                "shoulder_back": pt(28, 25 + squash),
                "shoulder_front": pt(38, 25 + squash),
                "elbow_back": pt(26 - arm_raise * 0.5, 32 + squash - arm_raise * 0.3),
                "hand_back": pt(24 - arm_raise, 38 + squash - arm_raise),
                "elbow_front": pt(40 + arm_raise * 0.5, 32 + squash - arm_raise * 0.3),
                "hand_front": pt(42 + arm_raise, 38 + squash - arm_raise),
                "hip_back": pt(29, 39 + squash),
                "hip_front": pt(35, 39 + squash),
                "knee_back": pt(24, 49 + squash * 0.2),
                "foot_back": pt(20, 58),
                "knee_front": pt(40, 49 + squash * 0.2),
                "foot_front": pt(46, 58),
                "torso_lean": 2,
            }
        )
    elif kind == "roll":
        angle = phase
        cx, cy = 32, 43
        p.update(
            {
                "head": pt(cx + math.cos(angle) * 11, cy + math.sin(angle) * 9),
                "neck": pt(cx + math.cos(angle + 0.5) * 6, cy + math.sin(angle + 0.5) * 5),
                "shoulder_back": pt(cx - 5, cy - 4),
                "shoulder_front": pt(cx + 4, cy - 3),
                "elbow_back": pt(cx - 10, cy),
                "hand_back": pt(cx - 5, cy + 6),
                "elbow_front": pt(cx + 9, cy - 1),
                "hand_front": pt(cx + 5, cy + 7),
                "hip_back": pt(cx - 4, cy + 5),
                "hip_front": pt(cx + 3, cy + 5),
                "knee_back": pt(cx - 10, cy + 8),
                "foot_back": pt(cx - 3, cy + 13),
                "knee_front": pt(cx + 10, cy + 8),
                "foot_front": pt(cx + 4, cy + 13),
                "eye_squint": 1,
            }
        )
    elif kind == "web_shoot":
        reach = [3, 9, 15, 10][index]
        p.update(
            {
                "head": (35, 15),
                "neck": (34, 22),
                "shoulder_front": (39, 24),
                "elbow_front": pt(43 + reach * 0.5, 25),
                "hand_front": pt(45 + reach, 24),
                "elbow_back": (25, 33),
                "hand_back": (23, 39),
                "torso_lean": 2,
                "web_hand": index >= 1,
            }
        )
    elif kind == "skid":
        lean = [2, 5, 7, 3][index]
        p.update(
            {
                "head": pt(35 + lean, 17),
                "neck": pt(34 + lean, 23),
                "shoulder_back": pt(28 + lean, 26),
                "shoulder_front": pt(39 + lean, 25),
                "elbow_back": (27, 32),
                "hand_back": (21, 36),
                "elbow_front": (44, 31),
                "hand_front": (48, 38),
                "knee_back": (25, 49),
                "foot_back": (17, 58),
                "knee_front": (39, 48),
                "foot_front": (51, 58),
                "torso_lean": lean,
            }
        )
    elif kind == "hurt":
        recoil = [2, 6, 3][index]
        p.update(
            {
                "head": pt(30 - recoil, 15),
                "neck": pt(31 - recoil, 22),
                "shoulder_back": pt(26 - recoil, 25),
                "shoulder_front": pt(36 - recoil, 25),
                "elbow_back": (20, 27),
                "hand_back": (16, 23),
                "elbow_front": (42, 29),
                "hand_front": (47, 25),
                "knee_back": (27, 48),
                "foot_back": (24, 58),
                "knee_front": (39, 48),
                "foot_front": (43, 58),
                "eye_squint": 1,
                "torso_lean": -recoil,
            }
        )
    elif kind == "celebrate":
        wave = math.sin(phase) * 4
        p.update(
            {
                "elbow_back": (23, 23),
                "hand_back": pt(20 + wave, 15),
                "elbow_front": (42, 23),
                "hand_front": pt(45 - wave, 13),
                "head": pt(33, 13 + abs(math.sin(phase)) * -2),
            }
        )
    return p


def airborne_pose(kind: str, t: float, index: int) -> Pose:
    p = move_pose(base_pose(), 0, -5)
    phase = 2 * math.pi * t

    if kind == "jump_rise":
        tuck = [8, 5, 2][index]
        p.update(
            {
                "hand_back": (22, 19),
                "elbow_back": (25, 24),
                "hand_front": (45, 17),
                "elbow_front": (41, 24),
                "knee_back": pt(28 - tuck, 43),
                "foot_back": pt(25 - tuck, 48),
                "knee_front": pt(38 + tuck * 0.5, 43),
                "foot_front": pt(42 + tuck, 48),
            }
        )
    elif kind == "apex":
        float_y = index
        p = move_pose(p, 0, float_y)
        p.update(
            {
                "elbow_back": (23, 26),
                "hand_back": (19, 30),
                "elbow_front": (43, 26),
                "hand_front": (47, 30),
                "knee_back": (26, 43),
                "foot_back": (22, 49),
                "knee_front": (40, 43),
                "foot_front": (45, 49),
            }
        )
    elif kind == "fall":
        spread = [8, 11, 9][index]
        p.update(
            {
                "elbow_back": pt(25 - spread * 0.5, 27),
                "hand_back": pt(24 - spread, 30),
                "elbow_front": pt(40 + spread * 0.5, 27),
                "hand_front": pt(41 + spread, 30),
                "knee_back": (27, 45),
                "foot_back": (25, 53),
                "knee_front": (38, 45),
                "foot_front": (40, 53),
            }
        )
    elif kind == "swing_attach":
        raise_amount = [4, 10, 16][index]
        p.update(
            {
                "elbow_front": pt(40 + raise_amount * 0.2, 25 - raise_amount * 0.4),
                "hand_front": pt(42 + raise_amount * 0.2, 27 - raise_amount),
                "elbow_back": (25, 29),
                "hand_back": (22, 34),
                "knee_back": (27, 44),
                "foot_back": (23, 50),
                "knee_front": (39, 43),
                "foot_front": (45, 47),
                "web_hand": index == 2,
            }
        )
    elif kind == "swing_loop":
        arc = math.sin(phase)
        lift = math.cos(phase) * 2
        p.update(
            {
                "head": pt(34 + arc * 2, 12 + lift),
                "neck": pt(33 + arc * 2, 19 + lift),
                "shoulder_back": pt(28 + arc * 2, 22 + lift),
                "shoulder_front": pt(38 + arc * 2, 22 + lift),
                "elbow_front": pt(39 + arc * 2, 15 + lift),
                "hand_front": pt(38 + arc * 2, 5),
                "elbow_back": pt(27 - arc * 5, 28 + lift),
                "hand_back": pt(23 - arc * 8, 33 + lift),
                "hip_back": pt(29 + arc * 3, 37 + lift),
                "hip_front": pt(35 + arc * 3, 37 + lift),
                "knee_back": pt(25 - arc * 7, 44 + lift),
                "foot_back": pt(18 - arc * 10, 48 + lift),
                "knee_front": pt(38 - arc * 6, 44 + lift),
                "foot_front": pt(43 - arc * 12, 50 + lift),
                "torso_lean": arc * 4,
                "web_hand": True,
            }
        )
    elif kind == "swing_release":
        reach = [0, 5, 10][index]
        p.update(
            {
                "elbow_front": pt(40 + reach * 0.3, 17 + reach * 0.5),
                "hand_front": pt(39 + reach, 7 + reach),
                "elbow_back": (25, 26),
                "hand_back": pt(19 - reach * 0.5, 29),
                "knee_back": (25, 43),
                "foot_back": pt(18 - reach, 48),
                "knee_front": (39, 44),
                "foot_front": pt(45 - reach, 52),
                "web_hand": index == 0,
            }
        )
    elif kind == "defeat":
        drop = [0, 5, 10, 13][index]
        p = move_pose(p, 0, drop)
        p.update(
            {
                "head": pt(29, 17 + drop),
                "neck": pt(31, 23 + drop),
                "elbow_back": pt(20, 30 + drop),
                "hand_back": pt(15, 34 + drop),
                "elbow_front": pt(42, 31 + drop),
                "hand_front": pt(48, 36 + drop),
                "knee_back": pt(26, 45 + drop * 0.5),
                "foot_back": pt(20, 52 + drop * 0.4),
                "knee_front": pt(39, 45 + drop * 0.5),
                "foot_front": pt(45, 52 + drop * 0.4),
                "eye_squint": 1,
            }
        )
    return p


def wall_pose(kind: str, t: float, index: int) -> Pose:
    p = base_pose()
    phase = 2 * math.pi * t
    if kind in {"wall_cling", "wall_crawl", "ledge_grab", "ledge_climb"}:
        p.update(
            {
                "head": (39, 17),
                "neck": (36, 23),
                "shoulder_back": (31, 25),
                "shoulder_front": (40, 25),
                "hip_back": (31, 39),
                "hip_front": (37, 39),
            }
        )
    if kind == "wall_cling":
        twitch = index
        p.update(
            {
                "elbow_back": (27, 31),
                "hand_back": (29, 38),
                "elbow_front": (46, 30),
                "hand_front": pt(52, 34 + twitch),
                "knee_back": (27, 47),
                "foot_back": (24, 55),
                "knee_front": (44, 46),
                "foot_front": pt(52, 49 + twitch),
            }
        )
    elif kind == "wall_crawl":
        reach = math.sin(phase) * 7
        p.update(
            {
                "elbow_back": pt(27, 30 - reach * 0.3),
                "hand_back": pt(30, 37 - reach),
                "elbow_front": pt(45, 30 + reach * 0.3),
                "hand_front": pt(52, 35 + reach),
                "knee_back": pt(27, 47 + reach * 0.4),
                "foot_back": pt(24, 55 + reach),
                "knee_front": pt(44, 46 - reach * 0.4),
                "foot_front": pt(52, 50 - reach),
            }
        )
    elif kind == "ledge_grab":
        p = move_pose(p, 0, 5 + index)
        p.update(
            {
                "elbow_back": (29, 19),
                "hand_back": (31, 12),
                "elbow_front": (43, 19),
                "hand_front": (45, 12),
                "knee_back": (28, 49),
                "foot_back": (24, 57),
                "knee_front": (40, 49),
                "foot_front": (46, 57),
            }
        )
    elif kind == "ledge_climb":
        progress = index / 4
        y = lerp(8, -6, progress)
        p = move_pose(p, progress * 5, y)
        p.update(
            {
                "elbow_back": pt(28 + progress * 7, 21 - progress * 7),
                "hand_back": pt(31 + progress * 7, 12),
                "elbow_front": pt(43 + progress * 4, 20 - progress * 7),
                "hand_front": pt(45 + progress * 4, 12),
                "knee_back": pt(27, lerp(50, 40, progress)),
                "foot_back": pt(23, lerp(58, 48, progress)),
                "knee_front": pt(43, lerp(49, 36, progress)),
                "foot_front": pt(51, lerp(55, 40, progress)),
            }
        )
    return p


def pose_for(kind: str, index: int, count: int) -> Pose:
    t = index / count
    if kind in {
        "idle",
        "run",
        "crouch",
        "jump_start",
        "land",
        "roll",
        "web_shoot",
        "skid",
        "hurt",
        "celebrate",
    }:
        return grounded_pose(kind, t, index)
    if kind in {
        "jump_rise",
        "apex",
        "fall",
        "swing_attach",
        "swing_loop",
        "swing_release",
        "defeat",
    }:
        return airborne_pose(kind, t, index)
    return wall_pose(kind, t, index)


def draw_hero(pose: Pose) -> Image.Image:
    image = Image.new("RGBA", (FRAME, FRAME), TRANSPARENT)
    draw = ImageDraw.Draw(image)

    shoulder_back = pose["shoulder_back"]
    shoulder_front = pose["shoulder_front"]
    elbow_back = pose["elbow_back"]
    elbow_front = pose["elbow_front"]
    hand_back = pose["hand_back"]
    hand_front = pose["hand_front"]
    hip_back = pose["hip_back"]
    hip_front = pose["hip_front"]
    knee_back = pose["knee_back"]
    knee_front = pose["knee_front"]
    foot_back = pose["foot_back"]
    foot_front = pose["foot_front"]
    neck = pose["neck"]
    head = pose["head"]

    # Back limbs first.
    outlined_limb(draw, hip_back, knee_back, foot_back, NAVY_DARK, CRIMSON_DARK, 8, 7)
    outlined_limb(draw, shoulder_back, elbow_back, hand_back, CRIMSON_DARK, CRIMSON, 7, 6)

    # Torso and waist.
    torso = [
        shifted(shoulder_back, -2, -1),
        shifted(shoulder_front, 2, -1),
        shifted(hip_front, 3, 1),
        shifted(hip_back, -3, 1),
    ]
    draw.polygon(torso, fill=INK)
    torso_inner = [
        shifted(shoulder_back, 0, 1),
        shifted(shoulder_front, 0, 1),
        shifted(hip_front, 1, -1),
        shifted(hip_back, -1, -1),
    ]
    draw.polygon(torso_inner, fill=NAVY)
    chest_y = clamp_int((shoulder_front[1] + neck[1]) / 2 + 3)
    line(draw, [shifted(shoulder_back, 1, 2), (neck[0], chest_y), shifted(shoulder_front, -1, 2)], CRIMSON, 4)
    draw.rectangle((hip_back[0] - 1, hip_back[1] - 3, hip_front[0] + 1, hip_front[1] + 2), fill=CRIMSON_DARK)

    # Abstract eight-point chest mark.
    chest = (neck[0], chest_y + 5)
    draw.rectangle((chest[0] - 1, chest[1] - 2, chest[0] + 1, chest[1] + 3), fill=INK)
    line(draw, [(chest[0] - 4, chest[1] - 1), (chest[0] + 4, chest[1] + 2)], INK, 1)
    line(draw, [(chest[0] + 4, chest[1] - 1), (chest[0] - 4, chest[1] + 2)], INK, 1)

    # Front limbs.
    outlined_limb(draw, hip_front, knee_front, foot_front, NAVY, CRIMSON, 8, 7)
    outlined_limb(draw, shoulder_front, elbow_front, hand_front, CRIMSON, CRIMSON_LIGHT, 7, 6)

    # Neck and head.
    line(draw, [neck, head], INK, 8)
    line(draw, [neck, head], CRIMSON_DARK, 5)
    hx, hy = head
    draw.ellipse((hx - 8, hy - 9, hx + 8, hy + 9), fill=INK)
    draw.ellipse((hx - 6, hy - 7, hx + 6, hy + 7), fill=CRIMSON)
    draw.rectangle((hx - 5, hy + 2, hx + 5, hy + 6), fill=CRIMSON_DARK)
    draw.rectangle((hx - 4, hy - 5, hx + 4, hy - 3), fill=CRIMSON_LIGHT)

    squint = int(pose.get("eye_squint", 0))
    eye_height = 5 - squint * 2
    # Asymmetric mask lenses give a readable three-quarter direction.
    draw.polygon(
        [(hx - 5, hy - 3), (hx - 1, hy - 1), (hx - 2, hy + eye_height), (hx - 6, hy + 2)],
        fill=INK,
    )
    draw.polygon(
        [(hx - 4, hy - 2), (hx - 2, hy - 1), (hx - 3, hy + eye_height - 1), (hx - 5, hy + 1)],
        fill=IVORY,
    )
    draw.polygon(
        [(hx + 1, hy - 2), (hx + 6, hy - 4), (hx + 6, hy + 2), (hx + 2, hy + eye_height)],
        fill=INK,
    )
    draw.polygon(
        [(hx + 2, hy - 1), (hx + 5, hy - 2), (hx + 5, hy + 1), (hx + 3, hy + eye_height - 1)],
        fill=IVORY,
    )

    # Sparse hard-pixel mask seam accents.
    line(draw, [(hx, hy - 7), (hx, hy + 5)], CRIMSON_DARK, 1)
    line(draw, [(hx - 5, hy + 1), (hx + 5, hy + 1)], CRIMSON_DARK, 1)

    if pose.get("web_hand"):
        x, y = hand_front
        line(draw, [(x, y), (min(63, x + 10), max(0, y - 9))], WEB, 2)
        draw.rectangle((min(62, x + 9), max(0, y - 10), min(63, x + 11), max(2, y - 8)), fill=IVORY)

    return image


ANIMATIONS = [
    ("idle", 6, 8, True),
    ("run", 8, 12, True),
    ("crouch", 2, 6, True),
    ("jump_start", 3, 12, False),
    ("jump_rise", 3, 10, False),
    ("apex", 2, 6, True),
    ("fall", 3, 8, True),
    ("land", 4, 12, False),
    ("roll", 6, 14, False),
    ("web_shoot", 4, 12, False),
    ("swing_attach", 3, 12, False),
    ("swing_loop", 8, 12, True),
    ("swing_release", 3, 12, False),
    ("wall_cling", 2, 6, True),
    ("wall_crawl", 6, 10, True),
    ("ledge_grab", 2, 6, True),
    ("ledge_climb", 5, 10, False),
    ("skid", 4, 12, False),
    ("hurt", 3, 10, False),
    ("defeat", 4, 8, False),
    ("celebrate", 6, 8, True),
]


def save_character_assets() -> None:
    frames_by_animation: Dict[str, List[Image.Image]] = {}
    atlas_frames: List[Tuple[str, int, Image.Image]] = []
    manifest = {
        "schema": "spider-projection.sprite-library.v1",
        "character": "Crimson Crawler",
        "frame_size_px": [FRAME, FRAME],
        "pixels_per_unit": PPU,
        "pivot_normalized": [0.5, 0.32],
        "filter_mode": "Point",
        "compression": "None",
        "wrap_mode": "Clamp",
        "animations": [],
    }

    for name, count, fps, loop in ANIMATIONS:
        frames = [draw_hero(pose_for(name, i, count)) for i in range(count)]
        frames_by_animation[name] = frames
        strip = Image.new("RGBA", (count * FRAME, FRAME), TRANSPARENT)
        for i, frame in enumerate(frames):
            strip.alpha_composite(frame, (i * FRAME, 0))
            atlas_frames.append((name, i, frame))
        strip.save(CHARACTER / f"crimson_crawler_{name}.png", optimize=True)
        manifest["animations"].append(
            {
                "name": name,
                "frames": count,
                "fps": fps,
                "loop": loop,
                "strip": f"crimson_crawler_{name}.png",
                "unity_state": "".join(part.title() for part in name.split("_")),
            }
        )

    columns = 8
    rows = math.ceil(len(atlas_frames) / columns)
    atlas = Image.new("RGBA", (columns * FRAME, rows * FRAME), TRANSPARENT)
    for atlas_index, (animation, frame_index, frame) in enumerate(atlas_frames):
        col = atlas_index % columns
        row = atlas_index // columns
        atlas.alpha_composite(frame, (col * FRAME, row * FRAME))
        for animation_entry in manifest["animations"]:
            if animation_entry["name"] == animation:
                animation_entry.setdefault("atlas_frames", []).append(
                    {
                        "frame": frame_index,
                        "atlas_index": atlas_index,
                        "rect_top_left_px": [col * FRAME, row * FRAME, FRAME, FRAME],
                        "rect_unity_bottom_left_px": [
                            col * FRAME,
                            atlas.height - ((row + 1) * FRAME),
                            FRAME,
                            FRAME,
                        ],
                    }
                )
                break
    atlas.save(CHARACTER / "crimson_crawler_atlas.png", optimize=True)
    manifest["atlas"] = {
        "file": "crimson_crawler_atlas.png",
        "columns": columns,
        "rows": rows,
        "size_px": [atlas.width, atlas.height],
        "total_frames": len(atlas_frames),
    }
    (CHARACTER / "crimson_crawler_manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )

    # Animated preview reel. Runtime uses the PNG strips, not this GIF.
    reel_frames: List[Image.Image] = []
    for name in ("idle", "run", "jump_rise", "swing_loop", "land", "roll", "wall_crawl", "celebrate"):
        for frame in frames_by_animation[name]:
            reel_frames.append(frame.resize((FRAME * 4, FRAME * 4), Image.Resampling.NEAREST))
    reel_frames[0].save(
        PREVIEWS / "crimson_crawler_animation_reel.gif",
        save_all=True,
        append_images=reel_frames[1:],
        duration=90,
        loop=0,
        disposal=2,
        transparency=0,
    )

    # Static contact sheet for easy visual QA.
    font = ImageFont.load_default()
    preview = Image.new("RGB", (4 * 272, math.ceil(len(ANIMATIONS) / 4) * 296), (28, 30, 38))
    d = ImageDraw.Draw(preview)
    for index, (name, _, _, _) in enumerate(ANIMATIONS):
        col = index % 4
        row = index // 4
        x = col * 272
        y = row * 296
        frame = frames_by_animation[name][0].resize((256, 256), Image.Resampling.NEAREST)
        preview.paste(frame, (x + 8, y + 8), frame)
        d.text((x + 10, y + 268), name, fill=(236, 239, 248), font=font)
    preview.save(PREVIEWS / "crimson_crawler_contact_sheet.png", optimize=True)


def rect_outline(draw: ImageDraw.ImageDraw, box, outer, inner, border=5) -> None:
    draw.rectangle(box, fill=outer)
    x0, y0, x1, y1 = box
    draw.rectangle((x0 + border, y0 + border, x1 - border, y1 - border), fill=inner)


def prop_picture_frame(size=(128, 128), variant=0) -> Image.Image:
    im = Image.new("RGBA", size, TRANSPARENT)
    d = ImageDraw.Draw(im)
    styles = [
        (GOLD_DARK, GOLD, (55, 78, 112, 255)),
        (INK, GRAY, (90, 35, 48, 255)),
        (WOOD_DARK, WOOD, (48, 92, 85, 255)),
        (NAVY_DARK, NAVY, (189, 126, 59, 255)),
    ]
    outer, inner, art_bg = styles[variant % len(styles)]
    box = (15, 18, 112, 108) if variant % 2 == 0 else (22, 12, 105, 114)
    rect_outline(d, box, INK, outer, 5)
    x0, y0, x1, y1 = box
    rect_outline(d, (x0 + 7, y0 + 7, x1 - 7, y1 - 7), inner, art_bg, 5)
    # Abstract in-frame picture: original geometric skyline/mountain.
    d.rectangle((x0 + 17, y1 - 36, x1 - 17, y1 - 17), fill=(31, 42, 69, 255))
    d.polygon(
        [(x0 + 20, y1 - 25), (x0 + 38, y0 + 28), (x0 + 53, y1 - 26), (x0 + 68, y0 + 35), (x1 - 18, y1 - 25)],
        fill=(CRIMSON_DARK if variant % 2 else GOLD_LIGHT),
    )
    d.rectangle((x0 + 16, y0 + 16, x0 + 22, y0 + 22), fill=IVORY)
    return im


def prop_shelf(size=(128, 128), books=False, long=False) -> Image.Image:
    im = Image.new("RGBA", size, TRANSPARENT)
    d = ImageDraw.Draw(im)
    x0, x1 = (5, 123) if long else (16, 112)
    d.rectangle((x0, 73, x1, 84), fill=INK)
    d.rectangle((x0 + 2, 75, x1 - 2, 80), fill=WOOD_LIGHT)
    d.rectangle((x0 + 7, 84, x0 + 17, 104), fill=INK)
    d.rectangle((x1 - 17, 84, x1 - 7, 104), fill=INK)
    d.polygon([(x0 + 8, 84), (x0 + 24, 84), (x0 + 12, 101)], fill=WOOD_DARK)
    d.polygon([(x1 - 24, 84), (x1 - 8, 84), (x1 - 12, 101)], fill=WOOD_DARK)
    if books:
        colors = (CRIMSON, NAVY_LIGHT, GOLD, (71, 137, 85, 255))
        cursor = x0 + 12
        for i, color in enumerate(colors):
            width = 13 + (i % 2) * 3
            height = 27 + (i % 3) * 5
            d.rectangle((cursor, 72 - height, cursor + width, 72), fill=INK)
            d.rectangle((cursor + 2, 74 - height, cursor + width - 2, 70), fill=color)
            cursor += width + 3
    return im


def prop_object(kind: str) -> Image.Image:
    im = Image.new("RGBA", (128, 128), TRANSPARENT)
    d = ImageDraw.Draw(im)
    if kind.startswith("frame_"):
        return prop_picture_frame(variant=int(kind[-1]))
    if kind == "shelf_short":
        return prop_shelf()
    if kind == "shelf_long":
        return prop_shelf(long=True)
    if kind == "shelf_books":
        return prop_shelf(books=True, long=True)
    if kind == "wall_vent":
        rect_outline(d, (20, 35, 108, 93), INK, GRAY, 5)
        d.rectangle((30, 45, 98, 83), fill=(40, 44, 54, 255))
        for y in range(50, 82, 8):
            d.rectangle((34, y, 94, y + 3), fill=(139, 147, 164, 255))
    elif kind == "speaker":
        rect_outline(d, (33, 15, 96, 112), INK, (46, 49, 62, 255), 5)
        for cy, r in ((46, 15), (84, 21)):
            d.ellipse((64 - r, cy - r, 64 + r, cy + r), fill=INK)
            d.ellipse((64 - r + 4, cy - r + 4, 64 + r - 4, cy + r - 4), fill=GRAY)
            d.ellipse((60, cy - 4, 68, cy + 4), fill=INK)
    elif kind == "plant":
        d.rectangle((42, 77, 86, 111), fill=INK)
        d.polygon([(46, 80), (82, 80), (77, 106), (51, 106)], fill=CRIMSON_DARK)
        for box, color in (
            ((29, 28, 61, 79), (49, 119, 72, 255)),
            ((55, 16, 89, 77), (64, 151, 82, 255)),
            ((68, 36, 104, 82), (43, 104, 67, 255)),
        ):
            d.ellipse(box, fill=INK)
            x0, y0, x1, y1 = box
            d.ellipse((x0 + 4, y0 + 4, x1 - 4, y1 - 4), fill=color)
    elif kind == "clock":
        d.ellipse((20, 20, 108, 108), fill=INK)
        d.ellipse((27, 27, 101, 101), fill=IVORY)
        d.ellipse((59, 59, 69, 69), fill=INK)
        line(d, [(64, 64), (64, 39)], INK, 5)
        line(d, [(64, 64), (84, 72)], CRIMSON_DARK, 5)
    elif kind == "wall_hook":
        d.rectangle((52, 14, 76, 44), fill=INK)
        d.rectangle((57, 19, 71, 39), fill=GOLD)
        line(d, [(64, 40), (64, 80), (82, 94)], INK, 10)
        line(d, [(64, 40), (64, 80), (82, 94)], GOLD_LIGHT, 5)
    elif kind == "window":
        rect_outline(d, (14, 12, 114, 116), INK, NAVY_DARK, 6)
        d.rectangle((25, 23, 103, 105), fill=(60, 103, 143, 255))
        d.rectangle((59, 23, 69, 105), fill=INK)
        d.rectangle((25, 59, 103, 69), fill=INK)
        d.rectangle((31, 29, 53, 53), fill=(111, 171, 203, 255))
    elif kind == "cabinet":
        rect_outline(d, (13, 33, 115, 119), INK, WOOD_DARK, 6)
        d.rectangle((23, 43, 105, 109), fill=WOOD)
        d.rectangle((59, 43, 69, 109), fill=INK)
        d.rectangle((48, 72, 56, 80), fill=GOLD_LIGHT)
        d.rectangle((72, 72, 80, 80), fill=GOLD_LIGHT)
    return im


PROPS = [
    "frame_0",
    "frame_1",
    "frame_2",
    "frame_3",
    "shelf_short",
    "shelf_long",
    "shelf_books",
    "wall_vent",
    "speaker",
    "plant",
    "clock",
    "wall_hook",
    "window",
    "cabinet",
]


def save_environment_assets() -> None:
    atlas = Image.new("RGBA", (4 * 128, 4 * 128), TRANSPARENT)
    entries = []
    for index, name in enumerate(PROPS):
        image = prop_object(name)
        image.save(ENVIRONMENT / f"{name}.png", optimize=True)
        col, row = index % 4, index // 4
        atlas.alpha_composite(image, (col * 128, row * 128))
        entries.append(
            {
                "name": name,
                "file": f"{name}.png",
                "atlas_rect_top_left_px": [col * 128, row * 128, 128, 128],
                "suggested_collider": "BoxCollider2D",
                "is_web_anchor_surface": True,
            }
        )
    atlas.save(ENVIRONMENT / "wall_obstacle_atlas.png", optimize=True)
    manifest = {
        "schema": "spider-projection.environment-library.v1",
        "cell_size_px": [128, 128],
        "pixels_per_unit": PPU,
        "atlas": "wall_obstacle_atlas.png",
        "entries": entries,
        "note": "These visible props are demo stand-ins. Projection-mapped real frames/shelves should use invisible Collider2D geometry and WebAnchor2D components.",
    }
    (ENVIRONMENT / "wall_obstacle_manifest.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )


VFX_NAMES = [
    "web_muzzle",
    "web_attach",
    "dust_small",
    "dust_large",
    "impact_star",
    "swing_arc",
    "speed_lines",
    "danger_pulse",
    "land_ring",
    "roll_dust",
    "goal_sparkle",
    "anchor_debug",
]


def draw_vfx(name: str) -> Image.Image:
    im = Image.new("RGBA", (64, 64), TRANSPARENT)
    d = ImageDraw.Draw(im)
    cx, cy = 32, 32
    if name == "web_muzzle":
        for angle in range(0, 360, 45):
            r = 10 if angle % 90 else 17
            end = pt(cx + math.cos(math.radians(angle)) * r, cy + math.sin(math.radians(angle)) * r)
            line(d, [(cx, cy), end], WEB, 2)
        d.ellipse((27, 27, 37, 37), outline=IVORY, width=2)
    elif name == "web_attach":
        for r in (6, 13, 20):
            d.ellipse((cx - r, cy - r, cx + r, cy + r), outline=WEB, width=2)
        for angle in range(0, 360, 60):
            end = pt(cx + math.cos(math.radians(angle)) * 22, cy + math.sin(math.radians(angle)) * 22)
            line(d, [(cx, cy), end], IVORY, 2)
    elif name in {"dust_small", "dust_large", "roll_dust"}:
        radius = 18 if name == "dust_small" else 25
        blobs = 5 if name == "dust_small" else 8
        if name == "roll_dust":
            blobs = 7
        for i in range(blobs):
            angle = i * math.tau / blobs
            x = cx + math.cos(angle) * radius * 0.55
            y = cy + math.sin(angle) * radius * 0.25 + 8
            r = 4 + (i % 3) * 2
            d.ellipse((x - r, y - r, x + r, y + r), fill=(174, 182, 197, 220))
    elif name == "impact_star":
        points = []
        for i in range(16):
            angle = -math.pi / 2 + i * math.pi / 8
            r = 25 if i % 2 == 0 else 10
            points.append(pt(cx + math.cos(angle) * r, cy + math.sin(angle) * r))
        d.polygon(points, fill=GOLD_LIGHT, outline=INK)
        d.ellipse((26, 26, 38, 38), fill=IVORY)
    elif name == "swing_arc":
        d.arc((4, 8, 60, 60), 195, 342, fill=WEB, width=4)
        d.arc((9, 13, 55, 55), 195, 342, fill=IVORY, width=2)
    elif name == "speed_lines":
        for i, y in enumerate((16, 25, 34, 43, 51)):
            x0 = 7 + (i % 2) * 9
            line(d, [(x0, y), (55 - i * 2, y - (i % 2) * 2)], IVORY if i % 2 else WEB, 2)
    elif name == "danger_pulse":
        for r, alpha in ((9, 255), (17, 210), (25, 150)):
            d.arc((cx - r, cy - r, cx + r, cy + r), 205, 335, fill=(255, 70, 74, alpha), width=3)
    elif name == "land_ring":
        d.ellipse((7, 25, 57, 45), outline=IVORY, width=3)
        d.ellipse((16, 29, 48, 41), outline=WEB, width=2)
    elif name == "goal_sparkle":
        for x, y, r in ((32, 30, 18), (12, 45, 7), (52, 13, 6)):
            d.polygon([(x, y - r), (x + 3, y - 3), (x + r, y), (x + 3, y + 3), (x, y + r), (x - 3, y + 3), (x - r, y), (x - 3, y - 3)], fill=GOLD_LIGHT)
    elif name == "anchor_debug":
        d.ellipse((13, 13, 51, 51), outline=(65, 230, 255, 255), width=3)
        line(d, [(32, 7), (32, 57)], (65, 230, 255, 255), 2)
        line(d, [(7, 32), (57, 32)], (65, 230, 255, 255), 2)
        d.rectangle((27, 27, 37, 37), fill=IVORY)
    return im


def save_vfx_assets() -> None:
    atlas = Image.new("RGBA", (4 * 64, 3 * 64), TRANSPARENT)
    entries = []
    for index, name in enumerate(VFX_NAMES):
        image = draw_vfx(name)
        image.save(VFX / f"{name}.png", optimize=True)
        col, row = index % 4, index // 4
        atlas.alpha_composite(image, (col * 64, row * 64))
        entries.append({"name": name, "file": f"{name}.png", "atlas_index": index})
    atlas.save(VFX / "vfx_atlas.png", optimize=True)
    (VFX / "vfx_manifest.json").write_text(
        json.dumps(
            {
                "schema": "spider-projection.vfx-library.v1",
                "cell_size_px": [64, 64],
                "pixels_per_unit": PPU,
                "atlas": "vfx_atlas.png",
                "entries": entries,
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )


def save_input_glyphs() -> None:
    labels = ["A", "D", "SPACE", "SHIFT", "E", "LMB", "LS", "A", "X", "RT", "LB", "START"]
    atlas = Image.new("RGBA", (4 * 96, 3 * 64), TRANSPARENT)
    font = ImageFont.load_default()
    for index, label in enumerate(labels):
        col, row = index % 4, index // 4
        x, y = col * 96, row * 64
        d = ImageDraw.Draw(atlas)
        if index < 6:
            d.rounded_rectangle((x + 8, y + 10, x + 88, y + 54), radius=5, fill=INK, outline=IVORY, width=2)
        else:
            d.ellipse((x + 24, y + 8, x + 72, y + 56), fill=INK, outline=IVORY, width=2)
        bbox = d.textbbox((0, 0), label, font=font)
        tx = x + 48 - (bbox[2] - bbox[0]) // 2
        ty = y + 32 - (bbox[3] - bbox[1]) // 2
        d.text((tx, ty), label, fill=IVORY, font=font)
    atlas.save(UI / "input_glyphs.png", optimize=True)
    (UI / "input_glyphs_manifest.json").write_text(
        json.dumps(
            {
                "schema": "spider-projection.input-glyphs.v1",
                "cell_size_px": [96, 64],
                "columns": 4,
                "labels": labels,
                "note": "Use Input System control schemes to choose keyboard or generic gamepad glyphs at runtime.",
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )


def save_projection_assets() -> None:
    width, height = 1920, 1080
    image = Image.new("RGB", (width, height), (0, 0, 0))
    d = ImageDraw.Draw(image)
    font = ImageFont.load_default()
    for x in range(0, width, 120):
        color = (44, 44, 50) if x % 480 else (110, 110, 122)
        d.line((x, 0, x, height), fill=color, width=1)
    for y in range(0, height, 120):
        color = (44, 44, 50) if y % 480 else (110, 110, 122)
        d.line((0, y, width, y), fill=color, width=1)
    d.rectangle((1, 1, width - 2, height - 2), outline=(245, 248, 255), width=3)
    d.rectangle((96, 54, width - 97, height - 55), outline=(65, 230, 255), width=2)
    d.rectangle((192, 108, width - 193, height - 109), outline=(255, 67, 177), width=2)
    d.line((width // 2, 0, width // 2, height), fill=(255, 67, 68), width=2)
    d.line((0, height // 2, width, height // 2), fill=(255, 67, 68), width=2)
    for x, y, label in (
        (12, 12, "TOP LEFT"),
        (width - 90, 12, "TOP RIGHT"),
        (12, height - 24, "BOTTOM LEFT"),
        (width - 108, height - 24, "BOTTOM RIGHT"),
        (width // 2 - 26, height // 2 - 18, "CENTER"),
    ):
        d.rectangle((x - 4, y - 4, x + 84, y + 16), fill=(0, 0, 0))
        d.text((x, y), label, fill=(245, 248, 255), font=font)
    image.save(PROJECTION / "projection_calibration_1920x1080.png", optimize=True)

    safe = Image.new("RGBA", (width, height), TRANSPARENT)
    sd = ImageDraw.Draw(safe)
    sd.rectangle((96, 54, width - 97, height - 55), outline=(65, 230, 255, 255), width=3)
    sd.rectangle((192, 108, width - 193, height - 109), outline=(255, 67, 177, 255), width=3)
    sd.line((width // 2, 0, width // 2, height), fill=(255, 67, 68, 170), width=2)
    sd.line((0, height // 2, width, height // 2), fill=(255, 67, 68, 170), width=2)
    safe.save(PROJECTION / "projection_safe_area_overlay.png", optimize=True)

    Image.new("RGB", (width, height), (0, 0, 0)).save(
        PROJECTION / "projection_blackout_1920x1080.png", optimize=True
    )


def ensure_directories() -> None:
    for directory in (CHARACTER, ENVIRONMENT, VFX, UI, PROJECTION, PREVIEWS):
        directory.mkdir(parents=True, exist_ok=True)


def main() -> None:
    ensure_directories()
    save_character_assets()
    save_environment_assets()
    save_vfx_assets()
    save_input_glyphs()
    save_projection_assets()
    print(f"Generated visual assets under {ART}")


if __name__ == "__main__":
    main()
