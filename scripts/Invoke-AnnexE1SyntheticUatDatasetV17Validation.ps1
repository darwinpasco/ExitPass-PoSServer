[CmdletBinding()]
param(
    [string] $DatasetPath,
    [switch] $GenerateDataset,
    [switch] $FunctionsOnly
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

function Assert-Condition {
    param($Condition, [string] $Message)
    if ($Condition -is [Array]) {
        throw "Assertion condition was not scalar: $Message"
    }
    if (-not $Condition) {
        throw $Message
    }
}

$script:Utf8 = New-Object System.Text.UTF8Encoding($false, $true)

function Join-Bytes {
    param([object[]] $Parts)
    $length = 0
    foreach ($part in $Parts) { $length += ([byte[]] $part).Length }
    $result = New-Object byte[] $length
    $offset = 0
    foreach ($part in $Parts) {
        $bytes = [byte[]] $part
        [Array]::Copy($bytes, 0, $result, $offset, $bytes.Length)
        $offset += $bytes.Length
    }
    return ,$result
}

function Get-U32Be {
    param([uint32] $Value)
    return ,([byte[]]@(
        (($Value -shr 24) -band 0xff),
        (($Value -shr 16) -band 0xff),
        (($Value -shr 8) -band 0xff),
        ($Value -band 0xff)
    ))
}

function Get-U64Be {
    param([uint64] $Value)
    $bytes = New-Object byte[] 8
    for ($i = 7; $i -ge 0; $i--) {
        $bytes[$i] = [byte]($Value -band 0xff)
        $Value = $Value -shr 8
    }
    return ,$bytes
}

function Convert-HexToBytes {
    param([string] $Hex)
    Assert-Condition (($Hex.Length % 2) -eq 0) "Hex value has odd length."
    $bytes = New-Object byte[] ($Hex.Length / 2)
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        $bytes[$i] = [Convert]::ToByte($Hex.Substring($i * 2, 2), 16)
    }
    return ,$bytes
}

function Convert-BytesToHex {
    param([byte[]] $Bytes)
    return ([BitConverter]::ToString($Bytes).Replace("-", "").ToLowerInvariant())
}

function Get-Sha256Hex {
    param([byte[]] $Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return Convert-BytesToHex ($sha.ComputeHash($Bytes)) }
    finally { $sha.Dispose() }
}

