<#
.SYNOPSIS
    Run IPFLang examples interactively.

.DESCRIPTION
    Interactive helper script to explore and run example sets in the examples folder.
    Presents example categories (basics, composition, errors) and lets you select files to execute.
#>

$ExamplesRoot = $PSScriptRoot
if (-not $ExamplesRoot) { $ExamplesRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition }

Write-Host "`nIPFLang Examples - Interactive Runner" -ForegroundColor Cyan
Write-Host "Examples root: " -NoNewline -ForegroundColor DarkGray
Write-Host "$ExamplesRoot`n" -ForegroundColor White

# Discover example sets (directories)
$sets = Get-ChildItem -Path $ExamplesRoot -Directory | Sort-Object Name
if ($sets.Count -eq 0) { Write-Host "[red]Error:[/] No example sets found in $ExamplesRoot" -ForegroundColor Red; exit 1 }

function Show-Menu {
    Write-Host "Select which example set to explore:`n" -ForegroundColor Yellow
    $i = 1
    foreach ($s in $sets) {
        switch ($s.Name.ToLower()) {
            'basics' { $desc = 'Basic examples demonstrating core syntax and simple programs' }
            'composition' { $desc = 'Composition examples showing how to combine modules and features' }
            'errors' { $desc = 'Error examples illustrating common mistakes and diagnostics' }
            default { $desc = '' }
        }
        Write-Host "  [$i] " -NoNewline -ForegroundColor Cyan
        Write-Host "$($s.Name)" -NoNewline -ForegroundColor White
        Write-Host " - $desc" -ForegroundColor DarkGray
        $i++
    }
    Write-Host "`n  [Q] " -NoNewline -ForegroundColor Cyan
    Write-Host "Quit`n" -ForegroundColor White
}

function Choose-Set {
    while ($true) {
        Show-Menu
        $choice = Read-Host "Choose option"
        if ([string]::IsNullOrWhiteSpace($choice)) { continue }
        $choice = $choice.Trim()
        if ($choice -match '^[Qq]$') { 
            Write-Host "`n✗ Exiting.`n" -ForegroundColor Yellow
            exit 0 
        }
        if ($choice -as [int]) {
            $idx = [int]$choice - 1
            if ($idx -ge 0 -and $idx -lt $sets.Count) { return ,$sets[$idx] }
        }
        # try match by name
        $match = $sets | Where-Object { $_.Name -ieq $choice }
        if ($match) { return $match }
        Write-Host "✗ Invalid choice: '$choice'. Try again.`n" -ForegroundColor Red
    }
}


function Get-FileDescription {
    param([string]$filename)
    $name = [System.IO.Path]::GetFileNameWithoutExtension($filename)
    # remove common numeric prefixes like err_01_ or basics_01_
    $name = $name -replace '^[a-z]+_\d+_','' -replace '^[a-z]+_',''
    $readable = ($name -replace '[_-]+',' ')
    $readable = [System.Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($readable)
    return "$readable example"
}

$selectedSets = Choose-Set
$RepoRoot = Split-Path -Parent $ExamplesRoot
$cliProj = Join-Path $RepoRoot 'src\IPFLang.CLI\IPFLang.CLI.csproj'
if (Test-Path $cliProj) {
    Write-Host "`n✓ Using CLI project: " -NoNewline -ForegroundColor Green
    Write-Host "$cliProj`n" -ForegroundColor DarkGray
    $canRun = $true
} else {
    Write-Host "`n✗ CLI project not found. Files will only be listed.`n" -ForegroundColor Red
    $canRun = $false
}

foreach ($set in $selectedSets) {
    Write-Host "`nExample Set: " -NoNewline -ForegroundColor Yellow
    Write-Host "$($set.Name)`n" -ForegroundColor White
    
    $files = Get-ChildItem -Path $set.FullName -File -Recurse | Sort-Object FullName
    if ($files.Count -eq 0) { 
        Write-Host "✗ No files found in $($set.FullName)`n" -ForegroundColor Yellow
        continue 
    }

    Write-Host "Available examples ($($files.Count)):`n" -ForegroundColor Yellow
    $filesList = $files | ForEach-Object { $_ }
    $idx = 1
    foreach ($f in $filesList) {
        $desc = Get-FileDescription $f.Name
        Write-Host "  [$idx] " -NoNewline -ForegroundColor Cyan
        Write-Host "$desc " -NoNewline -ForegroundColor White
        Write-Host "($($f.Name))" -ForegroundColor DarkGray
        $idx++
    }
    Write-Host ""

    if ($canRun) {
        while ($true) {
            Write-Host "Enter " -NoNewline -ForegroundColor DarkGray
            Write-Host "number" -NoNewline -ForegroundColor Cyan
            Write-Host " or " -NoNewline -ForegroundColor DarkGray
            Write-Host "filename" -NoNewline -ForegroundColor Cyan
            Write-Host " to run; " -NoNewline -ForegroundColor DarkGray
            Write-Host "Enter" -NoNewline -ForegroundColor Cyan
            Write-Host " to skip; " -NoNewline -ForegroundColor DarkGray
            Write-Host "Q" -NoNewline -ForegroundColor Cyan
            Write-Host " to quit" -ForegroundColor DarkGray
            $sel = Read-Host "Choice"
            
            if ([string]::IsNullOrWhiteSpace($sel)) { 
                Write-Host "→ Skipping set`n" -ForegroundColor Yellow
                break 
            }
            if ($sel -match '^[Qq]$') { 
                Write-Host "`n✗ Exiting.`n" -ForegroundColor Yellow
                exit 0 
            }

            $fileToRun = $null
            if ($sel -as [int]) {
                $idx = [int]$sel - 1
                if ($idx -ge 0 -and $idx -lt $filesList.Count) { $fileToRun = $filesList[$idx] }
            } else {
                $fileToRun = $filesList | Where-Object { $_.Name -ieq $sel -or ([System.IO.Path]::GetFileNameWithoutExtension($_.Name) -ieq $sel) } | Select-Object -First 1
            }

            if (-not $fileToRun) {
                Write-Host "✗ No matching file for '$sel'. Try again.`n" -ForegroundColor Red
                continue
            }

            Write-Host "`n▶ Running: " -NoNewline -ForegroundColor Cyan
            Write-Host "$($fileToRun.Name)`n" -ForegroundColor White
            
            try {
                & dotnet run --project $cliProj -- run $fileToRun.FullName
                Write-Host "`n✓ Completed`n" -ForegroundColor Green
            } catch {
                Write-Host "`n✗ Failed: $_`n" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "✗ CLI project not available; cannot run examples`n" -ForegroundColor Yellow
    }

    Write-Host "Press " -NoNewline -ForegroundColor DarkGray
    Write-Host "Enter" -NoNewline -ForegroundColor Cyan
    Write-Host " to continue to next set or " -NoNewline -ForegroundColor DarkGray
    Write-Host "Q" -NoNewline -ForegroundColor Cyan
    Write-Host " to quit" -ForegroundColor DarkGray
    $resp = Read-Host "Choice"
    if ($resp -match '^[Qq]$') { 
        Write-Host "`n✗ Exiting.`n" -ForegroundColor Yellow
        exit 0 
    }
    continue
}

Write-Host "`n✓ All sets processed.`n" -ForegroundColor Green
