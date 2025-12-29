@echo off
setlocal

REM Путь к корню репозитория
cd /d d:\MyIS

echo Starting backend (dotnet watch run) on http://localhost:5000 ...
start "MyIS Backend" cmd /k "cd /d d:\MyIS && dotnet build backend/src/Core.WebApi/MyIS.Core.WebApi.csproj && dotnet watch run --project backend/src/Core.WebApi/MyIS.Core.WebApi.csproj --urls http://localhost:5000"

echo Starting frontend (Vite dev server) on http://0.0.0.0:5173 ...
start "MyIS Frontend" cmd /k "cd /d d:\MyIS\frontend && npm run dev -- --host 0.0.0.0 --port 5173"

echo.
echo Backend and frontend are starting in separate windows.
echo Backend:  http://localhost:5000   or   http://<IP>:5000
echo Frontend: http://localhost:5173   or   http://<IP>:5173
echo.

endlocal
