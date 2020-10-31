# Fhi.Smittestopp.Verification
![Build and test](https://github.com/folkehelseinstituttet/Fhi.Smittestopp.Verification/workflows/Build%20and%20test/badge.svg) [![Coverage Status](https://coveralls.io/repos/github/folkehelseinstituttet/Fhi.Smittestopp.Verification/badge.svg?branch=main)](https://coveralls.io/github/folkehelseinstituttet/Fhi.Smittestopp.Verification?branch=main) [![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE)

## Getting started

To run this application you will need the .NET Core 3.1 SDK. You can launch the web application through the `dotnet run` command from the `Fhi.Smittestopp.Verification.Server` folder.

### ID-porten

To perform logins through ID-porten (Test environment) you will also need a registered client for ID-porten Ver1 with "https://localhost:5001/signin-oidc" as a valid post login return url, and "https://localhost:5001/signout-callback-oidc" as a post logout return url. You will also need a valid test user to perform a login, ID-porten has a list of available test users [here](https://difi.github.io/felleslosninger/idporten_testbrukere.html)

Run the following command from the `Fhi.Smittestopp.Verification.Server` folder to use your own client.

`dotnet user-secrets set "idPorten:clientId" "<your-client-id>"`

`dotnet user-secrets set "idPorten:clientSecret" "<your-client-secret>"`

### Test client

A basic test SPA client has been included in this repository to test the OIDC logins against the application. To run this client you will need Node.js installed. You can then install necessary dependencies throught the command `npm install` in the `test-client` folder, and then launch the client through `npm run start` in the same folder. This client presents a "Login"-button to start the OIDC login flow, and on completed login presents the raw ID-token and access-token, all ID-token claims, and a button to log out.
