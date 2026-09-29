"""Reads a .NET assembly the way the runtime does (ECMA-335): its PE layout, metadata tables, heaps and IL.

Standard library only; used by preflight.py to check the mod's committed DLL without the game or a .NET SDK.
Anything it can't read exactly raises MetadataError: a reader that guesses would let a check pass on nothing.

    python dev-scripts/dotnet_metadata.py --selftest <dll>...   parses each and prints what it found
"""
import struct
import sys

sys.dont_write_bytecode = True


class MetadataError(Exception):
    pass


# Table number -> (name, columns). Column kinds: 1/2/4 fixed width, 'S' string, 'G' guid, 'B' blob,
# ('T', n) an index into table n, ('C', name) a coded index.
TABLES = {
    0x00: ('Module', [2, 'S', 'G', 'G', 'G']),
    0x01: ('TypeRef', [('C', 'ResolutionScope'), 'S', 'S']),
    0x02: ('TypeDef', [4, 'S', 'S', ('C', 'TypeDefOrRef'), ('T', 0x04), ('T', 0x06)]),
    0x03: ('FieldPtr', [('T', 0x04)]),
    0x04: ('Field', [2, 'S', 'B']),
    0x05: ('MethodPtr', [('T', 0x06)]),
    0x06: ('MethodDef', [4, 2, 2, 'S', 'B', ('T', 0x08)]),
    0x07: ('ParamPtr', [('T', 0x08)]),
    0x08: ('Param', [2, 2, 'S']),
    0x09: ('InterfaceImpl', [('T', 0x02), ('C', 'TypeDefOrRef')]),
    0x0A: ('MemberRef', [('C', 'MemberRefParent'), 'S', 'B']),
    0x0B: ('Constant', [1, 1, ('C', 'HasConstant'), 'B']),
    0x0C: ('CustomAttribute', [('C', 'HasCustomAttribute'), ('C', 'CustomAttributeType'), 'B']),
    0x0D: ('FieldMarshal', [('C', 'HasFieldMarshal'), 'B']),
    0x0E: ('DeclSecurity', [2, ('C', 'HasDeclSecurity'), 'B']),
    0x0F: ('ClassLayout', [2, 4, ('T', 0x02)]),
    0x10: ('FieldLayout', [4, ('T', 0x04)]),
    0x11: ('StandAloneSig', ['B']),
    0x12: ('EventMap', [('T', 0x02), ('T', 0x14)]),
    0x13: ('EventPtr', [('T', 0x14)]),
    0x14: ('Event', [2, 'S', ('C', 'TypeDefOrRef')]),
    0x15: ('PropertyMap', [('T', 0x02), ('T', 0x17)]),
    0x16: ('PropertyPtr', [('T', 0x17)]),
    0x17: ('Property', [2, 'S', 'B']),
    0x18: ('MethodSemantics', [2, ('T', 0x06), ('C', 'HasSemantics')]),
    0x19: ('MethodImpl', [('T', 0x02), ('C', 'MethodDefOrRef'), ('C', 'MethodDefOrRef')]),
    0x1A: ('ModuleRef', ['S']),
    0x1B: ('TypeSpec', ['B']),
    0x1C: ('ImplMap', [2, ('C', 'MemberForwarded'), 'S', ('T', 0x1A)]),
    0x1D: ('FieldRVA', [4, ('T', 0x04)]),
    0x1E: ('EncLog', [4, 4]),
    0x1F: ('EncMap', [4]),
    0x20: ('Assembly', [4, 2, 2, 2, 2, 4, 'B', 'S', 'S']),
    0x21: ('AssemblyProcessor', [4]),
    0x22: ('AssemblyOS', [4, 4, 4]),
    0x23: ('AssemblyRef', [2, 2, 2, 2, 4, 'B', 'S', 'S', 'B']),
    0x24: ('AssemblyRefProcessor', [4, ('T', 0x23)]),
    0x25: ('AssemblyRefOS', [4, 4, 4, ('T', 0x23)]),
    0x26: ('File', [4, 'S', 'B']),
    0x27: ('ExportedType', [4, 4, 'S', 'S', ('C', 'Implementation')]),
    0x28: ('ManifestResource', [4, 4, 'S', ('C', 'Implementation')]),
    0x29: ('NestedClass', [('T', 0x02), ('T', 0x02)]),
    0x2A: ('GenericParam', [2, 2, ('C', 'TypeOrMethodDef'), 'S']),
    0x2B: ('MethodSpec', [('C', 'MethodDefOrRef'), 'B']),
    0x2C: ('GenericParamConstraint', [('T', 0x2A), ('C', 'TypeDefOrRef')]),
}
# Coded index -> (tag bits, the tables its tags stand for; None is an unused tag).
CODED = {
    'TypeDefOrRef': (2, [0x02, 0x01, 0x1B]),
    'HasConstant': (2, [0x04, 0x08, 0x17]),
    'HasCustomAttribute': (5, [0x06, 0x04, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x00, 0x0E, 0x17, 0x14, 0x11, 0x1A, 0x1B,
                               0x20, 0x23, 0x26, 0x27, 0x28, 0x2A, 0x2C, 0x2B]),
    'HasFieldMarshal': (1, [0x04, 0x08]),
    'HasDeclSecurity': (2, [0x02, 0x06, 0x20]),
    'MemberRefParent': (3, [0x02, 0x01, 0x1A, 0x06, 0x1B]),
    'HasSemantics': (1, [0x14, 0x17]),
    'MethodDefOrRef': (1, [0x06, 0x0A]),
    'MemberForwarded': (1, [0x04, 0x06]),
    'Implementation': (2, [0x26, 0x23, 0x27]),
    'CustomAttributeType': (3, [None, None, 0x06, 0x0A, None]),
    'ResolutionScope': (2, [0x00, 0x1A, 0x23, 0x01]),
    'TypeOrMethodDef': (1, [0x02, 0x06]),
}
TABLE_NUMBER = {name: n for n, (name, cols) in TABLES.items()}

