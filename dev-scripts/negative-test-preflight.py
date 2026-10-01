"""Proves every preflight section can fail: a gate never seen failing can't be told from one that can't fail.

Each fixture plants one kind of violation in a throwaway clone (outside this repo, its origin removed, so nothing can
reach this repo or a remote), runs preflight there and checks that the section named for it reports FAIL and that
preflight exits non-zero. The clone holds what the next commit contains (HEAD plus everything staged), the way
preflight itself reads the index; --rev tests a commit instead (what pre-push does).
Then the hooks themselves: a real commit and a real push carrying a violation must both be refused, and the coding
agent's guard (.claude/) must refuse, ask and let through what it should. Coverage must be total: every section, in
every mode it runs in, needs a fixture. Standard library only.

    python dev-scripts/negative-test-preflight.py [--rev REV] [--only REGEX] [--keep] [--quiet]
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

sys.dont_write_bytecode = True

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# A stand-in machine name: real ones differ per machine, and a CI runner's ("runner") is a word in ci.yml.
MACHINE = 'preflight' + 'harness' + 'host'
DLL = 'release/mod/BepInEx/plugins/BugFablesAP/BugFablesAP.dll'
LIBRARY = 'release/mod/BepInEx/plugins/BugFablesAP/Newtonsoft.Json.dll'
NOTICES = 'release/mod/BepInEx/plugins/BugFablesAP/THIRD-PARTY-NOTICES.txt'


def env():
    e = {k: v for k, v in os.environ.items() if not k.startswith('GIT_') and k != 'CI'}
    e.update(USERNAME=MACHINE, USER=MACHINE, COMPUTERNAME=MACHINE, HOSTNAME=MACHINE, PYTHONDONTWRITEBYTECODE='1')
    return e


class Clone:
    def __init__(self, root):
        self.root = root

    def git(self, *args, stdin=None, check=True):
        r = subprocess.run(['git', '-C', self.root, *args], input=stdin, capture_output=True, env=env())
        if check and r.returncode != 0:
            raise RuntimeError(f"git {' '.join(args)}: {r.stderr.decode(errors='replace').strip()}")
        return r

    def path(self, rel):
        return os.path.join(self.root, *rel.split('/'))

    def write(self, rel, data):
        p = self.path(rel)
        os.makedirs(os.path.dirname(p), exist_ok=True)
        with open(p, 'wb') as f:
            f.write(data.encode('utf-8') if isinstance(data, str) else data)
        with open(p, 'rb') as f:
            if f.read() != (data.encode('utf-8') if isinstance(data, str) else data):
                raise RuntimeError(f'the plant did not land in {rel}')

    def read(self, rel):
        with open(self.path(rel), 'rb') as f:
            return f.read()

    def replace(self, rel, old, new):
        data = self.read(rel)
        if old not in data:
            raise RuntimeError(f'nothing in {rel} to replace: the plant would change nothing')
        self.write(rel, data.replace(old, new))

    def append(self, rel, text):
        self.write(rel, self.read(rel) + ('\n' + text + '\n').encode('utf-8'))

    def index_entry(self, mode, path, content=b'x\n'):
        sha = self.git('hash-object', '-w', '--stdin', stdin=content).stdout.decode().strip()
        self.git('update-index', '--add', '--cacheinfo', f'{mode},{sha},{path}')

    def commit(self, message, hooks=False):
        self.git('add', '-A')
        opts = [] if hooks else ['-c', 'core.hooksPath=.git/hooks']
        return self.git(*opts, 'commit', '-q', '--allow-empty', '-m', message, check=hooks is False)

    def preflight(self, *args, stdin=None):
        r = subprocess.run([sys.executable, '-B', self.path('dev-scripts/preflight.py'), '--repo', self.root, *args],
                           input=stdin, capture_output=True, env=env(), cwd=self.root)
        return r.returncode, parse(r.stdout.decode('utf-8', 'replace'))


def parse(output):
    """Section name -> (statuses, the section's text)."""
    report, name = {}, None
    for line in output.splitlines():
        m = re.match(r'^== (.+) ==$', line)
        if m:
            name = m.group(1)
            report[name] = (set(), [])
            continue
        if name:
            s = re.match(r'^  (PASS|FAIL|WARN|SKIP)\s', line)
            if s:
                report[name][0].add(s.group(1))
            report[name][1].append(line)
    return report


# ---------------------------------------------------------------------------------------------------------------------
# Samples are assembled at run time, so this file never holds a credential, a home path or a public address itself.
def secret_samples():
    return {
        'GitHub token': 'gh' + 'p_' + 'a1B2' * 9,
        'GitHub fine-grained token': 'github' + '_pat_' + 'A1b2_' * 11,
        'AWS access key': 'AK' + 'IA' + 'ABCDEFGHIJKLMNOP',
        'private key': '-----BEGIN ' + 'RSA PRIVATE' + ' KEY-----',
        'Discord bot token': 'M' + 'a' * 23 + '.' + 'b' * 6 + '.' + 'c' * 27,
        'Discord webhook': 'discord' + '.com/api/webhooks/' + '1234567' + '/' + 'abcDEF_x9',
        'Slack token': 'xo' + 'xb-' + '1234567890abc',
        'Google API key': 'AI' + 'za' + 'B' * 35,
        'NuGet API key': 'oy' + '2' + 'a' * 43,
        'PyPI token': 'py' + 'pi-' + 'AgEIcHlwaS5vcmc' + 'A' * 50,
    }


def personal_samples(patterns):
    return [p + 'someone' for p in patterns['personal_paths']]


# One line of code for each kind of call the patterns file denies; a kind without a sample fails the test.
MOD_SAMPLES = {
    'runs programs': 'System.Diagnostics.Process.Start("x");',
    'web requests': 'var w = new WebClient();',
    'raw sockets': 'var t = new TcpClient();',
    'loads code': 'Assembly.LoadFrom("x.dll");',
    'native code': '[DllImport("x.dll")] static extern int Planted(int a);',
    'the registry': 'Microsoft.Win32.Registry.GetValue("a", "b", null);',
    'hidden payloads': 'Convert.FromBase64String("QQ==");',
    'opens a web page': 'Application.OpenURL("x");',
    'who or where the player is': 'var u = Environment.UserName;',
    'data that picks a type': 'var s = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };',
    'code written at run time': 'var d = new DynamicMethod("x", null, null);',
    'screen capture': 'ScreenCapture.CaptureScreenshot("x.png");',
}
PS1_SAMPLES = {
    'runs text as code': 'Invoke-Expression $x',
    'hidden payloads': 'powershell -EncodedCommand AAAA',
    'downloads': 'Invoke-WebRequest $u',
    'compiles code': 'Add-Type -TypeDefinition $src',
    'changes system settings': 'Set-ExecutionPolicy Bypass',
    'starts detached programs': 'Start-Process notepad',
}
SH_SAMPLES = {
    'downloads': 'curl -s "$u" -o x',
    'runs text as code': 'eval "$x"',
    'hidden payloads': 'printf x | base64 -d',
}
SAMPLED = {'secrets': secret_samples, 'mod_denied': lambda: MOD_SAMPLES, 'ps1_denied': lambda: PS1_SAMPLES,
           'sh_denied': lambda: SH_SAMPLES}


def patterns_of(clone):
    import json
    return json.loads(clone.read('dev-scripts/preflight-patterns.json'))


class Fixture:
    def __init__(self, name, section, mode, plant, expect='FAIL', names=(), stage=True, args=()):
        self.name, self.section, self.mode, self.plant, self.expect = name, section, mode, plant, expect
        # stage=False: the plant writes the index itself, which 'git add -A' would undo.
        self.names, self.stage, self.args = names, stage, args


def patch_bytes(c, rel, old, new):
    """Same-length change inside a binary, so everything after it stays where the metadata says it is."""
    if len(old) != len(new):
        raise RuntimeError('a byte patch must keep the length')
    data = c.read(rel)
    if old not in data:
        raise RuntimeError(f'nothing in {rel} to patch: the plant would change nothing')
    c.write(rel, data.replace(old, new, 1))


def make_fresh(c):
    """Puts back the build inputs of the commit built-from.txt names, so the committed DLL is current: between releases
    the real tree is legitimately newer, which would hide what the staleness fixtures plant."""
    commit = re.search(r'^commit: (\w+)', c.read('release/built-from.txt').decode(), re.M).group(1)
    inputs = ['mod/BugFablesAP', 'global.json', 'nuget.config', 'Directory.Build.props']
    then = set(c.git('ls-tree', '-r', '--name-only', commit, '--', *inputs).stdout.decode().split('\n')) - {''}
    now = set(c.git('ls-files', '--', *inputs).stdout.decode().split('\n')) - {''}
    for gone in sorted(now - then):
        c.git('rm', '-q', '--', gone)
    c.git('checkout', commit, '--', *inputs)


def metadata_of(c):
    sys.path.insert(0, c.path('dev-scripts'))
    import dotnet_metadata
    return dotnet_metadata.Assembly(c.read(DLL))


def fixtures():
    F = []

    def add(name, section, mode='tree', expect='FAIL', names=(), stage=True, args=()):
        def register(plant):
            F.append(Fixture(name, section, mode, plant, expect, names, stage, args))
            return plant
        return register

    @add('hooks path unset', 'Hooks armed')
    def _(c):
        c.git('config', '--unset', 'core.hooksPath')

    @add('hook not executable', 'Hooks armed', stage=False)
    def _(c):
        c.git('update-index', '--chmod=-x', '.githooks/pre-commit')

    @add('a symlink', 'Index sanity', stage=False)
    def _(c):
        c.index_entry('120000', 'docs/link.md', b'capabilities.md')

    @add('a submodule', 'Index sanity', stage=False)
    def _(c):
        head = c.git('rev-parse', 'HEAD').stdout.decode().strip()
        c.git('update-index', '--add', '--cacheinfo', f'160000,{head},vendor/thing')

    @add('paths differing only in case', 'Index sanity', stage=False)
    def _(c):
        c.index_entry('100644', 'readme.md', b'# again\n')

    @add('a reserved Windows name', 'Index sanity', stage=False)
    def _(c):
        # Git on Windows refuses the name outright; a Linux clone holds it, and Windows can't check it out.
        c.git('config', 'core.protectNTFS', 'false')
        c.index_entry('100644', 'agent_docs/con.md', b'x\n')

    @add('a file of an unknown kind', 'Known kinds only')
    def _(c):
        c.write('notes.txt', 'a stray note\n')

    @add('a .gitattributes filter', 'Known kinds only')
    def _(c):
        c.append('.gitattributes', '*.cs filter=lfs')

    @add('a binary file under a text name', 'Binaries')
    def _(c):
        c.write('mod/BugFablesAP/Blob.cs', b'\x00\x01payload\x00')

    @add('a library not NuGet\'s', 'Binaries')
    def _(c):
        data = bytearray(c.read(LIBRARY))
        data[-1] ^= 0xFF
        c.write(LIBRARY, bytes(data))

    @add('a release DLL that is not .NET', 'Binaries')
    def _(c):
        c.write(DLL, b'MZ' + b'\x00' * 200)

    @add('a bidi override in a doc', 'Hidden characters')
    def _(c):
        c.append('docs/capabilities.md', 'reads one way ' + chr(0x202E) + ' runs another')

    @add('a zero-width space in code', 'Hidden characters')
    def _(c):
        c.append('mod/BugFablesAP/Core/Plugin.cs', '// hidden' + chr(0x200B) + 'here')

    @add('a homoglyph in code', 'Hidden characters')
    def _(c):
        c.append('mod/BugFablesAP/Core/Plugin.cs', '// ' + chr(0x0430) + 'pple')

    @add('a base64 blob', 'Encoded blobs and long lines')
    def _(c):
        c.append('agent_docs/log.md', 'QUJD' * 60)

    @add('a hex blob', 'Encoded blobs and long lines')
    def _(c):
        c.append('agent_docs/log.md', 'deadbeef' * 20)

    @add('a very long code line', 'Encoded blobs and long lines')
    def _(c):
        c.append('mod/BugFablesAP/Core/Plugin.cs', '// ' + 'x ' * 600)

    @add('every credential format, in a doc', 'Secrets', names=tuple(f'looks like a {n}' for n in secret_samples()))
    def _(c):
        c.append('agent_docs/log.md', '\n'.join(secret_samples().values()))

    @add('a credential inside the mod DLL', 'Secrets', names=('looks like a GitHub token',))
    def _(c):
        c.write(DLL, c.read(DLL) + secret_samples()['GitHub token'].encode('utf-16-le'))

    @add('every home-path pattern, and a machine name', 'Personal paths and names')
    def _(c):
        c.append('agent_docs/log.md', '\n'.join(personal_samples(patterns_of(c)) + ['built on ' + MACHINE]))

    @add('a home path inside the mod DLL', 'Personal paths and names')
    def _(c):
        c.write(DLL, c.read(DLL) + (personal_samples(patterns_of(c))[0] + '\\obj\\x.pdb').encode('latin-1'))

    @add('a game assembly', 'Game files')
    def _(c):
        c.write('mod/BugFablesAP/Assembly-CSharp.cs', '// nothing\n')

    @add('decompiler output', 'Game files')
    def _(c):
        c.write('mod/BugFablesAP/Copied.cs', '// Decompiled' + ' with SomeTool\nclass Copied { }\n')

    @add('a URL to an unlisted host', 'Hosts and addresses')
    def _(c):
        c.append('agent_docs/log.md', 'see https' + '://unlisted.example.org/page')

    @add('a host quoted in code', 'Hosts and addresses')
    def _(c):
        c.append('mod/BugFablesAP/Core/Plugin.cs', '// var server = "relay' + '.example.com";')

    @add('a public IP address', 'Hosts and addresses')
    def _(c):
        c.append('agent_docs/log.md', 'the server at 8.8.' + '4.4')

    @add('a private IP address', 'Hosts and addresses', expect='WARN')
    def _(c):
        c.append('agent_docs/log.md', 'my LAN box at 192.168.' + '44.7')

    @add('a listed host nothing names', 'Hosts and addresses')
    def _(c):
        c.replace('docs/capabilities.md', b'| `github.com` |', b'| `unused.example.net` | nothing uses it |\n| `github.com` |')

    @add('CLAUDE.md one line past its cap', 'Line caps', names=('CLAUDE.md',))
    def _(c):
        path, cap, why = patterns_of(c)['line_caps'][0]
        data = c.read(path)
        c.write(path, data + b'- one rule too many\n' * (cap - data.count(b'\n') + 1))

    # Spans of time are assembled at run time, so this file never holds one itself.
    @add('a vague span of time in a doc', 'Durations', names=('docs/reviewing.md',))
    def _(c):
        c.append('docs/reviewing.md', 'This took ' + 'week' + 's of work.')

    @add('a vague span of time in a commit message', 'Durations', mode='history', names=('weeks' + ' ago',))
    def _(c):
        c.git('-c', 'core.hooksPath=.git/hooks', 'commit', '-q', '--allow-empty', '-m', 'broke ' + 'weeks' + ' ago')

    @add('a project cited without a licence row', 'Licences')
    def _(c):
        c.append('agent_docs/references.md', 'see github' + '.com/someone-else/some-project')

    @add('a shipped library without its notice', 'Licences')
    def _(c):
        c.replace(NOTICES, b'websocket-sharp', b'a library')

    @add('imports the apworld may not make', 'Apworld imports',
         names=('import socket', 'from BaseClasses import Utils', 'import json as j', 'from os import path'))
    def _(c):
        c.write('apworld/bug_fables/planted.py',
                'import socket\nfrom BaseClasses import Utils\nimport json as j\nfrom os import path\n')

    @add('everything the apworld must never do', 'Apworld runs nothing unexpected',
         names=('the builtin open', '.__class__', '.write_text', 'json.dump', 'generate_output (', 'Planted.settings_key',
                'the annotation of a', 'return annotation', 'at import time', '"__globals__"', 'getattr(..., "system")',
                'While at the top level'))
    def _(c):
        c.write('apworld/bug_fables/planted.py', '\n'.join([
            'import json',
            'len([])',
            'while False:',
            '    pass',
            'class Planted:',
            '    settings_key = "x"',
            '    def generate_output(self, output_directory):',
            '        pass',
            'def f(a: (lambda: int)()) -> json.loads("1"):',
            '    open("x")',
            '    ().__class__',
            '    a.write_text("y")',
            '    json.dump({}, a)',
            '    getattr(a, "__globals__")',
            '    getattr(a, "system")',
            '']))

    @add('a lookup by a computed name, not listed', 'Apworld runs nothing unexpected', names=('(in planted_lookup)',))
    def _(c):
        c.write('apworld/bug_fables/planted.py', 'def planted_lookup(a, name):\n    return getattr(a, name)\n')

    @add('a listed lookup that no code has', 'Apworld runs nothing unexpected',
         names=('apworld/bug_fables/rules.py nowhere',))
    def _(c):
        c.replace('docs/capabilities.md', b'| `apworld/bug_fables/data_types.py present` |',
                  b'| `apworld/bug_fables/rules.py nowhere` | nothing |\n| `apworld/bug_fables/data_types.py present` |')

    @add('apworld data that is not plain data', 'Apworld data and docs',
         names=('a key given twice', 'NaN is not JSON', '"__class__"', 'archipelago.json: keys', 'raw HTML',
                'a script or data link'))
    def _(c):
        c.write('apworld/bug_fables/data/planted.json', '{"a": 1, "a": 2}\n')
        c.write('apworld/bug_fables/data/planted2.json', '{"x": NaN}\n')
        c.write('apworld/bug_fables/data/planted3.json', '{"x": "__class__"}\n')
        c.replace('apworld/bug_fables/archipelago.json', b'"game"', b'"extra": 1, "game"')
        c.append('apworld/bug_fables/docs/setup_en.md', 'see <img src=x> and [this](java' + 'script:alert)')

    @add('every denied kind of call in the mod', 'Mod source',
         names=tuple(f'({k})' for k in MOD_SAMPLES) + ('a \\u escape outside a string',))
    def _(c):
        body = '\n'.join('        ' + line for line in MOD_SAMPLES.values())
        c.write('mod/BugFablesAP/Planted.cs', 'class Planted\n{\n    void Run()\n    {\n' + body + '\n    }\n}\n'
                'class Planted\\u0041 { }\n')

    @add('a server name read raw', 'Mod source', names=('.Player.Name read raw', 'RoomState.Seed read raw'))
    def _(c):
        c.write('mod/BugFablesAP/Planted.cs', 'class Planted { string A(dynamic i, dynamic s) => i.Player.Name '
                '+ s.RoomState.Seed; }\n')

    @add('a denied call only in a comment', 'Mod source', expect='PASS')
    def _(c):
        c.write('mod/BugFablesAP/Planted.cs', '// ' + MOD_SAMPLES['runs programs'] + '\n/* '
                + MOD_SAMPLES['web requests'] + ' */\nclass Planted { }\n')

    @add('a mod file doing something unlisted', 'Mod source', names=('Planted.cs: writes files',))
    def _(c):
        c.write('mod/BugFablesAP/Planted.cs', 'class Planted { void Run() { System.IO.File.WriteAllText("x", "y"); } }\n')

    @add('a mod row no code matches', 'Mod source', names=('Nowhere.cs: writes files',))
    def _(c):
        c.replace('docs/capabilities.md', b'| `mod/BugFablesAP/Core/ApConnection.cs` |',
                  b'| `mod/BugFablesAP/Nowhere.cs` | writes files | nothing |\n| `mod/BugFablesAP/Core/ApConnection.cs` |')

    @add('every denied kind of call in a script', 'Dev scripts and hooks',
         names=tuple(f'({k})' for k in PS1_SAMPLES) + tuple(f'({k})' for k in SH_SAMPLES)
         + ('imports pickle', 'eval()', '.system()', 'shell=True'))
    def _(c):
        c.write('dev-scripts/planted.ps1', '\n'.join(PS1_SAMPLES.values()) + '\n')
        c.write('.githooks/planted.sh', '\n'.join(SH_SAMPLES.values()) + '\n')
        c.write('dev-scripts/planted.py', 'import os\nimport pickle\nimport subprocess\n'
                'eval("1")\nos.system("x")\nsubprocess.run("x", shell=True)\n')

    @add('agent settings that do more than guard', 'Dev scripts and hooks',
         names=('sets env', 'permissions.allow', 'a SessionStart hook', 'a command that is not the listed one'))
    def _(c):
        settings = json.loads(c.read('.claude/settings.json'))
        settings['env'] = {'PLANTED': '1'}
        settings['permissions']['allow'] = ['Bash']
        settings['hooks']['SessionStart'] = [{'hooks': [{'type': 'command', 'command': 'echo planted'}]}]
        settings['hooks']['PreToolUse'][0]['hooks'].append({'type': 'command', 'command': 'echo planted'})
        c.write('.claude/settings.json', json.dumps(settings, indent=2) + '\n')

    @add('a script doing something unlisted', 'Dev scripts and hooks', names=('planted.py: runs programs',))
    def _(c):
        c.write('dev-scripts/planted.py', 'import subprocess\n')

    @add('a script row no code matches', 'Dev scripts and hooks', names=('nowhere.py: runs programs',))
    def _(c):
        c.replace('docs/capabilities.md', b'| `dev-scripts/preflight.py` |',
                  b'| `dev-scripts/nowhere.py` | runs programs | nothing |\n| `dev-scripts/preflight.py` |')

    @add('a hook doing what it has no row for', 'Dev scripts and hooks',
         names=('planted.sh: runs programs', 'planted.sh: talks to GitHub', 'planted.sh: writes files'))
    def _(c):
        c.write('.githooks/planted.sh', 'git status\ngh repo view\nprintf x > planted.txt\n')

    @add('a DLL that is not its record', 'Release staging', names=('the DLL changed without its record',))
    def _(c):
        c.write(DLL, c.read(DLL) + b'\0' * 16)

    @add('a stray file in the download', 'Release staging', names=('release/mod holds',))
    def _(c):
        c.write('release/mod/extra.txt', 'x\n')

    @add('the DLL\'s own sources are not stale', 'Release staging', expect='PASS', args=('--release',))
    def _(c):
        make_fresh(c)

    @add('sources changed since the DLL was built', 'Release staging', expect='WARN')
    def _(c):
        make_fresh(c)
        c.append('mod/BugFablesAP/Core/Plugin.cs', '// changed')

    @add('a release with a stale DLL', 'Release staging', args=('--release',),
         names=('the committed DLL is older than its sources',))
    def _(c):
        make_fresh(c)
        c.append('mod/BugFablesAP/Core/Plugin.cs', '// changed')

    @add('data after the last section', 'Shipped DLL structure', names=('bytes after the last section',))
    def _(c):
        c.write(DLL, c.read(DLL) + b'\0' * 512)

    @add('a native entry point flag', 'Shipped DLL structure', names=('not IL-only, or a native entry point',))
    def _(c):
        a = metadata_of(c)
        at = a.offset(a.directories[14][0]) + 16
        data = bytearray(c.read(DLL))
        data[at] |= 0x10
        c.write(DLL, bytes(data))

    @add('a namespace pointed at System.Net', 'Shipped DLL reach', names=('System.Net.',))
    def _(c):
        net = ('System' + '.Net').encode()  # in two pieces: whole, it reads as a host name to preflight itself
        old = b'\0System.Collections.Generic\0'
        patch_bytes(c, DLL, old, b'\0' + net + b'\0' + b'x' * (len(old) - len(net) - 3) + b'\0')

    @add('a Harmony patch aimed outside the game', 'Shipped DLL reach',
         names=('patched, not listed: Assembly-CSharq',))
    def _(c):
        patch_bytes(c, DLL, b'Assembly-CSharp, Version=0.0.0.0', b'Assembly-CSharq, Version=0.0.0.0')

    @add('a listed patch neither the DLL nor the source makes', 'Shipped DLL reach',
         names=('listed, not patched: Nowhere:Nowhere.Thing::Nothing',))
    def _(c):
        c.replace('docs/capabilities.md', b'| `UnityEngine.AnimationModule:',
                  b'| `Nowhere:Nowhere.Thing::Nothing` | nothing |\n| `UnityEngine.AnimationModule:')

    @add('a listed patch on a current DLL that lacks it', 'Shipped DLL reach',
         names=('listed, not patched: Nowhere:Nowhere.Thing::Nothing',))
    def _(c):
        make_fresh(c)
        c.replace('docs/capabilities.md', b'| `UnityEngine.AnimationModule:',
                  b'| `Nowhere:Nowhere.Thing::Nothing` | nothing |\n| `UnityEngine.AnimationModule:')

    @add('a listed patch only the source makes yet', 'Shipped DLL reach', expect='WARN',
         names=('Nowhere:Nowhere.Thing::Nothing',))
    def _(c):
        c.replace('docs/capabilities.md', b'| `UnityEngine.AnimationModule:',
                  b'| `Nowhere:Nowhere.Thing::Nothing` | nothing |\n| `UnityEngine.AnimationModule:')
        c.write('mod/BugFablesAP/Planted.cs', '// Thing\n[HarmonyPatch("Nowhere.Thing, Nowhere", "Nothing")]\n')

    @add('an unlisted host in a DLL string', 'Shipped DLL reach', names=('unlisted host evil.example.org',))
    def _(c):
        old = '[saves] redirect installed; Archipelago mod '
        new = ('see https' + '://evil.example.org/x').ljust(len(old))
        patch_bytes(c, DLL, old.encode('utf-16-le'), new.encode('utf-16-le'))

    @add('a DLL string no source holds', 'The DLL says only what its source says', names=('in no source file',))
    def _(c):
        old = '[saves] redirect installed; Archipelago mod '
        patch_bytes(c, DLL, old.encode('utf-16-le'), 'a line nobody wrote, planted in the DLL.'.ljust(len(old))
                    .encode('utf-16-le'))

    @add('a DLL type no source declares', 'The DLL says only what its source says',
         names=('SaveRedirecx: declared in no source file',))
    def _(c):
        patch_bytes(c, DLL, b'\0SaveRedirect\0', b'\0SaveRedirecx\0')

    @add('everything a workflow must never do', 'Workflows',
         names=('is not pinned to a full commit hash', 'must be exactly {}', 'triggered by pull_request_target',
                'runs on self-hosted', 'the secret DEPLOY_KEY', 'an expression inside a script', 'continue-on-error',
                'a YAML anchor or alias', 'job build asks for contents: write'))
    def _(c):
        c.write('.github/workflows/planted.yml', '\n'.join([
            'name: Planted',
            'on:',
            '  pull_request_target:',
            'permissions: write-all',
            'jobs:',
            '  build:',
            '    runs-on: self-hosted',
            '    permissions:',
            '      contents: write',
            '    steps:',
            '      - uses: actions/checkout@v7',
            '        with: &opts',
            '          token: ${{ secrets.DEPLOY_KEY }}',
            '      - run: |',
            '          echo "${{ github.event.pull_request.title }}"',
            '        continue-on-error: true',
            '']))

    @add('a release that no longer waits for its gates', 'Workflows', names=('publish does not wait for preflight',))
    def _(c):
        c.replace('.github/workflows/release.yml', b'needs: [guard, ci, mod-dll, preflight]', b'needs: [guard, ci, mod-dll]')

    @add('a workflow reaching what its rows do not name', 'Workflows',
         names=('actions/cache (uses actions), which its row does not name', 'preflight.yml: talks to GitHub'))
    def _(c):
        c.replace('.github/workflows/preflight.yml', b'      - name: Every section, on this commit',
                  b'      - uses: actions/cache@' + b'0' * 40 + b' # v4.0.0\n\n      - run: gh release list\n\n'
                  b'      - name: Every section, on this commit')

    @add('workflow rows nothing matches', 'Workflows',
         names=('nowhere.yml: runs programs', 'names someone/gone-action, which it no longer uses'))
    def _(c):
        c.replace('docs/capabilities.md', b'| `.github/workflows/ci.yml` | uses actions | ',
                  b'| `.github/workflows/nowhere.yml` | runs programs | nothing |\n'
                  b'| `.github/workflows/ci.yml` | uses actions | `someone/gone-action`, ')

    @add('dependencies that could drift', 'Dependencies pinned',
         names=('not one exact version', 'does not set RestoreLockedMode', 'nuget.config feeds',
                'global.json pins', 'does not turn off ImportDirectoryBuildTargets'))
    def _(c):
        c.replace('mod/BugFablesAP/BugFablesAP.csproj', b'Version="5.4.21"', b'Version="5.4.*"')
        c.replace('mod/BugFablesAP/BugFablesAP.csproj', b'<RestoreLockedMode>true', b'<RestoreLockedMode>false')
        c.replace('nuget.config', b'<clear />',
                  b'<clear />\n    <add key="extra" value="https' + b'://nuget.example.org/v3/index.json" />')
        c.replace('global.json', b'"latestPatch"', b'"latestMajor"')
        c.replace('Directory.Build.props', b'<ImportDirectoryBuildTargets>false', b'<ImportDirectoryBuildTargets>true')

    @add('a lock file that disagrees with the project', 'Dependencies pinned', names=('the lock file resolves',))
    def _(c):
        c.replace('mod/BugFablesAP/packages.lock.json', b'"resolved": "5.4.21"', b'"resolved": "5.4.22"')

    @add('a capabilities table nothing checks', 'Capabilities list', names=('Mod: extra powers',))
    def _(c):
        c.append('docs/capabilities.md', '## Mod: extra powers\n\n| File | Does | Why |\n|---|---|---|\n'
                 '| `mod/BugFablesAP/Core/Plugin.cs` | anything | trust me |')

    @add('a capabilities row without a reason', 'Capabilities list', names=('rows without a reason',))
    def _(c):
        c.replace('docs/capabilities.md', b'| `github.com` |', b'| `nothing.example` |  |\n| `github.com` |')

    # History: a violation committed and removed again is gone from the tree, not from what was published.
    def committed_then_removed(rel, data):
        def plant(c):
            c.write(rel, data)
            c.commit('add')
            os.remove(c.path(rel))
            c.commit('remove')
        return plant

    F.append(Fixture('a binary, committed then removed', 'Binaries', 'history',
                     committed_then_removed('mod/BugFablesAP/Gone.cs', b'\x00binary\x00')))
    F.append(Fixture('a bidi override, committed then removed', 'Hidden characters', 'history',
                     committed_then_removed('agent_docs/gone.md', 'x' + chr(0x202E) + 'y\n')))
    F.append(Fixture('a base64 blob, committed then removed', 'Encoded blobs and long lines', 'history',
                     committed_then_removed('agent_docs/gone.md', 'QUJD' * 60 + '\n')))
    F.append(Fixture('a credential, committed then removed', 'Secrets', 'history',
                     committed_then_removed('agent_docs/gone.md', secret_samples()['AWS access key'] + '\n')))
    F.append(Fixture('a home path, committed then removed', 'Personal paths and names', 'history',
                     lambda c: committed_then_removed('agent_docs/gone.md',
                                                      personal_samples(patterns_of(c))[1] + '\n')(c)))
    F.append(Fixture('a game file, committed then removed', 'Game files', 'history',
                     committed_then_removed('mod/BugFablesAP/assembly-csharp.cs', '// x\n')))

    @add('a credential in a commit message', 'Commit messages', mode='history')
    def _(c):
        c.git('-c', 'core.hooksPath=.git/hooks', 'commit', '-q', '--allow-empty', '-m',
              'oops ' + secret_samples()['Slack token'])

    # Text: what --text-stdin reads (release notes, commit subjects).
    F.append(Fixture('text: a bidi override', 'Hidden characters', 'text', 'notes ' + chr(0x202E) + ' here'))
    F.append(Fixture('text: a credential', 'Secrets', 'text', 'notes ' + secret_samples()['Google API key']))
    F.append(Fixture('text: a home path', 'Personal paths and names', 'text',
                     lambda c: 'built in ' + personal_samples(patterns_of(c))[0]))
    F.append(Fixture('text: an unlisted host', 'Hosts and addresses', 'text', 'see https' + '://unlisted.example.org'))
    F.append(Fixture('text: every kind of vague span of time', 'Durations', 'text',
                     '\n'.join(['stuck on it for ' + 'months', 'broke ' + 'weeks' + ' ago', 'fixed th' + 'is year',
                                'hours' + ' of reading', 'a long' + ' time', 'over the' + ' years']),
                     names=('for ' + 'months', 'weeks' + ' ago', 'th' + 'is year', 'hours' + ' of', 'long' + ' time',
                            'over the' + ' years')))
    F.append(Fixture('text: a figure with its number', 'Durations', 'text',
                     'a save 2-3 ' + 'years' + ' old, and three ' + 'days' + ' later', expect='PASS'))
    return F


# ---------------------------------------------------------------------------------------------------------------------
class Harness:
    def __init__(self, quiet):
        self.quiet, self.failures, self.lines = quiet, 0, []

    def say(self, status, msg, details=()):
        if status == 'FAIL':
            self.failures += 1
        if not self.quiet or status == 'FAIL':
            print(f'  {status:<4}  {msg}')
            for d in details:
                print(f'          {d}')


def git_repo(*args):
    r = subprocess.run(['git', '-C', REPO, *args], capture_output=True, env=env())
    if r.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)}: {r.stderr.decode(errors='replace').strip()}")
    return r.stdout


