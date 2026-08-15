# ExitPass POS Server Annex E-1 Synthetic UAT Canonical Source Row and Hash Contract v1.3

## 1. Authority and incorporated ledger

This document is normative for v1.3 offline construction. It creates no data and authorizes no implementation. The v1.2 148-row Electronic Journal ledger in `ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.2.md`, exact SHA-256 `ec060c47148b8d6ec24118584b34efae4b4460684569fe5866b708a6dcffb63f`, is incorporated for its section 8 ledger only. The ledger remains byte-valid because statutory `originalAmount` is not an EJ semantic field.

## 2. Algorithms and output

Every governed digest uses SHA-256 over the exact preimage bytes and is rendered as 64 lowercase hexadecimal ASCII characters. No BOM, line ending, terminal NUL, or trailing newline is added unless a runtime family expressly includes it. Inputs are fixed offline; current time, locale, environment, filesystem ordering, dictionary ordering, randomness, and database defaults are forbidden.

Runtime canonicalizers remain authoritative for runtime-shaped hashes:

| Family | Domain/version | Authoritative source at baseline `cfb9d0c...` |
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

Missing fields are omitted only where the row schema marks `OMIT`; a required or nullable field is always present, nullable absence is tag 00, and empty string is tag 01 length zero. Duplicate object names and duplicate SET frame bytes are invalid. Arrays preserve declared order and duplicates are allowed only where the family explicitly permits them; no v1.3 family permits duplicates. Objects use the exact field order in the population contract, never alphabetical or map iteration. Binary floating point is prohibited. Monetary values are tag 03 integer PHP minor units.

## 4. Offline hash families

| Domain | Version | Family token | Root object and ordering | Exclusions |
|---|---|---|---|---|
| `annex-e1-source-row` | `sha256:v1.3` | exact row-family kebab token | `rowId`, `role`, `fields` in that order; `fields` follows its schema order | row `semantic_hash`, `integrity_hash`, package hashes |
| `annex-e1-evidence-row` | `sha256:v1.3` | replay/conflict/recovery/audit token | same source-row shape | self hash and package hashes |
| `annex-e1-case-package` | `sha256:v1.3` | case ID | `specificationId`, `caseId`, ordered `sourceRows`, ordered `journalPairs`, ordered H members, ordered Annex rows, `outcome`, ordered evidence rows | case hash itself |
| `annex-e1-integrity-row` | `sha256:v1.3` | row-family token | family fields including semantic hash and predecessor hash | integrity hash itself |
| `annex-e1-specification-package-root` | `sha256:v1.3` | `v1.3` | ordered member objects `path`, `length`, `sha256` | manifest itself and package-root digest |

Source rows sort by family order 1-29, then scenario number, period ordinal, object ordinal, UUID. Journal pairs sort stream UUID bytes then sequence. H members are H01-H10. Annex rows sort case number then period sequence. Evidence rows sort family order replay, conflict, recovery, audit then ordinal. Package member descriptors sort by unsigned UTF-8 path bytes. A sort-key tie is invalid.

File member SHA-256 is over exact repository bytes. Path is the exact repository-relative ASCII path using `/`. Member `sha256` is tag 08 containing the 32 digest bytes, not hex text. The manifest excludes itself to avoid recursion.

## 5. Runtime family rules

Fiscal-document request arrays use current runtime sort rules: discount by line sequence null-last then type UUID; lines by sequence; links by target UUID then link type; tax by line sequence null-last, tax type UUID, classification UUID; tender by type UUID then finality reference; total by total-type UUID. Null properties are omitted only where current `JsonIgnoreCondition.WhenWritingNull` does so. The v1.3 population fixes every non-null choice.

Workbook request sources sort period sequence then period UUID; fact IDs and matching hashes sort fact ordinal then UUID. Accounting-fact nullable properties are present as JSON null in current hasher order. Runtime JSON uses exact `Utf8JsonWriter` output, UTF-8 without BOM, no trailing LF.

EJ semantic uses the 20 LF-terminated lines in v1.2 section 3 and EJ integrity uses its 11 LF-terminated lines. Facts JSON keys sort ordinal. Genesis predecessor is 64 ASCII zeroes; later predecessor is the previous lowercase integrity digest. Self semantic/integrity fields are excluded from their own preimages.

## 6. Normative non-empty vectors

These vectors are built from section 3, not copied JSON serialization. Hex is the exact complete preimage.

| Vector | Coverage | Bytes | SHA-256 |
|---|---|---:|---|
| V13-01 | null, empty string, Boolean, signed integer, money, timestamp, UUID, code, binary, LF, `\|`, NFC `Ñ` | 391 | `70cc646de58b4ddd5cfef4f0717605f11d24f29271a0fcfbca1ce6da42a09654` |
| V13-02 | nested object, fixed decimal, ordered array, unordered set sorting | 249 | `ec60d71d6d207376fb2070aa26b50112e2a2d5ac0279701f7cd628b7cb36ef20` |
| V13-03 | semantic digest and predecessor digest as binary | 222 | `cf46b20c5ea7d37646a5695fdcb90a6b0e3e0c4efb86299c53fc11ddc2dda4f8` |
| V13-04 | complete framed source-row envelope | 497 | `e9836e6a35ac50a523dbaae12a0a2f18b2755dff5c536e0a0f22af3fb19a36ff` |

