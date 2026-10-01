"""Checks a published release against this repository: what players download is exactly what a commit holds.

Download the release's files first (gh release download vX.Y.Z, or from its page); this script reads only files and
git, never the network. Standard library only.

    python dev-scripts/verify-release.py --ref vX.Y.Z --zip bugfables-archipelago.zip --apworld bug_fables.apworld
        [--yaml bug_fables.yaml] [--nupkg archipelago.multiclient.net.6.7.1.nupkg]
    python dev-scripts/verify-release.py --ref HEAD --nupkg archipelago.multiclient.net.6.7.1.nupkg

- The mod zip must hold exactly the files of release/mod at the ref, byte for byte.
- The apworld must hold exactly apworld/bug_fables at the ref, byte for byte, but for the licence CI copies in
  (bug_fables/LICENSE, the repo's LICENSE) and the two version fields Archipelago's builder adds to archipelago.json.
- The three library DLLs (in the zip, or at the ref) must be the NuGet package's own files.
- When the ref has a preflight, its DLL sections run on the zip's DLL too.
"""
import argparse
import hashlib
import io
import json
import os
import subprocess
import sys
import zipfile

sys.dont_write_bytecode = True

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLUGIN = 'BepInEx/plugins/BugFablesAP/'
LIBRARIES = {'Archipelago.MultiClient.Net.dll': 'lib/net40/Archipelago.MultiClient.Net.dll',
             'websocket-sharp.dll': 'lib/net40/websocket-sharp.dll',
             'Newtonsoft.Json.dll': 'lib/netstandard2.0/Newtonsoft.Json.dll'}
# What "Build APWorlds" adds to the manifest at the Archipelago version this world targets (worlds/Files.py).
BUILDER_FIELDS = {'compatible_version': 7, 'version': 7}


def git(*args):
    r = subprocess.run(['git', '-C', REPO, *args], capture_output=True)
    if r.returncode != 0:
        raise SystemExit(f'git {" ".join(args)}: {r.stderr.decode(errors="replace").strip()}')
    return r.stdout


def files_at(ref, prefix):
    """{path under prefix: bytes} of every file git holds there at ref."""
    names = [n for n in git('ls-tree', '-r', '-z', '--name-only', '--full-tree', ref).decode().split('\0') if n]
    return {n[len(prefix):]: git('show', f'{ref}:{n}') for n in names if n.startswith(prefix)}


def zip_files(path):
    with zipfile.ZipFile(path) as z:
        out = {}
        for info in z.infolist():
            name = info.filename
            if name.startswith('/') or '..' in name.split('/') or '\\' in name:
                raise SystemExit(f'{path}: the entry {name!r} would land outside its folder')
            if not info.is_dir():
                out[name] = z.read(info)
        return out


class Report:
    def __init__(self):
        self.failed = 0

    def section(self, name, problems, passed):
        print(f'== {name} ==')
        if problems:
            self.failed += 1
            print(f'  FAIL  {len(problems)} problem(s)')
            for p in problems:
                print(f'          {p}')
        else:
            print(f'  PASS  {passed}')


