[CmdletBinding()]
param(
    [string]$Image = 'postgres:16-alpine'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$suffix = "{0}-{1}" -f (Get-Date -Format 'yyyyMMddHHmmss'), ([guid]::NewGuid().ToString('N').Substring(0, 8))
$containerName = "posserver-z006b-proof-$suffix"
$databaseName = "posserver_z006b_proof_$($suffix.Replace('-', '_'))"
$previousConnection = $env:POSSERVER_X_READING_TEST_DB_URL

try {
    $port = [int](Get-Random -Minimum 52000 -Maximum 62000)
    docker run --detach --rm --name $containerName --publish "127.0.0.1:${port}:5432" `
        --env POSTGRES_HOST_AUTH_METHOD=trust --env "POSTGRES_DB=$databaseName" $Image | Out-Null

    $ready = $false
    foreach ($attempt in 1..60) {
        docker exec $containerName pg_isready --username postgres --dbname $databaseName | Out-Null
        if ($LASTEXITCODE -eq 0) { $ready = $true; break }
        Start-Sleep -Milliseconds 500
    }
    if (-not $ready) { throw 'Disposable PostgreSQL did not become ready.' }

    $env:POSSERVER_X_READING_TEST_DB_URL = "Host=127.0.0.1;Port=$port;Database=$databaseName;Username=postgres;Pooling=false;Include Error Detail=false"
    dotnet test "$repositoryRoot\tests\ExitPass.PosServer.Api.IntegrationTests\ExitPass.PosServer.Api.IntegrationTests.csproj" `
        -c Release --no-build --filter 'FullyQualifiedName~FiscalXReadingPostgresIntegrationTests'
    if ($LASTEXITCODE -ne 0) { throw 'X Reading runtime proof failed.' }

    Write-Output 'X Reading disposable API/PostgreSQL proof passed; no credential material was emitted.'
}
finally {
    if ($null -eq $previousConnection) {
        Remove-Item Env:POSSERVER_X_READING_TEST_DB_URL -ErrorAction SilentlyContinue
    } else {
        $env:POSSERVER_X_READING_TEST_DB_URL = $previousConnection
    }
    docker rm --force $containerName 2>$null | Out-Null
}
