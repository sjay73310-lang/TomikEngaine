@echo off
setlocal
set "EDITOR_EXE=%~dp0build\TomkEngineEditor\Tomk.Editor.exe"

if not exist "%EDITOR_EXE%" (
  echo Tomk.Editor.exe was not found.
  echo Run this command first:
  echo dotnet publish "editor\Tomk.Editor\Tomk.Editor.csproj" -c Release -r win-x64 --self-contained false -o "build\TomkEngineEditor"
  pause
  exit /b 1
)

start "" "%EDITOR_EXE%"
