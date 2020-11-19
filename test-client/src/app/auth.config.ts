import { AuthConfig } from 'angular-oauth2-oidc';
import { environment } from 'src/environments/environment';

export function createAuthConfig(scopes: string = null, queryParams: object = null): AuthConfig {
  console.log('creating config', scopes, queryParams);

  return {
    // Url of the Identity Provider
    issuer: environment.authIssuer,

    // URL of the SPA to redirect the user to after login
    // redirectUri: window.location.origin
    //   + ((localStorage.getItem('useHashLocationStrategy') === 'true')
    //     ? '/#/index.html'
    //     : '/index.html'),

    redirectUri: window.location.origin + "/", // + '/index.html',

    responseType: 'code',

    // The SPA's id. The SPA is registerd with this id at the auth-server
    clientId: environment.clientId,

    // set the scope for the permissions the client should request
    scope: scopes ?? 'openid verification-info upload-api',

    // silentRefreshShowIFrame: true,

    customQueryParams: queryParams,

    showDebugInformation: true,

    sessionChecksEnabled: true

    // timeoutFactor: 0.01,
  }
};