"""Refuses what this repository must never hold: anything that couldn't be published, or that could harm whoever runs it.

Reads what git holds, never the working copy: the index by default (what the next commit contains), one commit with
--rev, every commit with --history, or free text with --text-stdin. Standard library only, so a reviewer needs
nothing but Python and git. docs/reviewing.md explains every section; dev-scripts/preflight-patterns.json holds what
each one refuses. Exit 0 when every section passes, 1 on any FAIL (a WARN never fails).

    python dev-scripts/preflight.py [--rev REV | --history [RANGE] | --text-stdin LABEL] [--ci] [--quiet]
"""
import argparse
import ast
import builtins
import fnmatch
import hashlib
import ipaddress
import json
import os
import re
import struct
import subprocess
import sys
import time
import unicodedata

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import dotnet_metadata  # noqa: E402 (next to this file)

PATTERNS = 'dev-scripts/preflight-patterns.json'
CAPABILITIES = 'docs/capabilities.md'
LICENSING = 'agent_docs/licensing.md'
# Files a machine reads as instructions; in these, non-ASCII is limited and lines stay short.
CODE = re.compile(r'\.(cs|csproj|props|py|ps1|sh|yml|yaml|json|config)$|^\.githooks/[^./]+$|^\.git(attributes|ignore)$'
                  r'|^\.editorconfig$')
URL = re.compile(r'\b([a-z][a-z0-9+.-]*)://([^\s/?#)>\]"\'`<|*,;\\]+)', re.I)
WEB_SCHEMES = {'http', 'https', 'ws', 'wss', 'ftp'}
QUOTED_HOST = re.compile(r'["\']((?:[a-z0-9-]+\.)+(?:com|net|org|io|gg|dev|app|xyz|me|co|info|eu|de|uk|ru|cn|tv|site'
                         r'|online|cloud))["\']', re.I)
DOTTED_QUAD = re.compile(r'(?<![\d.])(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?![\d.])')
GITHUB_REPO = re.compile(r'github\.com/([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)')
RESERVED = re.compile(r'(?i)^(con|prn|aux|nul|com[0-9]|lpt[0-9])(\..*)?$')
BUILTIN_NAMES = set(dir(builtins))


class Unreadable(Exception):
    """Preflight couldn't read what it checks: never a clean result."""


class File:
    __slots__ = ('path', 'mode', 'sha', 'data', 'text')

    def __init__(self, path, mode, sha, data):
        self.path, self.mode, self.sha, self.data = path, mode, sha, data
        self.text = None
        if b'\0' not in data:
            try:
                self.text = data.decode('utf-8')
            except UnicodeDecodeError:
                pass

    @property
    def binary(self):
        return self.text is None

    def lines(self):
        return enumerate(self.text.split('\n'), 1)


class Repo:
    def __init__(self, root):
        self.root = root

    def git(self, *args, stdin=None, ok=(0,)):
        r = subprocess.run(['git', '-C', self.root, *args], input=stdin, capture_output=True)
        if r.returncode not in ok:
            raise Unreadable(f"git {' '.join(args[:2])} failed ({r.returncode}): {r.stderr.decode(errors='replace').strip()}")
        return r.stdout

    def blobs(self, shas):
        shas = sorted(set(shas))
        raw = self.git('cat-file', '--batch', stdin=''.join(s + '\n' for s in shas).encode())
        found, i = {}, 0
        while i < len(raw):
            nl = raw.index(b'\n', i)
            head = raw[i:nl].decode().split()
            if len(head) != 3:
                raise Unreadable(f'git cat-file: {raw[i:nl].decode(errors="replace")}')
            size = int(head[2])
            found[head[0]] = raw[nl + 1:nl + 1 + size]
            i = nl + 1 + size + 1
        missing = set(shas) - set(found)
        if missing:
            raise Unreadable(f'{len(missing)} blob(s) could not be read')
        return found


def read_tree(repo, rev):
    """Every file of the index (rev None) or of a commit, with its content; conflicted index entries apart."""
    if rev is None:
        records = [(m, s, st, p) for m, s, st, p in
                   ((*meta.decode().split(), path) for meta, path in
                    (rec.split(b'\t', 1) for rec in repo.git('ls-files', '-s', '-z').split(b'\0') if rec))]
    else:
        records = [(m, s, '0', p) for m, t, s, p in
                   ((*meta.decode().split(), path) for meta, path in
                    (rec.split(b'\t', 1) for rec in repo.git('ls-tree', '-r', '-z', '--full-tree', rev).split(b'\0')
                     if rec))]
    blobs = repo.blobs(s for m, s, st, p in records if m != '160000')
    files, odd = {}, []
    for mode, sha, stage, raw_path in records:
        path = raw_path.decode('utf-8', 'surrogateescape')
        if stage != '0' or mode == '160000':
            odd.append((path, mode, stage))
            continue
        files[path] = File(path, mode, sha, blobs[sha])
    return files, odd


def read_history(repo, spec):
    """Every blob ever reachable in spec (default: every ref), with a path it was seen at, and every commit message."""
    listed = repo.git('rev-list', '--objects', *spec).decode('utf-8', 'surrogateescape').splitlines()
    paths = {}
    for line in listed:
        sha, _, path = line.partition(' ')
        if path:
            paths.setdefault(sha, path)
    kinds = repo.git('cat-file', '--batch-check', stdin=''.join(s + '\n' for s in paths).encode()).decode().splitlines()
    blob_shas = [k.split()[0] for k in kinds if k.split()[1] == 'blob']
    blobs = repo.blobs(blob_shas)
    files = [File(paths[s], '100644', s, blobs[s]) for s in blob_shas]
    log = repo.git('log', '--format=%H%x00%B%x00', *spec).decode('utf-8', 'replace').split('\0')
    messages = [(log[i].strip(), log[i + 1]) for i in range(0, len(log) - 1, 2)]
    return files, messages


def glob_regex(pattern):
    out = ''
    for part in re.split(r'(\*\*/|\*|\?)', pattern):
        out += {'**/': '(?:.*/)?', '*': '[^/]*', '?': '[^/]'}.get(part, re.escape(part))
    return re.compile(out + r'\Z')


def capability_tables(text):
    """Each '## Heading' of docs/capabilities.md -> its rows as (key in backticks, cells)."""
    tables, heading = {}, None
    for line in text.split('\n'):
        if line.startswith('## '):
            heading = line[3:].strip()
            tables.setdefault(heading, [])
        elif heading and line.startswith('|'):
            cells = [c.strip() for c in line.strip().strip('|').split('|')]
            key = re.fullmatch(r'`([^`]+)`', cells[0])
            if key:
                tables[heading].append((key.group(1), cells))
    return tables


def is_pe_managed(data):
    """A PE file with a CLI header: a .NET assembly, not native code."""
    try:
        if data[:2] != b'MZ':
            return False
        pe = struct.unpack_from('<I', data, 0x3C)[0]
        if data[pe:pe + 4] != b'PE\0\0':
            return False
        opt = pe + 24
        magic = struct.unpack_from('<H', data, opt)[0]
        dirs = opt + (96 if magic == 0x10B else 112 if magic == 0x20B else -1)
        if dirs < opt:
            return False
        rva, size = struct.unpack_from('<II', data, dirs + 14 * 8)
        return rva != 0 and size >= 72
    except struct.error:
        return False


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def show(ch):
    return f'U+{ord(ch):04X} {unicodedata.name(ch, "unnamed")}'


class Out:
    """One section's verdicts. A section that reports nothing is a failure: it proved nothing."""

    def __init__(self, name):
        self.name, self.lines, self.status = name, [], set()

    def _add(self, status, msg, details=()):
        self.status.add(status)
        self.lines.append(f'  {status:<4}  {msg}')
        details = list(details)
        for d in details[:25]:
            self.lines.append(f'          {d}')
        if len(details) > 25:
            self.lines.append(f'          ... and {len(details) - 25} more')

    def ok(self, msg):
        self._add('PASS', msg)

    def fail(self, msg, details=()):
        self._add('FAIL', msg, details)

    def warn(self, msg, details=()):
        self._add('WARN', msg, details)

    def skip(self, msg):
        self._add('SKIP', msg)


SECTIONS = []


def section(name, modes=('tree',)):
    """modes: 'tree' (the index or a commit), 'history' (every blob ever reachable), 'text' (--text-stdin)."""
    def register(fn):
        SECTIONS.append((name, fn, frozenset(modes)))
        return fn
    return register


class Context:
    def __init__(self, repo, files, odd, patterns, caps, args, messages=None, history=False):
        self.repo, self.files, self.odd, self.patterns, self.args = repo, files, odd, patterns, args
        self.caps, self.messages, self.history = caps, messages or [], history
        self.dll = self.dll_refs = None

    def texts(self, exempt=(PATTERNS,)):
        return [f for f in self.files if not f.binary and f.path not in exempt]

    def file(self, path):
        for f in self.files:
            if f.path == path:
                return f
        return None

    def capabilities(self):
        if self.caps is None:
            raise Unreadable(f'{CAPABILITIES} is not in the tree')
        return self.caps


# ---------------------------------------------------------------------------------------------------------------------
@section('Hooks armed')
def hooks_armed(ctx, out):
    if ctx.args.ci or ctx.args.rev:
        out.skip('hooks are per-clone git config: only a working clone can answer')
        return
    hooks = [f for f in ctx.files if re.fullmatch(r'\.githooks/[^./]+', f.path)]
    if len(hooks) < 2:
        out.fail(f'only {len(hooks)} hook(s) in .githooks/: the listing is wrong, so nothing below proves anything')
        return
    configured = ctx.repo.git('config', '--get', 'core.hooksPath', ok=(0, 1)).decode().strip()
    if configured != '.githooks':
        out.fail('core.hooksPath is not .githooks, so no hook runs in this clone: git config core.hooksPath .githooks')
    plain = [f.path for f in hooks if f.mode != '100755']
    if plain:
        out.fail('hooks not executable in the index (git ignores them on Linux and macOS): '
                 'git update-index --chmod=+x <hook>', plain)
    if configured == '.githooks' and not plain:
        out.ok(f'core.hooksPath is .githooks and all {len(hooks)} hooks are executable')