def setup(tmp, rev):
    root = os.path.join(tmp, 'clone')
    r = subprocess.run(['git', 'clone', '--quiet', '--no-local', '--no-hardlinks', REPO, root],
                       capture_output=True, env=env())
    if r.returncode != 0:
        raise RuntimeError('git clone failed: ' + r.stderr.decode(errors='replace'))
    c = Clone(root)
    c.git('checkout', '-q', '--detach', git_repo('rev-parse', rev or 'HEAD').decode().strip())
    # Nothing done here may reach the repo it came from.
    c.git('remote', 'remove', 'origin')
    c.git('config', 'core.hooksPath', '.githooks')
    c.git('config', 'user.name', 'harness')
    c.git('config', 'user.email', 'harness@example.invalid')
    c.git('config', 'core.autocrlf', 'false')
    if rev is None:
        # What is staged, exactly as the index holds it: content and mode.
        staged = git_repo('diff', '--cached', '--name-only', '-z', 'HEAD').decode().split('\0')
        index = {line.split('\t', 1)[1]: line.split()[0] for line in
                 git_repo('ls-files', '-s', '-z').decode().split('\0') if line}
        for rel in filter(None, staged):
            if rel in index:
                c.write(rel, git_repo('show', ':' + rel))
                c.git('add', '--', rel)
                c.git('update-index', '--chmod=' + ('+x' if index[rel] == '100755' else '-x'), '--', rel)
            elif os.path.exists(c.path(rel)):
                c.git('rm', '-q', '--', rel)
    c.git('-c', 'core.hooksPath=.git/hooks', 'commit', '-q', '--allow-empty', '-m', 'what is staged')
    return c


