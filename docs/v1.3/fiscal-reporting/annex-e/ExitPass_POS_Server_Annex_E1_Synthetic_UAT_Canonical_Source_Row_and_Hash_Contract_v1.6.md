# ExitPass POS Server Annex E-1 Synthetic UAT Canonical Source Row and Hash Contract v1.6

## 1. Authority and incorporated ledger

This document is normative for v1.6 offline construction. It creates no data and authorizes no implementation. It supersedes v1.3 only for M01-M04. The v1.2 148-row Electronic Journal ledger in `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.2.md`, exact SHA-256 `ec060c47148b8d6ec24118584b34efae4b4460684569fe5866b708a6dcffb63f`, is incorporated for its section 8 ledger only. The ledger remains byte-valid because neither statutory `originalAmount` nor the Central PMS parking-session reference is an EJ semantic field.

## 2. Algorithms and output

Every governed digest uses SHA-256 over the exact preimage bytes and is rendered as 64 lowercase hexadecimal ASCII characters. No BOM, line ending, terminal NUL, or trailing newline is added unless a runtime family expressly includes it. Inputs are fixed offline; current time, locale, environment, filesystem ordering, dictionary ordering, randomness, and database defaults are forbidden.

Runtime canonicalizers remain authoritative for runtime-shaped hashes:

| Family | Domain/version | Authoritative source at baseline `2761a41b5ba7d4d5482d2973238238666425fa16` |
|---|---|---|
| fiscal-document request | `sha256:v1` / `pos-server-fiscal-document-create:sha256:v2` | `FiscalDocumentSemanticRequestHasher.cs` |
| workbook request | `pos-server-annex-e1-request:sha256:v1` | `AnnexE1SemanticHasher.cs` |
| Accounting fact | `pos-server-annex-e1-period-fact:sha256:v1` | `AnnexE1SemanticHasher.cs` |
| EJ semantic | `pos-server-electronic-journal-event-semantic:sha256:v1` | `ElectronicJournalSemanticHasher.cs` |
| EJ integrity | `pos-server-electronic-journal-integrity:sha256:v1` | `ElectronicJournalIntegrityHasher.cs` |

Their exact property/line order, null behavior, sort keys, timestamp format, and final-LF behavior are the current source contract. The canonical population supplies every input. Offline source/evidence/case/package hashing uses the closed framing below and never substitutes for a runtime hash.

## 3. AE1H binary framing v1

All multi-byte unsigned integers are big-endian. `U32(n)` is four bytes; `U64(n)` is eight bytes. `UTF8(s)` is strict UTF-8 of Unicode NFC `s`; unpaired surrogates are invalid. Names are case-sensitive. No escaping is performed because lengths frame bytes.

```text
FRAME(value) = tag:u8 || U64(payload-byte-length) || payload
MEMBER(name,value) = U32(UTF8(name).length) || UTF8(name) || FRAME(value)
OBJECT(ordered-members) payload = U32(member-count) || MEMBER...
ARRAY(ordered-values) payload = U32(value-count) || FRAME(value)...
SET(values) payload = U32(value-count) || FRAME(value)... sorted by complete FRAME bytes
PREIMAGE(domain,version,family,root) =
  UTF8("AE1H") || 0x01 ||
  U32(domain.length) || UTF8(domain) ||
  U32(version.length) || UTF8(version) ||
  U32(family.length) || UTF8(family) ||
  FRAME(root)
```

Lengths in `PREIMAGE` are UTF-8 byte lengths, not character counts.

| Tag | Type | Payload |
|---:|---|---|
| `00` | null | zero bytes |
| `01` | string | UTF-8 NFC bytes; empty string has zero-length payload |
| `02` | Boolean | one byte `00` false or `01` true |
| `03` | integer/minor units | ASCII base-10; zero `0`; negative leading `-`; no plus/leading zero |
| `04` | decimal | ASCII fixed scale declared by field; no exponent/plus; e.g. `0.120000` |
| `05` | UTC timestamp | ASCII `yyyy-MM-ddTHH:mm:ss.fffffffZ` |
| `06` | UUID | 36 lowercase ASCII RFC text with hyphens |
| `07` | controlled code | exact case-sensitive UTF-8 code bytes |
| `08` | binary | raw bytes, not Base64 or hex |
| `09` | object | OBJECT payload |
| `0a` | ordered array | ARRAY payload; family-declared order |
| `0b` | unordered set | SET payload; sort by unsigned lexicographic FRAME bytes |