@section('Index sanity')
def index_sanity(ctx, out):
    problems = [f'{p}: {"a submodule" if m == "160000" else f"unmerged (stage {st})"}' for p, m, st in ctx.odd]
    seen = {}
    for f in ctx.files:
        if f.mode not in ('100644', '100755'):
            problems.append(f'{f.path}: mode {f.mode} ({"a symlink" if f.mode == "120000" else "not a plain file"})')
        if not f.path.isascii() or re.search(r'[\x00-\x1f\x7f<>:"|?*\\]', f.path):
            problems.append(f'{ascii(f.path)}: a character a path must not hold')
        for seg in f.path.split('/'):
            if RESERVED.match(seg) or seg.endswith(('.', ' ')) or seg != seg.strip():
                problems.append(f'{f.path}: "{seg}" is not a portable file name')
        other = seen.setdefault(f.path.lower(), f.path)
        if other != f.path:
            problems.append(f'{f.path} and {other} differ only in case (one file on Windows, two on Linux)')
    if len(ctx.files) < 50:
        problems.append(f'only {len(ctx.files)} files read: the listing is wrong')
    if problems:
        out.fail('the index holds entries that are not plain, portable files', problems)
    else:
        out.ok(f'{len(ctx.files)} plain files, no conflicts, links or submodules, portable names')


@section('Known kinds only')
def known_kinds(ctx, out):
    kinds = [(glob_regex(g), g) for g, why in ctx.patterns['kinds']]
    unknown = [f.path for f in ctx.files if not any(rx.match(f.path) for rx, g in kinds)]
    unused = [g for rx, g in kinds if not any(rx.match(f.path) for f in ctx.files)]
    attributes = ctx.file('.gitattributes')
    bad_attr = []
    for n, line in (attributes.lines() if attributes else []):
        words = line.split('#', 1)[0].split()
        for w in words[1:]:
            if w not in ('text', '-text', 'text=auto', 'eol=lf', 'eol=crlf', 'binary'):
                bad_attr.append(f'.gitattributes:{n}: {w} (only text, eol and binary may be set)')
    if unknown:
        out.fail('files of a kind this repo never holds (dev-scripts/preflight-patterns.json, "kinds")', unknown)
    if bad_attr:
        out.fail('.gitattributes sets something that changes what git stores or shows', bad_attr)
    if unused:
        out.warn('kinds no file matches (a stale entry?)', unused)
    if not unknown and not bad_attr:
        out.ok(f'all {len(ctx.files)} files are of the {len(kinds)} known kinds')


@section('Binaries', modes=('tree', 'history'))
def binaries(ctx, out):
    allowed = ctx.patterns['binaries']
    libraries = ctx.patterns['libraries']
    found = [f for f in ctx.files if f.binary]
    stray = [f.path for f in found if f.path not in allowed]
    native = [f.path for f in found if f.path in allowed and not is_pe_managed(f.data)]
    changed = [] if ctx.history else [
        f'{f.path}: sha256 {sha256(f.data)[:16]}..., NuGet\'s is {libraries[f.path][:16]}...'
        for f in found if f.path in libraries and sha256(f.data) != libraries[f.path]]
    if stray:
        out.fail('binary files outside the list of the release DLLs', stray)
    if native:
        out.fail('a listed DLL is not a .NET assembly', native)
    if changed:
        out.fail('a library DLL is not the file NuGet ships', changed)
    if not ctx.history:
        missing = sorted(set(allowed) - {f.path for f in found})
        if missing:
            out.fail('listed release DLLs missing from the tree', missing)
    if not (stray or native or changed) and (ctx.history or len(found) == len(allowed)):
        out.ok(f'{len(found)} binary file(s), each a listed .NET assembly'
               + ('' if ctx.history else f'; the {len(libraries)} libraries are byte-for-byte NuGet\'s'))


@section('Hidden characters', modes=('tree', 'history', 'text'))
def hidden_characters(ctx, out):
    allowed_code = set(ctx.patterns['code_non_ascii'])
    hits, scanned = [], 0
    for f in ctx.texts(exempt=()):
        scanned += 1
        code = bool(CODE.search(f.path))
        for n, line in f.lines():
            for col, ch in enumerate(line):
                if ch.isascii() and (ch.isprintable() or ch in '\t\r'):
                    continue
                if ch == chr(0xFEFF) and n == 1 and col == 0:
                    continue
                cat = unicodedata.category(ch)
                if cat in ('Cf', 'Cc', 'Co', 'Cn', 'Cs', 'Zl', 'Zp') and ch not in '\t\r':
                    hits.append(f'{f.path}:{n}: {show(ch)} (invisible or control)')
                elif code and not ctx.history and not ch.isascii() and ch not in allowed_code:
                    hits.append(f'{f.path}:{n}: {show(ch)} in a code file')
    if scanned < (0 if ctx.history else 1 if ctx.args.text_stdin else 100):
        out.fail(f'only {scanned} text file(s) read: the listing is wrong')
    elif hits:
        out.fail('characters that hide or disguise what the text says', hits)
    else:
        out.ok(f'{scanned} text file(s): no invisible, bidi or control characters; code non-ASCII only '
               f'{", ".join(show(c) for c in sorted(allowed_code))}')


@section('Encoded blobs and long lines', modes=('tree', 'history'))
def encoded_blobs(ctx, out):
    base64 = re.compile(r'[A-Za-z0-9+/]{200,}')
    hexrun = re.compile(r'[0-9a-fA-F]{128,}')
    hits = []
    for f in ctx.texts(exempt=()):
        code = bool(CODE.search(f.path))
        for n, line in f.lines():
            if base64.search(line) or hexrun.search(line):
                hits.append(f'{f.path}:{n}: a long encoded run (base64 or hex)')
            elif code and len(line) > 1000 and not f.path.endswith('.json'):
                hits.append(f'{f.path}:{n}: a code line of {len(line)} characters')
    if hits:
        out.fail('text that could carry a hidden payload', hits)
    else:
        out.ok('no base64 run of 200+ characters, no hex run of 128+, no code line over 1000 characters '
               '(JSON is data, parsed, never run)')


@section('Secrets', modes=('tree', 'history', 'text'))
def secrets(ctx, out):
    rules = {name: re.compile(rx) for name, rx in ctx.patterns['secrets'].items()}
    hits = []
    for f in ctx.files:
        if f.path in ctx.patterns['libraries']:
            continue
        texts = [f.text] if not f.binary else [f.data.decode('latin-1'), f.data.decode('utf-16-le', 'replace')]
        for t in texts:
            for name, rx in rules.items():
                for m in rx.finditer(t):
                    hits.append(f'{f.path}:{t.count(chr(10), 0, m.start()) + 1}: looks like a {name}')
    if len(rules) < 8:
        out.fail(f'only {len(rules)} secret formats: the patterns file is wrong')
    elif hits:
        out.fail('something that looks like a credential', hits)
    else:
        out.ok(f'none of {len(rules)} credential formats in {len(ctx.files)} file(s), binaries included')


def machine_names(ctx):
    if ctx.args.ci:
        return []
    names = {os.environ.get(k, '') for k in ('USERNAME', 'USER', 'COMPUTERNAME', 'HOSTNAME')}
    return sorted(n for n in names if len(n) >= 3)


@section('Personal paths and names', modes=('tree', 'history', 'text'))
def personal_paths(ctx, out):
    patterns = [p.lower() for p in ctx.patterns['personal_paths']]
    names = [n.lower() for n in machine_names(ctx)]
    reviewed = ctx.patterns['history_reviewed'].get('Personal paths and names', {}) if ctx.history else {}
    hits = []
    for f in ctx.files:
        if f.path == PATTERNS or f.path in ctx.patterns['libraries'] or f.sha in reviewed:
            continue
        texts = [f.text] if not f.binary else [f.data.decode('latin-1'), f.data.decode('utf-16-le', 'replace')]
        for t in texts:
            low = t.lower()
            for p in patterns + names:
                at = low.find(p)
                if at >= 0:
                    what = 'this machine\'s user or computer name' if p in names else f'"{p}"'
                    hits.append(f'{f.path}:{low.count(chr(10), 0, at) + 1}: {what}')
    if len(patterns) < 5:
        out.fail('the personal-path patterns are missing: the patterns file is wrong')
    elif hits:
        out.fail('a home path or a machine\'s name; write "your Bug Fables install" instead', hits)
    else:
        out.ok(f'no home path in {len(ctx.files)} file(s), binaries included'
               + (f', nor this machine\'s {len(names)} name(s)' if names else ' (machine names: not checked here)'))


@section('Game files', modes=('tree', 'history'))
def game_files(ctx, out):
    paths = [re.compile(p) for p in ctx.patterns['game_paths']]
    markers = [re.compile(p) for p in ctx.patterns['decompiler_markers']]
    hits = [f'{f.path}: a game file\'s name' for f in ctx.files if any(p.search(f.path) for p in paths)]
    for f in ctx.texts():
        for m in markers:
            found = m.search(f.text)
            if found:
                hits.append(f'{f.path}:{f.text.count(chr(10), 0, found.start()) + 1}: decompiler output')
    if hits:
        out.fail('the game\'s own files or code never enter the repo', hits)
    else:
        out.ok('no game assembly, asset, save or decompiled code')


def hosts_in(f):
    """Each (host key, line) a file names: a URL's host (for a non-web scheme, the scheme with the host), or a host quoted in code."""
    found = []
    for n, line in f.lines():
        for m in URL.finditer(line):
            scheme, host = m.group(1).lower(), m.group(2).lower().rsplit('@', 1)[-1]
            host = re.sub(r':\d*$', '', host).strip('.')
            if not re.search(r'[a-z0-9]', host):
                continue
            found.append((host if scheme in WEB_SCHEMES else scheme + '://' + host, n))
        if re.search(r'\.(cs|py|ps1)$', f.path):
            found += [(m.group(1).lower(), n) for m in QUOTED_HOST.finditer(line)]
    return found


def loopback(host):
    try:
        return host == 'localhost' or ipaddress.ip_address(host).is_loopback
    except ValueError:
        return False


