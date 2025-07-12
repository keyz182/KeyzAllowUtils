@ECHO OFF
ECHO Building KeyzAllowUtilities 1.6 - Started

dotnet --list-sdks | findstr /R /C:"8\..*" 1>nul
if %errorlevel% neq 0 (
    ECHO .NET 8 SDK not found. Please install it using the following command:
    ECHO winget install Microsoft.DotNet.SDK.8
    PAUSE > NUL
    EXIT /B
)

@ECHO ON
dotnet restore 1.6/Source/KeyzAllowUtilities.sln
dotnet build 1.6/Source/KeyzAllowUtilities.sln /p:Configuration=Release
@ECHO OFF
ECHO Building KeyzAllowUtilities 1.6 - Complete
ECHO Press any key to exit...
PAUSE > NUL