Missing fields are omitted only where the row schema marks `OMIT`; a required or nullable field is always present, nullable absence is tag 00, and empty string is tag 01 length zero. Duplicate object names and duplicate SET frame bytes are invalid. Arrays preserve declared order and duplicates are allowed only where the family explicitly permits them; no v1.6 family permits duplicates. Objects use the exact field order in the population contract, never alphabetical or map iteration. Binary floating point is prohibited. Monetary values are tag 03 integer PHP minor units.

## 4. Offline hash families

| Domain | Version | Family token | Root object and ordering | Exclusions |
|---|---|---|---|---|
| `annex-e1-source-row` | `sha256:v1.4` | exact row-family kebab token | `rowId`, `role`, `fields` in that order; `fields` follows its schema order | row `semantic_hash`, `integrity_hash`, package hashes |
| `annex-e1-evidence-row` | `sha256:v1.4` | replay/conflict/recovery/audit token | same source-row shape | self hash and package hashes |
| `annex-e1-workbook-content` | `sha256:v1.4` | case ID | `workbookId`, `profile`, ordered H01-H10, ordered Annex-row semantic hashes, ordered fact-link semantic hashes | workbook content hash and every package/case digest |
| `annex-e1-workbook-calculation` | `sha256:v1.6` | `AE1-UAT-NNN` | `workbookId`, `annexRowCalculationHashes` in that order; hashes sort by Annex row ordinal | aggregate hash itself and every ancestor digest |
| `annex-e1-case-package` | `sha256:v1.4` | case ID | `specificationId`, `caseId`, ordered source-row semantic hashes, ordered journal semantic/integrity pairs, ordered H01-H10, ordered Annex-row semantic hashes, `outcome`, ordered evidence-row semantic hashes | case hash itself and every ancestor digest |
| `annex-e1-integrity-row` | `sha256:v1.4` | row-family token | family fields including semantic hash and predecessor hash | integrity hash itself |
| `annex-e1-specification-package-root` | `sha256:v1.6` | `v1.6` | ordered member objects `path`, `length`, `sha256` | manifest itself and package-root digest |

Source rows sort by family order 1-29, then scenario number, period ordinal, object ordinal, UUID. Journal pairs sort stream UUID bytes then sequence. H members are H01-H10. Annex rows sort case number then period sequence. Evidence rows sort family order replay, conflict, recovery, audit then ordinal. Package member descriptors sort by unsigned UTF-8 path bytes. A sort-key tie is invalid. Child collections contain raw 32-byte digests in tag `08`, not hexadecimal strings. For the fifteen corrected SP-A workbooks, `calculation_semantic_hash` is SHA-256 over the `annex-e1-workbook-calculation` preimage above. The four unaffected B/D-profile workbook tuples remain byte-identical to v1.5 and retain their published aggregate values.

File member SHA-256 is over exact repository bytes. Path is the exact repository-relative ASCII path using `/`. Member `sha256` is tag 08 containing the 32 digest bytes, not hex text. The manifest excludes itself to avoid recursion.

### 4.1 Exact specification package-root algorithm

The eight non-manifest v1.6 members listed in the manifest are the complete member set. Encode each descriptor as an AE1H object with members in this exact order: `path:string`, `length:integer`, `sha256:binary32`. `path` is strict UTF-8 NFC repository-relative text with `/`; its byte count is carried by the string frame's big-endian U64 payload length. `length` is the exact file-byte count encoded as tag `03` canonical unsigned base-10 ASCII. `sha256` is tag `08`, payload length 32, followed by the raw digest bytes. Sort descriptors by unsigned lexicographic UTF-8 path bytes and reject duplicate paths.

Encode root object `{members:ordered-array<descriptor>}` and then apply `PREIMAGE` with domain `annex-e1-specification-package-root`, version `sha256:v1.6`, and family `v1.6`. AE1H supplies the `AE1H` domain separator, framing version byte `01`, big-endian U32 name lengths, big-endian U64 value lengths, type tags, and member count. SHA-256 output is 64 lowercase hexadecimal ASCII. The manifest, its path, its digest, and the package-root digest are excluded. Exact member bytes are not normalized; no BOM or final newline is added or removed. The final manifest publishes the complete root-preimage Base64 so an implementation can recover every byte without prose interpretation.

### 4.2 Acyclic hash graph

Edges point from a digest node to each byte or child digest included in its preimage. The only permitted edges are:

```text
source-row-semantic -> canonical source-row fields
EJ-integrity -> EJ-semantic, predecessor-integrity, immutable EJ metadata
workbook-calculation -> annex-row-calculation[]
workbook-content -> H01-H10, annex-row-semantic[], fact-link-semantic[]
workbook-row-semantic -> workbook-content, workbook metadata
evidence-row-semantic -> source-row-semantic or workbook-content
case-package -> source-row-semantic[], EJ pairs[], H01-H10, annex-row-semantic[], evidence-row-semantic[]
specification-package-root -> exact non-manifest Markdown member bytes
```

