"""Uta の wiki・スキル・CLAUDE.md を機械的に点検する（LLM を使わない lint）。

使い方: python -I .claude/skills/linting-wiki/lint_wiki.py [リポジトリのルート]

結果は「エラー」（直すべきもの）と「警告」（確かめるもの）に分けて出す。
エラーが 1 件でもあれば終了コード 1 を返す。標準ライブラリだけを使う。
"""

import datetime
import re
import subprocess
import sys
from pathlib import Path

WIKI_STATUS = {"draft", "stable", "deprecated", "deferred"}
ADR_STATUS = {"proposed", "accepted", "superseded"}
PLAN_STATUS = {"draft", "ready", "in-progress", "done"}

LINK_RE = re.compile(r"(?<!!)\[([^\]]*)\]\(([^)\s]+)\)")
CODE_PATH_RE = re.compile(r"`((?:uta_docs|\.claude|shared|server|client|masterdata|external)/[^`\s]*)`")
ADR_FILE_RE = re.compile(r"^(\d{4})-.+\.md$")
ADR_TEXT_RE = re.compile(r"ADR\s*(\d{4})")
PLACEHOLDER_RE = re.compile(r"[<>*{}]|NNNN|YYYY|\.\.\.|…")


class Report:
    def __init__(self):
        self.items = {}

    def add(self, level, check, path, message):
        self.items.setdefault(check, []).append((level, path, message))

    def count(self, level):
        return sum(1 for rows in self.items.values() for row in rows if row[0] == level)


def submodule_missing(root, dest):
    """行き先が external/<名前>/ の中で、そのサブモジュールが空（未初期化）なら名前を返す。"""
    try:
        parts = dest.relative_to(root).parts
    except ValueError:
        return None
    if len(parts) >= 2 and parts[0] == "external":
        module = root / "external" / parts[1]
        if module.is_dir() and not any(module.iterdir()):
            return parts[1]
    return None


def git_ignored(root, value):
    try:
        result = subprocess.run(["git", "-C", str(root), "check-ignore", "-q", value],
                                capture_output=True)
        return result.returncode == 0
    except OSError:
        return False


def rel(root, path):
    return path.relative_to(root).as_posix()


def strip_code_blocks(text):
    """フェンスで囲まれたコードブロックを空行に置き換える（行番号は保つ）。"""
    lines = text.split("\n")
    inside = False
    for i, line in enumerate(lines):
        if line.lstrip().startswith("```"):
            inside = not inside
            lines[i] = ""
        elif inside:
            lines[i] = ""
    return "\n".join(lines)


def read_frontmatter(text):
    if not text.startswith("---\n"):
        return None
    end = text.find("\n---", 4)
    if end < 0:
        return None
    result = {}
    for line in text[4:end].split("\n"):
        if ":" in line and not line.startswith(" "):
            key, value = line.split(":", 1)
            result[key.strip()] = value.strip()
    return result


def collect_files(root):
    files = []
    docs = root / "uta_docs"
    for path in sorted(docs.rglob("*.md")):
        if ".obsidian" in path.parts:
            continue
        files.append(path)
    for base in (root / ".claude" / "skills", root / ".claude" / "agents"):
        if base.exists():
            files.extend(sorted(base.rglob("*.md")))
    for name in ("CLAUDE.md", "README.md"):
        if (root / name).exists():
            files.append(root / name)
    return files


