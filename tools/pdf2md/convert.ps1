# Requires Python 3.10+ and PyMuPDF (see requirements.txt).
# Reads every listed source PDF and writes the matching folder under docs/specs.
# Re-running is idempotent: each output folder is regenerated from scratch.

param(
    [int]$MaxPages = 15,
    [int]$MaxChars = 60000
)

$ErrorActionPreference = "Stop"
$env:PYTHONIOENCODING = "utf-8"

$root = Split-Path -Parent (Split-Path -Parent -Path $PSScriptRoot)
$Sources = Join-Path $root "docs/specs/sources"
$Out = Join-Path $root "docs/specs"
$Convert = Join-Path $PSScriptRoot "convert.py"

if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
    throw "Python was not found on PATH. Install Python 3.10+ and 'pip install -r $PSScriptRoot\requirements.txt'."
}

# source filename -> output folder name.
$Docs = [ordered]@{
    "INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf" = "zwave-500-series-programmers-guide"
    "SDS13781 Z-Wave Application Command Class Specification.pdf"                = "application-command-class-specification"
    "SDS13782 Z-Wave Management Command Class Specification.pdf"                 = "management-command-class-specification"
    "SDS13783 Z-Wave Transport-Encapsulation Command Class Specification.pdf"    = "transport-encapsulation-command-class-specification"
    "SDS13784 Z-Wave Network-Protocol Command Class Specification.pdf"           = "network-protocol-command-class-specification"
    "Z-Wave Host API Specification.pdf"                                          = "zwave-host-api-specification"
}

foreach ($name in $Docs.Keys) {
    $pdf = Join-Path $Sources $name
    if (-not (Test-Path $pdf)) {
        throw "Missing source PDF: $pdf  (place it in docs/specs/sources/)"
    }
    Write-Host "==> $name"
    python $Convert $pdf -o $Out -s $Docs[$name] --max-pages $MaxPages --max-chars $MaxChars
    if ($LASTEXITCODE -ne 0) { throw "convert.py failed for $name (exit $LASTEXITCODE)" }
}

Write-Host "Done. Output under: $Out"
