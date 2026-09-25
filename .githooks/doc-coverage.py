"""Refuses a commit when a feature's handle isn't written up: every yaml option, player setting and slot_data key
must be named in a process guide, every Debug setting in development.md, every source file in code-map.md."""
import ast
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

world_src = read("apworld/bug_fables/world.py")
slot_data = world_src[world_src.index("def fill_slot_data"):]
for key in re.findall(r'^\s{12}"([a-z_]+)":', slot_data, re.M):
    if squash(key) not in guides:
        missing.append(f"slot_data key {key}: name it in a guide")

files = subprocess.run(["git", "ls-files", "mod", "apworld", "dev-scripts"], capture_output=True, text=True).stdout.split()
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

if missing:
    print("doc-coverage: these aren't written up yet:", file=sys.stderr)
    for line in missing:
        print("  " + line, file=sys.stderr)
    sys.exit(1)