def check_links(root, files, report):
    """相対リンクの行き先が実在するか。リンクの文字の ADR 番号と行き先の番号が合うか。"""
    inbound = {}
    for path in files:
        text = strip_code_blocks(path.read_text(encoding="utf-8"))
        is_raw = "raw" in path.relative_to(root).parts[:2]
        for lineno, line in enumerate(text.split("\n"), 1):
            for label, target in LINK_RE.findall(line):
                if re.match(r"^[a-z]+:", target) or target.startswith("#"):
                    continue
                target_path = target.split("#", 1)[0]
                if not target_path or PLACEHOLDER_RE.search(target_path):
                    continue
                dest = (path.parent / target_path).resolve()
                where = f"{rel(root, path)}:{lineno}"
                if not dest.exists():
                    module = submodule_missing(root, dest)
                    if module:
                        report.add("警告", "確かめられなかったリンク", where,
                                   f"{target}（サブモジュール external/{module} が未初期化。"
                                   "git submodule update --init してから点検し直す）")
                        continue
                    level = "警告" if is_raw else "エラー"
                    report.add(level, "リンク切れ", where, f"{target} が見つからない")
                    continue
                inbound.setdefault(dest, set()).add(path.resolve())
                label_adr = ADR_TEXT_RE.search(label) or re.fullmatch(r"(\d{4})", label.strip())
                file_adr = ADR_FILE_RE.match(dest.name) if dest.parent.name == "decisions" else None
                if label_adr and file_adr and label_adr.group(1) != file_adr.group(1):
                    report.add("エラー", "ADR の番号の食い違い", where,
                               f"リンクの文字は ADR {label_adr.group(1)} だが、行き先は {dest.name}")
    return inbound


def check_code_paths(root, report):
    """CLAUDE.md・スキル・エージェントのコード書式のパスが実在するか。同じパスは 1 件にまとめる。"""
    missing = {}
    targets = [root / "CLAUDE.md"]
    for base in (root / ".claude" / "skills", root / ".claude" / "agents"):
        if base.exists():
            targets.extend(sorted(base.rglob("*.md")))
    for path in targets:
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8")
        for lineno, line in enumerate(text.split("\n"), 1):
            for value in CODE_PATH_RE.findall(line):
                if PLACEHOLDER_RE.search(value):
                    continue
                dest = root / value.rstrip("/")
                if dest.exists() or git_ignored(root, value):
                    continue
                if submodule_missing(root, dest):
                    report.add("警告", "確かめられなかったリンク", f"{rel(root, path)}:{lineno}",
                               f"{value}（サブモジュールが未初期化）")
                    continue
                missing.setdefault(value, []).append(f"{rel(root, path)}:{lineno}")
    for value, places in sorted(missing.items()):
        report.add("警告", "書かれたパスがない", value,
                   f"{len(places)} か所（{', '.join(places)}）。これから作る予定なら、そう分かるように書く")


def check_frontmatter(root, report):
    today = datetime.date.today()
    wiki = root / "uta_docs" / "wiki"
    plans = root / "uta_docs" / "plans"
    pages = sorted(wiki.rglob("*.md")) + (sorted(plans.glob("*.md")) if plans.exists() else [])
    for path in pages:
        name = rel(root, path)
        if path.name == "README.md" or path.name == "0000-template.md":
            continue
        meta = read_frontmatter(path.read_text(encoding="utf-8"))
        if meta is None:
            report.add("エラー", "frontmatter", name, "先頭に frontmatter がない")
            continue
        for key in ("title", "status", "updated"):
            if not meta.get(key):
                report.add("エラー", "frontmatter", name, f"{key} がない")
        if path.parent.name == "decisions":
            allowed, kind = ADR_STATUS, "ADR"
        elif path.parent == plans:
            allowed, kind = PLAN_STATUS, "実装計画"
            if not meta.get("spec"):
                report.add("エラー", "frontmatter", name, "実装計画に spec（仕様ページ）がない")
        else:
            allowed, kind = WIKI_STATUS, "wiki"
        status = meta.get("status", "")
        if status and status not in allowed:
            report.add("エラー", "frontmatter", name,
                       f"status: {status} は {kind} で使えない（{' / '.join(sorted(allowed))}）")
        updated = meta.get("updated", "")
        if updated:
            try:
                date = datetime.date.fromisoformat(updated)
                if date > today:
                    report.add("警告", "frontmatter", name, f"updated: {updated} が未来の日付")
            except ValueError:
                report.add("エラー", "frontmatter", name, f"updated: {updated} が YYYY-MM-DD でない")