function Get-FileSha256Hex {
    param([string] $Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Compare-ByteArrays {
    param([byte[]] $Left, [byte[]] $Right)
    if ($Left.Length -ne $Right.Length) { return $false }
    for ($i = 0; $i -lt $Left.Length; $i++) {
        if ($Left[$i] -ne $Right[$i]) { return $false }
    }
    return $true
}

function Compare-ByteArraysLexically {
    param([byte[]] $Left, [byte[]] $Right)
    $length = [Math]::Min($Left.Length, $Right.Length)
    for ($i = 0; $i -lt $length; $i++) {
        if ($Left[$i] -lt $Right[$i]) { return -1 }
        if ($Left[$i] -gt $Right[$i]) { return 1 }
    }
    return $Left.Length.CompareTo($Right.Length)
}

function Get-Frame {
    param([string] $Type, $Value)

    if ($Type.StartsWith("null:")) {
        Assert-Condition ($null -eq $Value) "Null type '$Type' has a non-null value."
        return ,(Join-Bytes @([byte[]]@(0), (Get-U64Be 0)))
    }

    $tag = $null
    $payload = $null
    switch ($Type) {
        { $_ -in @("string", "date", "time") } {
            $tag = 1; $payload = $script:Utf8.GetBytes(([string]$Value).Normalize())
        }
        "bool" {
            $tag = 2; $payload = [byte[]]@($(if ([bool]$Value) { 1 } else { 0 }))
        }
        { $_ -in @("integer", "int32", "int64", "minor") } {
            $tag = 3; $payload = $script:Utf8.GetBytes(([Convert]::ToInt64($Value)).ToString([Globalization.CultureInfo]::InvariantCulture))
        }
        "decimal" {
            $tag = 4; $payload = $script:Utf8.GetBytes([string]$Value)
        }
        "timestamp" {
            $tag = 5; $payload = $script:Utf8.GetBytes([string]$Value)
        }
        "uuid" {
            $tag = 6; $payload = $script:Utf8.GetBytes(([Guid]$Value).ToString("D").ToLowerInvariant())
        }
        "code" {
            $tag = 7; $payload = $script:Utf8.GetBytes([string]$Value)
        }
        { $_ -in @("binary", "binary32") } {
            $tag = 8; $payload = Convert-HexToBytes ([string]$Value)
            if ($Type -eq "binary32") { Assert-Condition ($payload.Length -eq 32) "binary32 value is not 32 bytes." }
        }
        "object" {
            $tag = 9
            $members = @($Value)
            $parts = New-Object System.Collections.ArrayList
            [void]$parts.Add((Get-U32Be $members.Count))
            $seen = @{}
            foreach ($member in $members) {
                $name = [string]$member.n
                Assert-Condition (-not $seen.ContainsKey($name)) "Duplicate object member '$name'."
                $seen[$name] = $true
                $nameBytes = $script:Utf8.GetBytes($name.Normalize())
                [void]$parts.Add((Get-U32Be $nameBytes.Length))
                [void]$parts.Add($nameBytes)
                [void]$parts.Add((Get-Frame ([string]($member.t)) $member.v))
            }
            $payload = Join-Bytes $parts.ToArray()
        }
        "array" {
            $tag = 10
            $items = @($Value)
            $parts = New-Object System.Collections.ArrayList
            [void]$parts.Add((Get-U32Be $items.Count))
            foreach ($item in $items) { [void]$parts.Add((Get-Frame ([string]($item.t)) $item.v)) }
            $payload = Join-Bytes $parts.ToArray()
        }
        "set" {
            $tag = 11
            $encoded = @($Value | ForEach-Object { Get-Frame ([string]($_.t)) $_.v })
            [Array]::Sort($encoded, [Comparison[byte[]]]{ param($a, $b) Compare-ByteArraysLexically $a $b })
            for ($i = 1; $i -lt $encoded.Count; $i++) {
                Assert-Condition (-not (Compare-ByteArrays $encoded[$i - 1] $encoded[$i])) "Set contains a duplicate encoded value."
            }
            $parts = New-Object System.Collections.ArrayList
            [void]$parts.Add((Get-U32Be $encoded.Count))
            foreach ($item in $encoded) { [void]$parts.Add($item) }
            $payload = Join-Bytes $parts.ToArray()
        }
        default { throw "Unsupported AE1H type '$Type'." }
    }
    return ,(Join-Bytes @([byte[]]@([byte]$tag), (Get-U64Be $payload.Length), $payload))
}

function Get-Ae1hPreimage {
    param(
        [string] $Domain,
        [string] $Version,
        [string] $Family,
        [object[]] $RootMembers
    )
    $domainBytes = $script:Utf8.GetBytes($Domain.Normalize())
    $versionBytes = $script:Utf8.GetBytes($Version.Normalize())
    $familyBytes = $script:Utf8.GetBytes($Family.Normalize())
    return ,(Join-Bytes @(
        $script:Utf8.GetBytes("AE1H"),
        [byte[]]@(1),
        (Get-U32Be $domainBytes.Length), $domainBytes,
        (Get-U32Be $versionBytes.Length), $versionBytes,
        (Get-U32Be $familyBytes.Length), $familyBytes,
        (Get-Frame "object" $RootMembers)
    ))
}

function Test-Ae1hVectors {
    $scalarMembers = @(
        [pscustomobject][ordered]@{n='nullValue';t='null:string';v=$null},
        [pscustomobject][ordered]@{n='emptyString';t='string';v=''},
        [pscustomobject][ordered]@{n='boolean';t='bool';v=$true},
        [pscustomobject][ordered]@{n='integer';t='integer';v=-42},
        [pscustomobject][ordered]@{n='moneyMinorUnits';t='integer';v=11200},
        [pscustomobject][ordered]@{n='timestamp';t='timestamp';v='2026-09-10T01:00:00.0000000Z'},
        [pscustomobject][ordered]@{n='uuid';t='uuid';v='192219dd-a7c9-5a53-adb8-ee1342aff771'},
        [pscustomobject][ordered]@{n='code';t='code';v='SENIOR_CITIZEN'},
        [pscustomobject][ordered]@{n='binary';t='binary';v='0001feff'},
        [pscustomobject][ordered]@{n='escapedUnicode';t='string';v="SYN|SC`n$([char]0x00d1)"}
    )
    $v1 = Get-Ae1hPreimage 'annex-e1-source-row' 'sha256:v1.4' 'scalar-coverage' $scalarMembers
    Assert-Condition ($v1.Length -eq 391 -and (Get-Sha256Hex $v1) -eq '98114cfa0a0a163f2796255de0d1d42d0fcd720ccebb07266553e58dc0a45e49') 'AE1H vector V14-01 failed.'

    $v2 = Get-Ae1hPreimage 'annex-e1-source-row' 'sha256:v1.4' 'nested-coverage' @(
        [pscustomobject][ordered]@{n='child';t='object';v=@(
            [pscustomobject][ordered]@{n='name';t='string';v='alpha'},
            [pscustomobject][ordered]@{n='value';t='decimal';v='12.3400'}
        )},
        [pscustomobject][ordered]@{n='ordered';t='array';v=@(
            [pscustomobject][ordered]@{t='string';v='b'},
            [pscustomobject][ordered]@{t='string';v='a'}
        )},
        [pscustomobject][ordered]@{n='unordered';t='set';v=@(
            [pscustomobject][ordered]@{t='code';v='PWD'},
            [pscustomobject][ordered]@{t='code';v='SENIOR_CITIZEN'}
        )}
    )
    Assert-Condition ($v2.Length -eq 249 -and (Get-Sha256Hex $v2) -eq '2577237b1919bc01fcfcc049a36fcd92435e4de8bdfd2d80eb7d6197daa5f1e6') 'AE1H vector V14-02 failed.'

    $v3 = Get-Ae1hPreimage 'annex-e1-integrity-row' 'sha256:v1.4' 'chain-coverage' @(
        [pscustomobject][ordered]@{n='sequence';t='integer';v=2},
        [pscustomobject][ordered]@{n='semanticHash';t='binary32';v='0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20'},
        [pscustomobject][ordered]@{n='previousIntegrityHash';t='binary32';v='2122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f40'}
    )
    Assert-Condition ($v3.Length -eq 222 -and (Get-Sha256Hex $v3) -eq 'e83baf882bebdb9bdf889b84e1314e43daf819a640a328e29af594e21cb64732') 'AE1H vector V14-03 failed.'

    $v4 = Get-Ae1hPreimage 'annex-e1-source-row' 'sha256:v1.4' 'fiscal-document' @(
        [pscustomobject][ordered]@{n='rowId';t='uuid';v='192219dd-a7c9-5a53-adb8-ee1342aff771'},
        [pscustomobject][ordered]@{n='role';t='code';v='INPUT_SOURCE'},
        [pscustomobject][ordered]@{n='fields';t='object';v=$scalarMembers}
    )
    Assert-Condition ($v4.Length -eq 497 -and (Get-Sha256Hex $v4) -eq '7e2f0395f9bf3fb007984aae1ee4d1f95c5fd1fb2101a83aed698c89f726c74e') 'AE1H vector V14-04 failed.'
}

function Get-SourceRowPreimage {
    param($Row)
    $root = @(
        [pscustomobject][ordered]@{ n = "rowId"; t = "uuid"; v = $Row.instanceUuid },
        [pscustomobject][ordered]@{ n = "role"; t = "code"; v = $Row.role },
        [pscustomobject][ordered]@{ n = "fields"; t = "object"; v = @($Row.members) }
    )
    return ,(Get-Ae1hPreimage "annex-e1-source-row" "sha256:v1.4" $Row.familyToken $root)
}

function Get-AnnexCalculationPreimage {
    param($Row)
    $root = @(
        [pscustomobject][ordered]@{ n = "headers"; t = "object"; v = (Get-TopValue $Row "headers") },
        [pscustomobject][ordered]@{ n = "details"; t = "object"; v = (Get-TopValue $Row "details") },
        [pscustomobject][ordered]@{ n = "reconciliations"; t = "array"; v = (Get-TopValue $Row "reconciliations") }
    )
    return ,(Get-Ae1hPreimage "annex-e1-annex-calculation" "sha256:v1.4" "AE1-UAT-$($Row.scenario)" $root)
}

function Get-CorrectedWorkbookCalculationPreimage {
    param($WorkbookRow, [object[]] $AnnexRows)
    $hashes = @($AnnexRows | Sort-Object ordinal | ForEach-Object {
        [pscustomobject][ordered]@{ t = "binary32"; v = (Get-TopValue $_ "calculation_semantic_hash") }
    })
    $root = @(
        [pscustomobject][ordered]@{ n = "workbookId"; t = "uuid"; v = (Get-TopValue $WorkbookRow "id") },
        [pscustomobject][ordered]@{ n = "annexRowCalculationHashes"; t = "array"; v = $hashes }
    )
    return ,(Get-Ae1hPreimage "annex-e1-workbook-calculation" "sha256:v1.6" "AE1-UAT-$($WorkbookRow.scenario)" $root)
}

function Get-WorkbookContentPreimage {
    param($WorkbookRow, [object[]] $AnnexRows, [object[]] $FactLinkRows)
    $firstAnnex = @($AnnexRows | Sort-Object ordinal)[0]
    $annexHashes = @($AnnexRows | Sort-Object ordinal | ForEach-Object {
        [pscustomobject][ordered]@{ t = "binary32"; v = $_.expectedSemanticSha256 }
    })
    $factLinkHashes = @($FactLinkRows | Sort-Object ordinal | ForEach-Object {
        [pscustomobject][ordered]@{ t = "binary32"; v = $_.expectedSemanticSha256 }
    })
    $root = @(
        [pscustomobject][ordered]@{ n = "workbookId"; t = "uuid"; v = (Get-TopValue $WorkbookRow "id") },
        [pscustomobject][ordered]@{ n = "profile"; t = "code"; v = (Get-TopValue $WorkbookRow "profile") },
        [pscustomobject][ordered]@{ n = "headers"; t = "object"; v = (Get-TopValue $firstAnnex "headers") },
        [pscustomobject][ordered]@{ n = "annexRowHashes"; t = "array"; v = $annexHashes },
        [pscustomobject][ordered]@{ n = "factLinkHashes"; t = "array"; v = $factLinkHashes }
    )
    return ,(Get-Ae1hPreimage "annex-e1-workbook-content" "sha256:v1.4" "AE1-UAT-$($WorkbookRow.scenario)" $root)
}

function Get-CasePackageRecord {
    param([string] $Scenario, [object[]] $Rows)
    $scenarioRows = @($Rows | Where-Object scenario -eq $Scenario)
    $sourceRows = @($scenarioRows | Where-Object { $_.family -notin @('F22','F26','F27','F28','F29') } |
        Sort-Object @{Expression={ [int]$_.family.Substring(1) }}, ordinal | ForEach-Object {
            [pscustomobject][ordered]@{ t='binary32'; v=$_.expectedSemanticSha256 }
        })
    $journalPairs = @($scenarioRows | Where-Object family -eq 'F22' | Sort-Object ordinal | ForEach-Object {
        [pscustomobject][ordered]@{ t='object'; v=@(
            [pscustomobject][ordered]@{ n='semanticHash'; t='binary32'; v=(Get-TopValue $_ 'runtime_semantic_hash') },
            [pscustomobject][ordered]@{ n='integrityHash'; t='binary32'; v=(Get-TopValue $_ 'runtime_integrity_hash') }
        ) }
    })
    $annexRows = @($scenarioRows | Where-Object family -eq 'F20' | Sort-Object ordinal)
    $annexHashes = @($annexRows | ForEach-Object {
        [pscustomobject][ordered]@{ t='binary32'; v=$_.expectedSemanticSha256 }
    })
    $evidenceRows = @($scenarioRows | Where-Object { $_.family -in @('F26','F27','F28','F29') } |
        Sort-Object @{Expression={ [int]$_.family.Substring(1) }}, ordinal | ForEach-Object {
            [pscustomobject][ordered]@{ t='binary32'; v=$_.expectedSemanticSha256 }
        })
    $root = @(
        [pscustomobject][ordered]@{ n='specificationId'; t='string'; v='ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_v1.7' },
        [pscustomobject][ordered]@{ n='caseId'; t='code'; v="DS-AE1-$Scenario" },
        [pscustomobject][ordered]@{ n='sourceRows'; t='array'; v=$sourceRows },
        [pscustomobject][ordered]@{ n='journalPairs'; t='array'; v=$journalPairs },
        [pscustomobject][ordered]@{ n='headers'; t='object'; v=(Get-TopValue $annexRows[0] 'headers') },
        [pscustomobject][ordered]@{ n='annexRows'; t='array'; v=$annexHashes },
        [pscustomobject][ordered]@{ n='outcome'; t='code'; v='EXPECTED_PASS' },
        [pscustomobject][ordered]@{ n='evidenceRows'; t='array'; v=$evidenceRows }
    )
    $preimage = Get-Ae1hPreimage 'annex-e1-case-package' 'sha256:v1.4' "AE1-UAT-$Scenario" $root
    return [pscustomobject][ordered]@{
        recordType='case-package'; scenario=$Scenario; caseId="DS-AE1-$Scenario"
        preimageBytes=$preimage.Length; preimageBase64=[Convert]::ToBase64String($preimage)
        sha256=Get-Sha256Hex $preimage
    }
}

function Get-ElectronicJournalSemanticPreimage {
    param($Row, [object[]] $Rows)
    $scenario = $Row.scenario
    $eventType = [string](Get-TopValue $Row 'event_type')
    $sourceObjectId = [string](Get-TopValue $Row 'source_object_id')
    $periodId = [string](Get-TopValue $Row 'period_id')
    $period = @($Rows | Where-Object { $_.family -eq 'F04' -and (Get-TopValue $_ 'id') -eq $periodId })
    $site = @($Rows | Where-Object { $_.family -eq 'F01' -and $_.scenario -eq $scenario })
    $identity = @($Rows | Where-Object { $_.family -eq 'F02' -and $_.scenario -eq $scenario })
    Assert-Condition ($period.Count -eq 1 -and $site.Count -eq 1 -and $identity.Count -eq 1) "[$($Row.key)] Electronic Journal scope is incomplete."
    $documentId = ''; $requestId = ''; $xzId = ''; $birId = ''
    if ($eventType -in @('fiscal_document_committed','fiscal_document_voided')) { $documentId = $sourceObjectId }
    if ($eventType -in @('x_reading_committed','z_reading_committed','bir_sales_summary_committed')) {
        $transitionRef = [string](Get-TopValue $Row 'source_transition_ref')
        $requestId = $transitionRef.Substring($transitionRef.LastIndexOf(':') + 1)
    }
    if ($eventType -in @('x_reading_committed','z_reading_committed')) { $xzId = $sourceObjectId }
    if ($eventType -eq 'bir_sales_summary_committed') { $birId = $sourceObjectId }
    $namespace = [Guid]'ae1d5e7a-7e2d-5c4d-9a11-202608140001'
    $policyName = "annex-e1-synthetic-uat:v1.1|AE1-UAT-$scenario|sequence-policy|0001"
    $policyId = if ($eventType -eq 'fiscal_document_committed') { Get-UuidV5 $namespace $policyName } else { '' }
    $lines = @(
        'profile=pos-server-electronic-journal-event-semantic:sha256:v1',
        'event_schema=pos-server-electronic-journal-event:v1',
        "site_pos_server_id=$(Get-TopValue $site[0] 'site_pos_server_id')",
        "fiscal_identity_id=$(Get-TopValue $identity[0] 'id')",
        'currency=PHP',
        "fiscal_reporting_period_id=$periodId",
        "event_type=$eventType",
        "source_transition_ref=$(Get-TopValue $Row 'source_transition_ref')",
        "source_transition_version=$(Get-TopValue $Row 'source_transition_version')",
        "effective_at=$(Get-TopValue $Row 'effective_at')",
        "fiscal_document_id=$documentId",
        "fiscal_report_request_id=$requestId",
        "x_z_report_id=$xzId",
        "bir_sales_summary_report_id=$birId",
        'reprint_request_id=',
        "fiscal_sequence_policy_id=$policyId",
        "business_day_date=$(Get-TopValue $period[0] 'business_date')",
        "idempotency_ref=$(Get-TopValue $Row 'idempotency_key')",
        "facts=$(Get-TopValue $Row 'facts_json')"
    )
    return ,$script:Utf8.GetBytes(($lines -join "`n") + "`n")
}

function Get-ElectronicJournalIntegrityPreimage {
    param($Row)
    $eventType = [string](Get-TopValue $Row 'event_type')
    $isDocument = $eventType -in @('fiscal_document_committed','fiscal_document_voided')
    $actor = if ($eventType -eq 'fiscal_document_committed') { 'pos-server-fiscal-document-runtime' } else { 'synthetic-data-steward' }
    $service = if ($isDocument) { 'pos-server-fiscal-document-runtime' } else { 'pos-server-annex-e1-uat-preparer' }
    $stream = ([Guid](Get-TopValue $Row 'stream_id')).ToString('N').ToUpperInvariant()
    $sequence = [int64](Get-TopValue $Row 'sequence')
    $eventReference = 'EJ-{0}-{1}' -f $stream, $sequence.ToString('00000000000000000000')
    $lines = @(
        'profile=pos-server-electronic-journal-integrity:sha256:v1',
        "event_reference=$eventReference",
        "stream_sequence=$sequence",
        "recorded_at=$(Get-TopValue $Row 'recorded_at')",
        "semantic_hash=$(Get-TopValue $Row 'runtime_semantic_hash')",
        "previous_integrity_hash=$(Get-TopValue $Row 'previous_integrity_hash')",
        "actor_ref=$actor",
        "service_identity_ref=$service",
        "correlation_ref=$(Get-TopValue $Row 'correlation_id')",
        'retention_policy=fiscal_reconstruction_hold'
    )
    return ,$script:Utf8.GetBytes(($lines -join "`n") + "`n")
}

function Get-UuidV5 {
    param([Guid] $Namespace, [string] $Name)
    $namespaceBytes = $Namespace.ToByteArray()
    $networkNamespace = [byte[]]@(
        $namespaceBytes[3], $namespaceBytes[2], $namespaceBytes[1], $namespaceBytes[0],
        $namespaceBytes[5], $namespaceBytes[4], $namespaceBytes[7], $namespaceBytes[6],
        $namespaceBytes[8], $namespaceBytes[9], $namespaceBytes[10], $namespaceBytes[11],
        $namespaceBytes[12], $namespaceBytes[13], $namespaceBytes[14], $namespaceBytes[15]
    )
    $sha1 = [Security.Cryptography.SHA1]::Create()
    try { $hash = $sha1.ComputeHash((Join-Bytes @($networkNamespace, $script:Utf8.GetBytes($Name.Normalize())))) }
    finally { $sha1.Dispose() }
    $hash[6] = [byte](($hash[6] -band 0x0f) -bor 0x50)
    $hash[8] = [byte](($hash[8] -band 0x3f) -bor 0x80)
    $guidBytes = [byte[]]@(
        $hash[3], $hash[2], $hash[1], $hash[0], $hash[5], $hash[4], $hash[7], $hash[6],
        $hash[8], $hash[9], $hash[10], $hash[11], $hash[12], $hash[13], $hash[14], $hash[15]
    )
    return (New-Object Guid (,$guidBytes)).ToString("D").ToLowerInvariant()
}

function Get-TopValue {
    param($Row, [string] $Name)
    $member = @($Row.members | Where-Object { $_.n -eq $Name })
    Assert-Condition ($member.Count -eq 1) "[$($Row.key)] Expected one member '$Name', found $($member.Count)."
    return $member[0].v
}

function Get-NestedValue {
    param($Members, [string] $Name, [string] $Context)
    $member = @($Members | Where-Object { $_.n -eq $Name })
    Assert-Condition ($member.Count -eq 1) "[$Context] Expected one nested member '$Name', found $($member.Count)."
    return $member[0].v
}

function Read-SemanticRegistry {
    param([string] $Path)
    $rows = New-Object System.Collections.ArrayList
    foreach ($line in [IO.File]::ReadAllLines($Path, $script:Utf8)) {
        if ($line -notmatch '^\| `F[0-9]{2}\\\|[0-9]{3}\\\|[0-9]{4}` \|') { continue }
        $parts = $line.Substring(2, $line.Length - 4) -split ' \| ', 15
        Assert-Condition ($parts.Count -eq 15) "Semantic registry row did not contain 15 columns."
        for ($i = 0; $i -lt $parts.Count; $i++) { $parts[$i] = $parts[$i].Trim().Trim([char]0x60) }
        $schemaJson = $script:Utf8.GetString([Convert]::FromBase64String($parts[9]))
        $valuesJson = $script:Utf8.GetString([Convert]::FromBase64String($parts[10]))
        $schemaObject = $schemaJson | ConvertFrom-Json
        $valuesObject = $valuesJson | ConvertFrom-Json
        $schema = @($schemaObject)
        $values = @($valuesObject)
        Assert-Condition ($schema.Count -eq $values.Count) "[$($parts[0])] Schema/value member count differs."
        for ($i = 0; $i -lt $schema.Count; $i++) {
            Assert-Condition ([string]($schema[$i].n) -ceq [string]($values[$i].n)) "[$($parts[0])] Member name mismatch at $i."
            Assert-Condition ([string]($schema[$i].t) -ceq [string]($values[$i].t)) "[$($parts[0])] Member type mismatch at $i."
        }
        [void]$rows.Add([pscustomobject][ordered]@{
            key = $parts[0].Replace('\|', '|')
            family = $parts[1]
            schemaVersion = $parts[2]
            instanceUuid = $parts[3]
            scenario = $parts[4]
            ordinal = [int]$parts[5]
            familyToken = $parts[6]
            hashPurpose = $parts[7]
            role = $parts[8]
            members = $values
            expectedPreimageBytes = [int]$parts[11]
            expectedPreimageHex = $parts[12]
            expectedSemanticSha256 = $parts[13]
            provenance = $parts[14]
        })
    }
    return $rows.ToArray()
}

function Read-SpecificationManifest {
    param([string] $Path)
    $text = [IO.File]::ReadAllText($Path, $script:Utf8)
    $members = New-Object System.Collections.ArrayList
    foreach ($line in ($text -split "`n")) {
        if ($line -match '^\| `(?<path>docs/[^`]+)` \| (?<length>[0-9]+) \| `(?<hash>[0-9a-f]{64})` \|$') {
            [void]$members.Add([pscustomobject][ordered]@{ path = $Matches.path; length = [int64]$Matches.length; sha256 = $Matches.hash })
        }
    }
    Assert-Condition ($text -match 'Package-root preimage bytes: `(?<rootLength>[0-9]+)`') "Manifest package-root length is missing."
    $rootLength = [int]$Matches.rootLength
    Assert-Condition ($text -match 'Package-root preimage Base64: `(?<rootBase64>[A-Za-z0-9+/=]+)`') "Manifest package-root Base64 is missing."
    $rootBase64 = $Matches.rootBase64
    Assert-Condition ($text -match 'Package-root SHA-256: `(?<rootHash>[0-9a-f]{64})`') "Manifest package-root hash is missing."
    return [pscustomobject]@{ members = $members.ToArray(); rootLength = $rootLength; rootBase64 = $rootBase64; rootHash = $Matches.rootHash }
}

function Read-CasePackageRegistry {
    param([string] $Path)
    $rows = New-Object System.Collections.ArrayList
    foreach ($line in [IO.File]::ReadAllLines($Path, $script:Utf8)) {
        if ($line -match '^\| `(?<case>DS-AE1-(?<scenario>[0-9]{3}))` \| (?<length>[0-9]+) \| `(?<hash>[0-9a-f]{64})` \|$') {
            [void]$rows.Add([pscustomobject][ordered]@{
                caseId = $Matches.case
                scenario = $Matches.scenario
                preimageBytes = [int]$Matches.length
                sha256 = $Matches.hash
            })
        }
    }
    return $rows.ToArray()
}

function Get-ExpectedIdentities {
    param([object[]] $Rows, [string[]] $Scenarios)
    $familyType = [ordered]@{
        F01='site-pos-server'; F02='fiscal-identity'; F03='header-profile'; F04='period'; F05='fiscal-range'
        F06='fiscal-document'; F07='document-line'; F08='document-total'; F09='tax-detail'; F10='discount-detail'
        F11='statutory-fact'; F12='tender'; F13='tender-breakdown'; F14='discount-breakdown'; F15='status-history'
        F16='accounting-fact'; F17='x-report'; F18='z-report'; F19='bir-summary'; F20='annex-row'
        F21='annex-fact-source'; F22='electronic-journal-record'; F23='source-transition'; F24='annex-workbook'
        F25='annex-generation-request'; F26='replay-record'; F27='conflict-record'; F28='recovery-record'; F29='audit-record'
    }
    $namespace = [Guid]'ae1d5e7a-7e2d-5c4d-9a11-202608140001'
    $identities = @{}
    foreach ($row in $Rows) {
        $objectType = $familyType[$row.family]
        Assert-Condition ($null -ne $objectType) "Unknown identity family $($row.family)."
        $name = "annex-e1-synthetic-uat:v1.1|AE1-UAT-$($row.scenario)|$objectType|$($row.ordinal.ToString('0000'))"
        $uuid = Get-UuidV5 $namespace $name
        Assert-Condition ($uuid -eq $row.instanceUuid) "[$($row.key)] UUIDv5 mismatch: expected $($row.instanceUuid), calculated $uuid."
        $identityKey = "$($row.scenario)|$objectType|$($row.ordinal.ToString('0000'))"
        Assert-Condition (-not $identities.ContainsKey($identityKey)) "Duplicate identity key $identityKey."
        $identities[$identityKey] = [pscustomobject][ordered]@{
            recordType = 'identity'; scenario = $row.scenario; objectType = $objectType; ordinal = $row.ordinal
            name = $name; uuid = $uuid
        }
    }

    $profileA = @('001','007','010','011','012','013','014','016','017','018','019','020','021','023','024')
    $accessCounts = @{ '001'=1; '011'=1; '014'=1; '016'=2; '018'=2; '019'=2; '023'=1; '024'=2 }
    foreach ($scenario in $Scenarios) {
        $periods = @($Rows | Where-Object { $_.family -eq 'F04' -and $_.scenario -eq $scenario }).Count
        $documents = @($Rows | Where-Object { $_.family -eq 'F06' -and $_.scenario -eq $scenario }).Count
        $facts = @($Rows | Where-Object { $_.family -eq 'F16' -and $_.scenario -eq $scenario }).Count
        $attempts = @($Rows | Where-Object { $_.family -eq 'F25' -and $_.scenario -eq $scenario }).Count
        $access = if ($accessCounts.ContainsKey($scenario)) { [int]$accessCounts[$scenario] } else { 0 }
        $voids = if ($profileA -contains $scenario) { 1 } else { 0 }
        $operationRequests = $documents + $voids + (3 * $periods) + $facts + $attempts + $access
        $counts = [ordered]@{
            'dataset-case'=1; 'sequence-policy'=1; 'sequence-state'=1; 'z-close-state'=1
            'x-request'=$periods; 'x-scope'=$periods; 'z-request'=$periods; 'z-scope'=$periods
            'z-transition'=$periods; 'z-transition-value'=$periods; 'bir-request'=$periods; 'bir-scope'=$periods
            'electronic-journal-stream'=1; 'operation-request'=$operationRequests; 'evidence-record'=3
        }
        foreach ($objectType in $counts.Keys) {
            for ($ordinal = 1; $ordinal -le $counts[$objectType]; $ordinal++) {
                $identityKey = "$scenario|$objectType|$($ordinal.ToString('0000'))"
                Assert-Condition (-not $identities.ContainsKey($identityKey)) "Unexpected generated identity collision $identityKey."
                $name = "annex-e1-synthetic-uat:v1.1|AE1-UAT-$scenario|$objectType|$($ordinal.ToString('0000'))"
                $identities[$identityKey] = [pscustomobject][ordered]@{
                    recordType = 'identity'; scenario = $scenario; objectType = $objectType; ordinal = $ordinal
                    name = $name; uuid = Get-UuidV5 $namespace $name
                }
            }
        }
    }
    return @($identities.Values | Sort-Object scenario, objectType, ordinal)
}

function Convert-ToDatasetSemanticRecord {
    param($Row)
    return [pscustomobject][ordered]@{
        recordType = 'semantic-instance'; key = $Row.key; family = $Row.family; schemaVersion = $Row.schemaVersion
        instanceUuid = $Row.instanceUuid; scenario = $Row.scenario; ordinal = $Row.ordinal; familyToken = $Row.familyToken
        hashPurpose = $Row.hashPurpose; role = $Row.role; members = @($Row.members)
        expectedPreimageBytes = $Row.expectedPreimageBytes; expectedPreimageHex = $Row.expectedPreimageHex
        expectedSemanticSha256 = $Row.expectedSemanticSha256; provenance = $Row.provenance
    }
}

function Convert-ToCompactJson {
    param($Value)
    return ($Value | ConvertTo-Json -Depth 100 -Compress)
}

function Write-DatasetFile {
    param([string] $Path, $Metadata, [object[]] $Identities, [object[]] $Rows, [object[]] $CasePackages)
    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent)) { [void](New-Item -ItemType Directory -Path $parent) }
    $writer = New-Object IO.StreamWriter($Path, $false, $script:Utf8)
    try {
        $writer.NewLine = "`n"
        $writer.WriteLine((Convert-ToCompactJson $Metadata))
        foreach ($identity in $Identities) { $writer.WriteLine((Convert-ToCompactJson $identity)) }
        foreach ($row in $Rows) { $writer.WriteLine((Convert-ToCompactJson (Convert-ToDatasetSemanticRecord $row))) }
        foreach ($casePackage in $CasePackages) { $writer.WriteLine((Convert-ToCompactJson $casePackage)) }
    }
    finally { $writer.Dispose() }
}

