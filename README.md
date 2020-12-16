# Fhi.Smittestopp.Verification
![Build and test](https://github.com/folkehelseinstituttet/Fhi.Smittestopp.Verification/workflows/Build%20and%20test/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/folkehelseinstituttet/Fhi.Smittestopp.Verification/badge.svg?branch=main)](https://coveralls.io/github/folkehelseinstituttet/Fhi.Smittestopp.Verification?branch=main) [![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)

## Getting startet

To run this application you will need the .NET Core 3.1 SDK. You can launch the web application through the `dotnet run` command from the `Fhi.Smittestopp.Verification.Server` folder.

### ID-porten

To perform logins through ID-porten (Test environment) you will also need a registered client for ID-porten Ver1 with "http://localhost:5001/signin-oidc" as a valid post login return url, and "https://localhost:5001/signout-callback-oidc" as a post logout return url. You will also need a valid test user to perform a login, ID-porten has a list of available test users [here](https://difi.github.io/felleslosninger/idporten_testbrukere.html)

Run the following command from the `Fhi.Smittestopp.Verification.Server` folder to use your own client.

1. `dotnet user-secrets set "idPorten:clientId" "<your-client-id>"`
2. `dotnet user-secrets set "idPorten:clientSecret" "<your-client-secret>"`

### Database

The default configuration attempts to create a MSSQL localdb which should work for most users. Change the connectionstring in appsettings.json or override the config if needed. A connection string value of `in-memory` will use an in-memory database instead of MSSQL.

### Certificate

In order to work with the Fhi.Smittestopp.App, you need to configure a local certificate

1. Generate a certificate for Local Computer, e.g. with PowerShell as Administrator: `New-SelfsignedCertificate -KeyExportPolicy Exportable -Subject "CN=SmittestoppIdCert" -KeySpec Signature -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -CertStoreLocation "cert:\LocalMachine\My"`
2. Copy the fingerprint of the certificate as `signingCredentials.Signing` in `appsettings.Development.json`
3. Start "Manage Computer Certificates" and find the certificate under Personal\Certificates. Right click > All tasks > Manage Private Keys and add your user to the list of users that can access the certificate
4. After starting the server, go to http://localhost:5001/.well-known/openid-configuration/jwks and copy the `x5c` value into `OAuthConf.cs` in Fhi.Smittestopp.App as the value of `OAUTH2_VERIFY_TOKEN_PUBLIC_KEY

### Running on http for localhost

Verification Server can run with http or https, but when running in Visual Studio, a self signed certificate will be used. This will be trusted by the app. The following steps must be in place to run on http:

1. ID-porten client must have http://localhost:5001/signin-oidc as a valid redirect uri (http, not https)
2. launchSettings.json must specificy `"applicationUrl": "http://localhost:5001"`
3. In runtimeconfig.template.json `"Microsoft.AspNetCore.SuppressSameSiteNone": "true"` must be specified


### Debugging with App and Verification server

The app must run on a real device, as Expose Notification framework is not enabled on the Android emulator or iPhone simulator. The app must be attached with debugger and you must have set up `adb` correctly.

1. Connect the phone
2. Make the phone able to connect to the host computer: In a terminal window, run `adb reverse tcp:5001 tcp:5001`
3. Make sure the app has the correct value for OAUTH2_VERIFY_TOKEN_PUBLIC_KEY in OAuthConf
4. Start the app from Visual Studio in the debugger
5. The app should now be able to authorize with ID-porten when reporting infection. You can debug both the app and the Verification server in Visual Studio


### Test client

A basic test SPA client has been included in this repository to test the OIDC logins against the application. To run this client you will need Node.js installed. You can then install necessary dependencies throught the command `npm install` in the `test-client` folder, and then launch the client through `npm run start` in the same folder. This client presents a "Login"-button to start the OIDC login flow, and on completed login presents the raw ID-token and access-token, all ID-token claims, and a button to log out.

## Contributing

For anyone who would like to contribute to this project, we refer to our [Code of Conduct](CODE_OF_CONDUCT.md).

We prefer any changes are discussed through raising an issue before implementing the change.
