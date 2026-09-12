import hashlib
import io
import json
import re
import sys
import tarfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PACKAGE = ROOT / "Packages" / "com.azen.gameloop"
TARGET = "Assets/Azen.GameLoop"
INCLUDE = ["Runtime", "Editor", "Tests", "Samples~", "README.md", "LICENSE", "CHANGELOG.md"]
RENAME = {"Samples~": "Samples"}

GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-f]{32})", re.MULTILINE)

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

SCRIPT_META = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

ASMDEF_META = """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

TEXT_META = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def generated_guid(asset_path: str) -> str:
    return hashlib.md5(("azen.gameloop:" + asset_path).encode("utf-8")).hexdigest()


def meta_for(source: Path, asset_path: str) -> bytes:
    meta = source.with_name(source.name + ".meta")
    if meta.exists():
        return meta.read_bytes()

    guid = generated_guid(asset_path)
    if source.is_dir():
        template = FOLDER_META
    elif source.suffix == ".cs":
        template = SCRIPT_META
    elif source.suffix == ".asmdef":
        template = ASMDEF_META
    else:
        template = TEXT_META
    return template.format(guid=guid).encode("utf-8")


def asset_path_for(source: Path) -> str:
    parts = list(source.relative_to(PACKAGE).parts)
    parts[0] = RENAME.get(parts[0], parts[0])
    return "/".join([TARGET, *parts])


def collect() -> list[Path]:
    sources = []
    for name in INCLUDE:
        entry = PACKAGE / name
        if not entry.exists():
            continue
        sources.append(entry)
        if entry.is_dir():
            sources.extend(p for p in sorted(entry.rglob("*")) if p.suffix != ".meta")
    return sources


def add_bytes(archive: tarfile.TarFile, name: str, data: bytes) -> None:
    info = tarfile.TarInfo(name)
    info.size = len(data)
    info.mtime = 0
    archive.addfile(info, io.BytesIO(data))


def add_entry(archive: tarfile.TarFile, asset_path: str, meta: bytes, content: bytes | None) -> str:
    guid = GUID_PATTERN.search(meta.decode("utf-8")).group(1)
    add_bytes(archive, f"{guid}/pathname", asset_path.encode("utf-8"))
    add_bytes(archive, f"{guid}/asset.meta", meta)
    if content is not None:
        add_bytes(archive, f"{guid}/asset", content)
    return guid


def main() -> None:
    version = json.loads((PACKAGE / "package.json").read_text(encoding="utf-8"))["version"]
    output = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "dist" / f"Azen.GameLoop-{version}.unitypackage"
    output.parent.mkdir(parents=True, exist_ok=True)

    seen = set()
    with tarfile.open(output, "w:gz") as archive:
        root_meta = FOLDER_META.format(guid=generated_guid(TARGET)).encode("utf-8")
        seen.add(add_entry(archive, TARGET, root_meta, None))

        for source in collect():
            asset_path = asset_path_for(source)
            content = None if source.is_dir() else source.read_bytes()
            guid = add_entry(archive, asset_path, meta_for(source, asset_path), content)
            if guid in seen:
                raise SystemExit(f"Duplicate GUID {guid} at {asset_path}")
            seen.add(guid)

    print(f"{output} ({len(seen)} assets)")


if __name__ == "__main__":
    main()