@section('Hosts and addresses', modes=('tree', 'text'))
def hosts_and_addresses(ctx, out):
    listed = {}
    for key, cells in ctx.capabilities().get('Hosts', []):
        listed[key] = cells[-1]
    unlisted, used, public, private = [], set(), [], []
    not_addresses = ctx.patterns['not_addresses']
    for f in ctx.texts():
        for host, n in hosts_in(f):
            if loopback(host):
                continue
            key = next((k for k in listed if host == k or host.endswith('.' + k)), None)
            if key:
                used.add(key)
            else:
                unlisted.append(f'{f.path}:{n}: {host}')
        for n, line in f.lines():
            for m in DOTTED_QUAD.finditer(line):
                value = m.group(1)
                if value in not_addresses:
                    continue
                try:
                    ip = ipaddress.ip_address(value)
                except ValueError:
                    continue
                doc = any(ip in ipaddress.ip_network(r) for r in ('192.0.2.0/24', '198.51.100.0/24', '203.0.113.0/24'))
                if ip.is_loopback or ip.is_unspecified or ip.is_link_local or ip.is_multicast or ip.is_reserved or doc:
                    continue
                (private if ip.is_private else public).append(f'{f.path}:{n}: {value}')
    reasonless = [k for k, why in listed.items() if not why]
    stale = [] if ctx.args.text_stdin or ctx.history else sorted(set(listed) - used)
    if len(listed) < 3:
        out.fail(f'{CAPABILITIES} lists only {len(listed)} host(s): the table is missing or unreadable')
    if unlisted:
        out.fail(f'hosts not in {CAPABILITIES}, "Hosts": every place this project points to is listed with a reason',
                 unlisted)
    if public:
        out.fail('a public IP address (a real machine on the internet); if it is a version number, add it to '
                 '"not_addresses" in the patterns file', public)
    if private:
        out.warn('a private (LAN) address: fine in an example, not pasted from someone\'s log', private)
    if stale:
        out.fail(f'hosts listed in {CAPABILITIES} that nothing names any more: remove the row', stale)
    if reasonless:
        out.fail('host rows without a reason', reasonless)
    if not (unlisted or public or stale or reasonless) and len(listed) >= 3:
        out.ok(f'every host named is one of the {len(listed)} listed with a reason; no public IP address')


@section('Licences')
def licences(ctx, out):
    lic = ctx.file(LICENSING)
    own = {o.lower() for o in ctx.patterns['own_github_owners']}
    table = lic.text.lower() if lic else ''
    cited, unchecked = set(), []
    for f in ctx.texts():
        for n, line in f.lines():
            for m in GITHUB_REPO.finditer(line):
                repo = re.sub(r'(\.git)?\.*$', '', m.group(1))
                if repo.split('/')[0].lower() in own:
                    continue
                cited.add(repo.lower())
                if f.path != LICENSING and repo.lower() not in table:
                    unchecked.append(f'{f.path}:{n}: {repo}')
    problems = []
    licence = ctx.file('LICENSE')
    if not licence or not licence.text.startswith('MIT License'):
        problems.append('LICENSE: missing, or not the MIT licence')
    shipped = ctx.file('release/mod/BepInEx/plugins/BugFablesAP/LICENSE.txt')
    if licence and (not shipped or shipped.text.replace('\r', '') != licence.text.replace('\r', '')):
        problems.append('release/mod/.../LICENSE.txt: not the same text as LICENSE')
    notices = ctx.file('release/mod/BepInEx/plugins/BugFablesAP/THIRD-PARTY-NOTICES.txt')
    for lib in ctx.patterns['libraries']:
        name = lib.rsplit('/', 1)[-1][:-len('.dll')]
        if not notices or name not in notices.text:
            problems.append(f'THIRD-PARTY-NOTICES.txt: no notice for {name}, which ships')
    if not lic:
        out.fail(f'{LICENSING} is missing')
    elif len(cited) < 10:
        out.fail(f'only {len(cited)} projects cited: the scan is wrong')
    elif unchecked:
        out.fail(f'projects cited without a row in {LICENSING} (read the licence first, then add the row)', unchecked)
    if problems:
        out.fail('the licence files a release needs', problems)
    if lic and len(cited) >= 10 and not unchecked and not problems:
        out.ok(f'all {len(cited)} projects cited have a licence row; LICENSE and the notices for '
               f'{len(ctx.patterns["libraries"])} shipped libraries are in place')


APWORLD = 'apworld/bug_fables/'


def apworld_sources(ctx):
    """(file, parsed module) for every apworld .py; a file that doesn't parse is reported, never skipped."""
    parsed, broken = [], []
    for f in ctx.files:
        if f.path.startswith(APWORLD) and f.path.endswith('.py'):
            try:
                parsed.append((f, ast.parse(f.text, f.path)))
            except (SyntaxError, ValueError, TypeError) as e:
                broken.append(f'{f.path}: does not parse ({e})')
    return parsed, broken


def enclosing_functions(tree):
    """Each node -> the name of the function it sits in ('<module>' at the top level)."""
    owner = {}

    def visit(node, name):
        for child in ast.iter_child_nodes(node):
            inner = child.name if isinstance(child, (ast.FunctionDef, ast.AsyncFunctionDef)) else name
            owner[child] = inner
            visit(child, inner)
    visit(tree, '<module>')
    return owner


@section('Apworld imports')
def apworld_imports(ctx, out):
    names = ctx.patterns['apworld_imports']
    modules = ctx.patterns['apworld_modules']
    parsed, broken = apworld_sources(ctx)
    bad = list(broken)
    for f, tree in parsed:
        for node in ast.walk(tree):
            if isinstance(node, ast.ImportFrom) and node.level == 0:
                allowed = names.get(node.module, [])
                bad += [f'{f.path}:{node.lineno}: from {node.module} import {a.name}' for a in node.names
                        if a.name not in allowed]
            elif isinstance(node, ast.Import):
                bad += [f'{f.path}:{node.lineno}: import {a.name}' + (f' as {a.asname}' if a.asname else '')
                        for a in node.names if a.name not in modules or a.asname]
    if len(parsed) < 10:
        out.fail(f'only {len(parsed)} apworld source file(s) read: the listing is wrong')
    elif bad:
        out.fail('imports outside the list in the patterns file ("apworld_imports", "apworld_modules"): each name the '
                 'apworld takes from Archipelago or Python is listed, since a module hands on everything it imported',
                 bad)
    else:
        out.ok(f'{len(parsed)} apworld files import only listed names: '
               f'{sum(len(v) for v in names.values())} from {len(names)} modules, and {", ".join(sorted(modules))}')


def annotation_ok(node):
    """Archipelago evaluates option annotations as code (typing.get_type_hints): only plain type expressions pass."""
    if node is None:
        return True
    if isinstance(node, ast.Constant):
        if isinstance(node.value, str):
            try:
                return annotation_ok(ast.parse(node.value, mode='eval').body)
            except SyntaxError:
                return False
        return node.value is None or node.value is Ellipsis
    if isinstance(node, ast.Name):
        return not node.id.startswith('__')
    if isinstance(node, ast.Attribute):
        return not node.attr.startswith('__') and annotation_ok(node.value)
    if isinstance(node, ast.Subscript):
        return annotation_ok(node.value) and annotation_ok(node.slice)
    if isinstance(node, (ast.Tuple, ast.List)):
        return all(annotation_ok(e) for e in node.elts)
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.BitOr):
        return annotation_ok(node.left) and annotation_ok(node.right)
    return False


@section('Apworld runs nothing unexpected')
def apworld_behaviour(ctx, out):
    p = ctx.patterns
    allowed_builtins, dunders = set(p['apworld_builtins']), set(p['apworld_dunders'])
    denied_attrs, denied_members = set(p['apworld_denied_attributes']), set(p['apworld_denied_members'])
    modules = p['apworld_modules']
    listed = {key for key, cells in ctx.capabilities().get('Apworld: reflection by name', [])}
    parsed, broken = apworld_sources(ctx)
    bad, reflection, used_rows = list(broken), [], set()
    for f, tree in parsed:
        owner = enclosing_functions(tree)
        bound = {a.asname or a.name.split('.')[0] for n in ast.walk(tree) if isinstance(n, (ast.Import, ast.ImportFrom))
                 for a in n.names}
        bound |= {n.id for n in ast.walk(tree) if isinstance(n, ast.Name) and isinstance(n.ctx, ast.Store)}
        bound |= {n.name for n in ast.walk(tree) if isinstance(n, (ast.FunctionDef, ast.ClassDef, ast.AsyncFunctionDef))}
        bound |= {a.arg for n in ast.walk(tree) if isinstance(n, ast.arguments)
                  for a in n.posonlyargs + n.args + n.kwonlyargs + [x for x in (n.vararg, n.kwarg) if x]}
        imported_modules = {a.asname or a.name for n in ast.walk(tree) if isinstance(n, ast.Import) for a in n.names}
        for node in tree.body:
            if isinstance(node, ast.Expr) and not (isinstance(node.value, ast.Constant) and isinstance(node.value.value, str)):
                bad.append(f'{f.path}:{node.lineno}: a statement run at import time (every Archipelago start runs it)')
            elif isinstance(node, (ast.While, ast.With, ast.AsyncWith, ast.Try, ast.AsyncFunctionDef)):
                bad.append(f'{f.path}:{node.lineno}: {type(node).__name__} at the top level of a module')
        for node in ast.walk(tree):
            where = f'{f.path}:{getattr(node, "lineno", 0)}'
            if isinstance(node, ast.Name) and isinstance(node.ctx, ast.Load):
                if node.id in BUILTIN_NAMES and node.id not in bound | allowed_builtins | dunders:
                    bad.append(f'{where}: the builtin {node.id}')
                if node.id.startswith('__') and node.id not in dunders:
                    bad.append(f'{where}: {node.id}')
            elif isinstance(node, ast.Attribute):
                if node.attr.startswith('__') and node.attr not in dunders:
                    bad.append(f'{where}: .{node.attr}')
                if node.attr in denied_attrs:
                    bad.append(f'{where}: .{node.attr} (writes files, runs programs or opens connections)')
                if isinstance(node.value, ast.Name) and node.value.id in imported_modules \
                        and node.attr not in modules.get(node.value.id, []):
                    bad.append(f'{where}: {node.value.id}.{node.attr}')
            elif isinstance(node, ast.Constant) and isinstance(node.value, str) \
                    and re.fullmatch(r'__\w+__', node.value) and node.value not in dunders:
                bad.append(f'{where}: the string "{node.value}" (names a hidden attribute)')
            elif isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
                if node.name in denied_members:
                    bad.append(f'{where}: {node.name} (a world hook handed files or settings to write)')
                args = node.args
                for a in args.posonlyargs + args.args + args.kwonlyargs + [x for x in (args.vararg, args.kwarg) if x]:
                    if not annotation_ok(a.annotation):
                        bad.append(f'{where}: the annotation of {a.arg} is more than a type')
                if not annotation_ok(node.returns):
                    bad.append(f'{where}: the return annotation of {node.name} is more than a type')
            elif isinstance(node, ast.AnnAssign) and not annotation_ok(node.annotation):
                bad.append(f'{where}: an annotation that is more than a type')
            lookup = isinstance(node, ast.Call) and isinstance(node.func, ast.Name) \
                and node.func.id in ('getattr', 'hasattr') and len(node.args) >= 2
            if lookup and isinstance(node.args[1], ast.Constant) and node.args[1].value in denied_attrs:
                bad.append(f'{where}: {node.func.id}(..., "{node.args[1].value}")')
            if lookup and not (isinstance(node.args[1], ast.Constant) and isinstance(node.args[1].value, str)):
                key = f'{f.path} {owner.get(node, "<module>")}'
                if key in listed:
                    used_rows.add(key)
                else:
                    reflection.append(f'{where}: {ast.unparse(node)[:90]} (in {owner.get(node, "<module>")})')
        for cls in (n for n in ast.walk(tree) if isinstance(n, ast.ClassDef)):
            for stmt in cls.body:
                targets = (stmt.targets if isinstance(stmt, ast.Assign)
                           else [stmt.target] if isinstance(stmt, ast.AnnAssign) else [])
                bad += [f'{f.path}:{stmt.lineno}: {cls.name}.{t.id} (a world hook handed files or settings to write)'
                        for t in targets if isinstance(t, ast.Name) and t.id in denied_members]
    stale = sorted(listed - used_rows)
    if len(parsed) < 10:
        out.fail(f'only {len(parsed)} apworld source file(s) read: the listing is wrong')
        return
    if bad:
        out.fail('the apworld reaches for something it must never do on a generating machine', bad)
    if reflection:
        out.fail(f'an attribute looked up by a computed name, not listed in {CAPABILITIES}, '
                 '"Apworld: reflection by name" (path and function)', reflection)
    if stale:
        out.fail(f'rows in {CAPABILITIES}, "Apworld: reflection by name", that no code matches any more', stale)
    if not (bad or reflection or stale):
        out.ok(f'{len(parsed)} files: only listed builtins, no hidden attributes, no file, process or network calls, '
               f'plain annotations, nothing run at import but definitions; {len(used_rows)} lookups by name, all listed')


