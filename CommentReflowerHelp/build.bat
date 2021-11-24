rem Check for existence first due to the reversed error output.
if not exist "%ProgramFiles(x86)%\HTML Help Workshop\hhc.exe" (
	echo HTML Help Workshop is not installed. https://web.archive.org/web/20200918004813/https://download.microsoft.com/download/0/A/9/0A939EF6-E31C-430F-A3DF-DFAE7960D564/htmlhelp.exe
	exit /B 1
)

rem hhc returns 0 on failure and 1 on success.
"%ProgramFiles(x86)%\HTML Help Workshop\hhc.exe" %1.hhp
if not errorlevel 1 (
	echo Failed to compile help.
	exit /B 1
) else (
	exit /B 0
)