# ExitPass POS Server Annex E-1 v1.7 Internal Workbook Presentation and Controlled UAT Readiness Revalidation v1.0

## 1. Decision

**Decision: `READY_FOR_CONTROLLED_UAT_AUTHORIZATION`.**

The corrected v1.7 workbook generator passes independent semantic, deterministic, spreadsheet-engine, print, and visual validation. Microsoft Excel opens all 19 workbooks normally and read-only without repair, recovery, corruption, compatibility, Protected View, or external-link warnings. Excel exports each `E-1` print area as one landscape PDF page. Every one of the 19 rendered pages was visually inspected, all 22 Annex rows are readable, and the exact `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` warning is visible once in the printable worksheet body and PDF output.

No `BLOCKING_CONTROLLED_UAT` defect remains. The internal workbooks are suitable for use in a separately authorized Controlled UAT execution. This decision does not begin Controlled UAT, accept evidence, approve an official BIR workbook, authorize external delivery, submit to BIR, or authorize Production use.

Official BIR submission filename, delivery, signing, acceptance, and retention requirements remain outside this internal presentation decision. Their absence does not prevent technical Controlled UAT from producing usable internal evidence.

## 2. Purpose And Scope

This review repeats the independent workbook rendering, visual presentation, print, diagnostic-sheet, and Controlled UAT readiness assessment after the bounded interoperability correction. It uses the merged generator and committed v1.7 artifacts without changing them.

The review does not modify the generator, tests, package, dataset, runtime, API, database, migrations, controlled codes, dependencies, CI, or Production configuration. It does not begin Controlled UAT, load a shared environment, generate an official BIR submission workbook, deliver evidence, contact BIR or HikCentral, or authorize Production use.

## 3. Baseline And Provenance

| Item | Value |
| --- | --- |
| Repository | `D:\SourceCodes\ExitPass-PoSServer` |
| Base branch | `dev` |
| Review branch | `docs/annex-e1-v1-7-workbook-presentation-revalidation` |
| Review worktree | `D:\wt\AnnexE1V17WorkbookPresentationRevalidation` |
| Exact `origin/dev` baseline | `7ad877baa81ec97b966fd8dd8f22ec00ccc02fe6` |
| Interoperability-correction merge commit | `7ad877baa81ec97b966fd8dd8f22ec00ccc02fe6` |
| Merge parents | `00517057e2a2aeb1f8b91c3435653b61bb6d0615`, `4262168450296d47bc9443725525eeb0a2e24476` |
| Interoperability-correction content commit | `4262168450296d47bc9443725525eeb0a2e24476` |
| Merge provenance | PR `#121`, `fix/annex-e1-v1-7-workbook-interoperability` |
| Merge base at branch creation | `7ad877baa81ec97b966fd8dd8f22ec00ccc02fe6` |
| Review branch divergence at creation | `0 ahead / 0 behind` |

The primary repository was clean and fast-forwarded to `origin/dev`. The prior interoperability worktree and local branch were already absent. No unrelated worktree was changed.

## 4. Relationship To The Original Review

The historical [Internal Workbook Presentation and Controlled UAT Readiness Review v1.0](ExitPass_POS_Server_Annex_E1_v1.7_Internal_Workbook_Presentation_and_Controlled_UAT_Readiness_Review_v1.0.md) remains unchanged and retains `BLOCKED_WORKBOOK_RENDERING` as the result for the original generated artifacts.

This revalidation independently confirms that both original blocking defects are corrected:

| Historical defect | Corrected behavior | Revalidation |
| --- | --- | --- |
| Excel rejected all 19 workbooks with `0x800A03EC`. | The generator omits the incomplete optional `<fileVersion appName=xl/>` element from `xl/workbook.xml`. | PASS: Excel normal-open succeeds for 19 of 19 without warning or repair. |
| Printable `E-1` sheets omitted the internal-test warning. | The exact warning occupies `A11:AF11`, within each `E-1` print area. | PASS: visible in Excel and all 19 PDF renders. |

The earlier review is evidence about pre-correction artifacts; this document records the corrected-artifact outcome.

## 5. Reviewed Commitments