# IL: opcode -> operand size (-1: the switch table). Every opcode ECMA-335 defines; any other byte is an error.
ONE_BYTE = {}
for op in list(range(0x00, 0x0E)) + [0x14] + list(range(0x15, 0x1F)) + [0x25, 0x26, 0x2A] + list(range(0x46, 0x6F)) \
        + [0x76, 0x7A, 0x8E] + list(range(0x82, 0x8C)) + list(range(0x90, 0xA3)) + list(range(0xB3, 0xBB)) \
        + [0xC3] + list(range(0xD1, 0xDD)) + [0xDF, 0xE0]:
    ONE_BYTE[op] = 0
for op in list(range(0x0E, 0x14)) + [0x1F] + list(range(0x2B, 0x38)) + [0xDE]:
    ONE_BYTE[op] = 1
for op in [0x20, 0x22, 0x27, 0x28, 0x29] + list(range(0x38, 0x45)) + list(range(0x6F, 0x76)) + [0x79] \
        + list(range(0x7B, 0x82)) + [0x8C, 0x8D, 0x8F, 0xA3, 0xA4, 0xA5, 0xC2, 0xC6, 0xD0, 0xDD]:
    ONE_BYTE[op] = 4
ONE_BYTE[0x21] = ONE_BYTE[0x23] = 8
ONE_BYTE[0x45] = -1
TWO_BYTE = {0x00: 0, 0x01: 0, 0x02: 0, 0x03: 0, 0x04: 0, 0x05: 0, 0x06: 4, 0x07: 4, 0x09: 2, 0x0A: 2, 0x0B: 2,
            0x0C: 2, 0x0D: 2, 0x0E: 2, 0x0F: 0, 0x11: 0, 0x12: 1, 0x13: 0, 0x14: 0, 0x15: 4, 0x16: 4, 0x17: 0,
            0x18: 0, 0x19: 1, 0x1A: 0, 0x1C: 4, 0x1D: 0, 0x1E: 0}
# Opcodes whose operand is a metadata token, by what they do with it.
CALLS = {0x28: 'call', 0x6F: 'callvirt', 0x73: 'newobj', 0x27: 'jmp', 0xFE06: 'ldftn', 0xFE07: 'ldvirtftn'}
FIELDS = {0x7B: 'ldfld', 0x7C: 'ldflda', 0x7D: 'stfld', 0x7E: 'ldsfld', 0x7F: 'ldsflda', 0x80: 'stsfld'}
LDSTR, LDTOKEN = 0x72, 0xD0


def compressed(data, pos):
    """ECMA-335 II.23.2 compressed unsigned integer: (value, next position)."""
    b = data[pos]
    if b & 0x80 == 0:
        return b, pos + 1
    if b & 0xC0 == 0x80:
        return ((b & 0x3F) << 8) | data[pos + 1], pos + 2
    if b & 0xE0 == 0xC0:
        return ((b & 0x1F) << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3], pos + 4
    raise MetadataError(f'bad compressed integer at {pos}')


