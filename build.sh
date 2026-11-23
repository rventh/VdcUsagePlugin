#!/bin/bash
# Copyright (c) Veeam Software Group GmbH.

# Remove specified directories and files
rm -rf ./src/Service/bin/
rm -rf ./content/any/any/ui-layout-config/*.*
rm -rf ./content/any/any/ui-content/*.*
rm -rf ./content/any/any/ui-script/*.*
rm -rf ./content/any/any/service/*.*
rm -rf ./content/any/any/agent/*.*

# Install and build the UI
cd ./src/UI/
npm install
npm run build:production
cd ../..

# Copy necessary files
cp ./src/UI/External/public/favicon.png ./src/UI/build/ui-content/favicon.png
cp -r ./src/UI/build/ ./content/any/any/

# Set configuration
CONFIGURATION=Release

# Publish projects
dotnet publish ./src/Service/MyPlugin.Service.csproj -c:$CONFIGURATION -p:PublishProfile=FolderProfile

# Copy published files
cp -r ./src/Service/bin/publish/ ./content/any/any/service/

# Create build directory if it doesn't exist
[ ! -d "./build" ] && mkdir ./build

# Pack plugin into .nupkg file
dotnet pack ./src/Service/MyPlugin.Service.csproj --output "./build" --no-build --no-dependencies --no-restore


CERT_PATH="sign.p12"
CERT_PASSWORD="CertificatePassword123"

# Check if the certificate file exists
if [ ! -f "$CERT_PATH" ]; then
  echo "Certificate file '$CERT_PATH' not found."
  echo "Please ensure you have created a self-signed certificate or have a valid certificate."
  echo "For more information, refer to the documentation: https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package"
  exit 1
fi

# Sign .nupkg file
dotnet nuget sign ./build/*.nupkg --certificate-path "$CERT_PATH" --certificate-password "$CERT_PASSWORD" --timestamper http://timestamp.digicert.com --verbosity detailed