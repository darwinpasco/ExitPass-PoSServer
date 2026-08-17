# Annex E-1 Synthetic UAT Dataset v1.7

`annex-e1-synthetic-uat-dataset-v1.7.jsonl` is the corrected machine-readable implementation of the v1.7 package. It contains one metadata record, 2,364 deterministic identity records, 1,690 semantic-instance records, and 19 case-package records in UTF-8 JSON Lines with LF terminators.

Validate the committed bytes offline:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1SyntheticUatDatasetV17Validation.ps1
```

Run the invocation-owned PostgreSQL load and execution harness:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1V17IsolatedExecution.ps1
```

The harness permits only loopback access to uniquely named disposable PostgreSQL resources. It stages all 4,074 records, maps all 29 semantic families, loads the canonical scope and Accounting-fact subset needed for current application behavior, executes all 19 included cases, compares two clean normalized reports, runs corruption and missing-reference tests, and removes every resource it creates.

Generate and validate deterministic internal test workbooks after the offline and isolated execution checks:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1V17WorkbookGeneration.ps1
```

The runner creates 19 invocation-owned `.xlsx` files containing all 22 authorized Annex rows, validates H01-H10, D01-D32, R01-R12, identities, and fact links through independent Open XML readback, and compares two clean generations byte-for-byte. It also applies `DocumentFormat.OpenXml` 3.5.1 schema validation and an Excel interoperability profile to every generated workbook. Its normalized manifest distinguishes each generated XLSX artifact SHA-256 from the authorized AE1H workbook-content commitment. All temporary workbooks, reports, directories, and disposable database resources are removed after validation. A caller may supply a new `-OutputDirectory` solely for internal inspection.

The corrected generator removes the incomplete optional `<fileVersion appName="xl"/>` element emitted by the existing runtime renderer. Microsoft Excel 16.0 build 20228 rejected packages containing that element with `0x800A03EC`; removing only that element restored normal open behavior. The regression validator rejects the same malformed element profile with an exact part, element, rule, path, and description.

Every printable `E-1` sheet now contains exactly one visible `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION` warning at `A11`, merged through `AF11` and included in the configured print area. The corrected aggregate workbook-manifest SHA-256 is `d965ec39286ea3d92fc32e64a968ed014f0486a8698819ae6caff2b95d7d72b5`; the corrected manifest-file SHA-256 is `b2961b498ec7b6654366eb5ef30f99cde5def62326f52249f05cedf09405217d`. The v1.7 package root, dataset bytes, semantic values, authorized workbook-content commitments, and normalized semantic hashes are unchanged.

The schema-first internal workbook is not an official BIR presentation template or accepted evidence package. This correction requires another independent rendering and presentation review before Controlled UAT authorization can be considered. This workflow does not authorize shared database loading, Controlled UAT, external delivery, evidence acceptance, BIR submission, or Production use.