def strict_json(text):
    def pairs(items):
        keys = [k for k, v in items]
        if len(keys) != len(set(keys)):
            raise ValueError(f'a key given twice: {sorted(k for k in keys if keys.count(k) > 1)[0]}')
        return dict(items)

    def constant(name):
        raise ValueError(f'{name} is not JSON')
    return json.loads(text, object_pairs_hook=pairs, parse_constant=constant)


def json_strings(value):
    if isinstance(value, str):
        yield value
    elif isinstance(value, dict):
        for k, v in value.items():
            yield k
            yield from json_strings(v)
    elif isinstance(value, list):
        for v in value:
            yield from json_strings(v)


@section('Apworld data and docs')
def apworld_data(ctx, out):
    bad, data = [], 0
    for f in ctx.files:
        if not f.path.startswith(APWORLD):
            continue
        if f.path.endswith('.json'):
            data += 1
            try:
                value = strict_json(f.text)
            except ValueError as e:
                bad.append(f'{f.path}: not strict JSON ({e})')
                continue
            bad += [f'{f.path}: the string "{s}" (names a hidden attribute)' for s in json_strings(value)
                    if re.fullmatch(r'__\w+__', s)]
            if f.path == APWORLD + 'archipelago.json':
                keys = set(value) if isinstance(value, dict) else set()
                if keys != set(ctx.patterns['apworld_manifest_keys']):
                    bad.append(f'{f.path}: keys {sorted(keys)}, expected exactly '
                               f'{sorted(ctx.patterns["apworld_manifest_keys"])}')
        elif f.path.endswith('.md'):
            for n, line in f.lines():
                if re.search(r'<\s*[A-Za-z!/?]', line):
                    bad.append(f'{f.path}:{n}: raw HTML (the website renders player docs)')
                if re.search(r'(?i)\b(javascript|vbscript|data)\s*:', line):
                    bad.append(f'{f.path}:{n}: a script or data link')
    if data < 5:
        out.fail(f'only {data} apworld data file(s) read: the listing is wrong')
    elif bad:
        out.fail('apworld data or player docs that could carry something else', bad)
    else:
        out.ok(f'{data} data files are strict JSON with no hidden attribute names; the manifest has exactly its keys; '
               'the player docs hold no raw HTML or script links')


CS_TOKENS = re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\\n])*"|\'(?:\\.|[^\'\\\n])+\'|//[^\n]*|/\*.*?\*/', re.S)
PS_TOKENS = re.compile(r"'(?:''|[^'])*'|\"(?:`.|[^\"`])*\"|<#.*?#>|#[^\n]*", re.S)
SH_TOKENS = re.compile(r"'[^']*'|\"(?:\\.|[^\"\\])*\"|(?<![\w$])#[^\n]*", re.S)


def without_comments(text, tokens, comment_starts):
    """Comments blanked (line numbers kept); strings kept, since a name in a string can still be reached."""
    return tokens.sub(lambda m: re.sub(r'[^\n]', ' ', m.group(0)) if m.group(0).startswith(comment_starts)
                      else m.group(0), text)


def line_of(text, pos):
    return text.count('\n', 0, pos) + 1


def capability_rows(ctx, heading):
    """(path, capability) pairs listed under a heading of docs/capabilities.md, and those missing a reason."""
    rows, reasonless = set(), []
    for key, cells in ctx.capabilities().get(heading, []):
        if len(cells) < 3 or not cells[-1]:
            reasonless.append(key)
        rows.add((key, cells[1] if len(cells) > 1 else ''))
    return rows, reasonless


def compare_capabilities(out, heading, found, rows, reasonless, what):
    unlisted = sorted(found - rows)
    stale = sorted(rows - found)
    if unlisted:
        out.fail(f'{what} doing something not listed in {CAPABILITIES}, "{heading}"',
                 [f'{p}: {c}' for p, c in unlisted])
    if stale:
        out.fail(f'rows in {CAPABILITIES}, "{heading}", that the code no longer matches: remove them',
                 [f'{p}: {c}' for p, c in stale])
    if reasonless:
        out.fail(f'rows in "{heading}" without a reason', reasonless)
    return not (unlisted or stale or reasonless)


@section('Mod source')
def mod_source(ctx, out):
    denied = {k: re.compile(v, re.M) for k, v in ctx.patterns['mod_denied'].items()}
    kinds = {k: re.compile(v, re.M) for k, v in ctx.patterns['mod_capabilities'].items()}
    # Text the server decides reaches the game only through one cleaner (the game runs |commands| in text it shows).
    raw_text, cleaner = re.compile(ctx.patterns['mod_server_text']['reads']), ctx.patterns['mod_server_text']['only_in']
    heading = 'Mod: what the code touches'
    rows, reasonless = capability_rows(ctx, heading)
    bad, found, count = [], set(), 0
    for f in ctx.files:
        if not (f.path.startswith('mod/') and f.path.endswith('.cs')):
            continue
        count += 1
        code = without_comments(f.text, CS_TOKENS, ('//', '/*'))
        for name, rx in denied.items():
            bad += [f'{f.path}:{line_of(code, m.start())}: {m.group(0).strip()} ({name})' for m in rx.finditer(code)]
        if re.search(r'[A-Za-z_]\\u[0-9A-Fa-f]{4}|\\u[0-9A-Fa-f]{4}[A-Za-z_]', re.sub(r'"(?:\\.|[^"\\\n])*"', '', code)):
            bad.append(f'{f.path}: a \\u escape outside a string (an identifier spelled in escapes)')
        found |= {(f.path, k) for k, rx in kinds.items() if rx.search(code)}
        if f.path != cleaner:
            bad += [f'{f.path}:{line_of(code, m.start())}: {m.group(0)} read raw: text from the server goes through '
                    f'{cleaner.rsplit("/", 1)[-1]}' for m in raw_text.finditer(code)]
    if count < 50:
        out.fail(f'only {count} mod source file(s) read: the listing is wrong')
        return
    if bad:
        out.fail('the mod reaches for something it must never do on a player\'s machine', bad)
    ok = compare_capabilities(out, heading, found, rows, reasonless, 'mod files')
    if ok and not bad:
        out.ok(f'{count} C# files: none of {len(denied)} denied kinds of call; the {len(found)} things they touch '
               f'beyond the game are all listed')


def python_findings(f, tree, p):
    """(denied, capabilities) of one dev script or hook helper, read from its syntax tree."""
    bad, caps = [], set()
    modules = {}
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            modules.update({(a.asname or a.name): a.name for a in node.names})
        elif isinstance(node, ast.ImportFrom) and node.module:
            modules.update({(a.asname or a.name): f'{node.module}.{a.name}' for a in node.names})
    for module in modules.values():
        if module.split('.')[0] in p['script_denied_modules']:
            bad.append(f'{f.path}: imports {module}')
        caps |= {cap for cap, mods in p['script_capability_modules'].items() if module.split('.')[0] in mods}
    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        func = node.func
        if isinstance(func, ast.Name) and func.id in p['script_denied_builtins']:
            bad.append(f'{f.path}:{node.lineno}: {func.id}()')
        if isinstance(func, ast.Name):
            caps |= {cap for cap, names in p['script_capability_calls'].items() if f'*.{func.id}' in names}
        if isinstance(func, ast.Attribute):
            owner = modules.get(func.value.id, '') if isinstance(func.value, ast.Name) else ''
            if func.attr in p['script_denied_methods'] and owner.split('.')[0] in ('os', 'subprocess', ''):
                bad.append(f'{f.path}:{node.lineno}: .{func.attr}()')
            for cap, names in p['script_capability_calls'].items():
                if f'{owner}.{func.attr}' in names or f'*.{func.attr}' in names:
                    caps.add(cap)
        if any(k.arg == 'shell' and not (isinstance(k.value, ast.Constant) and k.value.value is False)
               for k in node.keywords):
            bad.append(f'{f.path}:{node.lineno}: shell=True (a command line the shell re-reads)')
        mode = node.args[1] if len(node.args) > 1 else next((k.value for k in node.keywords if k.arg == 'mode'), None)
        if isinstance(func, ast.Name) and func.id == 'open' and isinstance(mode, ast.Constant) \
                and re.search(r'[wax+]', str(mode.value)):
            caps.add('writes files')
    return bad, caps


