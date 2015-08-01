call clean %1
if errorlevel 1 exit /B 1
call build %1
if errorlevel 1 exit /B 1