def run_fixture(c, fx, base):
    """Plant, run preflight in the fixture's mode, and return (exit code, report)."""
    c.git('reset', '-q', '--hard', base)
    c.git('clean', '-q', '-fdx')
    c.git('config', 'core.hooksPath', '.githooks')
    if fx.mode == 'text':
        text = fx.plant(c) if callable(fx.plant) else fx.plant
        return c.preflight('--only', fx.section, '--text-stdin', 'fixture', stdin=text.encode('utf-8'))
    fx.plant(c)
    if fx.mode == 'history':
        return c.preflight('--only', fx.section, '--history', f'{base}..HEAD')
    if fx.stage:
        c.git('add', '-A')
    # Only the section under test: the baselines run them all, and this keeps each fixture to a fraction of a second.
    return c.preflight('--only', fx.section, *fx.args)


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--rev', help='test this commit instead of HEAD plus what is staged')
    ap.add_argument('--only', help='run only fixtures whose name matches this regex (skips the coverage check)')
    ap.add_argument('--keep', action='store_true', help='leave the throwaway clone in place')
    ap.add_argument('--quiet', action='store_true', help='print only what fails')
    args = ap.parse_args()
    for stream in (sys.stdout, sys.stderr):
        stream.reconfigure(errors='backslashreplace')
    h = Harness(args.quiet)
    tmp = tempfile.mkdtemp(prefix='bugfablesap-negtest-')
    try:
        c = setup(tmp, args.rev)
        base = c.git('rev-parse', 'HEAD').stdout.decode().strip()
        all_fx = fixtures()
        chosen = [f for f in all_fx if not args.only or re.search(args.only, f.name)]
        what = args.rev or 'HEAD plus what is staged'
        print(f'negative test: {len(chosen)} fixture(s), in a clean clone of {what}')

        print('== Baseline ==')
        baselines = {}
        for mode, run in (('tree', lambda: c.preflight()),
                          ('history', lambda: c.preflight('--history', f'{base}~1..{base}')),
                          ('text', lambda: c.preflight('--text-stdin', 'baseline', stdin=b'plain release notes\n'))):
            code, report = run()
            # A committed DLL older than its sources is expected between releases: those two warnings are allowed.
            between_releases = {'Release staging': 'fine between releases', 'Shipped DLL reach': 'DLL predates'}
            bad = [n for n, (st, lines) in report.items() if 'FAIL' in st or ('WARN' in st and not (
                n in between_releases and any(between_releases[n] in line for line in lines)))]
            baselines[mode] = report
            if code != 0 or bad or not report:
                h.say('FAIL', f'{mode}: the clean clone does not pass, so fixtures prove nothing: {", ".join(bad)}',
                      [l for n in bad for l in report[n][1]][:12])
            else:
                h.say('PASS', f'{mode}: all {len(report)} sections pass on the clean clone')

        for fx in chosen:
            if not args.quiet:
                print(f'== {fx.name} ({fx.section}, {fx.mode}) ==')
            if fx.section not in baselines[fx.mode]:
                h.say('FAIL', f'{fx.name}: no section "{fx.section}" in {fx.mode} mode: the fixture aims at nothing')
                continue
            try:
                code, report = run_fixture(c, fx, base)
            except Exception as e:
                h.say('FAIL', f'{fx.name}: the plant itself failed, so the section was never tested: {e}')
                continue
            status, lines = report.get(fx.section, (set(), []))
            missing = [n for n in fx.names if not any(n in l for l in lines)]
            if fx.expect == 'PASS' and status != {'PASS'}:
                h.say('FAIL', f'{fx.name}: "{fx.section}" did not pass: it flags what it must let through', lines[:6])
            elif fx.expect not in status:
                h.say('FAIL', f'{fx.name}: "{fx.section}" did not report {fx.expect}: the gate is blind to it', lines[:6])
            elif missing:
                h.say('FAIL', f'{fx.name}: "{fx.section}" failed but did not name every planted case', missing)
            elif fx.expect == 'FAIL' and code == 0:
                h.say('FAIL', f'{fx.name}: "{fx.section}" reported FAIL but preflight exited 0: nothing would stop')
            else:
                h.say('PASS', f'{fx.name}: "{fx.section}" reported {fx.expect}'
                      + (', and preflight exited 1' if fx.expect == 'FAIL' else ''))

        if not args.only:
            print('== The hooks, for real ==')
            hooks_test(c, base, h)
            print('== The coding agent\'s guard ==')
            agent_guard_test(c, base, h)
            print('== Coverage ==')
            patterns = patterns_of(c)
            unsampled = [f'{key}: {kind}' for key, samples in SAMPLED.items()
                         for kind in sorted(set(patterns[key]) ^ set(samples()))]
            if unsampled:
                h.say('FAIL', 'kinds in the patterns file with no sample here, or samples of kinds it no longer has',
                      unsampled)
            else:
                h.say('PASS', 'every credential format and denied kind of call in the patterns file has a sample')
            listed = subprocess.run([sys.executable, '-B', c.path('dev-scripts/preflight.py'), '--list-sections'],
                                    capture_output=True, env=env()).stdout.decode().splitlines()
            pairs = {(name, mode) for line in listed for modes, name in [line.split('\t')] for mode in modes.split(',')}
            covered = {(f.section, f.mode) for f in all_fx}
            gaps = sorted(pairs - covered)
            if len(pairs) < 20:
                h.say('FAIL', f'only {len(pairs)} (section, mode) pairs listed: the listing is wrong')
            elif gaps:
                h.say('FAIL', 'sections with no fixture in a mode they run in: their ability to fail is assumed',
                      [f'{n} ({m})' for n, m in gaps])
            else:
                h.say('PASS', f'every section has a fixture in every mode it runs in ({len(pairs)} pairs, '
                      f'{len(all_fx)} fixtures)')
    finally:
        if args.keep:
            print(f'clone kept at {tmp}')
        else:
            shutil.rmtree(tmp, onerror=lambda fn, p, e: (os.chmod(p, 0o700), fn(p)))
    verdict = 'FAILED' if h.failures else 'passed'
    print(f'negative test {verdict}: {h.failures} problem(s)')
    return 1 if h.failures else 0