class Assembly:
    def __init__(self, data):
        self.data = data
        try:
            self._pe()
            self._metadata()
            self._tables()
        except (struct.error, IndexError) as e:
            raise MetadataError(f'truncated or malformed: {e}') from e

    # --- PE ---------------------------------------------------------------------------------------------------------
    def _pe(self):
        d = self.data
        if d[:2] != b'MZ':
            raise MetadataError('no MZ header')
        pe = struct.unpack_from('<I', d, 0x3C)[0]
        if d[pe:pe + 4] != b'PE\0\0':
            raise MetadataError('no PE signature')
        self.machine, nsections = struct.unpack_from('<HH', d, pe + 4)
        opt_size = struct.unpack_from('<H', d, pe + 20)[0]
        opt = pe + 24
        self.magic = struct.unpack_from('<H', d, opt)[0]
        if self.magic == 0x10B:
            dirs, count_at = opt + 96, opt + 92
        elif self.magic == 0x20B:
            dirs, count_at = opt + 112, opt + 108
        else:
            raise MetadataError(f'unknown optional header magic {self.magic:#x}')
        self.entry_rva = struct.unpack_from('<I', d, opt + 16)[0]
        ndirs = struct.unpack_from('<I', d, count_at)[0]
        self.directories = [struct.unpack_from('<II', d, dirs + 8 * i) for i in range(min(ndirs, 16))]
        self.sections = []
        at = opt + opt_size
        for i in range(nsections):
            name = d[at:at + 8].rstrip(b'\0').decode('ascii', 'replace')
            vsize, va, rawsize, rawptr = struct.unpack_from('<IIII', d, at + 8)
            self.sections.append((name, va, vsize, rawptr, rawsize))
            at += 40
        if len(self.directories) <= 14 or self.directories[14][0] == 0:
            raise MetadataError('no CLI header: not a .NET assembly')
        cli = self.offset(self.directories[14][0])
        (cb, self.runtime_major, self.runtime_minor, md_rva, md_size, self.cli_flags, self.entry_token) = \
            struct.unpack_from('<IHHIIII', d, cli)
        self.cli_dirs = {name: struct.unpack_from('<II', d, cli + 24 + 8 * i) for i, name in enumerate(
            ['Resources', 'StrongNameSignature', 'CodeManagerTable', 'VTableFixups', 'ExportAddressTableJumps',
             'ManagedNativeHeader'])}
        self.metadata_at, self.metadata_size = self.offset(md_rva), md_size

    def offset(self, rva):
        for name, va, vsize, rawptr, rawsize in self.sections:
            if va <= rva < va + max(vsize, rawsize):
                return rva - va + rawptr
        raise MetadataError(f'RVA {rva:#x} is in no section')

    def imports(self):
        """[(dll, [function])] from the native import table."""
        rva, size = self.directories[1]
        if rva == 0:
            return []
        out, at = [], self.offset(rva)
        while True:
            lookup, _, _, name_rva, iat = struct.unpack_from('<IIIII', self.data, at)
            if name_rva == 0:
                return out
            dll = self._cstr(self.offset(name_rva))
            names, thunk = [], self.offset(lookup or iat)
            width = 8 if self.magic == 0x20B else 4
            while True:
                entry = int.from_bytes(self.data[thunk:thunk + width], 'little')
                if entry == 0:
                    break
                if entry >> (width * 8 - 1):
                    names.append(f'#{entry & 0xFFFF}')
                else:
                    names.append(self._cstr(self.offset(entry & 0x7FFFFFFF) + 2))
                thunk += width
            out.append((dll, names))
            at += 20

    def _cstr(self, at):
        end = self.data.index(b'\0', at)
        return self.data[at:end].decode('utf-8', 'replace')

    # --- metadata root and heaps -------------------------------------------------------------------------------------
    def _metadata(self):
        d, at = self.data, self.metadata_at
        if struct.unpack_from('<I', d, at)[0] != 0x424A5342:
            raise MetadataError('no BSJB metadata signature')
        vlen = struct.unpack_from('<I', d, at + 12)[0]
        self.version = d[at + 16:at + 16 + vlen].rstrip(b'\0').decode('ascii', 'replace')
        pos = at + 16 + vlen
        nstreams = struct.unpack_from('<H', d, pos + 2)[0]
        pos += 4
        self.streams = []
        for _ in range(nstreams):
            off, size = struct.unpack_from('<II', d, pos)
            end = d.index(b'\0', pos + 8)
            name = d[pos + 8:end].decode('ascii', 'replace')
            self.streams.append((name, at + off, size))
            pos = pos + 8 + ((end - (pos + 8)) // 4 + 1) * 4
        found = {name: (start, size) for name, start, size in self.streams}
        self.heap = {name: d[start:start + size] for name, (start, size) in found.items()}

    def string(self, index):
        heap = self.heap['#Strings']
        end = heap.index(b'\0', index)
        return heap[index:end].decode('utf-8')

    def blob(self, index):
        heap = self.heap['#Blob']
        size, at = compressed(heap, index)
        return heap[at:at + size]

    def user_string(self, index):
        heap = self.heap['#US']
        size, at = compressed(heap, index)
        return heap[at:at + size - 1].decode('utf-16-le') if size else ''

    def user_strings(self):
        """Every entry of #US in order, and whether the walk ended exactly at the heap's end."""
        heap, at, out = self.heap['#US'], 1, []
        while at < len(heap):
            size, start = compressed(heap, at)
            if size == 0:
                at = start
                continue
            out.append(heap[start:start + size - 1].decode('utf-16-le', 'replace'))
            at = start + size
        return out

    # --- tables ------------------------------------------------------------------------------------------------------
    def _tables(self):
        heap = self.heap.get('#~')
        if heap is None:
            raise MetadataError('no #~ table stream (an uncompressed #- stream is refused)')
        heap_sizes = heap[6]
        if heap_sizes & ~0x07:
            raise MetadataError(f'HeapSizes flags {heap_sizes:#x}: extra data is refused')
        valid = struct.unpack_from('<Q', heap, 8)[0]
        if valid >> 0x2D:
            raise MetadataError(f'tables past 0x2C present: {valid >> 0x2D:#x}')
        self.rows, pos = {}, 24
        for n in range(64):
            if valid >> n & 1:
                self.rows[n] = struct.unpack_from('<I', heap, pos)[0]
                pos += 4
        for n in TABLES:
            self.rows.setdefault(n, 0)
        width = {'S': 4 if heap_sizes & 1 else 2, 'G': 4 if heap_sizes & 2 else 2, 'B': 4 if heap_sizes & 4 else 2}

        def col_size(kind):
            if isinstance(kind, int):
                return kind
            if isinstance(kind, str):
                return width[kind]
            if kind[0] == 'T':
                return 2 if self.rows[kind[1]] < 65536 else 4
            bits, tables = CODED[kind[1]]
            biggest = max(self.rows[t] if t is not None else 0 for t in tables)
            return 2 if biggest < (1 << (16 - bits)) else 4

        self.table = {}
        for n in sorted(TABLES):
            name, cols = TABLES[n]
            sizes = [col_size(c) for c in cols]
            rows = []
            for _ in range(self.rows[n]):
                row = []
                for kind, size in zip(cols, sizes):
                    value = int.from_bytes(heap[pos:pos + size], 'little')
                    pos += size
                    if isinstance(kind, tuple) and kind[0] == 'C':
                        bits, tables = CODED[kind[1]]
                        tag, index = value & ((1 << bits) - 1), value >> bits
                        if tag >= len(tables) or tables[tag] is None:
                            raise MetadataError(f'{name}: coded index {kind[1]} with an unused tag {tag}')
                        value = (tables[tag], index)
                    row.append(value)
                rows.append(row)
            self.table[name] = rows
        self.tables_end = pos
        # The tables fill their stream exactly, then one zero byte and zero padding to 4 (as the .NET metadata writer
        # lays it out): nearly every column-width mistake breaks this.
        extra = len(heap) - pos
        if extra < 0:
            raise MetadataError(f'the tables run {-extra} bytes past the end of #~')
        if extra != 1 + (-(pos + 1)) % 4 or heap[pos:].strip(b'\0'):
            raise MetadataError(f'#~ has {extra} bytes after its tables where one zero and padding belong')
        self._owner = self._field_owner = self._nest = None

    def row(self, table, index):
        """A 1-based row of a table (a metadata token's low 24 bits)."""
        rows = self.table[table]
        if not 1 <= index <= len(rows):
            raise MetadataError(f'{table} row {index} of {len(rows)}')
        return rows[index - 1]

    # --- names -------------------------------------------------------------------------------------------------------
    def assembly_name(self):
        return self.string(self.table['Assembly'][0][7]) if self.table['Assembly'] else ''

    def assembly_refs(self):
        return [self.string(r[6]) for r in self.table['AssemblyRef']]

    def typeref_name(self, index):
        """(assembly, 'Namespace.Outer/Inner') of a TypeRef."""
        scope, name, ns = self.row('TypeRef', index)
        if scope[0] == 0x01:
            asm, outer = self.typeref_name(scope[1])
            return asm, f'{outer}/{self.string(name)}'
        full = f'{self.string(ns)}.{self.string(name)}' if self.string(ns) else self.string(name)
        if scope[0] == 0x23:
            return self.string(self.row('AssemblyRef', scope[1])[6]), full
        if scope[0] == 0x1A:
            return 'module ' + self.string(self.row('ModuleRef', scope[1])[0]), full
        return self.assembly_name(), full

    def enclosing(self):
        """TypeDef index -> the TypeDef it is nested in."""
        if self._nest is None:
            self._nest = {nested: outer for nested, outer in self.table['NestedClass']}
        return self._nest

    def typedef_name(self, index):
        flags, name, ns, extends, fields, methods = self.row('TypeDef', index)
        outer = self.enclosing().get(index)
        if outer:
            return f'{self.typedef_name(outer)}/{self.string(name)}'
        return f'{self.string(ns)}.{self.string(name)}' if self.string(ns) else self.string(name)

    def outermost(self, index):
        nest = self.enclosing()
        while index in nest:
            index = nest[index]
        return index

    def type_name(self, coded):
        """(assembly, name) of a TypeDefOrRef or a TypeSpec (for a generic instance, its generic type)."""
        table, index = coded
        if table == 0x02:
            return self.assembly_name(), self.typedef_name(index)
        if table == 0x01:
            return self.typeref_name(index)
        if table == 0x1B:
            sig = self.blob(self.row('TypeSpec', index)[0])
            if sig and sig[0] == 0x15 and len(sig) > 2 and sig[1] in (0x11, 0x12):
                value, _ = compressed(sig, 2)
                return self.type_name(([0x02, 0x01, 0x1B][value & 3], value >> 2))
            return self.assembly_name(), '<type spec>'
        raise MetadataError(f'not a type: table {table:#x}')

    def _ranges(self, column, table):
        """Member index -> the TypeDef whose list (FieldList or MethodList) holds it."""
        owner, defs = {}, self.table['TypeDef']
        starts = [r[column] for r in defs] + [len(self.table[table]) + 1]
        for t in range(len(defs)):
            if starts[t + 1] < starts[t]:
                raise MetadataError(f'TypeDef {t + 1}: its {table} list runs backwards')
            for m in range(starts[t], starts[t + 1]):
                owner[m] = t + 1
        return owner

    def method_owner(self):
        """MethodDef index -> the TypeDef that declares it."""
        if self._owner is None:
            self._owner = self._ranges(5, 'MethodDef')
        return self._owner

    def field_owner(self):
        if self._field_owner is None:
            self._field_owner = self._ranges(4, 'Field')
        return self._field_owner

    def member(self, token):
        """(assembly, type, member name) of a MethodDef, MemberRef, MethodSpec or Field token."""
        table, index = token >> 24, token & 0xFFFFFF
        if table == 0x06:
            owner = self.method_owner()[index]
            return self.assembly_name(), self.typedef_name(owner), self.string(self.row('MethodDef', index)[3])
        if table == 0x0A:
            parent, name, sig = self.row('MemberRef', index)
            if parent[0] == 0x06:
                return self.member(0x06000000 | parent[1])[:2] + (self.string(name),)
            if parent[0] == 0x1A:
                return 'module ' + self.string(self.row('ModuleRef', parent[1])[0]), '', self.string(name)
            asm, typ = self.type_name(parent)
            return asm, typ, self.string(name)
        if table == 0x2B:
            method, _ = self.row('MethodSpec', index)
            return self.member(((0x06 if method[0] == 0x06 else 0x0A) << 24) | method[1])
        if table == 0x04:
            owner = self.field_owner().get(index)
            if owner:
                return self.assembly_name(), self.typedef_name(owner), self.string(self.row('Field', index)[1])
        raise MetadataError(f'token {token:#010x} is not a member')

    # --- IL ----------------------------------------------------------------------------------------------------------
    def il(self, method_index):
        """The decoded instructions of a MethodDef body: [(opcode, operand)]; [] when it has no body."""
        rva = self.row('MethodDef', method_index)[0]
        if rva == 0:
            return []
        at = self.offset(rva)
        head = self.data[at]
        if head & 3 == 2:
            size, code = head >> 2, at + 1
        elif head & 3 == 3:
            header = (struct.unpack_from('<H', self.data, at)[0] >> 12) * 4
            size, code = struct.unpack_from('<I', self.data, at + 4)[0], at + header
        else:
            raise MetadataError(f'method {method_index}: unknown body header {head:#x}')
        out, pos, end = [], code, code + size
        while pos < end:
            op = self.data[pos]
            pos += 1
            if op == 0xFE:
                op2 = self.data[pos]
                pos += 1
                if op2 not in TWO_BYTE:
                    raise MetadataError(f'method {method_index}: unknown opcode FE {op2:02X}')
                op, width = 0xFE00 | op2, TWO_BYTE[op2]
            elif op in ONE_BYTE:
                width = ONE_BYTE[op]
            else:
                raise MetadataError(f'method {method_index}: unknown opcode {op:02X}')
            if width == -1:
                count = struct.unpack_from('<I', self.data, pos)[0]
                operand, pos = None, pos + 4 + 4 * count
            else:
                operand = int.from_bytes(self.data[pos:pos + width], 'little') if width else None
                pos += width
            out.append((op, operand))
        if pos != end:
            raise MetadataError(f'method {method_index}: the last instruction runs past the body')
        return out

    def references(self):
        """For every method with a body: (declaring TypeDef, method name, [(kind, (assembly, type, member))],
        [strings it loads], [(assembly, type) it names with ldtoken])."""
        owner = self.method_owner()
        out = []
        for m in range(1, len(self.table['MethodDef']) + 1):
            calls, strings, tokens = [], [], []
            for op, operand in self.il(m):
                if op in CALLS:
                    calls.append((CALLS[op], self.member(operand)))
                elif op in FIELDS and operand >> 24 in (0x04, 0x0A):
                    calls.append((FIELDS[op], self.member(operand)))
                elif op == LDSTR:
                    if operand >> 24 != 0x70:
                        raise MetadataError(f'ldstr with token {operand:#x}')
                    strings.append(self.user_string(operand & 0xFFFFFF))
                elif op == LDTOKEN and operand >> 24 in (0x01, 0x02, 0x1B):
                    tokens.append(self.type_name(({0x01: 0x01, 0x02: 0x02, 0x1B: 0x1B}[operand >> 24],
                                                  operand & 0xFFFFFF)))
            out.append((owner[m], self.string(self.row('MethodDef', m)[3]), calls, strings, tokens))
        return out

    def custom_attributes(self):
        """[(parent coded index, (assembly, attribute type), value blob)]."""
        out = []
        for parent, ctor, value in self.table['CustomAttribute']:
            table, index = ctor
            asm, typ, _ = self.member((table << 24) | index)
            out.append((parent, (asm, typ), self.blob(value)))
        return out


def summary(path):
    with open(path, 'rb') as f:
        a = Assembly(f.read())
    lines = [f'{path}: {a.assembly_name()}, metadata {a.version}',
             f'  sections {[s[0] for s in a.sections]}, streams {[s[0] for s in a.streams]}',
             f'  tables end at {a.tables_end} of {len(a.heap["#~"])} bytes; rows: ' + ', '.join(
                 f'{TABLES[n][0]} {a.rows[n]}' for n in sorted(TABLES) if a.rows[n]),
             f'  assembly refs {a.assembly_refs()}', f'  imports {a.imports()}']
    refs = a.references()
    lines.append(f'  {len(refs)} methods decoded, {sum(len(c) for _, _, c, _, _ in refs)} calls, '
                 f'{sum(len(s) for _, _, _, s, _ in refs)} string loads, {len(a.user_strings())} #US entries')
    return '\n'.join(lines)


if __name__ == '__main__':
    args = sys.argv[1:]
    if not args or args[0] != '--selftest':
        print(__doc__)
        sys.exit(2)
    failed = 0
    for p in args[1:]:
        try:
            print(summary(p))
        except MetadataError as e:
            failed += 1
            print(f'{p}: FAILED: {e}')
    sys.exit(1 if failed else 0)