| Commitment | Expected | Result |
| --- | --- | --- |
| v1.7 package root | `5d60cb50d7f43b29f79486801a470e57128e2b571c4e50a37494e4a82131b829` | PASS |
| Dataset records | `4,074` | PASS |
| Dataset canonical LF bytes | `8,070,439` | PASS |
| Dataset canonical LF SHA-256 | `ed6971df7a142a54492eea968587a150c0229618650cee56ea2116154692a1ac` | PASS |
| Deterministic identities | `2,364` | PASS |
| Semantic instances | `1,690` across 29 families | PASS |
| Included cases | `19` | PASS |
| Annex rows | `22` | PASS |
| Isolated execution normalized SHA-256 | `eb49cc8f3ba5a2935463e030f7e2b1707131aff68bcfd1d37c4b6ea1e224a18e` | PASS |
| Aggregate workbook manifest SHA-256 | `d965ec39286ea3d92fc32e64a968ed014f0486a8698819ae6caff2b95d7d72b5` | PASS |
| Manifest-file SHA-256 | `b2961b498ec7b6654366eb5ef30f99cde5def62326f52249f05cedf09405217d` | PASS |

No frozen package, dataset, identity, semantic, or normalized-execution commitment changed.

## 6. Tools And Method

| Tool | Version / posture | Use |
| --- | --- | --- |
| Microsoft Excel | `16.0.20228.20190` | Normal read-only open, sheet checks, and fixed-format PDF export |
| DocumentFormat.OpenXml | `3.5.1` | Standards-level workbook validation through the merged runtime test suite |
| Windows Runtime PDF renderer | `Windows.Data.Pdf` on Windows `10.0.26200.0` | Local rasterization of every Excel-exported PDF page |
| .NET | Repository target/runtime tooling | Full 209-test runtime suite |
| Windows PowerShell | `5.1` | Repository validators, generator, controlled Excel automation, and read-only checks |

Excel automation was hidden and read-only with alerts disabled. Workbooks were closed without saving. Only task-owned Excel processes were used. The repository-documented source workbook `D:\Docs\ExitPass\POS\RMO 24-2023 Annex E-1 to E-5.xlsx` opened read-only as the renderer control.

Excel exported only the `E-1` worksheet from each generated workbook. Each PDF was independently parsed for page objects and rasterized through the local Windows PDF API. All 19 rasterized pages were visually inspected at original resolution. Representative `Validation` and `Fact Links` sheets were also exported and visually inspected for a standard one-row case, cases 004 and 005, replay case 012, correction case 013, and finality/retry-boundary case 024.

## 7. Commands

