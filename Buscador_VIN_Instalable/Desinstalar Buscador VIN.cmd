@echo off
del /Q "%USERPROFILE%\Desktop\Buscador Autosur Objetivos.lnk" 2>nul
taskkill /IM "Buscador Autosur Objetivos.exe" /F >nul 2>nul
rmdir /S /Q "%LOCALAPPDATA%\Buscador Autosur Objetivos\app" 2>nul
echo Se conserva el historial en la carpeta datos.
echo Buscador VIN desinstalado.
pause
