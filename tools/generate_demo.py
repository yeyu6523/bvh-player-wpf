"""Generate an original synthetic BVH walking cycle; no third-party mocap data."""
from pathlib import Path
import math

ROOT = Path(__file__).resolve().parents[1]

# (name, offset, children). The preorder is exactly the channel storage order.
def joint(name, xyz, *children):
    return name, xyz, children

tree = joint("Hips", (0, 0, 0),
    joint("Spine", (0, 17, 0),
        joint("Chest", (0, 19, 0),
            joint("Neck", (0, 14, 0), joint("Head", (0, 10, 0), joint("End", (0, 12, 0)))),
            joint("LeftShoulder", (13, 11, 0), joint("LeftArm", (8, 0, 0),
                joint("LeftForeArm", (24, 0, 0), joint("LeftHand", (22, 0, 0), joint("End", (8, 0, 0)))))),
            joint("RightShoulder", (-13, 11, 0), joint("RightArm", (-8, 0, 0),
                joint("RightForeArm", (-24, 0, 0), joint("RightHand", (-22, 0, 0), joint("End", (-8, 0, 0)))))))),
    joint("LeftUpLeg", (9, -6, 0), joint("LeftLeg", (0, -40, 0),
        joint("LeftFoot", (0, -39, 0), joint("End", (0, -6, 14))))),
    joint("RightUpLeg", (-9, -6, 0), joint("RightLeg", (0, -40, 0),
        joint("RightFoot", (0, -39, 0), joint("End", (0, -6, 14))))))

lines = ["HIERARCHY"]
names = []
def hierarchy(node, depth=0):
    name, offset, children = node
    pad = "  " * depth
    lines.append(pad + ("End Site" if name == "End" else ("ROOT " if depth == 0 else "JOINT ") + name))
    lines.append(pad + "{")
    lines.append(pad + "  OFFSET " + " ".join(str(v) for v in offset))
    if name != "End":
        names.append(name)
        lines.append(pad + "  CHANNELS " + ("6 Xposition Yposition Zposition Zrotation Xrotation Yrotation" if depth == 0 else "3 Zrotation Xrotation Yrotation"))
    for child in children:
        hierarchy(child, depth + 1)
    lines.append(pad + "}")

hierarchy(tree)
lines += ["MOTION", "Frames: 240", "Frame Time: 0.016666666666666666"]
for frame in range(240):
    phase = 2 * math.pi * frame / 60
    swing = math.sin(phase)
    values = [2 * math.cos(phase), 96 + 1.8 * math.cos(phase * 2), frame * 0.75,
              2 * swing, 0, 4 * swing]
    rotations = {
        "Spine": (-1.5 * swing, 3, -2 * swing), "Chest": (0, 0, -4 * swing),
        "Neck": (0, -2, 0), "Head": (0, 0, 0),
        "LeftShoulder": (-8, 0, 0), "LeftArm": (-64, -25 * swing, 3),
        "LeftForeArm": (0, 0, -12 - 8 * max(0, -swing)), "LeftHand": (0, 0, 0),
        "RightShoulder": (8, 0, 0), "RightArm": (64, 25 * swing, -3),
        "RightForeArm": (0, 0, 12 + 8 * max(0, swing)), "RightHand": (0, 0, 0),
        "LeftUpLeg": (0, 29 * swing, 0), "LeftLeg": (0, -40 * max(0, swing), 0),
        "LeftFoot": (0, 10 * swing, 0),
        "RightUpLeg": (0, -29 * swing, 0), "RightLeg": (0, -40 * max(0, -swing), 0),
        "RightFoot": (0, -10 * swing, 0),
    }
    for name in names[1:]:
        values.extend(rotations[name])
    lines.append(" ".join(f"{v:.6f}" for v in values))

output = ROOT / "samples" / "studio-walk.bvh"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"Generated {output.name}: 240 frames, {len(names)} joints, {len(values)} channels")
