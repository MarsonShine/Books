from __future__ import annotations

import copy
import logging
import re
import shutil
from collections import defaultdict
from pathlib import Path, PurePosixPath


log = logging.getLogger("mkdocs")

ROOT = Path(__file__).resolve().parent.parent
SOURCE_DOCS_DIR = ROOT / "docs"
GENERATED_DOCS_DIR = ROOT / "_mkdocs_docs"
INDEX_FILE_NAMES = {"index.md", "readme.md"}
IGNORED_NAMES = {
    ".git",
    ".github",
    ".idea",
    ".vs",
    ".vscode",
    "__pycache__",
    "bin",
    "node_modules",
    "obj",
    "site",
    "wwwroot",
}
IGNORED_TOP_LEVEL_NAMES = {"_mkdocs_docs", "docs", "hooks", "site"}
IGNORED_DOC_FILE_NAMES = {"license.md"}
TRANSPARENT_CONTAINER_NAMES = {"doc", "docs"}
TITLE_CACHE: dict[str, str] = {}


def _natural_sort_key(value: str) -> list[tuple[int, object]]:
    parts = re.split(r"(\d+)", value)
    key: list[tuple[int, object]] = []
    for part in parts:
        if not part:
            continue
        if part.isdigit():
            key.append((0, int(part)))
        else:
            key.append((1, part.lower()))
    return key


def _clear_generated_docs_dir() -> None:
    GENERATED_DOCS_DIR.mkdir(exist_ok=True)

    for child in GENERATED_DOCS_DIR.iterdir():
        if child.name == ".gitignore":
            continue
        if child.is_dir():
            shutil.rmtree(child)
        else:
            child.unlink()


def _is_placeholder_link(path: Path) -> bool:
    if not path.is_file() or path.suffix:
        return False

    try:
        raw_target = path.read_text(encoding="utf-8").strip()
    except UnicodeDecodeError:
        return False

    if not raw_target or "\n" in raw_target or "\r" in raw_target:
        return False

    return (path.parent / raw_target).exists()


def _resolve_source_entry(path: Path) -> Path:
    if path.is_symlink():
        return path.resolve()

    if _is_placeholder_link(path):
        target = path.read_text(encoding="utf-8").strip()
        resolved = (path.parent / target).resolve()
        if not resolved.exists():
            raise FileNotFoundError(f"Broken docs placeholder link: {path} -> {target}")
        return resolved

    return path


def _is_ignored_doc_path(path: Path) -> bool:
    return any(part in IGNORED_NAMES for part in path.parts) or path.name.lower() in IGNORED_DOC_FILE_NAMES


def _iter_markdown_files(path: Path):
    if path.is_file():
        if path.suffix.lower() == ".md" and not _is_ignored_doc_path(path):
            yield path
        return

    for candidate in path.rglob("*"):
        if candidate.is_file() and candidate.suffix.lower() == ".md" and not _is_ignored_doc_path(candidate):
            yield candidate


def _directory_contains_markdown(path: Path) -> bool:
    return any(_iter_markdown_files(path))


def _ignore_files(_: str, names: list[str]) -> set[str]:
    ignored = {name for name in names if name in IGNORED_NAMES}
    ignored.update(name for name in names if name.lower() in IGNORED_DOC_FILE_NAMES)
    return ignored


def _copy_entry(source: Path, destination: Path) -> None:
    if source.is_dir():
        shutil.copytree(source, destination, ignore=_ignore_files)
        return

    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)


def _iter_source_entries() -> list[tuple[str, Path]]:
    if not SOURCE_DOCS_DIR.exists():
        raise FileNotFoundError(f"Source docs directory does not exist: {SOURCE_DOCS_DIR}")

    entries: list[tuple[str, Path]] = []
    covered_names: set[str] = set()

    for entry in sorted(SOURCE_DOCS_DIR.iterdir(), key=lambda path: _natural_sort_key(path.name)):
        if entry.name.startswith("."):
            continue
        resolved_entry = _resolve_source_entry(entry)
        entries.append((entry.name, resolved_entry))
        covered_names.add(entry.name.casefold())

    for entry in sorted(ROOT.iterdir(), key=lambda path: _natural_sort_key(path.name)):
        if entry.name.startswith("."):
            continue
        if entry.name.casefold() in covered_names or entry.name in IGNORED_TOP_LEVEL_NAMES:
            continue
        if entry.is_dir() and _directory_contains_markdown(entry):
            entries.append((entry.name, entry))

    return entries