function Test-DatasetFile {
    param([string] $Path, $Metadata, [object[]] $ExpectedIdentities, [object[]] $ExpectedRows, [object[]] $ExpectedCasePackages)
    Assert-Condition (Test-Path -LiteralPath $Path -PathType Leaf) "Dataset is missing: $Path"
    $lines = [IO.File]::ReadAllLines($Path, $script:Utf8)
    Assert-Condition ($lines.Count -eq (1 + $ExpectedIdentities.Count + $ExpectedRows.Count + $ExpectedCasePackages.Count)) "Dataset line count mismatch: expected $(1 + $ExpectedIdentities.Count + $ExpectedRows.Count + $ExpectedCasePackages.Count), got $($lines.Count)."
    $actualMetadata = $lines[0] | ConvertFrom-Json
    Assert-Condition ((Convert-ToCompactJson $actualMetadata) -ceq (Convert-ToCompactJson $Metadata)) "Dataset metadata differs from the authorized source."
    for ($i = 0; $i -lt $ExpectedIdentities.Count; $i++) {
        $actual = $lines[1 + $i] | ConvertFrom-Json
        $expectedJson = Convert-ToCompactJson $ExpectedIdentities[$i]
        Assert-Condition ((Convert-ToCompactJson $actual) -ceq $expectedJson) "Identity dataset mismatch at index $i ($($ExpectedIdentities[$i].name))."
    }
    $semanticOffset = 1 + $ExpectedIdentities.Count
    for ($i = 0; $i -lt $ExpectedRows.Count; $i++) {
        $actual = $lines[$semanticOffset + $i] | ConvertFrom-Json
        $expected = Convert-ToDatasetSemanticRecord $ExpectedRows[$i]
        foreach ($property in @('recordType','key','family','schemaVersion','instanceUuid','scenario','ordinal','familyToken','hashPurpose','role','expectedPreimageBytes','expectedPreimageHex','expectedSemanticSha256','provenance')) {
            $actualValue = Convert-ToCompactJson $actual.$property
            $expectedValue = Convert-ToCompactJson $expected.$property
            Assert-Condition ($actualValue -ceq $expectedValue) "[$($ExpectedRows[$i].key)] Dataset field '$property' mismatch: expected $expectedValue, got $actualValue."
        }
        $actualMembers = @($actual.members)
        $expectedMembers = @($expected.members)
        Assert-Condition ($actualMembers.Count -eq $expectedMembers.Count) "[$($ExpectedRows[$i].key)] Dataset member count mismatch: expected $($expectedMembers.Count), got $($actualMembers.Count)."
        for ($memberIndex = 0; $memberIndex -lt $expectedMembers.Count; $memberIndex++) {
            $expectedMember = $expectedMembers[$memberIndex]
            $actualMember = $actualMembers[$memberIndex]
            Assert-Condition ([string]($actualMember.n) -ceq [string]($expectedMember.n)) "[$($ExpectedRows[$i].key)] Dataset member name mismatch at ${memberIndex}: expected '$($expectedMember.n)', got '$($actualMember.n)'."
            Assert-Condition ([string]($actualMember.t) -ceq [string]($expectedMember.t)) "[$($ExpectedRows[$i].key)] Dataset member type '$($expectedMember.n)' mismatch: expected '$($expectedMember.t)', got '$($actualMember.t)'."
            $actualValue = Convert-ToCompactJson $actualMember.v
            $expectedValue = Convert-ToCompactJson $expectedMember.v
            Assert-Condition ($actualValue -ceq $expectedValue) "[$($ExpectedRows[$i].key)] Dataset source/runtime value '$($expectedMember.n)' mismatch: expected $expectedValue, got $actualValue."
        }
        $actualPreimage = Get-SourceRowPreimage $actual
        Assert-Condition ($actualPreimage.Length -eq $actual.expectedPreimageBytes) "[$($actual.key)] Dataset preimage length mismatch."
        Assert-Condition ((Convert-BytesToHex $actualPreimage) -ceq $actual.expectedPreimageHex) "[$($actual.key)] Dataset preimage bytes mismatch."
        Assert-Condition ((Get-Sha256Hex $actualPreimage) -ceq $actual.expectedSemanticSha256) "[$($actual.key)] Dataset semantic digest mismatch."
    }
    $caseOffset = $semanticOffset + $ExpectedRows.Count
    for ($i = 0; $i -lt $ExpectedCasePackages.Count; $i++) {
        $actual = $lines[$caseOffset + $i] | ConvertFrom-Json
        $expected = $ExpectedCasePackages[$i]
        Assert-Condition ((Convert-ToCompactJson $actual) -ceq (Convert-ToCompactJson $expected)) "[$($expected.caseId)] Case-package record mismatch."
        $preimage = [Convert]::FromBase64String($actual.preimageBase64)
        Assert-Condition ($preimage.Length -eq $actual.preimageBytes) "[$($expected.caseId)] Case-package length mismatch."
        Assert-Condition ((Get-Sha256Hex $preimage) -ceq $actual.sha256) "[$($expected.caseId)] Case-package digest mismatch."
    }
}

