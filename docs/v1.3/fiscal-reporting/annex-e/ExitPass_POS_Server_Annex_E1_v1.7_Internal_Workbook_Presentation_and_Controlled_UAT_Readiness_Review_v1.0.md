# ExitPass POS Server Annex E-1 v1.7 Internal Workbook Presentation and Controlled UAT Readiness Review v1.0

## 1. Decision

**Decision: `BLOCKED_WORKBOOK_RENDERING`.**

The semantic dataset, isolated execution, deterministic workbook generation, workbook commitments, and repository tests pass. The generated `.xlsx` artifacts do not pass the independent spreadsheet-engine boundary: Microsoft Excel rejects all 19 workbooks before a sheet can be rendered. The printable `E-1` sheet in every workbook also omits the required `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` marking; the marking exists only on the `Validation` and `Fact Links` sheets.

These are concrete `BLOCKING_CONTROLLED_UAT` defects. The internal workbooks are not suitable as Controlled UAT evidence in their current form, and a Controlled UAT authorization task must not proceed yet.

This decision is not a finding that a new BIR-facing presentation specification is required before technical Controlled UAT. Repository sources already govern a bounded internal E-1 physical profile, while official electronic submission, filename, delivery, signing, and acceptance details remain unresolved external matters. Those external matters may remain a later independent workstream after spreadsheet interoperability and printed internal-test marking are corrected.

## 2. Purpose And Scope

This review independently evaluates generated v1.7 internal-test workbooks for spreadsheet-engine interoperability, Controlled UAT usability, print posture, diagnostic-sheet separation, and the boundary between internal evidence and official BIR submission presentation.

The review does not change or redesign the generator, dataset, package, runtime, API, database, migration, controlled codes, dependencies, CI, or Production configuration. It does not begin Controlled UAT, accept evidence, deliver a workbook, contact BIR, or authorize Production use.

## 3. Baseline And Provenance

| Item | Value |
| --- | --- |
| Repository | `D:\SourceCodes\ExitPass-PoSServer` |
| Base branch | `dev` |
| Review branch | `docs/annex-e1-v1-7-workbook-presentation-review` |
| Review worktree | `D:\wt\AnnexE1V17WorkbookPresentationReview` |
| Exact `origin/dev` baseline | `d850e5f1dd2a4180ef8f559091bec7502b2568da` |
| Workbook-generator merge commit | `d850e5f1dd2a4180ef8f559091bec7502b2568da` |
| Merge parents | `609787e430fe2cf5facbb6ba6d1453ca44994d70`, `e8a2138347d65e8d3d0d435bca9692d15552a96e` |
| Workbook-generator content commit | `e8a2138347d65e8d3d0d435bca9692d15552a96e` |
| Merge provenance | PR `#119`, `feature/annex-e1-v1-7-workbook-generator` |
| Merge base | `d850e5f1dd2a4180ef8f559091bec7502b2568da` |
| Review branch divergence at creation | `0 ahead / 0 behind` |

The prior generator worktree and local feature branch were absent. Unrelated registered worktrees were not changed.

## 4. Reviewed Commitments

| Commitment | Expected | Result |
| --- | --- | --- |
| v1.7 package root | `5d60cb50d7f43b29f79486801a470e57128e2b571c4e50a37494e4a82131b829` | PASS |
| Dataset records | `4,074` | PASS |
| Dataset canonical LF bytes | `8,070,439` | PASS |
| Dataset canonical LF SHA-256 | `ed6971df7a142a54492eea968587a150c0229618650cee56ea2116154692a1ac` | PASS |
| Included cases | `19` | PASS |
| Annex rows | `22` | PASS |
| Isolated execution normalized SHA-256 | `eb49cc8f3ba5a2935463e030f7e2b1707131aff68bcfd1d37c4b6ea1e224a18e` | PASS |
| Aggregate workbook manifest SHA-256 | `45789e30e0af1b207dffbe1bce667d2cb65e44ff0a6a825e371eb7dd413cbc03` | PASS |
| Manifest-file SHA-256 | `45ba775bc01fa069303f9a65d4d2f4859a7a87964bc51bbbe4674d025dfe89ed` | PASS |

The Windows checkout contains CRLF working-tree bytes for the JSONL. The existing validator canonicalized CRLF to the governed LF form before checking the frozen dataset length and digest. No dataset byte was changed.

## 5. Review Tools And Method

The independent spreadsheet engine was Microsoft Excel `16.0`, build `20228` (`EXCEL.EXE` product version `16.0.20228.20190`). Automation used hidden, read-only COM access with alerts, link updates, events, and macros disabled. No interactive desktop workflow was required.