def _prepare_docs() -> None:
    _clear_generated_docs_dir()

    for name, source in _iter_source_entries():
        _copy_entry(source, GENERATED_DOCS_DIR / name)

    log.info("Prepared MkDocs docs directory at %s", GENERATED_DOCS_DIR)


def _read_markdown_title(path: str) -> str:
    cached_title = TITLE_CACHE.get(path)
    if cached_title:
        return cached_title

    relative_path = PurePosixPath(path)
    absolute_path = GENERATED_DOCS_DIR / relative_path

    try:
        with absolute_path.open("r", encoding="utf-8") as file_handle:
            for line in file_handle:
                stripped = line.strip()
                if not stripped.startswith("#"):
                    continue
                title = stripped.lstrip("#").strip()
                if title:
                    TITLE_CACHE[path] = title
                    return title
    except UnicodeDecodeError:
        pass

    fallback_source = relative_path.stem
    if relative_path.name.lower() in INDEX_FILE_NAMES:
        if relative_path.parent.name.lower() in TRANSPARENT_CONTAINER_NAMES and relative_path.parent.parent.name:
            fallback_source = relative_path.parent.parent.name
        elif relative_path.parent.name:
            fallback_source = relative_path.parent.name

    fallback = _humanize_name(fallback_source)
    TITLE_CACHE[path] = fallback
    return fallback


def _humanize_name(value: str) -> str:
    if value.lower() in {"doc", "docs"}:
        return "文档"
    normalized = re.sub(r"^\d+[-_. ]*", "", value).replace("_", " ").replace("-", " ")
    normalized = re.sub(r"\s+", " ", normalized).strip()
    return normalized or value


def _collect_generated_markdown_paths() -> list[str]:
    paths: list[str] = []
    for path in GENERATED_DOCS_DIR.rglob("*"):
        if not path.is_file() or path.suffix.lower() != ".md" or _is_ignored_doc_path(path):
            continue
        paths.append(path.relative_to(GENERATED_DOCS_DIR).as_posix())
    return sorted(paths, key=_natural_sort_key)


def _extract_nav_paths(node) -> list[str]:
    paths: list[str] = []

    def walk(value) -> None:
        if isinstance(value, list):
            for item in value:
                walk(item)
            return
        if isinstance(value, dict):
            for child in value.values():
                walk(child)
            return
        if isinstance(value, str) and value.lower().endswith(".md"):
            paths.append(value.replace("\\", "/"))

    walk(node)
    return paths


def _display_relative_path(relative_path: PurePosixPath) -> PurePosixPath:
    if not relative_path.parts:
        return relative_path

    first_part = relative_path.parts[0].lower()
    is_container_index = len(relative_path.parts) == 2 and relative_path.name.lower() in INDEX_FILE_NAMES
    if first_part in TRANSPARENT_CONTAINER_NAMES and not is_container_index:
        return PurePosixPath(*relative_path.parts[1:])

    return relative_path


def _build_tree_for_paths(group_paths: list[str]) -> dict[str, dict]:
    tree = {"files": {}, "children": {}}
    used_display_paths: dict[str, str] = {}

    for original_path in sorted(group_paths, key=_natural_sort_key):
        relative_path = PurePosixPath(*PurePosixPath(original_path).parts[1:])
        display_path = _display_relative_path(relative_path)
        display_key = display_path.as_posix()

        if display_key in used_display_paths and used_display_paths[display_key] != original_path:
            display_path = relative_path
            display_key = display_path.as_posix()

        used_display_paths[display_key] = original_path
        current = tree
        parts = list(display_path.parts)

        for part in parts[:-1]:
            current = current["children"].setdefault(part, {"files": {}, "children": {}})

        current["files"][parts[-1]] = original_path

    return tree


def _file_nav_title(display_name: str, original_path: str, has_siblings: bool) -> str:
    if display_name.lower() in INDEX_FILE_NAMES and has_siblings:
        return "概述"
    return _read_markdown_title(original_path)


def _child_nav_title(child_name: str, child_node: dict[str, dict]) -> str:
    for display_name, original_path in child_node["files"].items():
        if display_name.lower() in INDEX_FILE_NAMES:
            return _read_markdown_title(original_path)
    return _humanize_name(child_name)