@section('Dev scripts and hooks')
def dev_scripts(ctx, out):
    p = ctx.patterns
    ps_denied = {k: re.compile(v, re.M | re.I) for k, v in p['ps1_denied'].items()}
    ps_caps = {k: re.compile(v, re.M | re.I) for k, v in p['ps1_capabilities'].items()}
    sh_denied = {k: re.compile(v, re.M) for k, v in p['sh_denied'].items()}
    heading = 'Dev scripts and hooks: what they touch'
    rows, reasonless = capability_rows(ctx, heading)
    bad, found, count = [], set(), 0
    for f in ctx.files:
        path = f.path
        if not (path.startswith(('dev-scripts/', '.githooks/')) and not f.binary):
            continue
        if path.endswith('.py'):
            count += 1
            try:
                tree = ast.parse(f.text, path)
            except SyntaxError as e:
                bad.append(f'{path}: does not parse ({e})')
                continue
            b, caps = python_findings(f, tree, p)
            bad += b
            found |= {(path, c) for c in caps}
        elif path.endswith('.ps1'):
            count += 1
            code = without_comments(f.text, PS_TOKENS, ('#', '<#'))
            for name, rx in ps_denied.items():
                bad += [f'{path}:{line_of(code, m.start())}: {m.group(0).strip()} ({name})' for m in rx.finditer(code)]
            found |= {(path, k) for k, rx in ps_caps.items() if rx.search(code)}
        elif path.startswith('.githooks/') and (path.endswith('.sh') or '.' not in path.rsplit('/', 1)[-1]):
            count += 1
            code = without_comments(f.text, SH_TOKENS, ('#',))
            for name, rx in sh_denied.items():
                bad += [f'{path}:{line_of(code, m.start())}: {m.group(0).strip()} ({name})' for m in rx.finditer(code)]
    if count < 15:
        out.fail(f'only {count} script(s) read: the listing is wrong')
        return
    if bad:
        out.fail('a script does something no script here may do', bad)
    ok = compare_capabilities(out, heading, found, rows, reasonless, 'scripts')
    if ok and not bad:
        out.ok(f'{count} scripts and hooks: nothing denied; the {len(found)} things they do beyond reading are all listed')


RELEASE = 'release/mod/BepInEx/plugins/BugFablesAP/'
OUR_DLL = RELEASE + 'BugFablesAP.dll'
BUILT_FROM = 'release/built-from.txt'
CS_ESCAPES = {'n': '\n', 't': '\t', 'r': '\r', '0': '\0', 'a': '\a', 'b': '\b', 'f': '\f', 'v': '\v', '\\': '\\',
              '"': '"', "'": "'"}


def cs_literals(src):
    """Every string literal of C# source, unescaped; an interpolated string gives each literal part, and the strings
    inside its holes are read too. A raw or unterminated string is refused rather than guessed at."""
    pieces = []

    def char_literal(i):
        j = i + 1
        while j < len(src) and src[j] != "'":
            j += 2 if src[j] == '\\' else 1
        return j + 1

    def code(i, closer):
        depth = 0
        while i < len(src):
            c = src[i]
            if src.startswith('//', i):
                end = src.find('\n', i)
                i = len(src) if end < 0 else end
            elif src.startswith('/*', i):
                end = src.find('*/', i + 2)
                i = len(src) if end < 0 else end + 2
            elif c == "'":
                i = char_literal(i)
            elif c in '$@"' and re.match(r'(\$@|@\$|\$|@)?"', src[i:i + 3]):
                i = string(i)
            elif closer and depth == 0 and c == closer:
                return i
            else:
                depth += (c in '([{') - (c in ')]}')
                i += 1
        if closer:
            raise Unreadable('an interpolated string whose hole never closes')
        return i

    def string(i):
        prefix = re.match(r'(\$@|@\$|\$|@)?"', src[i:i + 3]).group(1) or ''
        if src.startswith('"""', i + len(prefix)):
            raise Unreadable('a raw string literal (""") that this reader does not follow')
        interp, verbatim = '$' in prefix, '@' in prefix
        j, buf = i + len(prefix) + 1, ''
        while True:
            if j >= len(src) or (not verbatim and src[j] == '\n'):
                raise Unreadable('an unterminated string literal')
            c = src[j]
            if c == '"':
                if verbatim and src.startswith('""', j):
                    buf, j = buf + '"', j + 2
                    continue
                pieces.append(buf)
                return j + 1
            if c == '\\' and not verbatim:
                n = src[j + 1]
                if n in 'uU':
                    width = 4 if n == 'u' else 8
                    buf, j = buf + chr(int(src[j + 2:j + 2 + width], 16)), j + 2 + width
                elif n == 'x':
                    hexdigits = re.match(r'[0-9a-fA-F]{1,4}', src[j + 2:]).group(0)
                    buf, j = buf + chr(int(hexdigits, 16)), j + 2 + len(hexdigits)
                else:
                    buf, j = buf + CS_ESCAPES.get(n, n), j + 2
                continue
            if interp and c in '{}':
                if src.startswith(c * 2, j):
                    buf, j = buf + c, j + 2
                    continue
                if c == '{':
                    pieces.append(buf)
                    buf, j = '', code(j + 1, '}') + 1
                    continue
            buf, j = buf + c, j + 1

    code(0, None)
    return {p for p in pieces if p}


def built_from(ctx):
    """built-from.txt: (commit, {name: hash}) of the committed release; --dll-commit names another build's commit."""
    if ctx.args.dll_commit:
        return ctx.args.dll_commit, {}
    f = ctx.file(BUILT_FROM)
    if f is None:
        raise Unreadable(f'{BUILT_FROM} is missing')
    commit, lines = None, {}
    for line in f.text.splitlines():
        key, _, value = line.partition(': ')
        if key == 'commit':
            commit = value.strip()
        elif value and not line.startswith('#'):
            lines[key] = value.strip()
    if not commit or not re.fullmatch(r'[0-9a-f]{7,40}', commit):
        raise Unreadable(f'{BUILT_FROM} names no commit')
    return commit, lines


def build_inputs(paths_and_shas):
    """The files the release build reads, as build-release.ps1 lists them: {path: blob sha}."""
    rx = re.compile(r'^(mod/BugFablesAP/.+\.(cs|csproj)|mod/BugFablesAP/packages\.lock\.json|global\.json|nuget\.config'
                    r'|Directory\.Build\.props)$')
    return {p: s for p, s in paths_and_shas if rx.match(p) and not p.startswith('mod/BugFablesAP/Dev/')}


def our_dll(ctx):
    if ctx.dll is None:
        f = ctx.file(OUR_DLL)
        if f is None:
            raise Unreadable(f'{OUR_DLL} is missing')
        try:
            ctx.dll = dotnet_metadata.Assembly(f.data)
            ctx.dll_refs = ctx.dll.references()
        except dotnet_metadata.MetadataError as e:
            raise Unreadable(f'{OUR_DLL} could not be read as .NET metadata: {e}')
    return ctx.dll


@section('Release staging')
def release_staging(ctx, out):
    staged = sorted(f.path for f in ctx.files if f.path.startswith('release/mod/'))
    expected = sorted(['release/mod/README.txt'] + [RELEASE + n for n in (
        'BugFablesAP.dll', 'Archipelago.MultiClient.Net.dll', 'websocket-sharp.dll', 'Newtonsoft.Json.dll', 'LICENSE.txt',
        'THIRD-PARTY-NOTICES.txt')])
    commit, recorded = built_from(ctx)
    problems = []
    if staged != expected:
        problems.append(f'release/mod holds {staged}, not exactly {expected}')
    for name in ('BugFablesAP.dll', 'Archipelago.MultiClient.Net.dll', 'websocket-sharp.dll', 'Newtonsoft.Json.dll'):
        f = ctx.file(RELEASE + name)
        if f and recorded.get(name) != sha256(f.data):
            problems.append(f'{name}: sha256 {sha256(f.data)[:16]}..., {BUILT_FROM} records '
                            f'{str(recorded.get(name))[:16]}...: the DLL changed without its record')
    for key in ('sdk', 'game'):
        if key not in recorded:
            problems.append(f'{BUILT_FROM} records no {key}')
    # The recorded sources must be exactly that commit's: what the DLL claims to be built from, it was.
    try:
        at_commit = [(p, s) for m, t, s, p in (line.split(None, 3) for line in ctx.repo.git(
            'ls-tree', '-r', '--full-tree', commit).decode('utf-8', 'surrogateescape').splitlines())]
    except Unreadable:
        at_commit = None
        problems.append(f'{BUILT_FROM} names commit {commit}, which this repository does not have')
    sources = {k: v for k, v in recorded.items() if k not in ('sdk', 'game') and not k.endswith('.dll')}
    if at_commit is not None and build_inputs(at_commit) != sources:
        diff = sorted(set(build_inputs(at_commit).items()) ^ set(sources.items()))
        problems.append(f'the sources {BUILT_FROM} lists are not commit {commit[:10]}\'s: ' +
                        ', '.join(p for p, s in diff[:6]))
    current = build_inputs((f.path, f.sha) for f in ctx.files)
    stale = sorted(p for p in set(current) | set(sources) if current.get(p) != sources.get(p))
    if problems:
        out.fail('the committed mod download does not match its own record', problems)
    elif stale and ctx.args.release:
        out.fail('the committed DLL is older than its sources: run dev-scripts/build-release.ps1 and commit', stale)
    elif stale:
        out.warn(f'the committed DLL was built from {commit[:10]}; {len(stale)} build input(s) changed since (fine '
                 f'between releases, a release rebuilds it)', stale[:8])
    if not problems and not (stale and ctx.args.release):
        out.ok(f'release/mod holds exactly its {len(expected)} files; each DLL is the one {BUILT_FROM} records, built '
               f'from commit {commit[:10]}, whose {len(sources)} build inputs it lists exactly')