Generation:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1V17WorkbookGeneration.ps1 -OutputDirectory <task-owned-empty-directory>
```

Runtime suite:

```powershell
dotnet test .\tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj --no-restore --configuration Release --verbosity minimal
```

Excel normal-open and E-1 export pattern:

```powershell
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$workbook = $excel.Workbooks.Open(<generated-workbook>, 0, $true)
$workbook.Worksheets.Item(E-1).ExportAsFixedFormat(0, <task-owned-pdf>)
$workbook.Close($false)
```

PDF page images were created locally with `Windows.Data.Pdf.PdfDocument.LoadFromFileAsync` and `PdfPage.RenderToStreamAsync`. No external renderer or service was used.

## 8. Workbook And Render Inventory

| Case | Annex rows | XLSX bytes | XLSX SHA-256 | Normalized semantic SHA-256 | Excel open | PDF pages | Visual result |
| --- | ---: | ---: | --- | --- | --- | ---: | --- |
| `DS-AE1-001` | 1 | 44,451 | `041238bd4cc149e3f9d44d414d47543419484e94886ce25685a67d6a7fcde36c` | `b22ceec4abbd98c6e45c0e41c03ee8fea2f79208585fa50010c04d0f7a0a7bb2` | PASS | 1 | PASS |
| `DS-AE1-003` | 1 | 44,391 | `7330a00762922984be5188a97ea4076a8d59c21ac5803b92f8447de6cd3e93b6` | `a68e508cd2e3fd4fdb8a1b03c012a3b2045d3e7a1f79e451c11bd045ab64b7a3` | PASS | 1 | PASS |
| `DS-AE1-004` | 2 | 57,015 | `639fe378ecd72ba292b183e6c49115c505c62d6cf0f4eaba31e8e0cd81f80a20` | `cc6b29f65a5e2078839e5dd2543e1f7b7963ccf2f6ce502ba2b2d6a62350ec97` | PASS | 1 | PASS |
| `DS-AE1-005` | 3 | 69,643 | `6a4f8a286ede0a695a683789c860f8d4a83ad54fa63d8752290330206a382eb1` | `3407b3795571afe457c421fbc691c4e79180c209795ea3745aeeb475a7ed08c2` | PASS | 1 | PASS |
| `DS-AE1-007` | 1 | 44,451 | `ba3a5f9d7b9c0e7c7ab0dfa685276493ec0e43e7e26753531d14665171223ebf` | `9f476e95ef33079decd707dda03516d8a2028643d42d33008309985537f33604` | PASS | 1 | PASS |
| `DS-AE1-009` | 1 | 44,397 | `91b879912e8920ab3a43c5c64e8f3e3053007773c20756bd342e0dc0d0916abb` | `ba3fa23e177435060ccc1a3878bf049018e795e2eea5ad7a2303dcf16b18d0e9` | PASS | 1 | PASS |
| `DS-AE1-010` | 1 | 44,451 | `2fb631946f4f14bdc6e4b6684f505ad350107e77d47186419878c51d75ea168f` | `c60c7ca5482c482e643677daa8c2344773bc5ef0d67fee58bf6d45440bd1eaab` | PASS | 1 | PASS |
| `DS-AE1-011` | 1 | 44,451 | `344cd79f1502625bcb5283a28970631f86028abe199a233ad72516e86add1777` | `6f98e4719bdc66b343ed9fdaeec7c0c2f3c333c967ae3d286cc764fc9fe231a6` | PASS | 1 | PASS |
| `DS-AE1-012` | 1 | 44,451 | `292c7f553227a87bb77d41b311b656c203fc00bbed7425c8fd1eef6d42e1fbbd` | `fe23fc9aa7246f7a29b4e8c841ff653777e6f4d086f3c5de206f3736e624aac1` | PASS | 1 | PASS |
| `DS-AE1-013` | 1 | 44,451 | `79f9224e703b79393736688f571b7584413d867a2cc5dabc5ab66616e59bf635` | `dd6460cd1be100dadbce9ad184439ef25ba90c199a02e33bf6dccbd3d6185bad` | PASS | 1 | PASS |
| `DS-AE1-014` | 1 | 44,451 | `fee4d4389fd0b4959ba01c8893c6ff2fe5abbcacfad774e30e4b195798ceb1f9` | `bc4d4e95792472536da2229afa2dc610cbdf4d31bd57a6c395383b0a96501a64` | PASS | 1 | PASS |
| `DS-AE1-016` | 1 | 44,451 | `151898dc4accea8999e4bf9c398abeabc47e1db96b7fd426466414ffdb4b2de7` | `704a70cb44bcee7d4938751ce815db259eef5d030514778b1106fcf3ecd3d66e` | PASS | 1 | PASS |
| `DS-AE1-017` | 1 | 44,451 | `b5e5c9f3912fe7386ace4b36cda71b417d1533f3d68a5ef0bcc057d7f6b42b8a` | `14573397397dee5e6319881277ba1f4121f003524f718434a8dac5eb785677a8` | PASS | 1 | PASS |
| `DS-AE1-018` | 1 | 44,451 | `beacd5344ec02ef207c8738e2917e3345092b46fa0aa49cfb2661ceb77048af1` | `274a581fab3c97c862943cce35704f1f3e608641d065aba29ca7df1a8d4558d5` | PASS | 1 | PASS |
| `DS-AE1-019` | 1 | 44,451 | `c86fc7e48599de7aa1364171445ecefbafb574f0e82e80a68ccaf4195a947884` | `7a1d269a7ad9547c7e35f84df956f2ac923bfbac4f266b79ff69928937963996` | PASS | 1 | PASS |
| `DS-AE1-020` | 1 | 44,451 | `b862002d200e18ee158d76d206e2ddfeff119884f5e50dac986a7179b4e95929` | `7a926b4cc54f907e9220d15542bbf4602d33372db3c2ca1fdf155b1c4f126050` | PASS | 1 | PASS |
| `DS-AE1-021` | 1 | 44,451 | `cb0ca9d75f5de5cc61ec5dc4dd24da211fb290ed552b3fd17aa31b76105ecf63` | `4d50b8ab0558c70807a40a9b51a97814b4dbe351233e0da1d66506bb10777b2d` | PASS | 1 | PASS |
| `DS-AE1-023` | 1 | 44,451 | `7c4ef12e0a08dc2c1a9175cdc776d9cb8c753b017d2b243550715ba87425825e` | `d7715a7230d1a4cf0631284f6a7040bbc4f4cd64be5d98287c01c6af214a41e6` | PASS | 1 | PASS |
| `DS-AE1-024` | 1 | 44,451 | `2b3268cec56381c25e154066d625faa9c50f485c322575a92d8a24d86a72b951` | `eadf2f7f67cca5575a99cb7c40e169856041b7a65bc7e338a5b6b05a7f75155d` | PASS | 1 | PASS |

The filename set, byte lengths, workbook SHA-256 values, normalized semantic SHA-256 values, manifest bytes, aggregate manifest digest, and manifest-file digest matched across two clean generations.

## 9. Excel Open Results

All 19 workbooks opened through Excel's normal `Workbooks.Open` path with `UpdateLinks=0` and `ReadOnly=true`. No repair or extraction mode was requested. Each workbook had exactly three visible sheets in stable order: `E-1`, `Validation`, `Fact Links`. `E-1` was the active sheet. No workbook entered Protected View, prompted for link updates, reported compatibility repair, or was saved or modified.

The official local Annex E source workbook also opened read-only, confirming the renderer control. Excel was closed after each controlled session, and no task-owned Excel process remained.

## 10. E-1 Visual Review

### 10.1 Internal-test warning

The exact warning `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` appears once at `A11:AF11` in every `E-1` worksheet. It is centered, bold, fully visible, visually distinct from the governed data, inside each print area, and present in all 19 PDFs. It does not cover or shift any H01-H10, D01-D32, or R01-R12 source value.

### 10.2 H01-H10

All governed header values are visible in the established header block. Legal name, address, TIN, software, permit, serial, machine, site, reporting timestamp, and report identity remain associated with stable positions. Dates and timestamps render as intended; identifiers do not convert to scientific notation; no required header value overlaps the detail table. The `Validation` sheet provides explicit H01-H10 identifiers for technical cross-checking.

### 10.3 D01-D32

All D01-D32 values are visible in stable business-column order. Labels, grouped discount columns, numeric alignment, two-decimal monetary formatting, dates, counters, and remarks are consistent. No value is clipped, overlapped, converted to scientific notation, or ambiguously split. All one-row cases remain readable.

Case 004's two rows and case 005's three rows remain contiguous, chronological, aligned under the same headers, and contained on one page without row splitting. No misleading blank row or repeated header interrupts either case.

### 10.4 R01-R12

R01-R12 are presented on the separate `Validation` sheet with explicit expected, calculated, difference, row-count, and PASS fields. The values agree with the E-1 detail rows and the deterministic isolated-execution result. Every reconciliation difference is zero. No contradictory total was observed.

### 10.5 General presentation

The case identifier, workbook identifier, report title, reporting period, and synthetic boundary are visible without consulting JSONL. Black borders and text remain legible against the white background. No layout depends on hidden rows, columns, sheets, formulas, external links, or external connections.

## 11. Print And Pagination

| Case shape | Print area | Orientation | PDF pages | Result |
| --- | --- | --- | ---: | --- |
| One Annex row | `A1:AF17` | Landscape, fit to one page wide | 1 each | PASS |
| Case 004, two Annex rows | `A1:AF18` | Landscape, fit to one page wide | 1 | PASS |
| Case 005, three Annex rows | `A1:AF19` | Landscape, fit to one page wide | 1 | PASS |

Every required field is inside the print area. There are no unexpected blank pages, clipped page boundaries, split Annex rows, diagnostic sheets in the PDF, or fields outside the page. The internal-test warning remains visible in print output. The compact 32-column layout is dense but readable in Excel and PDF at normal review zoom; no one-page requirement was inferred beyond the generator's own configured posture.

## 12. Validation And Fact Links

All 19 workbooks contain distinct `Validation` and `Fact Links` sheets after `E-1`. Structural readback confirms their values agree with the E-1 rows, expected-value matrix, isolated execution result, fact identities, source ordering, and semantic commitments.

The following layout variations were visually inspected:

- standard/statutory/finality case 001;
- two-row boundary case 004;
- three-row monthly case 005;
- exact replay case 012;
- correction/conflict case 013;
- retry/finality-boundary case 024.

`Fact Links` preserves stable columns and one row per linked fact. `Validation` exposes H01-H10, D01-D32, R01-R12, expected/calculated values, row counts, differences, and PASS outcomes. Both sheets carry the internal-test warning and are clearly diagnostic rather than BIR submission pages.

Long UUID, hash, and diagnostic field names exceed the default visible cell width in these diagnostic sheets. Full values remain present and can be read by selecting the cell or using Excel's formula bar; structural readback verifies every value. This is recorded as a non-blocking presentation issue because no value is missing, the diagnostic sheets are not the printable E-1 evidence, and the primary E-1 presentation remains independently understandable.

## 13. Safety Results

| Check | Result |
| --- | --- |
| DocumentFormat.OpenXml schema/package validation | PASS: 19 of 19 |
| Required parts, relationships, content types, worksheet ordering | PASS |
| Formulas and volatile formulas | PASS: none |
| Macros | PASS: none |
| External links | PASS: none |
| External data connections | PASS: none |
| Hidden sheets | PASS: none |
| Embedded executable or external payloads | PASS: none |
| Unexpected metadata or renderer repair warning | PASS: none |
| Real customer data or secrets | PASS: none found |
| Existing negative workbook tests | PASS: 15 of 15 |
| Runtime tests | PASS: 209 passed, 0 failed, 0 skipped |
| Two-run byte determinism | PASS |

## 14. Defect Inventory

| ID | Affected cases | Location | Expected | Observed | Severity | Recommended bounded action |
| --- | --- | --- | --- | --- | --- | --- |
| `WPRR-01` | All 19 | `Validation` and `Fact Links`, default Excel view | Long UUIDs, hashes, and technical column names remain inspectable. | Full values are stored and selectable, but default column widths truncate portions of long diagnostic text. No semantic value is missing or contradictory. | `NON_BLOCKING_PRESENTATION` | Consider wider columns, frozen identity columns, or an optional technical-review view only in a separately approved workbook-presentation refinement. Do not delay Controlled UAT authorization for this ergonomic issue. |
| `WPRR-02` | All 19 | `E-1` rendered table | The complete 32-column report remains readable on the configured page. | The one-page landscape layout is dense, but labels and values remain readable in Excel and PDF with no clipping or overlap. | `OBSERVATION` | Review at normal PDF zoom or in Excel. Do not infer official BIR print acceptance from this internal result. |

No `BLOCKING_CONTROLLED_UAT`, `BLOCKING_BIR_PRESENTATION_ONLY`, or `COSMETIC` defect was found.

## 15. BIR Presentation Boundary

| Presentation item | Classification | Revalidation result |
| --- | --- | --- |
| Exact official sheet title | `GOVERNED_AND_SATISFIED` | `E-1`, `ANNEX E-1`, and `BIR SALES SUMMARY REPORT` render consistently. |
| Bounded internal visual layout | `GOVERNED_AND_SATISFIED` | Header, detail, warning, borders, and ordering render without defect. |
| Page dimensions and orientation | `GOVERNED_AND_SATISFIED` | Configured page size, landscape orientation, and one-page-wide scaling render consistently. |
| Margins | `GOVERNED_AND_SATISFIED` | No print clipping or blank spill page occurs. |
| Fonts | `GOVERNED_AND_SATISFIED` | Excel and PDF output render text consistently without substitution warning. |
| Internal print sequence | `GOVERNED_AND_SATISFIED` | Stable case and row ordering; one E-1 page per case. |
| Signature or certification areas | `NOT_GOVERNED` | No approved exact official block was found. |
| Footer and page numbering | `NOT_GOVERNED` | No approved mandatory contract was found. |
| Official submission filename | `NOT_GOVERNED` | Repository recommendations are not BIR approval. |
| Physical or electronic delivery format | `NOT_GOVERNED` | Delivery channel, portal, and mandatory external format remain unresolved. |
| Approval, signing, and evidence acceptance | `NOT_GOVERNED` | External authority and acceptance workflow remain separate. |
| Retention requirements | `NOT_GOVERNED` | No new retention posture is authorized by this review. |

The generated workbooks remain synthetic internal evidence and are not approved for official BIR submission. A future BIR-facing presentation or submission workstream may resolve external requirements independently; it is not required before a bounded technical Controlled UAT authorization task.

## 16. Controlled UAT Suitability

The corrected internal workbooks are suitable for a separate Controlled UAT authorization task. A reviewer can open each workbook normally, identify the case and reporting period, inspect every Annex row, compare R01-R12 results and fact links, print or export the E-1 evidence, and distinguish the artifact from an official BIR submission.

This suitability result does not itself authorize execution. Controlled UAT must remain separately approved, invocation-bounded, synthetic-only, isolated from shared and Production infrastructure, and explicit about evidence acceptance and disposal.

## 17. Validation Summary

| Validation | Result |
| --- | --- |
| PowerShell 5.1 parsing | PASS |
| v1.7 offline validator | PASS |
| v1.7 isolated execution | PASS: two clean deterministic runs |
| Corrected workbook generation | PASS: 19 workbooks / 22 Annex rows |
| Corrected manifest and workbook hashes | PASS |
| DocumentFormat.OpenXml standards validation | PASS |
| Workbook negative tests | PASS: 15 of 15 |
| Runtime tests | PASS: 209 of 209 |
| Excel normal open | PASS: 19 of 19 |
| Official source workbook renderer control | PASS |
| E-1 PDF export | PASS: 19 of 19 |
| PDF page count | PASS: 1 per case, 19 total |
| E-1 visual inspection | PASS: 19 pages / 22 rows |
| Representative diagnostic-sheet visual inspection | PASS with `WPRR-01` noted |
| Warning visibility and print inclusion | PASS: 19 of 19 |
| H01-H10, D01-D32, and R01-R12 | PASS |
| Formula, macro, link, connection, hidden-sheet, executable scans | PASS |
| Historical v1.0-v1.7 immutability | PASS |
| Two-run determinism | PASS |
| Changed-path and scope boundary | PASS |

## 18. Resources And Safety

The review used one task-owned temporary root containing the generator output, 19 E-1 PDFs, 19 E-1 page images, representative diagnostic PDFs and images, and renderer probes. The generator created invocation-owned temporary directories and three uniquely named disposable PostgreSQL execution sets. The generator removed its databases, containers, volumes, networks, and internal temporary directory. The review removes all retained workbooks, manifests, PDFs, images, profiles, probes, temporary scripts, and the outer task directory after evidence capture.

No shared, development, UAT, Production, `exitpass_v12_dev`, HikCentral, BIR, or other external business service was accessed. The official source workbook was opened read-only and not changed. No generated workbook, PDF, image, report, or manifest is committed.

## 19. Residual Risks

- The installed Excel engine is the only independent spreadsheet engine used. A second engine is not required by the review boundary.
- Diagnostic UUIDs, hashes, and long field names require horizontal scrolling or cell selection for full display at default widths.
- The internal E-1 page is intentionally compact. This review establishes internal technical readability, not external BIR presentation acceptance.
- Official submission, delivery, signing, acceptance, and retention requirements remain unresolved and must not be inferred from Controlled UAT readiness.

## 20. Exact Next Bounded Task

**Prepare Annex E-1 Controlled UAT execution authorization.**

The next task should define the exact synthetic case set, isolated environment, named roles, execution command, evidence manifest, acceptance criteria, failure handling, cleanup, and approval boundary. It must not begin execution within the authorization-preparation task and must continue to exclude official BIR submission, external delivery, shared environments, and Production use unless separately authorized.
