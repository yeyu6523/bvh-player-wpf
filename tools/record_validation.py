"""Record hashes for the actual built executable and the current source files."""
from pathlib import Path
import argparse
import hashlib
import json
from datetime import datetime, timezone, timedelta

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--exe", required=True)
parser.add_argument("--desktop-copy")
args = parser.parse_args()

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

exe = Path(args.exe).resolve()
published = {"file": exe.name, "bytes": exe.stat().st_size, "sha256": sha(exe)}
if args.desktop_copy:
    copy = Path(args.desktop_copy).resolve()
    published["desktopCopyMatches"] = sha(copy) == published["sha256"]
    if not published["desktopCopyMatches"]:
        raise SystemExit("Desktop copy differs from published executable")

source = {}
for folder in ["src", "tests", "samples"]:
    for path in sorted((root / folder).rglob("*")):
        if path.is_file() and not any(part in {"bin", "obj"} for part in path.relative_to(root).parts):
            source[path.relative_to(root).as_posix()] = sha(path)
for name in ["BVHPlayer.sln", "Directory.Build.props", "build.ps1"]:
    source[name] = sha(root / name)
out = root / "docs" / "validation"
out.mkdir(parents=True, exist_ok=True)
(out / "release.json").write_text(json.dumps({"verifiedAt": datetime.now(timezone(timedelta(hours=8))).isoformat(), **published}, indent=2) + "\n", encoding="utf-8")
(out / "source-hashes.json").write_text(json.dumps(source, indent=2) + "\n", encoding="utf-8")
print(json.dumps(published))