@section('Shipped DLL structure')
def dll_structure(ctx, out):
    a = our_dll(ctx)
    data = ctx.file(OUR_DLL).data
    bad = []
    names = [s[0] for s in a.sections]
    if names != ['.text', '.rsrc', '.reloc']:
        bad.append(f'sections {names}, expected .text, .rsrc, .reloc')
    end = max(rawptr + rawsize for _, _, _, rawptr, rawsize in a.sections)
    if len(data) != end:
        bad.append(f'{len(data) - end} bytes after the last section (data no loader reads)')
    for number, what in ((0, 'exports'), (4, 'an appended certificate'), (9, 'thread-local storage callbacks'),
                         (11, 'bound imports'), (13, 'delay-loaded imports')):
        if len(a.directories) > number and a.directories[number][1]:
            bad.append(f'the PE has {what}')
    if a.imports() != [('mscoree.dll', ['_CorDllMain'])]:
        bad.append(f'native imports {a.imports()}, expected only mscoree.dll!_CorDllMain')
    if not a.cli_flags & 1 or a.cli_flags & 0x10:
        bad.append(f'CLI flags {a.cli_flags:#x}: not IL-only, or a native entry point')
    bad += [f'the CLI header has {name}' for name, (rva, size) in a.cli_dirs.items()
            if size and name != 'StrongNameSignature']
    streams = [s[0] for s in a.streams]
    if sorted(streams) != sorted(['#~', '#Strings', '#US', '#GUID', '#Blob']) or len(streams) != 5:
        bad.append(f'metadata streams {streams}, expected exactly #~ #Strings #US #GUID #Blob')
    for table in ('ModuleRef', 'ImplMap', 'ManifestResource', 'File', 'ExportedType', 'FieldPtr', 'MethodPtr',
                  'ParamPtr', 'EventPtr', 'PropertyPtr', 'EncLog', 'EncMap', 'AssemblyProcessor', 'AssemblyOS',
                  'AssemblyRefProcessor', 'AssemblyRefOS', 'DeclSecurity'):
        if a.table[table]:
            bad.append(f'{len(a.table[table])} {table} row(s)'
                       + (' (native calls)' if table in ('ModuleRef', 'ImplMap') else
                          ' (embedded resources)' if table == 'ManifestResource' else ''))
    owner = a.method_owner()
    for m, (rva, impl, flags, name, sig, params) in enumerate(a.table['MethodDef'], 1):
        typ = a.typedef_name(owner[m])
        if flags & 0x2000 or impl & 0x1000 or impl & 3 in (1, 2):
            bad.append(f'{typ}::{a.string(name)}: native, internal-call or P/Invoke')
        elif impl & 3 == 3:
            extends = a.row('TypeDef', owner[m])[3]
            if a.type_name(extends)[1] != 'System.MulticastDelegate':
                bad.append(f'{typ}::{a.string(name)}: a runtime-implemented method outside a delegate')
    for rva, field in a.table['FieldRVA']:
        typ = a.member(0x04000000 | field)[1]
        if not typ.startswith('<PrivateImplementationDetails>'):
            bad.append(f'{typ}: data stored in the file itself (FieldRVA), outside the compiler\'s array initialisers')
    module = a.string(a.table['Module'][0][1])
    if module != 'BugFablesAP.dll' or a.assembly_name() != 'BugFablesAP':
        bad.append(f'module {module}, assembly {a.assembly_name()}: not the mod')
    if bad:
        out.fail('the shipped DLL is not a plain compiled library', bad)
    else:
        out.ok(f'a plain IL-only library: .text/.rsrc/.reloc and nothing after, only mscoree!_CorDllMain imported, '
               f'the five standard streams, no native calls, resources or embedded data; {len(a.table["MethodDef"])} '
               f'methods, {len(a.table["TypeDef"])} types')


def dll_calls(ctx):
    """(caller's outermost type, kind, 'Type::Member', assembly) for every reference in every method body."""
    a, out = our_dll(ctx), []
    for owner, method, calls, strings, tokens in ctx.dll_refs:
        caller = a.typedef_name(a.outermost(owner))
        for kind, (asm, typ, member) in calls:
            out.append((caller, kind, f'{typ}::{member}', asm))
    return out


def glob_any(text, globs):
    return any(fnmatch.fnmatchcase(text, g) for g in globs)


def printable_runs(data, minimum=6):
    """ASCII and UTF-16 text runs inside binary data."""
    runs = re.findall(rb'[\x20-\x7e]{%d,}' % minimum, data)
    runs += [r.decode('utf-16-le').encode() for r in re.findall(rb'(?:[\x20-\x7e]\x00){%d,}' % minimum, data)]
    return [r.decode('ascii', 'replace') for r in runs]


@section('Shipped DLL reach')
def dll_reach(ctx, out):
    p = ctx.patterns
    a = our_dll(ctx)
    bad = []
    refs = set(a.assembly_refs())
    if not refs <= set(p['dll_assembly_refs']):
        bad.append(f'references assemblies outside the list: {sorted(refs - set(p["dll_assembly_refs"]))}')
    calls = dll_calls(ctx)
    denied = p['dll_denied']
    bad += sorted({f'{caller}: {kind} {target}' for caller, kind, target, asm in calls if glob_any(target, denied)})
    # Names reached through strings or attributes (typeof in an attribute leaves only a name in #Blob).
    names = p['dll_denied_names']
    texts = a.user_strings() + printable_runs(a.heap['#Blob'])
    bad += sorted({f'the text "{t[:80]}" names {n}' for t in texts for n in names if n in t})
    listed = {key for key, cells in ctx.capabilities().get('Hosts', [])}
    for t in a.user_strings():
        for host, _ in hosts_in(File('dll', '100644', '', t.encode('utf-8'))):
            if not loopback(host) and not any(host == k or host.endswith('.' + k) for k in listed):
                bad.append(f'the string "{t[:80]}" names the unlisted host {host}')
        for m in QUOTED_HOST.finditer('"' + t + '"'):
            host = m.group(1).lower()
            if not any(host == k or host.endswith('.' + k) for k in listed):
                bad.append(f'the string "{t}" is an unlisted host')
    # What the DLL does, by the type that does it, must be in the source list for a file declaring that type.
    rows, reasonless = capability_rows(ctx, 'Mod: what the code touches')
    declared = {}
    for f in ctx.files:
        if f.path.startswith('mod/BugFablesAP/') and f.path.endswith('.cs') and '/Dev/' not in f.path:
            for name in re.findall(r'\b(?:class|struct|interface|enum)\s+([A-Za-z_]\w*)', f.text):
                declared.setdefault(name, set()).add(f.path)
    unlisted = set()
    for cap, globs in p['dll_capabilities'].items():
        for caller, kind, target, asm in calls:
            if glob_any(target, globs):
                simple = caller.rsplit('.', 1)[-1]
                if not any((path, cap) in rows for path in declared.get(simple, ())):
                    unlisted.add(f'{caller}: {cap} ({target})')
    patched = harmony_targets(a)
    listed_patches = {key for key, cells in ctx.capabilities().get('Mod: patches outside the game', [])}
    outside = {t for t in patched if not t.startswith('Assembly-CSharp:')}
    if bad:
        out.fail('the shipped DLL reaches for something the mod must never do', bad)
    if unlisted:
        out.fail(f'the shipped DLL does something its source list ({CAPABILITIES}) doesn\'t say, by the type doing it',
                 sorted(unlisted))
    if outside != listed_patches:
        out.fail(f'Harmony patches on code outside the game, against {CAPABILITIES}, "Mod: patches outside the game"',
                 [f'patched, not listed: {t}' for t in sorted(outside - listed_patches)]
                 + [f'listed, not patched: {t}' for t in sorted(listed_patches - outside)])
    if not bad and not unlisted and outside == listed_patches:
        out.ok(f'{len(refs)} referenced assemblies, all expected; none of {len(denied)} denied calls in '
               f'{len(calls)} references; every capability in the binary is in the source list; '
               f'{len(patched)} Harmony patch targets by attribute, {len(outside)} outside the game, listed')


def attribute_arguments(a, signature, value):
    """The fixed arguments of a custom attribute, decoded by its constructor's signature: [(kind, value)].
    Only the shapes Harmony's attributes use are known; any other shape is refused rather than guessed at."""
    def param(sig, pos):
        tag = sig[pos]
        if tag == 0x0E:
            return 'string', pos + 1
        if tag in (0x08, 0x02):
            return {0x08: 'int', 0x02: 'bool'}[tag], pos + 1
        if tag in (0x11, 0x12):
            coded, nxt = dotnet_metadata.compressed(sig, pos + 1)
            name = a.type_name(([0x02, 0x01, 0x1B][coded & 3], coded >> 2))[1]
            if tag == 0x12 and name != 'System.Type':
                raise Unreadable(f'an attribute argument of type {name}')
            return ('type' if tag == 0x12 else 'enum'), nxt
        if tag == 0x1D:
            inner, nxt = param(sig, pos + 1)
            return 'array of ' + inner, nxt
        raise Unreadable(f'an attribute argument of element type {tag:#x}')

    if signature[0] & 0x20 == 0 or signature[2] != 0x01:
        raise Unreadable('an attribute constructor that is not an instance method returning void')
    count, pos = dotnet_metadata.compressed(signature, 1)
    kinds, pos = [], pos + 1
    for _ in range(count):
        kind, pos = param(signature, pos)
        kinds.append(kind)
    if value[:2] != b'\x01\x00':
        raise Unreadable('a custom attribute without its prolog')

    def read(kind, at):
        if kind in ('string', 'type'):
            if value[at] == 0xFF:
                return None, at + 1
            size, at = dotnet_metadata.compressed(value, at)
            return value[at:at + size].decode('utf-8'), at + size
        if kind in ('int', 'enum'):
            return struct.unpack_from('<i', value, at)[0], at + 4
        if kind == 'bool':
            return value[at] != 0, at + 1
        count = struct.unpack_from('<i', value, at)[0]
        items, at = [], at + 4
        for _ in range(max(count, 0)):
            item, at = read(kind[len('array of '):], at)
            items.append(item)
        return items, at

    out, at = [], 2
    for kind in kinds:
        item, at = read(kind, at)
        out.append((kind, item))
    return out


def harmony_targets(a):
    """'Assembly:Type::Method' for every [HarmonyPatch] target. As Harmony does, the attributes stacked on one method
    (or class) are merged, and a method's are completed by its class's."""
    merged = {}
    for parent, ctor, value in a.table['CustomAttribute']:
        table, index = ctor
        if a.member((table << 24) | index)[1] != 'HarmonyLib.HarmonyPatch':
            continue
        signature = a.blob(a.row('MemberRef', index)[2] if table == 0x0A else a.row('MethodDef', index)[4])
        args = attribute_arguments(a, signature, a.blob(value))
        if parent[0] not in (0x02, 0x06):
            raise Unreadable(f'a HarmonyPatch attribute on metadata table {parent[0]:#x}')
        typ, name = merged.get(parent, (None, None))
        merged[parent] = (typ or next((v for k, v in args if k == 'type'), None),
                          name or next((v for k, v in args if k == 'string'), None))
    by_type = {index: target for (table, index), target in merged.items() if table == 0x02}
    by_method = [(index, target) for (table, index), target in merged.items() if table == 0x06]

    def qualified(type_name, method):
        if not type_name:
            raise Unreadable('a HarmonyPatch whose target type is given nowhere')
        name, _, rest = type_name.partition(',')
        return f'{(rest.split(",")[0].strip() or a.assembly_name())}:{name.strip()}::{method or "*"}'

    owners, targets = a.method_owner(), set()
    for method, (typ, name) in by_method:
        outer_type, outer_name = by_type.get(owners[method], (None, None))
        targets.add(qualified(typ or outer_type, name or outer_name))
    for tdef, (typ, name) in by_type.items():
        if not any(owners[m] == tdef for m, _ in by_method):
            targets.add(qualified(typ, name))
    return targets


