@echo off

CHOICE /C YNC /M "Would you like to (Y)Activate or (N)Deactivate this virtual environment? Or (C)Cancel?"

if '%choice%'=='1' goto activate
if '%choice%'=='2' goto deactivate
if '%choice%'=='3' goto cancel


:activate
cd /d %~dp0
cmd.exe /k Scripts\activate


:deactivate
cd /d %~dp0
cmd.exe /k Scripts\deactivate

:cancel
goto end

:end
pause