Forbidden edges are child to case-package, child to specification-package-root, any digest to itself, and any predecessor to a later EJ integrity digest. The topological order is fixed literals/UUIDs, source rows, EJ semantic rows, EJ integrity chains, workbook content, workbook rows, evidence rows, case packages, member file digests, specification package root. The workbook row fields `artifact_sha256` and `artifact_length_bytes` are renamed normatively to `workbook_content_sha256` and `workbook_content_length_bytes`; they contain the workbook-content digest and preimage length. Replay, conflict, and recovery rows reference `workbook_content_sha256`, not case-package digest. Cycle detection must visit all declared nodes and return zero back edges.

### 4.3 Worked case boundary

For `DS-AE1-001`, compute four fiscal-document branches, seven Accounting facts, eight EJ pairs, the corrected Annex calculation hash, one Annex row, seven fact links, the corrected workbook-calculation aggregate, the workbook content, the workbook source row, applicable evidence rows, and only then the case package. The workbook-content root members are exactly `workbookId:uuid`, `profile:code`, `headers:object`, `annexRowHashes:array<binary32>`, and `factLinkHashes:array<binary32>`; its family is `AE1-UAT-001`. The workbook-content preimage excludes `workbook_content_sha256`, `workbook_content_length_bytes`, every case-package field, and every specification-package field. The case-package preimage consumes the completed workbook-row semantic hash as a binary child digest. Recomputing the workbook content after the case-package digest exists is prohibited and would be a cycle. Exact worked-case byte counts and digests are published in the population document's v1.6 digest registry.

## 5. Runtime family rules

Fiscal-document request arrays use current runtime sort rules: discount by line sequence null-last then type UUID; lines by sequence; links by target UUID then link type; tax by line sequence null-last, tax type UUID, classification UUID; tender by type UUID then finality reference; total by total-type UUID. Null properties are omitted only where current `JsonIgnoreCondition.WhenWritingNull` does so. The v1.6 population fixes every non-null choice.

Workbook request sources sort period sequence then period UUID; fact IDs and matching hashes sort fact ordinal then UUID. Accounting-fact nullable properties are present as JSON null in current hasher order. Runtime JSON uses exact `Utf8JsonWriter` output, UTF-8 without BOM, no trailing LF.

EJ semantic uses the 20 LF-terminated lines in v1.2 section 3 and EJ integrity uses its 11 LF-terminated lines. Facts JSON keys sort ordinal. Genesis predecessor is 64 ASCII zeroes; later predecessor is the previous lowercase integrity digest. Self semantic/integrity fields are excluded from their own preimages.

## 6. Normative non-empty vectors

These vectors are built from section 3, not copied JSON serialization. Hex is the exact complete preimage.

| Vector | Coverage | Bytes | SHA-256 |
|---|---|---:|---|
| V14-01 | null, empty string, Boolean, signed integer, money, timestamp, UUID, code, binary, LF, `\|`, NFC `Ñ` | 391 | `98114cfa0a0a163f2796255de0d1d42d0fcd720ccebb07266553e58dc0a45e49` |
| V14-02 | nested object, fixed decimal, ordered array, unordered set sorting | 249 | `2577237b1919bc01fcfcc049a36fcd92435e4de8bdfd2d80eb7d6197daa5f1e6` |
| V14-03 | semantic digest and predecessor digest as binary | 222 | `e83baf882bebdb9bdf889b84e1314e43daf819a640a328e29af594e21cb64732` |
| V14-04 | complete framed source-row envelope | 497 | `7e2f0395f9bf3fb007984aae1ee4d1f95c5fd1fb2101a83aed698c89f726c74e` |

V14-01 exact hex:

```text
414531480100000013616e6e65782d65312d736f757263652d726f770000000b7368613235363a76312e340000000f7363616c61722d636f7665726167650900000000000001400000000a000000096e756c6c56616c75650000000000000000000000000b656d707479537472696e6701000000000000000000000007626f6f6c65616e0200000000000000010100000007696e74656765720300000000000000032d34320000000f6d6f6e65794d696e6f72556e69747303000000000000000531313230300000000974696d657374616d7005000000000000001c323032362d30392d31305430313a30303a30302e303030303030305a000000047575696406000000000000002431393232313964642d613763392d356135332d616462382d65653133343261666637373100000004636f646507000000000000000e53454e494f525f434954495a454e0000000662696e6172790800000000000000040001feff0000000e65736361706564556e69636f646501000000000000000953594e7c53430ac391
```

