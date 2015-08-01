copy /Y ..\gpl.rtf ..\CommentReflower
if errorlevel 1 exit /B 1

rem hhc returns 0 on failure and 1 on success
hhc %1.hhp
if not errorlevel 1 exit /B 1

copy /Y %1.chm ..\CommentReflower
if errorlevel 1 exit /B 1