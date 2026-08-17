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

The runner creates 19 invocation-owned `.xlsx` files containing all 22 authorized Annex rows, validates H01-H10, D01-D32, R01-R12, identities, and fact links through independent Open XML readback, and compares two clean generations byte-for-byte. Its normalized manifest distinguishes each generated XLSX artifact SHA-256 from the authorized AE1H workbook-content commitment. All temporary workbooks, reports, directories, and disposable database resources are removed after validation. A caller may supply a new `-OutputDirectory` solely for internal inspection.

Every workbook is marked `SYNTHETIC / INTERNAL TEST ONLY / NOT FOR BIR SUBMISSION`. The schema-first internal workbook is not an official BIR presentation template or accepted evidence package. This workflow does not authorize shared database loading, Controlled UAT, external delivery, evidence acceptance, BIR submission, or Production use.