@section('The DLL says only what its source says')
def dll_matches_source(ctx, out):
    p = ctx.patterns
    a = our_dll(ctx)
    commit, recorded = built_from(ctx)
    listing = ctx.repo.git('ls-tree', '-r', '--full-tree', commit).decode('utf-8', 'surrogateescape').splitlines()
    entries = [line.split(None, 3) for line in listing]
    shas = [s for m, t, s, path in entries
            if path.startswith('mod/BugFablesAP/') and path.endswith('.cs') and '/Dev/' not in path]
    dev = [s for m, t, s, path in entries if path.startswith('mod/BugFablesAP/Dev/') and path.endswith('.cs')]
    blobs = ctx.repo.blobs(shas + dev)
    code = '\n'.join(blobs[s].decode('utf-8') for s in shas)
    idents = set(re.findall(r'[A-Za-z_]\w*', code))
    pieces = cs_literals(code)
    declared = set(re.findall(r'\b(?:class|struct|enum|interface|record)\s+([A-Za-z_]\w*)', code)) | set(
        re.findall(r'\bdelegate\s+[\w<>\[\],. ]+?\s+([A-Za-z_]\w*)\s*[(<]', code))
    dev_types = set(re.findall(r'\b(?:class|struct|enum|interface)\s+([A-Za-z_]\w*)',
                               '\n'.join(blobs[s].decode('utf-8') for s in dev))) - declared
    synthesized, compiler_types = set(p['dll_synthesized_members']), set(p['dll_compiler_types'])

    def generated_ok(name):
        """A compiler-made name (<Run>b__3_0, <>c) must be built from a name in the source."""
        return all(inner in idents or inner in ('', '.ctor', '.cctor', 'PrivateImplementationDetails', 'Module')
                   or re.fullmatch(r'\d+', inner) for inner in re.findall(r'<([^<>]*)>', name))

    def plain(name):
        return name in idents or re.sub(r'^(get_|set_|add_|remove_)', '', name) in idents

    bad = []
    for i, row in enumerate(a.table['TypeDef'], 1):
        name, full = a.string(row[1]), a.typedef_name(i)
        if name in dev_types:
            bad.append(f'type {full}: a Dev/ type, which the release build leaves out')
        elif '<' in name or full.startswith('<PrivateImplementationDetails>'):
            if not generated_ok(full):
                bad.append(f'type {full}: a compiler-made name built from nothing in the source')
        elif full not in compiler_types and name not in declared:
            bad.append(f'type {full}: declared in no source file')
    for rva, impl, flags, name, sig, params in a.table['MethodDef']:
        n = a.string(name)
        last = n.rsplit('.', 1)[-1]
        if not (n in ('.ctor', '.cctor') or ('<' in n and generated_ok(n)) or plain(n) or n in synthesized
                or ('.' in n and (plain(last) or last in synthesized))):
            bad.append(f'method {n}: in no source file')
    field_owner = a.field_owner()
    for i, (flags, name, sig) in enumerate(a.table['Field'], 1):
        n = a.string(name)
        # Array initialiser data: the compiler names each field after its bytes' hash.
        initialiser = a.typedef_name(field_owner[i]).startswith('<PrivateImplementationDetails>') and re.fullmatch(
            r'[0-9A-F]{64}|__StaticArrayInit\w*', n)
        if not (plain(n) or ('<' in n and generated_ok(n)) or n in synthesized or initialiser):
            bad.append(f'field {n}: in no source file')
    for parent, name, sig in a.table['MemberRef']:
        n = a.string(name)
        if not (plain(n) or n in synthesized or ('<' in n and generated_ok(n))):
            bad.append(f'a call to {n}: a name in no source file')
    by_first = {}
    for piece in pieces:
        by_first.setdefault(piece[0], []).append(piece)

    def explained(s):
        if s in pieces or s in idents:
            return True
        parts = [x.replace('{{', '{').replace('}}', '}') for x in re.split(r'\{\d+(?:[,:][^}]*)?\}', s) if x]
        if parts and all(x in pieces for x in parts):
            return True
        reach = [False] * (len(s) + 1)
        reach[0] = True
        for i in range(len(s)):
            if reach[i]:
                for piece in by_first.get(s[i], ()):
                    if s.startswith(piece, i):
                        reach[i + len(piece)] = True
        return reach[-1]

    strings = {s for owner, m, calls, ss, tokens in ctx.dll_refs for s in ss}
    bad += sorted(f'the string {ascii(s)[:90]}: in no source file' for s in strings if not explained(s))
    if len(shas) < 40 or len(pieces) < 500:
        out.fail(f'only {len(shas)} source files and {len(pieces)} literals read at {commit[:10]}: the listing is wrong')
    elif bad:
        out.fail(f'the shipped DLL holds what its source at {commit[:10]} does not (a DLL not built from it, or a '
                 f'build step that added code)', bad)
    else:
        out.ok(f'against its source at {commit[:10]}: all {len(a.table["TypeDef"])} types, '
               f'{len(a.table["MethodDef"])} methods, {len(a.table["Field"])} fields, {len(a.table["MemberRef"])} '
               f'member names and {len(strings)} strings come from it; none of Dev/\'s {len(dev_types)} types')


def yaml_code(line):
    """A workflow line without its comment (a '#' outside quotes, after a space or at the start)."""
    quote = None
    for i, c in enumerate(line):
        if quote:
            quote = None if c == quote else quote
        elif c in '\'"':
            quote = c
        elif c == '#' and (i == 0 or line[i - 1] == ' '):
            return line[:i].rstrip()
    return line.rstrip()


@section('Workflows')
def workflows(ctx, out):
    w = ctx.patterns['workflows']
    bad, jobs_seen = [], {}
    files = [f for f in ctx.files if f.path.startswith('.github/workflows/')]
    for f in files:
        top, job, section_key, scalar, script = None, None, None, None, False
        top_permissions, triggers, needs = None, [], {}
        job_permissions = {}
        for n, raw in enumerate(f.text.split('\n'), 1):
            where = f'{f.path}:{n}'
            indent = len(raw) - len(raw.lstrip(' '))
            if raw[:indent + 1].count('\t'):
                bad.append(f'{where}: a tab in the indentation')
            if scalar is not None:
                if not raw.strip() or indent > scalar:
                    if script and '${{' in raw:
                        bad.append(f'{where}: an expression inside a script (pass it through env:, where it is data)')
                    continue
                scalar = None
            line = yaml_code(raw)
            if not line.strip():
                continue
            body = line.strip()
            key_match = re.match(r'^(?:-\s+)?([\w.-]+):(?:\s+(.*))?$', body)
            key = key_match.group(1) if key_match else None
            value = (key_match.group(2) or '').strip() if key_match else ''
            if re.match(r'^[|>][-+]?$', value):
                scalar, script = indent + (2 if body.startswith('- ') else 0), key == 'run'
            outside_quotes = re.sub(r"'[^']*'|\"[^\"]*\"", '', line)
            if re.search(r'(?:^|[\s\[{,:-])[&*][A-Za-z_]', outside_quotes):
                bad.append(f'{where}: a YAML anchor or alias (a hidden copy of other text)')
            if indent == 0 and key:
                top = key
                if key == 'permissions':
                    top_permissions = value
            elif top == 'on' and indent == 2 and key:
                triggers.append(key)
            elif top == 'jobs' and indent == 2 and key:
                job, section_key = key, None
                jobs_seen[f'{f.path.rsplit("/", 1)[-1]}:{job}'] = True
            elif top == 'jobs' and indent == 4 and key:
                section_key = key
                if key == 'permissions':
                    job_permissions.setdefault(job, [])
                    if value:
                        job_permissions[job].append(value)
                if key == 'needs':
                    needs[job] = re.findall(r'[\w-]+', value)
                if key == 'runs-on' and value not in w['runners']:
                    bad.append(f'{where}: runs on {value} (only GitHub\'s own runners)')
            elif top == 'jobs' and section_key == 'permissions' and indent == 6 and key:
                job_permissions[job].append(f'{key}: {value}')
            if key == 'uses' and value and not value.startswith('./'):
                if not re.fullmatch(r'[\w.-]+/[\w./-]+@[0-9a-f]{40}', value):
                    bad.append(f'{where}: {value} is not pinned to a full commit hash')
                elif not re.search(r'#\s*v\d', raw):
                    bad.append(f'{where}: a pinned action without its version in a comment')
            if key == 'continue-on-error':
                bad.append(f'{where}: continue-on-error (a failing step would pass)')
            for secret in re.findall(r'secrets\.(\w+)', line):
                if secret != 'GITHUB_TOKEN':
                    bad.append(f'{where}: the secret {secret}')
        name = f.path.rsplit('/', 1)[-1]
        if top_permissions != '{}':
            bad.append(f'{f.path}: the workflow\'s own permissions must be exactly {{}} (each job asks for what it needs)')
        bad += [f'{f.path}: triggered by {t}' for t in triggers if t not in w['triggers']]
        if not triggers:
            bad.append(f'{f.path}: no trigger read: the parser is wrong')
        for job_name, grants in job_permissions.items():
            allowed = w['job_permissions'].get(f'{name}:{job_name}', [])
            bad += [f'{f.path}: job {job_name} asks for {g}' for g in grants if g not in allowed]
        if name == 'release.yml':
            missing = sorted(set(w['release_gates']) - set(needs.get('publish', [])))
            if missing:
                bad.append(f'{f.path}: publish does not wait for {", ".join(missing)}')
    if len(files) < 2 or len(jobs_seen) < 4:
        out.fail(f'only {len(files)} workflows and {len(jobs_seen)} jobs read: the listing is wrong')
    elif bad:
        out.fail('a workflow could run something other than what this repo says, or with more rights', bad)
    else:
        out.ok(f'{len(files)} workflows, {len(jobs_seen)} jobs: every action pinned to a commit, no permissions but '
               f'the listed ones, only known triggers and GitHub\'s runners, no secrets, no expression inside a script; '
               f'publish waits for {", ".join(w["release_gates"])}')


