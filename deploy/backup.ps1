<#
.SYNOPSIS
    Nightly backup of the Triathlon site on Windows: the database, then the uploaded media.

.DESCRIPTION
    Handles either database provider. PostgreSQL is dumped with pg_dump in custom format; SQL Server
    is backed up with its own BACKUP DATABASE, because a .bak is what a Windows DBA will expect to
    restore from and it is the only form that keeps the log chain intact.

    Register with Task Scheduler to run daily as a service account that can read the media folder:
        schtasks /create /tn "Triathlon backup" /tr "powershell -NoProfile -File C:\deploy\backup.ps1" `
                 /sc daily /st 02:15 /ru "DOMAIN\svc-triathlon"
#>
[CmdletBinding()]
param(
    [ValidateSet('Postgres', 'SqlServer')]
    [string]$Provider = $env:Database__Provider,

    [string]$ConnectionString = $env:ConnectionStrings__Default,

    [string]$BackupDir = 'C:\Backups\Triathlon',

    [string]$MediaDir = 'C:\ProgramData\Triathlon\media',

    [int]$RetentionDays = 30
)

$ErrorActionPreference = 'Stop'

if (-not $Provider) { $Provider = 'Postgres' }
if (-not $ConnectionString) { throw 'ConnectionStrings__Default is not set.' }

New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

# The connection string is the single source of truth for where the database lives; its fields are
# read out of it rather than duplicated in parameters that could drift from the running service.
$fields = @{}
foreach ($pair in $ConnectionString.Split(';')) {
    if ($pair -match '^\s*([^=]+)=(.*)$') { $fields[$Matches[1].Trim().ToLower()] = $Matches[2].Trim() }
}

if ($Provider -eq 'Postgres') {
    $env:PGHOST     = $fields['host']
    $env:PGPORT     = $fields['port']
    $env:PGUSER     = $fields['username']
    $env:PGPASSWORD = $fields['password']

    $database = $fields['database']
    $dump = Join-Path $BackupDir "triathlon-$stamp.dump"

    pg_dump --format=custom --file=$dump $database
    if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }

    Write-Output "Database  -> $dump"
}
else {
    # BACKUP DATABASE writes on the server, so the path has to be one SQL Server itself can reach.
    $database = if ($fields.ContainsKey('database')) { $fields['database'] } else { $fields['initial catalog'] }
    $bak = Join-Path $BackupDir "triathlon-$stamp.bak"

    $query = "BACKUP DATABASE [$database] TO DISK = N'$bak' WITH INIT, COMPRESSION, CHECKSUM, STATS = 10;"
    sqlcmd -b -Q $query -S $fields['server'] -d master
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE." }

    Write-Output "Database  -> $bak"
}

# Media is not in the database and is not regenerable, so it is backed up with it.
if (Test-Path $MediaDir) {
    $archive = Join-Path $BackupDir "media-$stamp.zip"
    Compress-Archive -Path (Join-Path $MediaDir '*') -DestinationPath $archive -CompressionLevel Optimal
    Write-Output "Media     -> $archive"
}
else {
    Write-Output "Media     -> skipped, $MediaDir does not exist"
}

# Retention last, and only after the new backup succeeded — never delete yesterday's copy before
# today's exists.
$cutoff = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem -Path $BackupDir -File |
    Where-Object { $_.Name -match '^(triathlon|media)-' -and $_.LastWriteTime -lt $cutoff } |
    ForEach-Object {
        Write-Output "Removing  -> $($_.FullName)"
        Remove-Item $_.FullName -Force
    }

Write-Output "Done. Retention: $RetentionDays days."
