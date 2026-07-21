# PowerShell Compile Script for Native WPF Etern-Notes

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outputExe = "C:\Users\paucr\.gemini\antigravity\scratch\Etern-Notes\EternNotes.exe"

$sourceFiles = @(
    "C:\Users\paucr\.gemini\antigravity\scratch\Etern-Notes\Models.cs",
    "C:\Users\paucr\.gemini\antigravity\scratch\Etern-Notes\WpfVectorIcons.cs",
    "C:\Users\paucr\.gemini\antigravity\scratch\Etern-Notes\WpfMainWindow.cs"
)

# WPF Assembly Paths
$wpfDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$references = @(
    "System.dll",
    "System.Runtime.Serialization.dll",
    "System.Xaml.dll",
    "$wpfDir\PresentationFramework.dll",
    "$wpfDir\PresentationCore.dll",
    "$wpfDir\WindowsBase.dll"
)

Write-Host "Compilando aplicación nativa WPF C#..." -ForegroundColor Cyan

# Compile arguments
# /target:winexe ensures it runs as a pure Windows Application with no console popup.
$args = @(
    "/target:winexe",
    "/out:$outputExe",
    "/optimize"
)

foreach ($ref in $references) {
    $args += "/reference:$ref"
}

$args += $sourceFiles

& $compiler $args

if ($LASTEXITCODE -eq 0) {
    Write-Host "¡Compilación nativa exitosa! Archivo creado en: $outputExe" -ForegroundColor Green

    # Create Desktop Shortcut
    Write-Host "Creando acceso directo en el Escritorio..." -ForegroundColor Cyan
    try {
        $WshShell = New-Object -ComObject WScript.Shell
        $desktopPath = [System.IO.Path]::Combine([System.Environment]::GetFolderPath("Desktop"), "EternNotes.lnk")
        $Shortcut = $WshShell.CreateShortcut($desktopPath)
        $Shortcut.TargetPath = $outputExe
        $Shortcut.WorkingDirectory = "C:\Users\paucr\.gemini\antigravity\scratch\Etern-Notes"
        $Shortcut.Description = "Native Dark Developer Task & Notes Planner"
        $Shortcut.Save()
        Write-Host "¡Acceso directo creado con éxito en: $desktopPath!" -ForegroundColor Green
    }
    catch {
        Write-Warning "No se pudo crear el acceso directo en el Escritorio. Detalles: $_"
    }
} else {
    Write-Error "Error de compilación nativa. Por favor revisa los mensajes de error arriba."
}
