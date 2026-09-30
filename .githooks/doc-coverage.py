"""Refuses a commit when a feature's handle isn't written up: every yaml option, player setting and slot_data key
must be named in a process guide, every Debug setting in development.md, every source file in code-map.md; when
a log.md heading isn't 'YYYY-MM-DD: title' in date order, or a Contents list (log.md's, and the two guides') doesn't
match its headings; and when a link into a Markdown heading leads nowhere."""
import ast
import os
import re
import subprocess
import sys

DOCS = "agent_docs/"


def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()


def squash(text):
    return re.sub(r"[\s_`-]", "", text).lower()


guides = squash(read(DOCS + "documentation.md") + read(DOCS + "apimplementation.md"))
development = squash(read(DOCS + "development.md"))
code_map = read(DOCS + "code-map.md")
missing = []

options_src = read("apworld/bug_fables/options.py")
for node in ast.parse(options_src).body:
    if isinstance(node, ast.ClassDef) and node.name != "BugFablesOptions":
        display = next((s.value.value for s in node.body if isinstance(s, ast.Assign)
                        and getattr(s.targets[0], "id", "") == "display_name"), node.name)
        if squash(node.name) not in guides and squash(display) not in guides:
            missing.append(f"yaml option {display} ({node.name}): name it in a guide")

slot_src = read("apworld/bug_fables/slot_data.py")
keys = re.findall(r'^\s{8}"([a-z_]+)":', slot_src[slot_src.index("def build_slot_data"):], re.M)
if not keys:
    missing.append("slot_data.py: no slot_data keys found; update this hook's pattern")
for key in keys:
    if squash(key) not in guides:
        missing.append(f"slot_data key {key}: name it in a guide")

tracked = [p for p in subprocess.run(["git", "ls-files", "-z"], capture_output=True, text=True).stdout.split("\0") if p]
files = [p for p in tracked if p.split("/", 1)[0] in ("mod", "apworld", "dev-scripts")]
for path in files:
    if not path.endswith(".cs"):
        continue
    for section, key in re.findall(r'[Cc]onfig\.Bind\("(\w+)", "(\w+)"', read(path)):
        if section == "Debug":
            if squash(key) not in development:
                missing.append(f"Debug setting {key}: name it in development.md")
        elif squash(key) not in guides:
            missing.append(f"setting {section}.{key}: name it in a guide")

for path in files:
    if re.search(r"\.(cs|py|ps1)$", path) and path.rsplit("/", 1)[-1] not in code_map:
        missing.append(f"{path}: give it a row in agent_docs/code-map.md")


def anchor(heading, seen):
    """GitHub's: lower case, punctuation dropped, spaces to hyphens, a repeat numbered."""
    slug = re.sub(r"[^\w\- ]", "", heading.lower()).replace(" ", "-")
    count = seen.get(slug, 0)
    seen[slug] = count + 1
    return slug if count == 0 else f"{slug}-{count}"


def prose(text):
    """The text without fenced code blocks or code spans, where a heading or link is only an example."""
    return re.sub(r"`[^`\n]*`", "", re.sub(r"^```.*?^```", "", text, flags=re.M | re.S))


def headings(text):
    """(level, title, anchor) of every heading, in order; a link in a title counts as its text."""
    seen = {}
    return [(len(hashes), title, anchor(re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", title), seen))
            for hashes, title in re.findall(r"^(#{1,6}) (.+?)\s*$", re.sub(r"^```.*?^```", "", text, flags=re.M | re.S),
                                            re.M)]


# Each guide's Contents: one numbered line per numbered '## ' heading, its text (the list's number standing for an
# 'N. ' of its own) and its link, in order.
for guide, section in (("apimplementation.md", "## Contents"), ("documentation.md", "## The steps")):
    text = read(DOCS + guide)
    wanted = ["[" + re.sub(r"^\d+\. ", "", title) + f"](#{slug})" for level, title, slug in headings(text)
              if level == 2 and re.match(r"(Build step \d+:|\d+\.) ", title)]
    listed = re.findall(r"^\d+\. (\[.+\]\(#[^)]+\))$", text.split(section, 1)[-1].split("\n## ", 1)[0], re.M)
    if listed != wanted:
        absent = [line for line in wanted if line not in listed]
        extra = [line for line in listed if line not in wanted]
        missing += [f"{guide} Contents: add this line: {line}" for line in absent]
        missing += [f"{guide} Contents: no heading has this line: {line}" for line in extra]
        if not absent and not extra:
            missing.append(f"{guide} Contents: the lines are out of the headings' order")

# Every link into a heading of a tracked Markdown file leads to one.
markdown = {p for p in tracked if p.endswith(".md")}
anchors = {}
for path in sorted(markdown):
    for target, fragment in re.findall(r"\]\(([^()\s#]*)#([^()\s]+)\)", prose(read(path))):
        if "://" in target:
            continue
        dest = path
        if target:
            target = re.sub(r"%([0-9A-Fa-f]{2})", lambda m: chr(int(m.group(1), 16)), target)
            dest = os.path.normpath(os.path.join(os.path.dirname(path), target)).replace("\\", "/")
        if not dest.endswith(".md"):
            continue
        if dest not in markdown:
            missing.append(f"{path}: links to {target}#{fragment}, which isn't a tracked file")
            continue
        if dest not in anchors:
            anchors[dest] = {slug for _, _, slug in headings(read(dest))}
        if fragment not in anchors[dest]:
            missing.append(f"{path}: links to {dest}#{fragment}, and no heading there has that anchor")

log = read(DOCS + "log.md")
headings = [h for h in re.findall(r"^## (.+)$", log, re.M) if h != "Contents"]
last_date = ""
for h in headings:
    dated = re.match(r"(\d{4}-\d{2}-\d{2}): \S", h)
    if not dated:
        missing.append(f"log.md heading: write it as 'YYYY-MM-DD: title', nothing between date and colon: {h}")
    elif dated.group(1) < last_date:
        missing.append(f"log.md heading: dated before the entry above it (newest last): {h}")
    else:
        last_date = dated.group(1)
seen = {}
wanted = [f"- [{h}](#{anchor(h, seen)})" for h in headings]
contents = log.split("## Contents", 1)[-1].split("\n## ", 1)[0]
listed = [line for line in contents.splitlines() if line.startswith("- [")]
if listed != wanted:
    absent = [line for line in wanted if line not in listed]
    extra = [line for line in listed if line not in wanted]
    missing += [f"log.md Contents: add this line: {line}" for line in absent]
    missing += [f"log.md Contents: no entry has this heading: {line}" for line in extra]
    if not absent and not extra:
        missing.append("log.md Contents: the lines are out of the entries' order")

if missing:
    print("doc-coverage: these aren't written up yet:", file=sys.stderr)
    for line in missing:
        print("  " + line, file=sys.stderr)
    sys.exit(1)