def hooks_test(c, base, h):
    """A commit carrying a violation, made the normal way, must be refused; one made past the hooks must not push."""
    c.git('reset', '-q', '--hard', base)
    c.git('clean', '-q', '-fdx')
    c.git('config', 'core.hooksPath', '.githooks')
    c.append('agent_docs/log.md', secret_samples()['AWS access key'])
    c.git('add', '-A')
    r = c.git('commit', '-q', '-m', 'a harmless-looking change', check=False)
    after = c.git('rev-parse', 'HEAD').stdout.decode().strip()
    if r.returncode != 0 and after == base:
        h.say('PASS', 'pre-commit refused a real commit carrying a credential; HEAD unchanged')
    else:
        h.say('FAIL', 'pre-commit let a commit with a credential through', [r.stderr.decode(errors='replace')[:300]])

    c.git('reset', '-q', '--hard', base)
    c.write('mod/BugFablesAP/Core/Plugin.cs', c.read('mod/BugFablesAP/Core/Plugin.cs') + b'// x\n')
    c.write('dev-scripts/preflight-patterns.json', c.read('dev-scripts/preflight-patterns.json') + b'\n')
    c.git('add', '-A')
    # commit-msg alone, so the test can't pass or fail on what pre-commit decides.
    only = os.path.join(os.path.dirname(c.root), 'commit-msg-only')
    os.makedirs(only, exist_ok=True)
    shutil.copy(c.path('.githooks/commit-msg'), os.path.join(only, 'commit-msg'))
    os.chmod(os.path.join(only, 'commit-msg'), 0o755)
    r = c.git('-c', f'core.hooksPath={only}', 'commit', '-q', '-m', 'mixed', check=False)
    if (r.returncode != 0 and b'preflight gate' in r.stderr
            and c.git('rev-parse', 'HEAD').stdout.decode().strip() == base):
        h.say('PASS', 'commit-msg refused a change to the gate mixed with mod code')
    else:
        h.say('FAIL', 'commit-msg let a gate change ride along with mod code, or refused it for another reason',
              [r.stderr.decode(errors='replace')[:300]])

    c.git('reset', '-q', '--hard', base)
    if not os.path.exists(c.path('.githooks/pre-push')):
        h.say('FAIL', 'no pre-push hook to test')
        return
    remote = os.path.join(os.path.dirname(c.root), 'remote.git')
    subprocess.run(['git', 'init', '-q', '--bare', remote], capture_output=True, env=env())
    c.git('remote', 'add', 'harness', remote)
    # The remote's starting point is pushed past the hooks: checking all history here is the history test's job.
    c.git('-c', 'core.hooksPath=.git/hooks', 'push', '-q', 'harness', f'{base}:refs/heads/main')
    c.append('agent_docs/log.md', personal_samples(patterns_of(c))[0])
    c.commit('made past the hooks')
    r = c.git('push', '-q', 'harness', 'HEAD:refs/heads/main', check=False)
    theirs = subprocess.run(['git', '-C', remote, 'rev-parse', 'main'], capture_output=True).stdout.decode().strip()
    if r.returncode != 0 and theirs == base:
        h.say('PASS', 'pre-push refused a commit made past the hooks; the remote is unchanged')
    else:
        h.say('FAIL', 'pre-push let a commit with a home path reach the remote', [r.stderr.decode(errors='replace')[:300]])