def compare(expected, actual, source='the repository'):
    problems = [f'missing: {n}' for n in sorted(set(expected) - set(actual))]
    problems += [f'not in {source}: {n}' for n in sorted(set(actual) - set(expected))]
    problems += [f'differs: {n} (sha256 {hashlib.sha256(actual[n]).hexdigest()[:16]}..., {source}\'s '
                 f'{hashlib.sha256(expected[n]).hexdigest()[:16]}...)'
                 for n in sorted(set(expected) & set(actual)) if expected[n] != actual[n]]
    return problems


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--ref', required=True, help='the tag or commit the release was made from')
    ap.add_argument('--zip')
    ap.add_argument('--apworld')
    ap.add_argument('--yaml')
    ap.add_argument('--nupkg', help='archipelago.multiclient.net.6.7.1.nupkg, from nuget.org')
    args = ap.parse_args()
    ref = git('rev-parse', '--verify', args.ref + '^{commit}').decode().strip()
    print(f'verify-release: {args.ref} ({ref[:10]})')
    report = Report()
    mod_files = None
    if args.zip:
        mod_files = zip_files(args.zip)
        expected = files_at(ref, 'release/mod/')
        report.section('The mod zip is release/mod', compare(expected, mod_files),
                       f'all {len(expected)} files byte-for-byte those of release/mod at {args.ref}')
    if args.apworld:
        built = zip_files(args.apworld)
        source = {f'bug_fables/{n}': b for n, b in files_at(ref, 'apworld/bug_fables/').items()}
        problems = []
        manifest = source.pop('bug_fables/archipelago.json', None)
        shipped_manifest = built.pop('bug_fables/archipelago.json', None)
        if manifest is None or shipped_manifest is None:
            problems.append('archipelago.json is missing from the repository or the apworld')
        else:
            want = dict(json.loads(manifest), **BUILDER_FIELDS)
            if json.loads(shipped_manifest) != want:
                problems.append(f'archipelago.json is {json.loads(shipped_manifest)}, expected {want}')
        source['bug_fables/LICENSE'] = git('show', f'{ref}:LICENSE')
        problems += compare(source, built)
        report.section('The apworld is apworld/bug_fables', problems,
                       f'all {len(source)} files byte-for-byte those at {args.ref}; the manifest only gains '
                       f'{", ".join(BUILDER_FIELDS)}; bug_fables/LICENSE is the repository\'s LICENSE')
    if args.yaml:
        with open(args.yaml, 'rb') as f:
            text = f.read()
        problems = [] if b'game: Bug Fables' in text else ['it does not name the game']
        # Archipelago writes it from the options' texts (checked in the apworld's source) and its own boilerplate, which
        # links sites of its own: so the hosts rule stays out, the rest applies.
        r = subprocess.run([sys.executable, '-B', os.path.join(REPO, 'dev-scripts', 'preflight.py'), '--text-stdin',
                            'the yaml', '--quiet', '--only', 'Hidden characters', '--only', 'Secrets', '--only',
                            'Personal paths and names'], input=text, capture_output=True)
        if r.returncode != 0:
            problems.append('preflight\'s text rules refused it:\n' + r.stdout.decode(errors='replace'))
        report.section('The yaml template', problems, 'generated by Archipelago (not in the repository): it names the '
                       'game, and holds no credential, home path or hidden character')
    if args.nupkg:
        with zipfile.ZipFile(args.nupkg) as z:
            package = {name: z.read(inside) for name, inside in LIBRARIES.items()}
        ours = ({name: mod_files[PLUGIN + name] for name in LIBRARIES if PLUGIN + name in mod_files} if mod_files
                else {name: b for name, b in files_at(ref, 'release/mod/' + PLUGIN).items() if name in LIBRARIES})
        report.section('The libraries are NuGet\'s', compare(package, ours, 'the NuGet package'),
                       f'all {len(LIBRARIES)} are byte-for-byte the files in {os.path.basename(args.nupkg)}')
    if mod_files and PLUGIN + 'BugFablesAP.dll' in mod_files:
        has_preflight = b'dev-scripts/preflight.py' in git('ls-tree', '-r', '--name-only', ref)
        if has_preflight:
            path = os.path.join(os.path.dirname(os.path.abspath(args.zip)), 'BugFablesAP.from-zip.dll')
            with open(path, 'wb') as f:
                f.write(mod_files[PLUGIN + 'BugFablesAP.dll'])
            r = subprocess.run([sys.executable, '-B', os.path.join(REPO, 'dev-scripts', 'preflight.py'), '--rev', ref,
                                '--dll', path, '--only', 'Shipped DLL structure', '--only', 'Shipped DLL reach',
                                '--only', 'The DLL says only what its source says'], capture_output=True)
            os.remove(path)
            output = r.stdout.decode(errors='replace')
            report.section('The zip\'s DLL passes preflight', [output] if r.returncode else [],
                           f'structure, reach and "says only what its source says", as of {args.ref}')
        else:
            print('== The zip\'s DLL passes preflight ==\n  SKIP  this release predates the preflight; its DLL is '
                  'compared byte-for-byte with the committed one above')
    if not (args.zip or args.apworld or args.yaml or args.nupkg):
        raise SystemExit('nothing to check: give --zip, --apworld, --yaml or --nupkg')
    print(f'verify-release {"FAILED" if report.failed else "passed"}: {report.failed} section(s) failed')
    return 1 if report.failed else 0


if __name__ == '__main__':
    sys.exit(main())
