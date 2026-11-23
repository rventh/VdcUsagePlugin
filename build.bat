@rem Copyright (c) Veeam Software Group GmbH.

@rem Cleanup build output directories
del /s /q .\src\Service\bin\
del /s /q .\content\any\any\ui-layout-config\*.*
del /s /q .\content\any\any\ui-content\*.*
del /s /q .\content\any\any\ui-script\*.*
del /s /q .\content\any\any\service\*.*
del /s /q .\content\any\any\agent\*.*

@rem Build UI
cd .\src\UI\
call npm install
call npm run build:production
cd ..\..

@rem Copy UI files into Nuget's content directory
xcopy .\src\UI\build\ .\content\any\any\ /S /E /Y

@rem Build Service & Agent
SET CONFIGURATION=Release
dotnet publish .\src\Service\MyPlugin.Service.csproj -c:%CONFIGURATION% -p:PublishProfile=FolderProfile

@rem Copy 
xcopy .\src\Service\bin\publish\ .\content\any\any\service\ /S /E /Y

@rem Pack plugin into .nupkg file
if not exist ".\build" mkdir .\build
dotnet pack .\src\Service\MyPlugin.Service.csproj --output ".\build" --no-build --no-dependencies --no-restore


set "CERT_PATH=sign.p12"
set "CERT_PASSWORD=CertificatePassword123"

@rem Check if the certificate file exists
if not exist "%CERT_PATH%" (
    echo Certificate file "%CERT_PATH%" not found.
    echo Please ensure you have created a self-signed certificate or have a valid certificate.
    echo For more information, refer to the documentation: https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package
    exit /b 1
)

@rem Sign .nupkg file
dotnet nuget sign .\build\*.nupkg --certificate-path "%CERT_PATH%" --certificate-password "%CERT_PASSWORD%" --timestamper http://timestamp.digicert.com --verbosity detailed