# Spelled apart, as the fixtures above are: these are the guard's test input, not places the project points to.
GH, WEB = 'github' + '.com', 'https' + '://'
# (tool, command, repo path or URL, what the guard must answer), in a clean clone. Commit messages may name anything.
GUARD_CASES = [
    ('Bash', 'git status', None),
    ('Bash', 'git commit --no-verify -m x', 'deny'),
    ('Bash', 'git commit --no-veri -m x', 'deny'),
    ('Bash', 'git push --no-verify', 'deny'),
    ('Bash', 'git commit -n -m x', 'deny'),
    ('Bash', 'git commit -anm x', 'deny'),
    ('Bash', 'git -C . commit -qn -m x', 'deny'),
    ('Bash', 'git -c core.hooksPath=/dev/null commit -m x', 'deny'),
    ('Bash', 'git config core.hooksPath /dev/null', 'deny'),
    ('Bash', 'git config --unset core.hooksPath', 'deny'),
    ('Bash', 'GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.hooksPath GIT_CONFIG_VALUE_0=x git commit -m y', 'deny'),
    ('Bash', 'git commit-tree HEAD^{tree} -m x', 'deny'),
    ('Bash', 'git update-ref refs/heads/main HEAD~1', 'deny'),
    ('Bash', 'bash -c "git commit --no-verify -m x"', 'deny'),
    ('PowerShell', 'git commit --no-ver`ify -m x', 'deny'),
    ('PowerShell', 'powershell -Command "git commit -n -m x"', 'deny'),
    ('PowerShell', '$env:GIT_CONFIG_COUNT = 1', 'deny'),
    ('Bash', 'git config core.hooksPath .githooks', None),
    ('Bash', 'git config --get core.hooksPath', None),
    ('Bash', 'git commit -m "refuses --no-verify and commit -n"', None),
    ('Bash', "git commit -q -F - <<'EOF'\nrefuses --no-verify, commit -n, commit-tree\nEOF", None),
    ('PowerShell', "git commit -m @'\nrefuses --no-verify\n'@", None),
    ('Bash', 'git log --oneline -n 5', None),
    ('Bash', 'git stash push -m wip', None),
    ('Bash', 'git push origin main', None),
    ('PowerShell', 'git status; if ($?) { git push }', None),
    ('Bash', 'cat .git/config', 'ask'),
    ('Bash', 'gh api repos/BepInEx/HarmonyX/contents/x -X PUT -f message=m', 'ask'),
    ('Bash', 'gh api -X GET repos/BepInEx/HarmonyX', None),
    ('Bash', 'gh api repos/BepInEx/HarmonyX/contents/README.md', None),
    ('Bash', 'gh api repos/someone/unlisted/contents/src/main.py', 'ask'),
    ('Bash', 'gh api repos/someone/unlisted --jq .description', 'ask'),
    ('Bash', 'gh api repos/someone/unlisted/license --jq .content', 'ask'),
    ('Bash', "gh api 'repos/someone/unlisted/contents/worlds/x/LICENSE?ref=dev'", 'ask'),
    ('Bash', 'gh repo view someone/unlisted', 'ask'),
    ('Bash', 'gh release list -R someone/unlisted', 'ask'),
    ('Bash', 'gh api "search/issues?q=repo:someone/unlisted+crash"', 'ask'),
    ('Bash', f'curl -s {WEB}raw.githubusercontent.com/someone/unlisted/main/README.md', 'ask'),
    ('Bash', f'curl -s {WEB}raw.githubusercontent.com/someone/unlisted/main/LICENSE', 'ask'),
    ('Bash', f'curl -s "{WEB}api.{GH}/search/issues?q=repo:someone/unlisted"', 'ask'),
    ('Bash', f'curl -s {WEB}docs.{GH}/en/actions', None),
    ('PowerShell', f'Invoke-WebRequest {WEB}{GH}/someone/unlisted/archive/main.zip -OutFile x.zip', 'ask'),
    ('Bash', f'git clone {WEB}{GH}/someone/unlisted.git', 'ask'),
    ('Bash', f'git commit -m "read {GH}/someone/unlisted later"', None),
    ('Bash', 'git commit -n -m "gh api repos/someone/unlisted"', 'deny'),
    ('WebFetch', f'{WEB}{GH}/someone/unlisted', 'ask'),
    ('WebFetch', f'{WEB}{GH}/someone/unlisted/blob/main/LICENSE', 'ask'),
    ('WebFetch', f'{WEB}docs.unity3d.com/ScriptReference/Time-timeScale.html', None),
    ('Edit', 'docs/capabilities.md', 'ask'),
    ('Write', 'dev-scripts/preflight-patterns.json', 'ask'),
    ('Edit', '.claude/settings.local.json', 'ask'),
    ('Edit', '.git/config', 'ask'),
    ('Edit', 'docs/reviewing.md', None),
    ('Read', 'docs/capabilities.md', None),
]


