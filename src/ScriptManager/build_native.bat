if "%1" == "release" (dotnet publish -c Release) else (dotnet publish -c Debug)