@section('Dependencies pinned')
def dependencies_pinned(ctx, out):
    d = ctx.patterns['dependencies']
    bad = []

    def text(path):
        f = ctx.file(path)
        if f is None:
            raise Unreadable(f'{path} is missing')
        return f.text

    csproj = text('mod/BugFablesAP/BugFablesAP.csproj')
    refs = dict(re.findall(r'<PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"', csproj))
    if len(refs) < 3:
        bad.append(f'only {len(refs)} package references read: the pattern is wrong')
    bad += [f'{name} {v}: not one exact version' for name, v in refs.items() if not re.fullmatch(r'\d+(\.\d+){1,3}', v)]
    for prop in ('RestorePackagesWithLockFile', 'RestoreLockedMode'):
        if f'<{prop}>true</{prop}>' not in csproj:
            bad.append(f'the csproj does not set {prop}')
    lock = strict_json(text('mod/BugFablesAP/packages.lock.json'))
    target = next(iter(lock.get('dependencies', {}).values()), {})
    direct = {k: v for k, v in target.items() if v.get('type') == 'Direct'}
    # The SDK adds some packages itself (NETStandard.Library for a netstandard2.0 build): listed, version included.
    refs = {**d['implicit_packages'], **refs}
    if set(direct) != set(refs):
        bad.append(f'the lock file\'s direct packages {sorted(direct)} are not the csproj\'s {sorted(refs)}')
    bad += [f'{k}: the lock file resolves {v.get("resolved")}, the csproj asks for {refs.get(k)}'
            for k, v in direct.items() if v.get('resolved') != refs.get(k)]
    bad += [f'{k}: no content hash in the lock file' for k, v in target.items()
            if not re.fullmatch(r'[A-Za-z0-9+/]{86}==', v.get('contentHash', ''))]
    config = text('nuget.config')
    sources = dict(re.findall(r'<add\s+key="([^"]+)"\s+value="([^"]+)"', config))
    if sources != d['nuget_sources']:
        bad.append(f'nuget.config feeds {sources}, expected {d["nuget_sources"]}')
    mapping = {key: re.findall(r'<package\s+pattern="([^"]+)"', block) for key, block in
               re.findall(r'<packageSource\s+key="([^"]+)">(.*?)</packageSource>', config, re.S)}
    if mapping != d['nuget_mapping']:
        bad.append(f'nuget.config maps {mapping}, expected {d["nuget_mapping"]}')
    if config.count('<clear />') != 2:
        bad.append('nuget.config must clear both the inherited feeds and the inherited mapping')
    sdk = strict_json(text('global.json')).get('sdk', {})
    if not re.fullmatch(r'\d+\.\d+\.\d+', str(sdk.get('version'))) or sdk.get('rollForward') not in d['roll_forward']:
        bad.append(f'global.json pins {sdk}: one exact SDK, rolling forward at most a patch')
    props = text('Directory.Build.props')
    bad += [f'Directory.Build.props does not turn off {switch}' for switch in d['msbuild_switches']
            if f'<{switch}>false</{switch}>' not in props]
    if bad:
        out.fail('a dependency or build input could change without a commit saying so', bad)
    else:
        out.ok(f'{len(refs)} packages at exact versions, {len(target)} locked with content hashes, each feed mapped to '
               f'its packages, one SDK, no build files from outside the repo')


@section('Capabilities list')
def capabilities_list(ctx, out):
    f = ctx.file(CAPABILITIES)
    if f is None:
        raise Unreadable(f'{CAPABILITIES} is missing')
    known = set(ctx.patterns['capability_tables'])
    headings = [line[3:].strip() for line in f.text.split('\n') if line.startswith('## ')]
    unknown = [h for h in headings if h not in known]
    missing = sorted(known - set(headings))
    reasonless = [f'{h}: {key}' for h, rows in ctx.capabilities().items() for key, cells in rows
                  if not cells[-1].strip() or len(cells) < 2]
    if unknown:
        out.fail(f'tables in {CAPABILITIES} that preflight does not check: a list nothing enforces would read as if it '
                 f'were', unknown)
    if missing:
        out.fail(f'tables preflight checks against that {CAPABILITIES} no longer has', missing)
    if reasonless:
        out.fail('rows without a reason', reasonless)
    if not (unknown or missing or reasonless):
        rows = sum(len(r) for r in ctx.capabilities().values())
        out.ok(f'{len(headings)} tables, {rows} rows, each checked against the code by its section and each with a '
               f'reason')


@section('Commit messages', modes=('history',))
def commit_messages(ctx, out):
    rules = {name: re.compile(rx) for name, rx in ctx.patterns['secrets'].items()}
    patterns = [p.lower() for p in ctx.patterns['personal_paths']] + [n.lower() for n in machine_names(ctx)]
    hits = []
    for sha, msg in ctx.messages:
        for name, rx in rules.items():
            if rx.search(msg):
                hits.append(f'{sha[:10]}: looks like a {name}')
        low = msg.lower()
        hits += [f'{sha[:10]}: a home path or machine name' for p in patterns if p in low]
        hits += [f'{sha[:10]}: {show(ch)}' for ch in msg
                 if unicodedata.category(ch) in ('Cf', 'Cc', 'Co', 'Cn', 'Zl', 'Zp') and ch not in '\t\r\n']
    if not ctx.messages:
        out.fail('no commit messages read: the range is wrong')
    elif hits:
        out.fail('commit messages that must not be published', hits)
    else:
        out.ok(f'{len(ctx.messages)} commit message(s): no credential, home path or hidden character')


# ---------------------------------------------------------------------------------------------------------------------
def load_patterns(files):
    f = files.get(PATTERNS)
    if f is None:
        raise Unreadable(f'{PATTERNS} is not in the tree')
    patterns = json.loads(f.text)
    expected = {'about', 'kinds', 'binaries', 'libraries', 'code_non_ascii', 'secrets', 'personal_paths', 'game_paths',
                'decompiler_markers', 'not_addresses', 'own_github_owners', 'history_reviewed', 'apworld_imports',
                'apworld_modules', 'apworld_builtins', 'apworld_dunders', 'apworld_denied_attributes',
                'apworld_denied_members', 'apworld_manifest_keys', 'mod_denied', 'mod_capabilities', 'ps1_denied',
                'ps1_capabilities', 'sh_denied', 'script_denied_modules', 'script_denied_builtins',
                'script_denied_methods', 'script_capability_modules', 'script_capability_calls', 'dll_assembly_refs',
                'dll_denied', 'dll_denied_names', 'dll_capabilities', 'dll_synthesized_members', 'dll_compiler_types',
                'workflows', 'dependencies', 'capability_tables', 'mod_server_text'}
    if set(patterns) != expected:
        raise Unreadable(f'{PATTERNS} keys differ from what preflight reads: {sorted(set(patterns) ^ expected)}')
    for rx in list(patterns['secrets'].values()) + patterns['game_paths'] + patterns['decompiler_markers'] + [
            rx for key in ('mod_denied', 'mod_capabilities', 'ps1_denied', 'ps1_capabilities', 'sh_denied')
            for rx in patterns[key].values()]:
        re.compile(rx)
    return patterns


def run(ctx, mode, only, quiet):
    failed = warned = 0
    started = time.perf_counter()
    for name, fn, modes in SECTIONS:
        if mode not in modes or (only and name not in only):
            continue
        out, t = Out(name), time.perf_counter()
        try:
            fn(ctx, out)
        except Unreadable as e:
            out.fail(f'could not read what this section checks: {e}')
        except Exception as e:  # a crashing check has checked nothing
            out.fail(f'the check itself failed: {type(e).__name__}: {e}')
        if not out.status:
            out.fail('the section reported nothing, so it proved nothing')
        failed += 'FAIL' in out.status
        warned += 'WARN' in out.status
        if not quiet or out.status & {'FAIL', 'WARN'}:
            print(f'== {name} ==')
            print('\n'.join(out.lines))
            if ctx.args.timing:
                print(f'          ({time.perf_counter() - t:.2f} s)')
    verdict = 'FAILED' if failed else 'passed'
    print(f'preflight {verdict}: {failed} section(s) failed, {warned} warned '
          f'({time.perf_counter() - started:.1f} s, {sys.executable})')
    return 1 if failed else 0


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(errors='backslashreplace')
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    mode = ap.add_mutually_exclusive_group()
    mode.add_argument('--rev', help='check this commit instead of the index')
    mode.add_argument('--history', nargs='*', metavar='RANGE', help='every blob and message ever reachable (default: all refs)')
    mode.add_argument('--text-stdin', metavar='LABEL', help='check text read from stdin (release notes, commit subjects)')
    ap.add_argument('--repo', default=os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    ap.add_argument('--ci', action='store_true', default=os.environ.get('CI') == 'true',
                    help='skip what only a working clone can answer (on by itself in CI)')
    ap.add_argument('--release', action='store_true', help='a release: the committed DLL must be built from the '
                    'current sources, not an older commit')
    ap.add_argument('--dll', help='check this file as the shipped DLL (a fresh build, before it is staged)')
    ap.add_argument('--dll-commit', help='the commit the --dll file was built from')
    ap.add_argument('--only', action='append', help='run only this section (repeatable)')
    ap.add_argument('--quiet', action='store_true', help='print only sections that fail or warn')
    ap.add_argument('--timing', action='store_true')
    ap.add_argument('--list-sections', action='store_true')
    args = ap.parse_args(argv)
    if args.list_sections:
        for name, fn, modes in SECTIONS:
            print(f'{",".join(sorted(modes))}\t{name}')
        return 0
    repo = Repo(args.repo)
    try:
        files, odd = read_tree(repo, None if args.history is not None or args.text_stdin else args.rev)
        if args.dll:
            with open(args.dll, 'rb') as f:
                files[OUR_DLL] = File(OUR_DLL, '100644', '', f.read())
        patterns = load_patterns(files)
        caps = capability_tables(files[CAPABILITIES].text) if CAPABILITIES in files else None
        if args.text_stdin:
            text = File(f'<{args.text_stdin}>', '100644', '', sys.stdin.buffer.read())
            return run(Context(repo, [text], [], patterns, caps, args), 'text', args.only, args.quiet)
        if args.history is not None:
            blobs, messages = read_history(repo, args.history or ['--all'])
            ctx = Context(repo, blobs, [], patterns, caps, args, messages, history=True)
            return run(ctx, 'history', args.only, args.quiet)
        ctx = Context(repo, list(files.values()), odd, patterns, caps, args)
        return run(ctx, 'tree', args.only, args.quiet)
    except (Unreadable, json.JSONDecodeError, re.error, KeyError) as e:
        print(f'preflight FAILED: could not read the tree: {type(e).__name__}: {e}')
        return 1


if __name__ == '__main__':
    sys.exit(main())