def git_sh():
    """The sh Claude Code would run the hook with: on Windows Git's own, never whatever bash PATH finds first."""
    if os.name != 'nt':
        return '/bin/sh'
    top = subprocess.run(['git', '--exec-path'], capture_output=True).stdout.decode().strip()
    for _ in range(4):
        top = os.path.dirname(top)
        for sh in ('bin/sh.exe', 'usr/bin/sh.exe'):
            if os.path.isfile(os.path.join(top, sh)):
                return os.path.join(top, sh)
    return None


def agent_guard_test(c, base, h):
    """The guard refuses what gets past the hooks, asks before any read of a GitHub project with no committed licence
    row (its licence included), a commit adding one, and the gate changes, lets the rest through, and refuses
    everything when it can't run."""
    c.git('reset', '-q', '--hard', base)
    c.git('clean', '-q', '-fdx')
    guard_env = dict(env(), CLAUDE_PROJECT_DIR=c.root)

    def decide(tool, given):
        tool_input = ({'command': given} if tool in ('Bash', 'PowerShell') else {'url': given} if tool == 'WebFetch'
                      else {'file_path': c.path(given)})
        r = subprocess.run([sys.executable, '-B', c.path('.claude/hooks/agent-guard.py')], capture_output=True,
                           input=json.dumps({'tool_name': tool, 'tool_input': tool_input}).encode(), env=guard_env)
        if r.returncode != 0:
            return f'exit {r.returncode}: {r.stderr.decode(errors="replace")[-200:]}'
        return json.loads(r.stdout)['hookSpecificOutput']['permissionDecision'] if r.stdout.strip() else None

    wrong = [f'{tool} {given!r}: {got}, not {want}' for tool, given, want in GUARD_CASES
             for got in [decide(tool, given)] if got != want]
    # A commit asks only when it may carry what the user decides, whoever wrote it; other gate work asks nothing.
    # A licence row permits reading its project only once the user lets the commit adding it through.
    row = f'| [Unlisted]({WEB}{GH}/someone/unlisted) | MIT | today | nothing |\n'
    for path, text, want in (('docs/capabilities.md', '\n', 'ask'), ('dev-scripts/preflight.py', '\n', None),
                             ('agent_docs/licensing.md', '\n', None), ('agent_docs/licensing.md', row, 'ask')):
        c.append(path, text)
        got = decide('Bash', 'git commit -q -m "a harmless-looking change"')
        if got != want:
            wrong.append(f'a commit while {path} gains {text.strip() or "a blank line"}: {got}, not {want}')
        if text == row and decide('Bash', 'gh api repos/someone/unlisted/license') != 'ask':
            wrong.append('an uncommitted licence row permits reading its project')
        c.git('checkout', '-q', '--', path)
    if wrong:
        h.say('FAIL', 'the guard answers wrongly', wrong)
    else:
        h.say('PASS', f'the guard refuses, asks and lets through as it should ({len(GUARD_CASES) + 5} cases)')

    command = json.loads(c.read('.claude/settings.json'))['hooks']['PreToolUse'][0]['hooks'][0]['command']
    sh = git_sh()
    if not sh:
        h.say('FAIL', 'no sh found to run the settings\' hook command with')
        return
    outcomes = []
    for payload, want in ((b'{"tool_name": "Bash", "tool_input": {"command": "git commit -n -m x"}}', 0),
                          (b'not an event', 2)):
        r = subprocess.run([sh, '-c', command], input=payload, capture_output=True, env=guard_env,
                           cwd=os.path.dirname(c.root))
        outcomes.append((r.returncode, want, b'"deny"' in r.stdout))
    if outcomes[0] == (0, 0, True) and outcomes[1][0] == 2:
        h.say('PASS', 'the settings\' hook command runs the guard from anywhere, and refuses (exit 2) when it breaks')
    else:
        h.say('FAIL', 'the settings\' hook command does not run the guard, or lets a broken guard through',
              [f'exit {got}, wanted {want}, denied: {denied}' for got, want, denied in outcomes])


if __name__ == '__main__':
    sys.exit(main())