V14-02 exact hex:

```text
414531480100000013616e6e65782d65312d736f757263652d726f770000000b7368613235363a76312e340000000f6e65737465642d636f7665726167650900000000000000b200000003000000056368696c6409000000000000003300000002000000046e616d65010000000000000005616c7068610000000576616c756504000000000000000731322e33343030000000076f7264657265640a000000000000001800000002010000000000000001620100000000000000016100000009756e6f7264657265640b00000000000000270000000207000000000000000350574407000000000000000e53454e494f525f434954495a454e
```

V14-03 exact hex:

```text
414531480100000016616e6e65782d65312d696e746567726974792d726f770000000b7368613235363a76312e340000000e636861696e2d636f766572616765090000000000000095000000030000000873657175656e6365030000000000000001320000000c73656d616e746963486173680800000000000000200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f200000001570726576696f7573496e74656772697479486173680800000000000000202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40
```

V14-04 exact hex:

```text
414531480100000013616e6e65782d65312d736f757263652d726f770000000b7368613235363a76312e340000000f66697363616c2d646f63756d656e740900000000000001aa0000000300000005726f77496406000000000000002431393232313964642d613763392d356135332d616462382d65653133343261666637373100000004726f6c6507000000000000000c494e5055545f534f55524345000000066669656c64730900000000000001400000000a000000096e756c6c56616c75650000000000000000000000000b656d707479537472696e6701000000000000000000000007626f6f6c65616e0200000000000000010100000007696e74656765720300000000000000032d34320000000f6d6f6e65794d696e6f72556e69747303000000000000000531313230300000000974696d657374616d7005000000000000001c323032362d30392d31305430313a30303a30302e303030303030305a000000047575696406000000000000002431393232313964642d613763392d356135332d616462382d65653133343261666637373100000004636f646507000000000000000e53454e494f525f434954495a454e0000000662696e6172790800000000000000040001feff0000000e65736361706564556e69636f646501000000000000000953594e7c53430ac391
```

## 7. All-row derivation

For every row, select its family schema and exact values from the v1.6 population document, construct the ordered `fields` object, omit only schema-marked `OMIT` members, construct the source/evidence envelope, and hash. For runtime families, construct the corresponding current runtime request/fact/report object from the same values and run the named runtime canonicalizer. This is the complete expected-hash derivation; no published digest is an input to its own preimage.

Workbook content bytes are not a generated workbook. They are the AE1H `annex-e1-workbook-content` preimage for the case. Therefore `workbook_content_sha256` is the SHA-256 of those bytes and `workbook_content_length_bytes` is their exact length. MIME is `application/vnd.exitpass.annex-e1.synthetic-workbook-content`, filename is `DS-AE1-NNN.workbook-content.ae1h`, and storage key is null. A workbook row never stores a case-package digest. These values are available before workbook-row and case-package hashing and require no runtime or workbook generation.

## 8. Complete row-family closure

The v1.6 population Appendix A is the only semantic-instance ledger. It contains all 1,690 governed rows under the stable `annex-e1-source-row` / `sha256:v1.4` envelope and publishes the exact family token, role, ordered schema, ordered values, complete preimage bytes, length, expected SHA-256, and provenance for each row. No informational binding digest, binding preimage length, or registry commitment remains. The 1,690 rows comprise 840 exact v1.4 Appendix B rows, 148 incorporated F22 Electronic Journal rows, and 702 corrected rows in F01, F07-F10, F12-F15, and F23. A PowerShell 5.1 reconstruction parsed the Markdown tuples and rebuilt all 1,690 preimages without consuming the published hexadecimal values: zero schema, value, length, byte, or digest mismatch; zero blocked instance.

The 148 F22 rows carry their separately governed runtime semantic and integrity hashes as exact binary members. The 148 F23 rows are distinct transition evidence with transition identity, stream, predecessor transition, source/destination sequence, source event, request identity, transition type/version, result record/hash, status, and exact chronology. Passing F22 never substitutes for F23 closure.

Current-runtime canonicalizers remain separately authoritative where section 2 names one; an AE1H source-row digest never substitutes for a runtime request or integrity digest. M01, M02, and M04 remain preserved. IR14-01 and IR14-02 remain resolved and E14-01 remains corrected. The v1.6 dataset implementation is authorized only for offline generation and deterministic validation; loading and execution remain outside scope.
## 9. Offline/runtime boundary

Offline rows marked expected runtime result are expectations, not assertions that current runtime can persist deterministic UUIDs or timestamps. Constructing them does not require a live clock, database, application, API, migration, environment, or external service. Runtime loading and execution remain unauthorized. External decisions do not enter any hash preimage.
