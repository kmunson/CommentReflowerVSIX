rem Check for existence first due to the reversed error output.
if not exist "%ProgramFiles(x86)%\HTML Help Workshop\hhc.exe" (
	echo HTML Help Workshop is not installed. https://www.microsoft.com/en-us/download/details.aspx?id=21138
	exit /B 1
)

rem hhc returns 0 on failure and 1 on success.
"%ProgramFiles(x86)%\HTML Help Workshop\hhc.exe" %1.hhp
if not errorlevel 1 (
	echo Failed to compile help.
	exit /B 1
)
