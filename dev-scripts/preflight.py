"""Refuses what this repository must never hold: anything that couldn't be published, or that could harm whoever runs it.

Reads what git holds, never the working copy: the index by default (what the next commit contains), one commit with
--rev, every commit with --history, or free text with --text-stdin. Standard library only, so a reviewer needs
nothing but Python and git. docs/reviewing.md explains every section; dev-scripts/preflight-patterns.json holds what
each one refuses. Exit 0 when every section passes, 1 on any FAIL (a WARN never fails).

    python dev-scripts/preflight.py [--rev REV | --history [RANGE] | --text-stdin LABEL] [--ci] [--quiet]
"""
import argparse
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
    if scanned < (1 if ctx.args.text_stdin else 100):
        out.fail(f'only {scanned} text file(s) read: the listing is wrong')
    elif hits:
        out.fail('characters that hide or disguise what the text says', hits)
    else:
        out.ok(f'{scanned} text file(s): no invisible, bidi or control characters; code non-ASCII only '
               f'{" ".join(sorted(allowed_code))}')


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
            elif code and len(line) > 1000:
                hits.append(f'{f.path}:{n}: a code line of {len(line)} characters')
    if hits:
        out.fail('text that could carry a hidden payload', hits)
    else:
        out.ok('no base64 run of 200+ characters, no hex run of 128+, no code line over 1000 characters')


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
    hits = []
    for f in ctx.files:
        if f.path == PATTERNS or f.path in ctx.patterns['libraries']:
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
                'decompiler_markers', 'not_addresses', 'own_github_owners'}
    if set(patterns) != expected:
        raise Unreadable(f'{PATTERNS} keys differ from what preflight reads: {sorted(set(patterns) ^ expected)}')
    for rx in list(patterns['secrets'].values()) + patterns['game_paths'] + patterns['decompiler_markers']:
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
