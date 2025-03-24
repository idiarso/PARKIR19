# Script untuk menghasilkan dokumentasi ParkIRC

# Fungsi untuk mengecek dependencies
function Check-Dependencies {
    Write-Host "Checking required dependencies..."
    
    # Check Node.js
    $nodeVersion = node -v
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Node.js tidak ditemukan. Silakan install Node.js dari https://nodejs.org/"
        exit 1
    }
    
    # Check npm packages
    npm list -g md-to-pdf
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installing md-to-pdf..."
        npm install -g md-to-pdf
    }
}

# Fungsi untuk mengkonversi Markdown ke PDF
function Convert-ToPDF {
    param (
        [string]$inputFile,
        [string]$outputFile
    )
    
    Write-Host "Converting $inputFile to PDF..."
    md-to-pdf $inputFile --output $outputFile
}

# Main script
Write-Host "=== ParkIRC Documentation Generator ==="

# Buat direktori docs jika belum ada
if (-not (Test-Path "docs")) {
    New-Item -ItemType Directory -Path "docs"
}

# Copy file markdown ke direktori docs
Copy-Item "ParkIRC-Manual.md" "docs/"
Copy-Item "ParkIRC-Quick-Guide.md" "docs/"

# Buat direktori untuk screenshots
if (-not (Test-Path "docs/images")) {
    New-Item -ItemType Directory -Path "docs/images"
}

# Generate PDF files
Check-Dependencies
Convert-ToPDF "docs/ParkIRC-Manual.md" "docs/ParkIRC-Manual.pdf"
Convert-ToPDF "docs/ParkIRC-Quick-Guide.md" "docs/ParkIRC-Quick-Guide.pdf"

Write-Host "Documentation generated successfully in 'docs' directory"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Add screenshots to 'docs/images' directory"
Write-Host "2. Update markdown files with screenshot references"
Write-Host "3. Re-run this script to generate updated PDFs" 