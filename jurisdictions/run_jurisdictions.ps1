# Run jurisdictions script (moved to jurisdictions_definitions)
# Presents groups from the local bases folder, lets user select jurisdictions and runs compose with empty inputs

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$baseDir = Join-Path $scriptDir 'bases'
$jurDir = Join-Path $scriptDir 'jurisdictions'
$inputsFile = Join-Path $scriptDir 'empty_inputs.json'
$projectPath = Join-Path $repoRoot 'src\IPFLang.CLI'

if (-not (Test-Path $baseDir)) { Write-Error "Bases folder not found: $baseDir"; exit 1 }
if (-not (Test-Path $jurDir)) { Write-Error "Jurisdictions folder not found: $jurDir"; exit 1 }
if (-not (Test-Path $inputsFile)) { Write-Warning "Inputs file not found, proceeding without --inputs: $inputsFile" }

$baseItems = Get-ChildItem -Path $baseDir -Filter *.ipf | Sort-Object Name
if ($baseItems.Count -eq 0) { Write-Error "No base (.ipf) files found in $baseDir"; exit 1 }

# Build friendly display names by reading header comments or VERSION DESCRIPTION
$baseFiles = @()
foreach ($f in $baseItems) {
    $display = $f.Name
    try {
        $lines = Get-Content $f.FullName -TotalCount 40
        foreach ($l in $lines) {
            if ($l -match '#\s*IPFLang Base:\s*(.+)') { $display = $matches[1].Trim(); break }
            if ($l -match "DESCRIPTION\s+'([^']+)'") { $display = $matches[1].Trim(); break }
        }
    } catch {}
    $baseFiles += [pscustomobject]@{ File = $f; DisplayName = $display }
}

Write-Host "Available jurisdiction groups (bases):"
for ($i = 0; $i -lt $baseFiles.Count; $i++) {
    Write-Host "[$($i+1)] $($baseFiles[$i].DisplayName) [$($baseFiles[$i].File.Name)]"
}

$baseSel = Read-Host "Select base number to use (enter number)"
if (-not ($baseSel -as [int]) -or [int]$baseSel -lt 1 -or [int]$baseSel -gt $baseFiles.Count) {
    Write-Error "Invalid selection"; exit 1
}
$selectedBase = $baseFiles[[int]$baseSel - 1].File.FullName
$selectedBaseName = $baseFiles[[int]$baseSel - 1].DisplayName
Write-Host "Selected base: $selectedBaseName ($selectedBase)"

$provChoice = Read-Host "Show provenance/audit trail? (y/N)"
$provFlag = @()
if ($provChoice -match '^[Yy]') { $provFlag += '--provenance' }

$mode = Read-Host "Run (A)ll jurisdictions, (S)elect specific, or (Q)uit? [A/S/Q]"
switch ($mode.ToUpper()) {
    'Q' { Write-Host 'Quitting.'; exit 0 }
    'A' {
        $jurFiles = Get-ChildItem -Path $jurDir -Filter *.ipf | Sort-Object Name
    }
    'S' {
        $allJurs = Get-ChildItem -Path $jurDir -Filter *.ipf | Sort-Object Name
        for ($i = 0; $i -lt $allJurs.Count; $i++) {
            Write-Host "[$($i+1)] $($allJurs[$i].Name)"
        }
        $sel = Read-Host "Enter comma-separated jurisdiction numbers to run (e.g. 1,3,5)"
        $indices = $sel -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | ForEach-Object { [int]$_ }
        $jurFiles = @()
        foreach ($idx in $indices) {
            if ($idx -ge 1 -and $idx -le $allJurs.Count) { $jurFiles += $allJurs[$idx - 1] }
        }
        if ($jurFiles.Count -eq 0) { Write-Error "No valid jurisdictions selected"; exit 1 }
    }
    Default { Write-Error "Unknown option"; exit 1 }
}

foreach ($jur in $jurFiles) {
    Write-Host "`n========== Running: $($jur.Name) (composed with $selectedBaseName) =========="
    $args = @('run','--project',$projectPath,'--','compose', $selectedBase, $jur.FullName)
    if (Test-Path $inputsFile) { $args += @('--inputs', $inputsFile) }
    if ($provFlag.Count -gt 0) { $args += $provFlag }

    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Command failed for $($jur.Name) with exit code $LASTEXITCODE"
    }
}

Write-Host "`nAll done."
