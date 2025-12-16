# Convert IPFLang article from Markdown to Word format
# This script works from its own directory and supports both local pandoc and Docker

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$inputFile = Join-Path $scriptDir "IPFLang_CSI_Article.md"
$outputFile = Join-Path $scriptDir "IPFLang_CSI_Article.docx"

Write-Host "Converting IPFLang_CSI_Article.md to DOCX format..." -ForegroundColor Cyan
Write-Host "Input file: $inputFile" -ForegroundColor Gray
Write-Host "Output file: $outputFile" -ForegroundColor Gray

# Check if local pandoc is available
$pandocLocal = Get-Command pandoc -ErrorAction SilentlyContinue

if ($pandocLocal) {
    Write-Host "`nUsing local pandoc installation..." -ForegroundColor Yellow
    Set-Location $scriptDir
    & pandoc -s IPFLang_CSI_Article.md -o IPFLang_CSI_Article.docx --mathml
    $exitCode = $LASTEXITCODE
} else {
    Write-Host "`nLocal pandoc not found, trying Docker..." -ForegroundColor Yellow
    
    # Check if Docker is available
    $dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCmd) {
        Write-Host "`nERROR: Neither pandoc nor Docker is available." -ForegroundColor Red
        Write-Host "Please install either:" -ForegroundColor Yellow
        Write-Host "  1. Pandoc: https://pandoc.org/installing.html" -ForegroundColor Yellow
        Write-Host "  2. Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
        exit 1
    }
    
    # Run pandoc in Docker with volume mounted to repo root
    docker run --rm -v "${repoRoot}:/data" pandoc/core -s /data/article/IPFLang_CSI_Article.md -o /data/article/IPFLang_CSI_Article.docx --mathml
    $exitCode = $LASTEXITCODE
}

if ($exitCode -eq 0) {
    if (Test-Path $outputFile) {
        $fileSize = (Get-Item $outputFile).Length
        Write-Host "`nConversion successful!" -ForegroundColor Green
        Write-Host "Output file: $outputFile" -ForegroundColor Green
        Write-Host "File size: $([math]::Round($fileSize/1KB, 2)) KB" -ForegroundColor Green
    } else {
        Write-Host "`nConversion reported success but output file not found!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "`nConversion failed with exit code $exitCode" -ForegroundColor Red
    exit $exitCode
}