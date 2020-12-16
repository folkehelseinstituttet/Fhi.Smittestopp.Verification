# Fhi.Smittestopp.Verification
![Build and test](https://github.com/folkehelseinstituttet/Fhi.Smittestopp.Verification/workflows/Build%20and%20test/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/folkehelseinstituttet/Fhi.Smittestopp.Verification/badge.svg?branch=main)](https://coveralls.io/github/folkehelseinstituttet/Fhi.Smittestopp.Verification?branch=main) [![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)

## Getting started

To run this application you will need the .NET Core 3.1 SDK.
You can launch the web application through the `dotnet run` command from the `Fhi.Smittestopp.Verification.Server` folder.

### ID-porten

To perform logins through ID-porten (Test environment) you will also need a registered client for ID-porten Ver1 with "https://localhost:5001/signin-oidc" as a valid post login return url, and "https://localhost:5001/signout-callback-oidc" as a post logout return url.
You will also need a valid test user to perform a login, ID-porten has a list of available test users [here](https://difi.github.io/felleslosninger/idporten_testbrukere.html)

Run the following command from the `Fhi.Smittestopp.Verification.Server` folder to use your own client.

`dotnet user-secrets set "idPorten:clientId" "<your-client-id>"`

`dotnet user-secrets set "idPorten:clientSecret" "<your-client-secret>"`

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
You must also grant access to the private key of the certificate to the user that will be running the application.

### Test client

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