if ($FunctionsOnly) { return }

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$annexRoot = Join-Path $repoRoot "docs\v1.3\fiscal-reporting\annex-e"
$populationPath = Join-Path $annexRoot "ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_Population_v1.7.md"
$manifestPath = Join-Path $annexRoot "ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Dataset_Specification_Manifest_v1.7.md"
$hashContractPath = Join-Path $annexRoot "ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Canonical_Source_Row_and_Hash_Contract_v1.7.md"
$mappingPath = Join-Path $annexRoot "ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Scenario_to_Dataset_Mapping_v1.7.md"
$expectedValuesPath = Join-Path $annexRoot "ExitPass_POS_Server_Annex_E1_Synthetic_UAT_Expected_Value_Matrix_v1.7.md"
$externalDecisionsPath = Join-Path $annexRoot "ExitPass_POS_Server_Annex_E1_External_Decision_to_Scenario_Reconciliation_Matrix_v1.7.md"
if ([string]::IsNullOrWhiteSpace($DatasetPath)) {
    $DatasetPath = Join-Path $annexRoot "dataset\v1.7\annex-e1-synthetic-uat-dataset-v1.7.jsonl"
}
$DatasetPath = [IO.Path]::GetFullPath($DatasetPath)

$includedScenarios = @('001','003','004','005','007','009','010','011','012','013','014','016','017','018','019','020','021','023','024')
$excludedScenarios = @('002','006','008','015','022','025')
$correctedSpaScenarios = @('001','007','010','011','012','013','014','016','017','018','019','020','021','023','024')
$expectedFamilyCounts = [ordered]@{
    F01=19; F02=19; F03=19; F04=22; F05=44; F06=67; F07=67; F08=67; F09=67; F10=45
    F11=30; F12=69; F13=48; F14=90; F15=82; F16=155; F17=22; F18=22; F19=22; F20=22
    F21=154; F22=148; F23=148; F24=19; F25=31; F26=2; F27=2; F28=2; F29=186
}