Renderer controls established that the engine itself was functional:

1. Excel created, saved, closed, and reopened a temporary control workbook.
2. Excel opened the repository-documented source workbook `D:\Docs\ExitPass\POS\RMO 24-2023 Annex E-1 to E-5.xlsx` read-only and exported its `E-1` sheet to a 203,589-byte control PDF. That source was 25,703 bytes with SHA-256 `7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197`. The temporary PDF was removed.
3. A generated workbook failed from both its original path and a short temporary path.
4. Excel normal open, repair mode, and extract-data mode all failed for the generated workbook.
5. Removing the `Validation` and `Fact Links` parts from a temporary copy did not make the core `E-1` workbook openable.
6. All 19 generated workbooks were then attempted independently and failed with the same COM HRESULT `0x800A03EC` at `Workbooks.Open`.

The failed open occurred before render export. Accordingly, this review makes no visual-layout or pagination approval from Open XML inspection alone.

## 6. Exact Commands

Generation command:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1V17WorkbookGeneration.ps1 -OutputDirectory "<invocation-owned-empty-directory>"
```

Independent renderer pattern:

```powershell
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$excel.AutomationSecurity = 3
$workbook = $excel.Workbooks.Open("<generated-workbook>", 0, $true)
```

Relevant runtime suite:

```powershell
dotnet test .\tests\ExitPass.PosServer.Runtime.Tests\ExitPass.PosServer.Runtime.Tests.csproj --no-restore --configuration Release --verbosity minimal
```

## 7. Generated Workbook Inventory

All byte lengths, artifact digests, normalized semantic digests, case identities, workbook identities, and row counts matched the generated deterministic manifest. The final column records the independent Excel result.

| Case | Annex rows | Bytes | Workbook SHA-256 | Normalized semantic SHA-256 | Excel result |
| --- | ---: | ---: | --- | --- | --- |
| `DS-AE1-001` | 1 | 44,329 | `d4f8d3e0588a92a66553d98509a0f25907570ce8e1cbe41016609a78f5929666` | `b22ceec4abbd98c6e45c0e41c03ee8fea2f79208585fa50010c04d0f7a0a7bb2` | FAIL `0x800A03EC` |
| `DS-AE1-003` | 1 | 44,269 | `8577ae41cb3de01aeebd04f58881d354a9b5a9f28b4b0a7d8cdf295b98f531f3` | `a68e508cd2e3fd4fdb8a1b03c012a3b2045d3e7a1f79e451c11bd045ab64b7a3` | FAIL `0x800A03EC` |
| `DS-AE1-004` | 2 | 56,893 | `1eaf2a974866b7337cf095b5a948e60b10839000ab4336976a1d8a555889c4bf` | `cc6b29f65a5e2078839e5dd2543e1f7b7963ccf2f6ce502ba2b2d6a62350ec97` | FAIL `0x800A03EC` |
| `DS-AE1-005` | 3 | 69,521 | `e674c7d24864d84e671f2017c1033b8e065504b7cf7569066143556ea2ad1903` | `3407b3795571afe457c421fbc691c4e79180c209795ea3745aeeb475a7ed08c2` | FAIL `0x800A03EC` |
| `DS-AE1-007` | 1 | 44,329 | `fdd9293c9bfa06e2063baaf57f64efafff32caf26e681355e8a1168a7bbbe9dd` | `9f476e95ef33079decd707dda03516d8a2028643d42d33008309985537f33604` | FAIL `0x800A03EC` |
| `DS-AE1-009` | 1 | 44,275 | `b6f949b586b1dc05ce88981390347278c276a848c1b18c6d663fdc0ca48e209c` | `ba3fa23e177435060ccc1a3878bf049018e795e2eea5ad7a2303dcf16b18d0e9` | FAIL `0x800A03EC` |
| `DS-AE1-010` | 1 | 44,329 | `a54e6730f72bbec5342cfc545d24f545ccf64a9e506dd273a3d2b13d908ec86a` | `c60c7ca5482c482e643677daa8c2344773bc5ef0d67fee58bf6d45440bd1eaab` | FAIL `0x800A03EC` |
| `DS-AE1-011` | 1 | 44,329 | `e0b27585c104b8f14b7ffe5710d7dc4a37c54d2e81efdb1e577c5063910e41a7` | `6f98e4719bdc66b343ed9fdaeec7c0c2f3c333c967ae3d286cc764fc9fe231a6` | FAIL `0x800A03EC` |
| `DS-AE1-012` | 1 | 44,329 | `3f5bcb0cc5759bbd08ceadd822979f5f91b799a532dcd81c14f14d5600a14354` | `fe23fc9aa7246f7a29b4e8c841ff653777e6f4d086f3c5de206f3736e624aac1` | FAIL `0x800A03EC` |
| `DS-AE1-013` | 1 | 44,329 | `127f42effe1ecd7c44ed2278f0c11975bcd37938b82062cf80adf036f4ce28a0` | `dd6460cd1be100dadbce9ad184439ef25ba90c199a02e33bf6dccbd3d6185bad` | FAIL `0x800A03EC` |
| `DS-AE1-014` | 1 | 44,329 | `0ba889b075cbb2ad8d2d2ebb0c2beffae6d714fef443c6bf44903b1061131d92` | `bc4d4e95792472536da2229afa2dc610cbdf4d31bd57a6c395383b0a96501a64` | FAIL `0x800A03EC` |
| `DS-AE1-016` | 1 | 44,329 | `7ba2e48dbf4a1771fd620b49571b1f36df7accc1cb434cc60a64c36eb2334609` | `704a70cb44bcee7d4938751ce815db259eef5d030514778b1106fcf3ecd3d66e` | FAIL `0x800A03EC` |
| `DS-AE1-017` | 1 | 44,329 | `60ba54e65be8cc08a84cf90017cefe7b9cc99def180895549bcfb17561d26cad` | `14573397397dee5e6319881277ba1f4121f003524f718434a8dac5eb785677a8` | FAIL `0x800A03EC` |
| `DS-AE1-018` | 1 | 44,329 | `69e5c538c0e815adea26ecc1d8da25f85fad76ea82d497ef7335e72b5285e55c` | `274a581fab3c97c862943cce35704f1f3e608641d065aba29ca7df1a8d4558d5` | FAIL `0x800A03EC` |
| `DS-AE1-019` | 1 | 44,329 | `6ba321e09310bf885a118a0c2dfbbe8cdf66e2ebc374c9e65dd26c4b4adb6ebb` | `7a1d269a7ad9547c7e35f84df956f2ac923bfbac4f266b79ff69928937963996` | FAIL `0x800A03EC` |
| `DS-AE1-020` | 1 | 44,329 | `7004b1e90a62cf7f65733025d9b2737d83ce4ede874c582fab7ad31b48ad6aa5` | `7a926b4cc54f907e9220d15542bbf4602d33372db3c2ca1fdf155b1c4f126050` | FAIL `0x800A03EC` |
| `DS-AE1-021` | 1 | 44,329 | `d4a38cceb5ca44978a0f75c911096b7875fc98eabf8b45b0ca87b7133000f47c` | `4d50b8ab0558c70807a40a9b51a97814b4dbe351233e0da1d66506bb10777b2d` | FAIL `0x800A03EC` |
| `DS-AE1-023` | 1 | 44,329 | `bff2b979d40283416a6976cc43c1e0c8656686cbcaad955ca89ab28f8e2300a0` | `d7715a7230d1a4cf0631284f6a7040bbc4f4cd64be5d98287c01c6af214a41e6` | FAIL `0x800A03EC` |
| `DS-AE1-024` | 1 | 44,329 | `66caaa1c2be8c602f8e2d99b3711c54dd16f0b409addf195ee19122680dc10ab` | `eadf2f7f67cca5575a99cb7c40e169856041b7a65bc7e338a5b6b05a7f75155d` | FAIL `0x800A03EC` |

## 8. Structural Results

| Check | Result |
| --- | --- |
| Workbook files | PASS: 19 |
| Annex rows | PASS: 22 |
| Manifest byte lengths and workbook SHA-256 values | PASS: 19 of 19 |
| Stable filenames and case identities | PASS |
| Sheet order | PASS: `E-1`, `Validation`, `Fact Links` |
| Hidden or unexpected sheets | PASS: none |
| Workbook formulas | PASS: none |
| External links | PASS: none |
| Data connections | PASS: none |
| Macro payloads | PASS: none |
| Unexpected ZIP members | PASS: none |
| XML well-formedness | PASS for every XML and relationship part |
| `E-1` print area | Present: `A1:AF17`, `A1:AF18`, or `A1:AF19` by row count |
| `E-1` page posture | Landscape, fit to one page wide |
| `E-1` internal-test marking | FAIL: absent in 19 of 19 printable sheets |
| `Validation` internal-test marking | PASS: present in 19 of 19 |
| `Fact Links` internal-test marking | PASS: present in 19 of 19 |
| Independent spreadsheet open | FAIL: 0 of 19 |

The structural checks establish that workbook content commitments and safety properties are internally consistent. They do not substitute for a real spreadsheet-engine open and render.

## 9. Visual, Print, And Pagination Results

No generated workbook reached a renderable sheet. Therefore the following required checks are blocked rather than passed:

- visibility and association of H01-H10;
- readability of D01-D32 and R01-R12;
- truncation, wrapping, overlap, scientific notation, negative-number formatting, and multi-row layout;
- per-case visual review, including cases 004 and 005;
- PDF/page output, page breaks, clipping, repeated headers, and blank-page checks;
- representative visual review of `Validation` and `Fact Links`;
- renderer warnings, repairs, or cross-engine differences.

Open XML inspection confirms a print area and page setup but cannot establish visual suitability. No PDF or image was produced from a generated workbook.

## 10. Validation And Fact Links Assessment

The diagnostic sheets are structurally present, separate from `E-1`, ordered after it, and carry the required internal-test warning. Their values passed the existing deterministic generator readback and the v1.7 semantic validations. No formulas, links, connections, macros, or hidden sheets were found.

Visual clarity, column sizing, wrapping, and technical-review usability remain unassessed because Excel rejected the workbook package before any sheet could render. The diagnostic sheets are not approved as Controlled UAT evidence by this review.

## 11. BIR Presentation Governance

The governing repository evidence is the source and applicability assessment, field dictionary, file-layout specification, approved calculation profile, and the locally documented source workbook. The matrix does not invent requirements absent from those sources.

| Presentation item | Classification | Evidence and result |
| --- | --- | --- |
| Exact official sheet title | `GOVERNED_AND_SATISFIED` | The E-1 sheet and `BIR SALES SUMMARY REPORT` title are structurally present. |
| Mandatory visual layout | `GOVERNED_BUT_NOT_SATISFIED` | The local source workbook governs a bounded physical profile, but the generated package cannot be opened or rendered. |
| Page dimensions | `GOVERNED_BUT_NOT_SATISFIED` | Paper size and print area are encoded, but rendered dimensions cannot be verified. |
| Orientation | `GOVERNED_AND_SATISFIED` | Landscape is encoded consistently in all 19 workbooks. |
| Margins | `GOVERNED_AND_SATISFIED` | Fixed margins are encoded consistently and trace to the bounded renderer profile. |
| Fonts | `GOVERNED_BUT_NOT_SATISFIED` | Font records are encoded, but actual spreadsheet rendering cannot be verified. |
| Signature or certification areas | `NOT_GOVERNED` | No approved exact signature or certification layout was found. |
| Footer and page numbering | `NOT_GOVERNED` | No approved mandatory footer or page-numbering contract was found. |
| Print sequence | `GOVERNED_BUT_NOT_SATISFIED` | Stable workbook and row order pass structurally; printable output cannot be reviewed. |
| Submission filename | `NOT_GOVERNED` | The repository records only a project recommendation; BIR prescription remains unresolved. |
| Physical or electronic delivery format | `NOT_GOVERNED` | Electronic submission, portal, physical delivery, signing, encryption, and mandatory format remain unresolved. |
| Approval and signing workflow | `NOT_GOVERNED` | External acceptance and signing authority remain unresolved. |

The absence of official submission filename, delivery, signing, and acceptance rules does not itself block technical Controlled UAT. It continues to block describing an internal workbook as BIR-approved or externally deliverable.

## 12. Defect Inventory

| ID | Affected cases | Workbook/sheet/location | Expected | Observed | Severity | Bounded correction |
| --- | --- | --- | --- | --- | --- | --- |
| `WPR-01` | All 19 | XLSX package open boundary, before active sheet | Excel opens each workbook read-only without repair or compatibility error. | Normal, repair, and extract-data open modes fail. All 19 return HRESULT `0x800A03EC`. Excel control workbooks and the repository-documented source template open successfully. | `BLOCKING_CONTROLLED_UAT` | Correct the deterministic Open XML writer so the same committed-content workbooks open without repair in Excel. Add an independent real-engine open test or an equivalent package-validation gate. Do not change dataset values. |
| `WPR-02` | All 19 | `E-1`, print areas `A1:AF17`, `A1:AF18`, or `A1:AF19` | `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` remains visible on printed E-1 evidence. | The exact warning is absent from `E-1` in 19 of 19 workbooks and appears only at `Validation!A1` and `Fact Links!A1`. | `BLOCKING_CONTROLLED_UAT` | Place the exact internal-test warning in the printable E-1 presentation without altering governed values or implying official submission approval. |
| `WPR-03` | All 19 | Visual and print review | Each workbook has an inspected rendering record. | Review is not possible downstream of `WPR-01`; no generated PDF or image exists. | `OBSERVATION` | Rerun this bounded independent review after `WPR-01` and `WPR-02` are corrected. |

No `BLOCKING_BIR_PRESENTATION_ONLY`, `NON_BLOCKING_PRESENTATION`, or `COSMETIC` finding was established because the generated artifacts never rendered.

## 13. Controlled UAT Suitability

**Not suitable.** Semantic correctness and deterministic generation remain verified, but a UAT reviewer cannot open or render the workbook artifacts in the available real spreadsheet engine. Printed E-1 output would also lack the required internal-test boundary if rendering succeeded. These failures prevent the artifacts from functioning as controlled evidence.

The decision does not invalidate the v1.7 package, JSONL dataset, isolated execution result, semantic commitments, or workbook manifest commitments. It identifies a presentation-container interoperability and evidence-labeling defect in the generated workbook layer.

## 14. Official BIR Submission Boundary

The workbooks remain synthetic internal artifacts and are not approved for BIR submission. Official electronic format, filename, delivery channel, signing, encryption, presentation acceptance, and external evidence acceptance remain unresolved. No BIR-facing workbook was generated, delivered, accepted, or submitted.

A separately governed BIR submission presentation workstream may proceed later when external authority is available. It is not required to define new technical Controlled UAT bytes before the two concrete internal-workbook defects are corrected.

## 15. Validation Summary

| Validation | Result |
| --- | --- |
| PowerShell 5.1 parsing | PASS |
| v1.7 offline validator | PASS |
| v1.7 isolated execution | PASS, two clean deterministic runs |
| Deterministic workbook generator | PASS under repository structural tests |
| Workbook manifest and artifact hashes | PASS |
| Workbook negative tests | PASS: 12 required corruptions rejected |
| Runtime tests | PASS: 209 passed, 0 failed, 0 skipped |
| Independent Excel control workbook | PASS |
| Independent official source-template open | PASS |
| Independent official source-template PDF render | PASS |
| Independent generated-workbook open | FAIL: 0 of 19 |
| Visual review | BLOCKED by generated-workbook open failure |
| Print and pagination review | BLOCKED by generated-workbook open failure |
| Open XML safety scan | PASS |
| Historical v1.0-v1.7 immutability | PASS |
| Scope and changed-path boundary | PASS |

## 16. Resource And Safety Record

The generator created invocation-owned temporary directories and three isolated execution runs, each with a uniquely named PostgreSQL container, database, volume, and network. The runner removed those execution resources. The review created one output directory containing 19 workbooks and one manifest, one empty render-output directory, temporary renderer probes, and transient Excel processes. All review-owned files, directories, and processes are removed after evidence capture.

No shared, development, UAT, Production, `exitpass_v12_dev`, HikCentral, or BIR service was accessed. The documented source workbook was opened read-only as an independent renderer control and was not changed.

## 17. Residual Risks

- The exact Open XML element or package rule rejected by Excel is not isolated in this documentation-only review; source correction belongs to the next bounded implementation task.
- Visual defects beyond the two established blockers may appear after package interoperability is fixed.
- A single installed independent renderer was used. A second engine is not required, but later cross-engine evidence would reduce presentation risk.
- Official BIR presentation and delivery acceptance remain externally unresolved and must not be inferred from a later technical UAT result.

## 18. Exact Next Bounded Task

**Correct Annex E-1 v1.7 internal workbook spreadsheet interoperability and printable internal-test marking.**

That task must preserve the v1.7 dataset and semantic commitments, change only the workbook-generation layer and focused tests, prove all 19 workbooks open without repair in a real spreadsheet engine, keep the exact internal-test warning visible in every printed E-1 output, and then request a fresh independent presentation review. It must not begin Controlled UAT or create an official BIR submission template.

## 19. Manual Revalidation Procedure

After the bounded correction is merged, a local reviewer should:

1. Generate the 19 workbooks into a new empty task-owned directory using the documented generator command.
2. Open the repository-documented source workbook read-only as a renderer control.
3. Open every generated workbook read-only in Excel without repair, conversion, or compatibility warnings.
4. Export every `E-1` sheet to PDF and verify all one-row cases plus cases 004 and 005.
5. Confirm the exact internal-test warning is visible on every printed E-1 page.
6. Inspect representative `Validation` and `Fact Links` sheets and compare all values with the deterministic manifest.
7. Delete the generated workbooks, PDFs, images, reports, and task-owned temporary resources.
