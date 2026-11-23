# Create a self-signed certificate
$cert = New-SelfSignedCertificate -Subject "CN=NuGet Test Developer, OU=Use for testing purposes ONLY" `
                                  -FriendlyName "NuGetTestDeveloper" `
                                  -Type CodeSigning `
                                  -KeyUsage DigitalSignature `
                                  -KeyLength 2048 `
                                  -KeyAlgorithm RSA `
                                  -HashAlgorithm SHA256 `
                                  -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
                                  -CertStoreLocation "Cert:\CurrentUser\My"

# Export the certificate
$path = ".\sign.p12"
$pwd = ConvertTo-SecureString -String "CertificatePassword123" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $path -Password $pwd

# Clean up: Remove the certificate from the store after export
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("My", "CurrentUser")
$store.Open("ReadWrite")
$store.Remove($cert)
$store.Close()

Write-Host "Certificate has been created."