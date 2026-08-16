# Annex E-1 Synthetic UAT Dataset v1.6

This directory contains the exact offline JSONL dataset derived from the corrected v1.6 specification package. It is synthetic and is not an environment seed, database load, UAT execution input, workbook, or external-delivery artifact.

Validate the committed dataset from the repository root with Windows PowerShell 5.1:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1SyntheticUatDatasetV16Validation.ps1
```

The command validates package-member commitments, the specification package root, 2,364 deterministic identities, all 1,690 semantic rows across 29 families, nineteen case-package commitments, C02 chronology, Electronic Journal transitions, statutory finality, Annex coverage, reconciliations, and the acyclic hash graph. It returns a nonzero exit code on any failure and performs no network, database, container, application, workbook, or external-service operation.

Dataset regeneration is deterministic and is reserved for maintaining this source-controlled artifact:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Invoke-AnnexE1SyntheticUatDatasetV16Validation.ps1 -GenerateDataset
```