def check_index_and_orphans(root, inbound, report):
    wiki = root / "uta_docs" / "wiki"
    index = (wiki / "index.md").resolve()
    for path in sorted(wiki.rglob("*.md")):
        resolved = path.resolve()
        if resolved == index:
            continue
        sources = inbound.get(resolved, set())
        if index not in sources:
            report.add("エラー", "index に未登録", rel(root, path), "index.md からリンクされていない")
        if path.name not in ("log.md", "0000-template.md") and not (sources - {index, resolved}):
            report.add("警告", "孤立ページ", rel(root, path), "index.md 以外のページからリンクされていない")


def check_adrs(root, report):
    decisions = root / "uta_docs" / "wiki" / "decisions"
    numbers = {}
    for path in sorted(decisions.glob("*.md")):
        match = ADR_FILE_RE.match(path.name)
        if not match:
            report.add("エラー", "ADR", rel(root, path), "ファイル名が NNNN-件名.md でない")
            continue
        number = match.group(1)
        numbers.setdefault(number, []).append(path)
        if number == "0000":
            continue
        meta = read_frontmatter(path.read_text(encoding="utf-8")) or {}
        title_number = ADR_TEXT_RE.search(meta.get("title", ""))
        if title_number and title_number.group(1) != number:
            report.add("エラー", "ADR", rel(root, path),
                       f"title の番号（ADR {title_number.group(1)}）がファイル名と違う")
    for number, paths in numbers.items():
        if len(paths) > 1:
            report.add("エラー", "ADR", ", ".join(rel(root, p) for p in paths), f"番号 {number} が重複している")

    # index.md の ADR の行に書いた status が、ADR 本体と合っているか
    index = root / "uta_docs" / "wiki" / "index.md"
    for lineno, line in enumerate(index.read_text(encoding="utf-8").split("\n"), 1):
        match = re.search(r"\(decisions/((\d{4})-[^)]+\.md)\)", line)
        if not match or match.group(2) == "0000":
            continue
        path = decisions / match.group(1)
        if not path.exists():
            continue
        status = (read_frontmatter(path.read_text(encoding="utf-8")) or {}).get("status", "")
        written = [s for s in ADR_STATUS if s in line]
        if status and written and status not in written:
            report.add("エラー", "ADR", f"uta_docs/wiki/index.md:{lineno}",
                       f"index では {'/'.join(written)} だが、{match.group(1)} は {status}")

    # 本文の「ADR NNNN」が実在する番号か
    existing = set(numbers)
    for path in [root / "CLAUDE.md"] + sorted((root / "uta_docs").rglob("*.md")) + sorted((root / ".claude").rglob("*.md")):
        if not path.exists() or "worktrees" in path.relative_to(root).parts:
            continue
        text = strip_code_blocks(path.read_text(encoding="utf-8"))
        for lineno, line in enumerate(text.split("\n"), 1):
            for number in ADR_TEXT_RE.findall(line):
                if number not in existing:
                    report.add("エラー", "ADR", f"{rel(root, path)}:{lineno}", f"ADR {number} は存在しない")


def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    root = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    if not (root / "uta_docs" / "wiki" / "index.md").exists():
        print(f"{root} は Uta のルートではない（uta_docs/wiki/index.md がない）")
        return 2

    report = Report()
    files = [f for f in collect_files(root) if "worktrees" not in f.relative_to(root).parts]
    inbound = check_links(root, files, report)
    check_code_paths(root, report)
    check_frontmatter(root, report)
    check_index_and_orphans(root, inbound, report)
    check_adrs(root, report)

    for check, rows in report.items.items():
        print(f"## {check}（{len(rows)} 件）")
        for level, path, message in sorted(rows, key=lambda r: (r[0] != "エラー", r[1])):
            print(f"- [{level}] {path}: {message}")
        print()
    errors, warnings = report.count("エラー"), report.count("警告")
    print(f"エラー {errors} 件、警告 {warnings} 件（{len(files)} ファイルを点検）")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