$manifest = Read-SpecificationManifest $manifestPath
Test-Ae1hVectors
Assert-Condition ($manifest.members.Count -eq 8) "Expected eight governed manifest members."
foreach ($member in $manifest.members) {
    $path = Join-Path $repoRoot ($member.path.Replace('/', '\'))
    Assert-Condition (Test-Path -LiteralPath $path -PathType Leaf) "Governed member is missing: $($member.path)"
    $bytes = [IO.File]::ReadAllBytes($path)
    Assert-Condition ($bytes.Length -eq $member.length) "Manifest length mismatch for $($member.path): expected $($member.length), got $($bytes.Length)."
    Assert-Condition ((Get-Sha256Hex $bytes) -ceq $member.sha256) "Manifest hash mismatch for $($member.path)."
}
$sortedMembers = @($manifest.members)
[Array]::Sort($sortedMembers, [Comparison[object]]{
    param($left, $right)
    Compare-ByteArraysLexically $script:Utf8.GetBytes($left.path) $script:Utf8.GetBytes($right.path)
})
$descriptorItems = @($sortedMembers | ForEach-Object {
    [pscustomobject][ordered]@{ t='object'; v=@(
        [pscustomobject][ordered]@{ n='path'; t='string'; v=$_.path },
        [pscustomobject][ordered]@{ n='length'; t='integer'; v=$_.length },
        [pscustomobject][ordered]@{ n='sha256'; t='binary32'; v=$_.sha256 }
    ) }
})
$packageRoot = Get-Ae1hPreimage 'annex-e1-specification-package-root' 'sha256:v1.7' 'v1.7' @(
    [pscustomobject][ordered]@{ n='members'; t='array'; v=$descriptorItems }
)
Assert-Condition ($packageRoot.Length -eq 1897) "Package-root preimage length mismatch: expected 1897, got $($packageRoot.Length)."
Assert-Condition ($packageRoot.Length -eq $manifest.rootLength) "Manifest package-root length does not match reconstruction."
Assert-Condition ((Convert-BytesToHex $packageRoot) -ceq (Convert-BytesToHex ([Convert]::FromBase64String($manifest.rootBase64)))) "Package-root preimage differs from the manifest Base64."
Assert-Condition ((Get-Sha256Hex $packageRoot) -ceq $manifest.rootHash) "Package-root digest differs from the manifest."

$rows = @(Read-SemanticRegistry $populationPath)
Assert-Condition ($rows.Count -eq 1690) "Expected 1,690 semantic rows, got $($rows.Count)."
Assert-Condition (@($rows | Group-Object key | Where-Object Count -ne 1).Count -eq 0) "Semantic registry contains duplicate keys."
Assert-Condition (@($rows | Group-Object instanceUuid | Where-Object Count -ne 1).Count -eq 0) "Semantic registry contains duplicate governed identities."
foreach ($family in $expectedFamilyCounts.Keys) {
    $count = @($rows | Where-Object family -eq $family).Count
    Assert-Condition ($count -eq $expectedFamilyCounts[$family]) "$family count mismatch: expected $($expectedFamilyCounts[$family]), got $count."
}
foreach ($row in $rows) {
    $preimage = Get-SourceRowPreimage $row
    Assert-Condition ($preimage.Length -eq $row.expectedPreimageBytes) "[$($row.key)] Preimage length mismatch: expected $($row.expectedPreimageBytes), got $($preimage.Length)."
    Assert-Condition ((Convert-BytesToHex $preimage) -ceq $row.expectedPreimageHex) "[$($row.key)] Preimage bytes mismatch."
    Assert-Condition ((Get-Sha256Hex $preimage) -ceq $row.expectedSemanticSha256) "[$($row.key)] Semantic digest mismatch."
}

$correctionFacts = @($rows | Where-Object key -eq 'F16|013|0008')
Assert-Condition ($correctionFacts.Count -eq 1) "Expected exactly one F16|013|0008 correction fact."
$correctionFact = $correctionFacts[0]
Assert-Condition ($correctionFact.instanceUuid -ceq 'e0602e8f-8662-5a63-8313-25672d017b8e') "[F16|013|0008] Identity changed."
Assert-Condition ((Get-TopValue $correctionFact 'correction_reason') -ceq 'source_correction') "[F16|013|0008] correction_reason must be source_correction."
Assert-Condition ((Get-TopValue $correctionFact 'supersedes_fact_id') -ceq '96cc7392-4dc3-550c-b7e0-8521f77d86ef') "[F16|013|0008] supersedes_fact_id changed."
Assert-Condition ([int64](Get-TopValue $correctionFact 'amount_minor_units') -eq 600) "[F16|013|0008] amount_minor_units changed."
Assert-Condition (@($rows | Where-Object { @($_.members | Where-Object { [string]$_.v -ceq 'SYNTHETIC_CORRECTION' }).Count -gt 0 }).Count -eq 0) "The semantic registry still contains SYNTHETIC_CORRECTION."

$correctionCodePath = Join-Path $repoRoot 'db\reference-data\controlled-codes\source\families\annex_e1_correction_reason.json'
$correctionCodeSource = [IO.File]::ReadAllText($correctionCodePath, $script:Utf8) | ConvertFrom-Json
$correctionCodes = @($correctionCodeSource.code_sets[0].codes)
$sourceCorrectionCodes = @($correctionCodes | Where-Object { $_.code_key -ceq 'source_correction' -and $_.is_active -eq $true })
Assert-Condition ($sourceCorrectionCodes.Count -eq 1) "Canonical correction-reason source does not contain one active source_correction code."
Assert-Condition (@($correctionCodes | Where-Object code_key -ceq 'SYNTHETIC_CORRECTION').Count -eq 0) "Canonical correction-reason source unexpectedly contains SYNTHETIC_CORRECTION."

foreach ($scenario in $correctedSpaScenarios) {
    $annex = @($rows | Where-Object { $_.family -eq 'F20' -and $_.scenario -eq $scenario })
    Assert-Condition ($annex.Count -eq 1) "[$scenario] Expected one corrected SP-A Annex row."
    $details = Get-TopValue $annex[0] 'details'
    Assert-Condition ([int64](Get-NestedValue $details 'D07' $annex[0].key) -eq 41200) "[$($annex[0].key)] D07 is not 41200."
    $reconciliations = @(Get-TopValue $annex[0] 'reconciliations')
    foreach ($ruleName in @('R01','R06')) {
        $entry = @($reconciliations | Where-Object { (Get-NestedValue $_.v 'rule' $annex[0].key) -eq $ruleName })
        Assert-Condition ($entry.Count -eq 1) "[$($annex[0].key)] Missing $ruleName."
        $expectedValue = if ($ruleName -eq 'R01') { 30000 } else { 41200 }
        Assert-Condition ([int64](Get-NestedValue $entry[0].v 'expected_minor_units' $annex[0].key) -eq $expectedValue) "[$($annex[0].key)] $ruleName expected value differs."
        Assert-Condition ([int64](Get-NestedValue $entry[0].v 'calculated_minor_units' $annex[0].key) -eq $expectedValue) "[$($annex[0].key)] $ruleName calculated value differs."
    }
    $annexCalculation = Get-AnnexCalculationPreimage $annex[0]
    Assert-Condition ((Get-Sha256Hex $annexCalculation) -ceq (Get-TopValue $annex[0] 'calculation_semantic_hash')) "[$($annex[0].key)] Annex calculation digest mismatch."
    $workbook = @($rows | Where-Object { $_.family -eq 'F24' -and $_.scenario -eq $scenario })[0]
    $workbookCalculation = Get-CorrectedWorkbookCalculationPreimage $workbook $annex
    Assert-Condition ((Get-Sha256Hex $workbookCalculation) -ceq (Get-TopValue $workbook 'calculation_semantic_hash')) "[$($workbook.key)] Workbook calculation aggregate mismatch."
    $factLinks = @($rows | Where-Object { $_.family -eq 'F21' -and $_.scenario -eq $scenario })
    $workbookContent = Get-WorkbookContentPreimage $workbook $annex $factLinks
    Assert-Condition ($workbookContent.Length -eq [int](Get-TopValue $workbook 'workbook_content_length_bytes')) "[$($workbook.key)] Workbook content length mismatch."
    Assert-Condition ((Get-Sha256Hex $workbookContent) -ceq (Get-TopValue $workbook 'workbook_content_sha256')) "[$($workbook.key)] Workbook content digest mismatch."
}

$workbooksByScenario = @{}
foreach ($workbook in @($rows | Where-Object family -eq 'F24')) { $workbooksByScenario[$workbook.scenario] = $workbook }
foreach ($replay in @($rows | Where-Object family -eq 'F26')) {
    $workbook = $workbooksByScenario[$replay.scenario]
    Assert-Condition ($null -ne $workbook) "[$($replay.key)] Referenced workbook is missing."
    Assert-Condition ((Get-TopValue $replay 'original_workbook_id') -eq (Get-TopValue $workbook 'id')) "[$($replay.key)] Replay workbook identity mismatch."
    Assert-Condition ((Get-TopValue $replay 'workbook_content_sha256') -eq (Get-TopValue $workbook 'workbook_content_sha256')) "[$($replay.key)] Replay workbook content hash mismatch."
    Assert-Condition ([int](Get-TopValue $replay 'workbook_content_length_bytes') -eq [int](Get-TopValue $workbook 'workbook_content_length_bytes')) "[$($replay.key)] Replay workbook content length mismatch."
}
foreach ($recovery in @($rows | Where-Object family -eq 'F28')) {
    $observedWorkbookId = Get-TopValue $recovery 'observed_workbook_id'
    if ($null -ne $observedWorkbookId) {
        $workbook = $workbooksByScenario[$recovery.scenario]
        Assert-Condition ($observedWorkbookId -eq (Get-TopValue $workbook 'id')) "[$($recovery.key)] Recovery workbook identity mismatch."
        Assert-Condition ((Get-TopValue $recovery 'observed_workbook_content_sha256') -eq (Get-TopValue $workbook 'workbook_content_sha256')) "[$($recovery.key)] Recovery workbook content hash mismatch."
    }
}
foreach ($audit in @($rows | Where-Object family -eq 'F29')) {
    if ((Get-TopValue $audit 'source_family') -eq 'annex-workbook') {
        $workbook = $workbooksByScenario[$audit.scenario]
        Assert-Condition ((Get-TopValue $audit 'source_id') -eq (Get-TopValue $workbook 'id')) "[$($audit.key)] Workbook audit source identity mismatch."
        Assert-Condition ((Get-TopValue $audit 'source_semantic_hash') -eq $workbook.expectedSemanticSha256) "[$($audit.key)] Workbook audit semantic hash mismatch."
        Assert-Condition ((Get-TopValue $audit 'source_content_hash') -eq (Get-TopValue $workbook 'workbook_content_sha256')) "[$($audit.key)] Workbook audit content hash mismatch."
    }
}

$identityRows = @(Get-ExpectedIdentities $rows $includedScenarios)
Assert-Condition ($identityRows.Count -eq 2364) "Expected 2,364 deterministic identities, got $($identityRows.Count)."
Assert-Condition (@($identityRows | Group-Object name | Where-Object Count -ne 1).Count -eq 0) "Identity names are not unique."
Assert-Condition (@($identityRows | Group-Object uuid | Where-Object Count -ne 1).Count -eq 0) "UUIDv5 collision detected."
$namespace = [Guid]'ae1d5e7a-7e2d-5c4d-9a11-202608140001'
Assert-Condition ((Get-UuidV5 $namespace 'annex-e1-synthetic-uat:v1.1|AE1-UAT-001|period|0001') -eq 'e1f31225-7457-5573-a8f2-1250e7382ee5') "Period UUID vector failed."
Assert-Condition ((Get-UuidV5 $namespace 'annex-e1-synthetic-uat:v1.1|AE1-UAT-001|fiscal-document|0001') -eq '192219dd-a7c9-5a53-adb8-ee1342aff771') "Fiscal-document UUID vector failed."
Assert-Condition ((Get-UuidV5 $namespace 'annex-e1-synthetic-uat:v1.1|AE1-UAT-013|accounting-fact|0008') -eq 'e0602e8f-8662-5a63-8313-25672d017b8e') "Accounting-fact UUID vector failed."

$actualScenarios = @($rows.scenario | Sort-Object -Unique)
Assert-Condition (($actualScenarios -join ',') -ceq ($includedScenarios -join ',')) "Semantic scenario partition differs from the 19 included scenarios."
Assert-Condition (@($includedScenarios | Where-Object { $excludedScenarios -contains $_ }).Count -eq 0) "Included and excluded scenarios overlap."
Assert-Condition ((@($includedScenarios + $excludedScenarios | Sort-Object -Unique).Count) -eq 25) "Scenario union does not contain 25 scenarios."
$mappingText = [IO.File]::ReadAllText($mappingPath, $script:Utf8)
foreach ($scenario in $includedScenarios) { Assert-Condition ($mappingText.Contains("AE1-UAT-$scenario")) "Mapping omits included scenario $scenario." }
foreach ($scenario in $excludedScenarios) { Assert-Condition ($mappingText.Contains("AE1-UAT-$scenario")) "Mapping omits excluded scenario $scenario." }
$externalDecisionText = [IO.File]::ReadAllText($externalDecisionsPath, $script:Utf8)
$externalDecisionIds = @('AE-DR-002','AE-DR-004','AE-DR-010','AE-DR-011A','AE-DR-012','AE-DR-016','AE-DR-016B','AE-DR-019','AE-DR-020A','AE-DR-024')
foreach ($decisionId in $externalDecisionIds) {
    $pattern = [regex]::Escape("| ``$decisionId`` | ``UNRESOLVED`` | None |")
    Assert-Condition ([regex]::Matches($externalDecisionText, $pattern).Count -eq 1) "External decision $decisionId is missing, duplicated, resolved, or byte-dependent."
}

$periodById = @{}
foreach ($period in @($rows | Where-Object family -eq 'F04')) { $periodById[(Get-TopValue $period 'id')] = $period }
$c02Failures = 0
foreach ($fact in @($rows | Where-Object family -eq 'F16')) {
    $periodId = Get-TopValue $fact 'period_id'
    Assert-Condition ($periodById.ContainsKey($periodId)) "[$($fact.key)] Period $periodId does not exist."
    $start = [DateTimeOffset]::ParseExact((Get-TopValue $periodById[$periodId] 'period_start_at'), 'yyyy-MM-ddTHH:mm:ss.fffffffZ', [Globalization.CultureInfo]::InvariantCulture)
    $end = [DateTimeOffset]::ParseExact((Get-TopValue $periodById[$periodId] 'period_end_at'), 'yyyy-MM-ddTHH:mm:ss.fffffffZ', [Globalization.CultureInfo]::InvariantCulture)
    $effective = [DateTimeOffset]::ParseExact((Get-TopValue $fact 'effective_at'), 'yyyy-MM-ddTHH:mm:ss.fffffffZ', [Globalization.CultureInfo]::InvariantCulture)
    if ($effective -lt $start -or $effective -ge $end) { $c02Failures++ }
}
Assert-Condition ($c02Failures -eq 0) "C02 contains $c02Failures out-of-period facts."

$statutoryFacts = @($rows | Where-Object family -eq 'F11')
Assert-Condition ($statutoryFacts.Count -eq 30) "Expected 30 statutory facts."
foreach ($fact in $statutoryFacts) {
    $document = @($rows | Where-Object { $_.family -eq 'F06' -and $_.scenario -eq $fact.scenario -and $_.ordinal -eq $fact.ordinal })
    Assert-Condition ($document.Count -eq 1) "[$($fact.key)] Matching fiscal document is missing."
    $parkingId = ([Guid](Get-TopValue $fact 'parking_session_id')).ToString('D').ToLowerInvariant()
    $commandRef = ([string](Get-TopValue $document[0] 'central_pms_parking_session_ref')).Trim()
    $applied = Get-TopValue $document[0] 'applied_statutory_fiscal_facts'
    $appliedParkingId = ([Guid](Get-NestedValue $applied 'parking_session_id' $document[0].key)).ToString('D').ToLowerInvariant()
    Assert-Condition ($commandRef -ieq $parkingId) "[$($fact.key)] Central PMS parking reference '$commandRef' differs from '$parkingId'."
    Assert-Condition ($appliedParkingId -eq $parkingId) "[$($fact.key)] Applied parking identity differs from the statutory fact."
    $original = [int64](Get-TopValue $fact 'original_amount_minor_units')
    $discount = [int64](Get-TopValue $fact 'statutory_discount_minor_units')
    $payable = [int64](Get-TopValue $fact 'final_payable_minor_units')
    $vat = [int64](Get-TopValue $fact 'vat_amount_minor_units')
    $basis = [int64](Get-TopValue $fact 'vat_exclusive_basis_minor_units')
    Assert-Condition ($original -eq 11200 -and $discount -eq 2000 -and $payable -eq 9200) "[$($fact.key)] Authorized statutory amounts differ."
    Assert-Condition (($payable + $discount) -le $original) "[$($fact.key)] Runtime finality payable-plus-discount invariant failed."
    Assert-Condition (($basis + $vat) -eq $original) "[$($fact.key)] Runtime finality VAT basis invariant failed."
}

$ejRows = @($rows | Where-Object family -eq 'F22')
$transitionRows = @($rows | Where-Object family -eq 'F23')
Assert-Condition ($ejRows.Count -eq 148 -and $transitionRows.Count -eq 148) "F22/F23 cardinality mismatch."
$genesisCount = 0; $predecessorCount = 0
foreach ($scenario in $includedScenarios) {
    $events = @($ejRows | Where-Object scenario -eq $scenario | Sort-Object ordinal)
    $transitions = @($transitionRows | Where-Object scenario -eq $scenario | Sort-Object ordinal)
    Assert-Condition ($events.Count -eq $transitions.Count) "[$scenario] F22/F23 stream count differs."
    $streamIds = @($events | ForEach-Object { Get-TopValue $_ 'stream_id' } | Sort-Object -Unique)
    Assert-Condition ($streamIds.Count -eq 1) "[$scenario] Expected one Electronic Journal stream."
    for ($i = 0; $i -lt $events.Count; $i++) {
        $sequence = [int](Get-TopValue $events[$i] 'sequence')
        Assert-Condition ($sequence -eq ($i + 1)) "[$scenario] Electronic Journal sequence gap at $sequence."
        Assert-Condition ((Get-TopValue $events[$i] 'source_transition_id') -eq (Get-TopValue $transitions[$i] 'transition_id')) "[$scenario/$sequence] F22/F23 transition identity mismatch."
        Assert-Condition ((Get-TopValue $events[$i] 'record_id') -eq (Get-TopValue $transitions[$i] 'resulting_electronic_journal_record_id')) "[$scenario/$sequence] F22/F23 record identity mismatch."
        Assert-Condition ((Get-TopValue $events[$i] 'runtime_semantic_hash') -eq (Get-TopValue $transitions[$i] 'resulting_event_semantic_hash')) "[$scenario/$sequence] F22/F23 semantic hash mismatch."
        $semanticPreimage = Get-ElectronicJournalSemanticPreimage $events[$i] $rows
        Assert-Condition ($script:Utf8.GetByteCount([string](Get-TopValue $events[$i] 'facts_json')) -eq [int](Get-TopValue $events[$i] 'facts_byte_length')) "[$scenario/$sequence] Electronic Journal facts byte length mismatch."
        Assert-Condition ((Get-Sha256Hex $semanticPreimage) -eq (Get-TopValue $events[$i] 'runtime_semantic_hash')) "[$scenario/$sequence] Electronic Journal runtime semantic hash mismatch."
        $integrityPreimage = Get-ElectronicJournalIntegrityPreimage $events[$i]
        Assert-Condition ((Get-Sha256Hex $integrityPreimage) -eq (Get-TopValue $events[$i] 'runtime_integrity_hash')) "[$scenario/$sequence] Electronic Journal runtime integrity hash mismatch."
        if ($i -eq 0) {
            Assert-Condition ($null -eq (Get-TopValue $events[$i] 'predecessor_event_id')) "[$scenario] Genesis event has a predecessor."
            Assert-Condition ($null -eq (Get-TopValue $transitions[$i] 'predecessor_transition_id')) "[$scenario] Genesis transition has a predecessor."
            Assert-Condition ([int](Get-TopValue $transitions[$i] 'source_sequence') -eq 0) "[$scenario] Genesis source sequence is not zero."
            $genesisCount++
        } else {
            Assert-Condition ((Get-TopValue $events[$i] 'predecessor_event_id') -eq (Get-TopValue $events[$i - 1] 'record_id')) "[$scenario/$sequence] Event predecessor mismatch."
            Assert-Condition ((Get-TopValue $transitions[$i] 'predecessor_transition_id') -eq (Get-TopValue $transitions[$i - 1] 'transition_id')) "[$scenario/$sequence] Transition predecessor mismatch."
            Assert-Condition ([int](Get-TopValue $transitions[$i] 'source_sequence') -eq $i) "[$scenario/$sequence] Transition source sequence mismatch."
            Assert-Condition ((Get-TopValue $events[$i] 'previous_integrity_hash') -eq (Get-TopValue $events[$i - 1] 'runtime_integrity_hash')) "[$scenario/$sequence] Electronic Journal integrity predecessor mismatch."
            $predecessorCount++
        }
        Assert-Condition ([int](Get-TopValue $transitions[$i] 'destination_sequence') -eq ($i + 1)) "[$scenario/$sequence] Transition destination sequence mismatch."
    }
}
Assert-Condition ($genesisCount -eq 19 -and $predecessorCount -eq 129) "Electronic Journal genesis/predecessor totals differ."
Assert-Condition (@($ejRows | Where-Object { (Get-TopValue $_ 'event_type') -eq 'z_reading_committed' }).Count -eq 22) "Expected 22 Z-report events."
Assert-Condition (@($ejRows | Where-Object { (Get-TopValue $_ 'event_type') -eq 'bir_sales_summary_committed' }).Count -eq 22) "Expected 22 BIR-report events."

$annexRows = @($rows | Where-Object family -eq 'F20')
Assert-Condition ($annexRows.Count -eq 22) "Expected 22 Annex rows."
foreach ($annexRow in $annexRows) {
    $headers = @(Get-TopValue $annexRow 'headers')
    $details = @(Get-TopValue $annexRow 'details')
    for ($i = 1; $i -le 10; $i++) { Assert-Condition (@($headers | Where-Object n -eq ('H{0:00}' -f $i)).Count -eq 1) "[$($annexRow.key)] H coverage is incomplete." }
    for ($i = 1; $i -le 32; $i++) { Assert-Condition (@($details | Where-Object n -eq ('D{0:00}' -f $i)).Count -eq 1) "[$($annexRow.key)] D coverage is incomplete." }
    $reconciliations = @(Get-TopValue $annexRow 'reconciliations')
    Assert-Condition ($reconciliations.Count -eq 12) "[$($annexRow.key)] Expected R01-R12."
    for ($i = 1; $i -le 12; $i++) {
        $entry = @($reconciliations | Where-Object { (Get-NestedValue $_.v 'rule' $annexRow.key) -eq ('R{0:00}' -f $i) })
        Assert-Condition ($entry.Count -eq 1) "[$($annexRow.key)] Missing reconciliation R$('{0:00}' -f $i)."
        Assert-Condition ([int64](Get-NestedValue $entry[0].v 'difference_minor_units' $annexRow.key) -eq 0) "[$($annexRow.key)] Monetary reconciliation differs."
        Assert-Condition ([int64](Get-NestedValue $entry[0].v 'row_count_difference' $annexRow.key) -eq 0) "[$($annexRow.key)] Row-count reconciliation differs."
        Assert-Condition ((Get-NestedValue $entry[0].v 'result' $annexRow.key) -eq 'PASS') "[$($annexRow.key)] Reconciliation is not PASS."
    }
}
$correctedAnnexRows = @($annexRows | Where-Object { $correctedSpaScenarios -contains $_.scenario })
Assert-Condition ($correctedAnnexRows.Count -eq 15) "Expected fifteen corrected SP-A Annex rows."
foreach ($annexRow in $correctedAnnexRows) {
    $details = Get-TopValue $annexRow 'details'
    $reconciliations = @(Get-TopValue $annexRow 'reconciliations')
    $r01 = @($reconciliations | Where-Object { (Get-NestedValue $_.v 'rule' $annexRow.key) -eq 'R01' })[0].v
    $r06 = @($reconciliations | Where-Object { (Get-NestedValue $_.v 'rule' $annexRow.key) -eq 'R06' })[0].v
    Assert-Condition ([int64](Get-NestedValue $details 'D07' $annexRow.key) -eq 41200) "[$($annexRow.key)] D07 must include active gross and void total."
    Assert-Condition ([int64](Get-NestedValue $r01 'expected_minor_units' $annexRow.key) -eq 30000) "[$($annexRow.key)] R01 expected result mismatch."
    Assert-Condition ([int64](Get-NestedValue $r06 'expected_minor_units' $annexRow.key) -eq 41200) "[$($annexRow.key)] R06 expected result mismatch."
}
$spa = @($annexRows | Where-Object { $_.scenario -eq '001' -and $_.ordinal -eq 1 })[0]
$spaDetails = Get-TopValue $spa 'details'
$spaExpected = @{ D04=128600; D05=100000; D07=41200; D08=30000; D09=3600; D12=2000; D13=2000; D16=1000; D18=11200; D19=16200; D27=21400; D29=22100 }
foreach ($name in $spaExpected.Keys) {
    $actualValue = [int64](Get-NestedValue $spaDetails $name $spa.key)
    Assert-Condition ($actualValue -eq $spaExpected[$name]) "[$($spa.key)] SP-A field '$name' mismatch: expected $($spaExpected[$name]), got $actualValue."
}
$expectedText = [IO.File]::ReadAllText($expectedValuesPath, $script:Utf8)
foreach ($token in @('H01','H10','D01','D32','R01','R12')) { Assert-Condition ($expectedText.Contains($token)) "Expected-value matrix omits $token." }
Assert-Condition ($expectedText.Contains('41200-0-11200=30000')) "Expected-value matrix does not publish the corrected R01 arithmetic."
Assert-Condition ($expectedText.Contains('21400+16200+3600=41200')) "Expected-value matrix does not publish the corrected R06 arithmetic."

$hashContractText = [IO.File]::ReadAllText($hashContractPath, $script:Utf8)
Assert-Condition ($hashContractText.Contains('return zero back edges')) "Hash contract does not require zero cycle-detection back edges."
$graph = [ordered]@{
    'source-row-semantic-hash'=@('canonical-source-row')
    'workbook-content-hash'=@('workbook-content-bytes')
    'workbook-row-semantic-hash'=@('workbook-content-hash','workbook-row-fields')
    'evidence-row-semantic-hash'=@('evidence-row-fields')
    'case-package-digest'=@('workbook-row-semantic-hash','evidence-row-semantic-hash','source-row-semantic-hash')
    'specification-member-digest'=@('specification-member-bytes')
    'specification-package-root'=@('specification-member-digest')
}
$visiting=@{}; $visited=@{}
function Visit-GraphNode {
    param([string]$Node)
    Assert-Condition (-not $visiting.ContainsKey($Node)) "M04 graph cycle detected at $Node."
    if ($visited.ContainsKey($Node)) { return }
    $visiting[$Node]=$true
    if ($graph.Contains($Node)) { foreach ($child in $graph[$Node]) { Visit-GraphNode $child } }
    $visiting.Remove($Node); $visited[$Node]=$true
}
foreach ($node in $graph.Keys) { Visit-GraphNode $node }
foreach ($row in @($rows | Where-Object { $_.family -in @('F24','F25','F26','F27','F28') })) {
    $names = @($row.members | ForEach-Object n)
    Assert-Condition (@($names | Where-Object { $_ -match 'case_package' }).Count -eq 0) "[$($row.key)] Descendant contains a case-package digest member."
}

$metadata = [pscustomobject][ordered]@{
    recordType = 'dataset-metadata'
    schemaVersion = 'annex-e1-synthetic-uat-dataset:v1.7'
    sourceSpecificationPackageRootSha256 = $manifest.rootHash
    identityNamespace = 'ae1d5e7a-7e2d-5c4d-9a11-202608140001'
    identityNameVersion = 'annex-e1-synthetic-uat:v1.1'
    includedScenarios = $includedScenarios
    excludedScenarios = $excludedScenarios
    semanticFamilies = 29
    semanticInstances = 1690
    deterministicIdentities = 2364
    accountingFacts = 155
    electronicJournalRecords = 148
    sourceTransitions = 148
    format = 'utf-8-json-lines-lf-v1'
}

$casePackages = @($includedScenarios | ForEach-Object { Get-CasePackageRecord $_ $rows })
Assert-Condition ($casePackages.Count -eq 19) "Expected 19 case packages."
Assert-Condition (@($casePackages | Group-Object caseId | Where-Object Count -ne 1).Count -eq 0) "Case-package identities are not unique."
$publishedCasePackages = @(Read-CasePackageRegistry $populationPath)
Assert-Condition ($publishedCasePackages.Count -eq 19) "Expected 19 published case-package commitments."
foreach ($casePackage in $casePackages) {
    $published = @($publishedCasePackages | Where-Object caseId -eq $casePackage.caseId)
    Assert-Condition ($published.Count -eq 1) "[$($casePackage.caseId)] Published case-package commitment is missing or duplicated."
    Assert-Condition ($casePackage.preimageBytes -eq $published[0].preimageBytes) "[$($casePackage.caseId)] Published case-package length mismatch."
    Assert-Condition ($casePackage.sha256 -ceq $published[0].sha256) "[$($casePackage.caseId)] Published case-package digest mismatch."
}

if ($GenerateDataset) { Write-DatasetFile $DatasetPath $metadata $identityRows $rows $casePackages }
Test-DatasetFile $DatasetPath $metadata $identityRows $rows $casePackages

$datasetBytes = [IO.File]::ReadAllBytes($DatasetPath)
$resultLines = @(
    'ANNEX_E1_V17_VALIDATION=PASS'
    'ACCOUNTING_FACTS=155'
    "DATASET_BYTES=$($datasetBytes.Length)"
    "DATASET_SHA256=$(Get-Sha256Hex $datasetBytes)"
    'DETERMINISTIC_IDENTITIES=2364'
    'EJ_GENESIS=19'
    'EJ_PREDECESSORS=129'
    'EJ_RECORDS=148'
    'EJ_STREAMS=19'
    'EXCLUDED_SCENARIOS=6'
    'EXTERNAL_DECISIONS_UNRESOLVED=10'
    'INCLUDED_SCENARIOS=19'
    'PACKAGE_MEMBERS=8'
    "PACKAGE_ROOT_SHA256=$($manifest.rootHash)"
    'CASE_PACKAGES=19'
    'SEMANTIC_FAMILIES=29'
    'SEMANTIC_INSTANCES=1690'
    'STATUTORY_FINALITY_CASES=30'
    'TRANSITIONS=148'
)
$resultLines | ForEach-Object { Write-Output $_ }