def _render_tree(tree: dict[str, dict]) -> list[dict[str, object]]:
    entries: list[dict[str, object]] = []
    file_items = list(tree["files"].items())
    has_siblings = len(file_items) + len(tree["children"]) > 1

    def file_sort_key(item: tuple[str, str]) -> tuple[int, list[tuple[int, object]]]:
        name = item[0]
        return (0 if name.lower() in INDEX_FILE_NAMES else 1, _natural_sort_key(name))

    for display_name, original_path in sorted(file_items, key=file_sort_key):
        entries.append({_file_nav_title(display_name, original_path, has_siblings): original_path})

    for child_name in sorted(tree["children"], key=_natural_sort_key):
        child_node = tree["children"][child_name]
        child_entries = _render_tree(child_node)
        child_title = _child_nav_title(child_name, child_node)

        if len(child_entries) == 1 and not child_node["children"] and len(child_node["files"]) == 1:
            only_entry = child_entries[0]
            entries.append({child_title: next(iter(only_entry.values()))})
            continue

        entries.append({child_title: child_entries})

    return entries


def _build_nav_entries(group_paths: list[str]) -> list[dict[str, object]]:
    if not group_paths:
        return []
    tree = _build_tree_for_paths(group_paths)
    return _render_tree(tree)


def _determine_group_title(group: str, group_paths: list[str]) -> str:
    relative_paths = [PurePosixPath(*PurePosixPath(path).parts[1:]) for path in group_paths]

    for candidate in relative_paths:
        if len(candidate.parts) == 1 and candidate.name.lower() in INDEX_FILE_NAMES:
            return _read_markdown_title(f"{group}/{candidate.as_posix()}")

    for candidate in relative_paths:
        if len(candidate.parts) == 2 and candidate.parts[0].lower() in TRANSPARENT_CONTAINER_NAMES and candidate.name.lower() in INDEX_FILE_NAMES:
            return _read_markdown_title(f"{group}/{candidate.as_posix()}")

    return _humanize_name(group)


def _complete_nav(base_nav: list[object]) -> list[object]:
    all_paths = _collect_generated_markdown_paths()
    group_to_paths: dict[str, list[str]] = defaultdict(list)

    for path in all_paths:
        parts = PurePosixPath(path).parts
        if len(parts) < 2:
            continue
        group_to_paths[parts[0]].append(path)

    referenced_paths = set(_extract_nav_paths(base_nav))
    completed_nav = copy.deepcopy(base_nav)
    groups_in_nav: set[str] = set()

    for index, item in enumerate(completed_nav):
        if not isinstance(item, dict) or len(item) != 1:
            continue

        title, value = next(iter(item.items()))
        item_paths = _extract_nav_paths(value)
        item_groups = {PurePosixPath(path).parts[0] for path in item_paths if len(PurePosixPath(path).parts) > 1}

        if len(item_groups) != 1:
            continue

        group = next(iter(item_groups))
        groups_in_nav.add(group)
        missing_paths = [path for path in group_to_paths[group] if path not in referenced_paths]
        auto_entries = _build_nav_entries(missing_paths)

        if not auto_entries:
            continue

        if isinstance(value, str):
            overview_label = "概述" if PurePosixPath(value).name.lower() in INDEX_FILE_NAMES else title
            completed_nav[index] = {title: [{overview_label: value}, *auto_entries]}
            continue

        if isinstance(value, list):
            completed_nav[index] = {title: [*value, *auto_entries]}

    for group in (name for name, _ in _iter_source_entries() if name in group_to_paths and name not in groups_in_nav):
        group_paths = group_to_paths[group]
        auto_entries = _build_nav_entries(group_paths)
        if not auto_entries:
            continue

        group_title = _determine_group_title(group, group_paths)
        if len(auto_entries) == 1 and len(PurePosixPath(group_paths[0]).parts) == 2:
            only_entry = auto_entries[0]
            completed_nav.append({group_title: next(iter(only_entry.values()))})
            continue

        completed_nav.append({group_title: auto_entries})

    return completed_nav


def _synchronize_docs_and_nav(config) -> None:
    TITLE_CACHE.clear()
    _prepare_docs()
    config["nav"] = _complete_nav(copy.deepcopy(config.get("nav") or []))


def on_config(config, **kwargs):
    _synchronize_docs_and_nav(config)
    return config


def on_pre_build(config, **kwargs) -> None:
    _synchronize_docs_and_nav(config)
