# Fhi.Smittestopp.Verification
![Build and test](https://github.com/folkehelseinstituttet/Fhi.Smittestopp.Verification/workflows/Build%20and%20test/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/folkehelseinstituttet/Fhi.Smittestopp.Verification/badge.svg?branch=main)](https://coveralls.io/github/folkehelseinstituttet/Fhi.Smittestopp.Verification?branch=main) [![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)

## Getting started

To run this application you will need the .NET Core 3.1 SDK.
You can launch the web application through the `dotnet run` command from the `Fhi.Smittestopp.Verification.Server` folder.

### Libraries and Frameworks

This application makes use of a number of libraries and frameworks that one would need to be familiar with when developing it further.

- [IdentityServer](https://identityserver4.readthedocs.io/en/latest/): OpenID Connect and OAuth 2.0 framework
- [MediatR](https://github.com/jbogard/MediatR): Simple mediator for messages and message handlers (IRequest and IRequestHandler)
- [Optional](https://github.com/nlkl/Optional): A different approach to handling missing values.
- [Serilog](https://github.com/serilog/serilog): Provides the different log sinks used in different environments
- [AnonymousTokens](https://github.com/HenrikWM/anonymous-tokens): Provides tools for exhanging a JWT-token for an anonymous token

Additionally, the following libraries are used for testing

- [NUnit](https://github.com/nunit/nunit): The test framework used
- [Moq](https://github.com/moq/moq4) (with [Moq.Automock](https://github.com/moq/Moq.AutoMocker)): Tools for arranging tests.
- [FluentAssertions](https://fluentassertions.com/introduction): More understandable asserts.


### ID-porten

To perform logins through ID-porten (Test environment) you will also need a registered client for ID-porten Ver1 with "https://localhost:5001/signin-oidc" as a valid post login return url, and "https://localhost:5001/signout-callback-oidc" as a post logout return url (add http:// equivalent return urls if you intent to use the "SelfHost-HttpAndMsisMock" launch profile).
You will also need a valid test user to perform a login, ID-porten has a list of available test users [here](https://difi.github.io/felleslosninger/idporten_testbrukere.html)

Run the following command from the `Fhi.Smittestopp.Verification.Server` folder to use your own client.

1. `dotnet user-secrets set "idPorten:clientId" "<your-client-id>"`
2. `dotnet user-secrets set "idPorten:clientSecret" "<your-client-secret>"`

### Database

The default configuration uses MSSQLLocalDB.
This should work out of the box, but if not, you can change the connection string in appsettings.json to suit your environment (connectionString:verificationDb).
A connection string value of `in-memory` will use an in-memory database instead.

### Launch profiles

Multiple different launch profiles have been created to suit different needs.

- **SelfHosted**:
  This is the default launch profile used during internal development by FHI.
  This profile depends on the certificate used by the MSIS-client being installed, but uses dev signing credentials for tokens.
  This launch profile tries to connect to the MSIS test-environment, which may not be available from external networks / external developers.
- **SelfHosted-MsisMock**:
  This launch profile is identical to the default profile, with the exception of the MSIS integration using a mock-implementation.
  This profile does not depend on any certificates being installed, nor access to the MSIS test-environment.
- **SelfHosted-HttpAndMsisMock**:
  This launch profile has the same changes as SelfHosted-MsisMock, with some additional changes to enable configuring the Smittestopp app to use a local verification solution.
  This includes changing the token signing credentials to usa a fixed X509Certificate, changing the application url to http, and using SameSite=Lax for cookies. This profile depends on the token signing certificate being installed.

### Certificates

The solution depends on different certificates depending on configuration.
A client certificate is used to connect to the MSIS-gateway when not using the MSIS mock-implementation.
The JWT-tokens are also signed using a X509Certificate, unless the setting for dev signing credentials is enabled.

Some setup may be required for your development environment.
For simplicity, the dev environment has been configured to use the same self signed certificate for all use cases (but the certificates may be ignored, depending on the launch profile used).
Install the self signed certificate dev-cert.pfx (password='dev', valid until 2021-11-18) to the "Local Machine" store location to support all the launch profiles above without changing the configuration, or alternatively change the config to use your own certificate.
You must also grant access to the private key of the certificate to the user that will be running the application (step 3 below).

In order to work with the Fhi.Smittestopp.App, you need to configure a local certificate using the following steps (skip to step 3. if you have installed the dev-cert.pfx from this repo)

1. Generate a certificate for Local Computer, e.g. with PowerShell as Administrator: `New-SelfsignedCertificate -KeyExportPolicy Exportable -Subject "CN=SmittestoppIdCert" -KeySpec Signature -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -CertStoreLocation "cert:\LocalMachine\My"`
2. Copy the fingerprint of the certificate as `signingCredentials.Signing` in `appsettings.Development.json`
3. Start "Manage Computer Certificates" and find the certificate under Personal\Certificates. Right click > All tasks > Manage Private Keys and add your user to the list of users that can access the certificate
4. After starting the server, go to http://localhost:5001/.well-known/openid-configuration/jwks and copy the `x5c` value into `OAuthConf.cs` in Fhi.Smittestopp.App as the value of `OAUTH2_VERIFY_TOKEN_PUBLIC_KEY

## Combining App and Verification in local development environment

### Running on http for localhost

Verification Server can run with http or https, but when running in Visual Studio, a self signed certificate will be used, which then will have to be trusted by the app. To avoid this problem, you could run the app on http using the following steps:

1. Your ID-porten client must have http://localhost:5001/signin-oidc as a valid redirect uri (http, not https)
2. Use the launch profile "SelfHost-HttpAndMsisMock" when starting the application

### Debugging with App and Verification server

The app must run on a real device, as Expose Notification framework is not enabled on the Android emulator or iPhone simulator. The app must be attached with debugger and you must have set up `adb` correctly.

1. Connect the phone
2. Make the phone able to connect to the host computer: In a terminal window, run `adb reverse tcp:5001 tcp:5001`
3. Make sure the app has the correct value for OAUTH2_VERIFY_TOKEN_PUBLIC_KEY in OAuthConf
4. Start the app from Visual Studio in the debugger
5. The app should now be able to authorize with ID-porten when reporting infection. You can debug both the app and the Verification server in Visual Studio

## Test client

A basic test SPA client has been included in this repository to test the OIDC logins against the application.
To run this client you will need Node.js installed.
You can then install necessary dependencies throught the command `npm install` in the `test-client` folder, and then launch the client through `npm run start` in the same folder.
A number of alternate start commands has been added to point the test-client to different verification environments, check the test client's [package.json](test-client/package.json) file for a complete list.

You first need to initialize the test-client for the appropriate setup.
To emulate the Smittestopp-application, use the default settings and initialize using "Init (DK-compatible)".
After initialization, the client presents a "Login"-button to start the OIDC login flow, and on completed login presents the raw ID-token and access-token, all ID-token claims, and a button to log out.
NB! You will also need to initialize the test-client after a completed login, for the client to retrieve and present the login result (this may take some time in some cases).

## Contributing

For anyone who would like to contribute to this project, we refer to our [Code of Conduct](CODE_OF_CONDUCT.md).

We do prefer that any significant changes are discussed through raising an issue before implementing the change.