V13-01 exact hex:

```text
414531480100000013616e6e65782d65312d736f757263652d726f770000000b7368613235363a76312e330000000f7363616c61722d636f7665726167650900000000000001400000000a000000096e756c6c56616c75650000000000000000000000000b656d707479537472696e6701000000000000000000000007626f6f6c65616e0200000000000000010100000007696e74656765720300000000000000032d34320000000f6d6f6e65794d696e6f72556e69747303000000000000000531313230300000000974696d657374616d7005000000000000001c323032362d30392d31305430313a30303a30302e303030303030305a000000047575696406000000000000002431393232313964642d613763392d356135332d616462382d65653133343261666637373100000004636f646507000000000000000e53454e494f525f434954495a454e0000000662696e6172790800000000000000040001feff0000000e65736361706564556e69636f646501000000000000000953594e7c53430ac391
```

V13-02 exact hex:

```text
414531480100000013616e6e65782d65312d736f757263652d726f770000000b7368613235363a76312e330000000f6e65737465642d636f7665726167650900000000000000b200000003000000056368696c6409000000000000003300000002000000046e616d65010000000000000005616c7068610000000576616c756504000000000000000731322e33343030000000076f7264657265640a000000000000001800000002010000000000000001620100000000000000016100000009756e6f7264657265640b00000000000000270000000207000000000000000350574407000000000000000e53454e494f525f434954495a454e
```

V13-03 exact hex:

```text
414531480100000016616e6e65782d65312d696e746567726974792d726f770000000b7368613235363a76312e330000000e636861696e2d636f766572616765090000000000000095000000030000000873657175656e6365030000000000000001320000000c73656d616e746963486173680800000000000000200102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f200000001570726576696f7573496e74656772697479486173680800000000000000202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40
```

V13-04 exact hex:

```text
414531480100000013616e6e65782d65312d736f757263652d726f770000000b7368613235363a76312e330000000f66697363616c2d646f63756d656e740900000000000001aa0000000300000005726f77496406000000000000002431393232313964642d613763392d356135332d616462382d65653133343261666637373100000004726f6c6507000000000000000c494e5055545f534f55524345000000066669656c64730900000000000001400000000a000000096e756c6c56616c75650000000000000000000000000b656d707479537472696e6701000000000000000000000007626f6f6c65616e0200000000000000010100000007696e74656765720300000000000000032d34320000000f6d6f6e65794d696e6f72556e69747303000000000000000531313230300000000974696d657374616d7005000000000000001c323032362d30392d31305430313a30303a30302e303030303030305a000000047575696406000000000000002431393232313964642d613763392d356135332d616462382d65653133343261666637373100000004636f646507000000000000000e53454e494f525f434954495a454e0000000662696e6172790800000000000000040001feff0000000e65736361706564556e69636f646501000000000000000953594e7c53430ac391
```

## 7. All-row derivation

For every row, select its family schema and exact values from the v1.3 population document, construct the ordered `fields` object, omit only schema-marked `OMIT` members, construct the source/evidence envelope, and hash. For runtime families, construct the corresponding current runtime request/fact/report object from the same values and run the named runtime canonicalizer. This is the complete expected-hash derivation; no published digest is an input to its own preimage.

Artifact bytes for a workbook definition are not a generated workbook. They are the future offline case-package preimage bytes for that case. Therefore `artifact_sha256` is the case-package SHA-256, `artifact_length_bytes` is its exact byte length, MIME is `application/vnd.exitpass.annex-e1.synthetic-package`, filename is `DS-AE1-NNN.annex-e1.bin`, and storage key is null. These values are deterministically available after the machine-readable package is constructed and require no runtime or workbook generation.

## 8. Complete row-family closure

The canonical population document defines the full field list, exact types, null/omission rules, deterministic values, ordering, relations, lifecycle, requests, audits, retention, and hashes for all 29 families. A missing field, unknown code, duplicate sort key, unresolved expression, mismatched runtime hash, or nonzero reconciliation difference fails construction.

The seven v1.2-complete families remain closed. The other 22 are closed in v1.3 by literal universal bindings, deterministic UUID references, fixed chronology, explicit null choices, the corrected SP-A original amount, deterministic offline artifact-envelope values, complete action/result registries, and AE1H source/package framing.

## 9. Offline/runtime boundary

Offline rows marked expected runtime result are expectations, not assertions that current runtime can persist deterministic UUIDs or timestamps. Constructing them does not require a live clock, database, application, API, migration, environment, or external service. Runtime loading and execution remain unauthorized. External decisions do not enter any hash preimage.
