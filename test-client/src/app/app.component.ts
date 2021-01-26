import { Component } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { createAuthConfig } from './auth.config';
import { filter } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'test-client';

  loggedIn: boolean = false;
  initialised: boolean = false;
  forceLoginPrompt: boolean = true;

  idToken: string;
  accessToken: string;
  grantedScopes: any;
  identityClaims: any;

  anonymousTokenResponse: any;
  anonymousTokenError: string;

  constructor(private oauthService: OAuthService, private http: HttpClient) {
    // Automatically load user profile
    this.oauthService.events
      .pipe(filter(e => e.type === 'token_received'))
      .subscribe(_ => {
        console.debug('state', this.oauthService.state);
        this.oauthService.loadUserProfile();
      });

    // remove this line to allow selecting init mode
    this.initDkCompatibleMode();
  }

  initDefaultMode() {
    this.initOauth('openid verification-info upload-api', this.forceLoginPrompt);
  }

  initDkCompatibleMode() {
    this.initOauth('openid smittestop', this.forceLoginPrompt);
  }

  startLogin() {
    this.oauthService.initLoginFlow();
  }

  startLogout() {
    this.oauthService.revokeTokenAndLogout();
  }

  reset() {
    this.initialised = false;
    this.loggedIn = false;
    this.idToken = null;
    this.accessToken = null;
    this.grantedScopes = null;
    this.identityClaims = null;
    this.anonymousTokenResponse = null;
    this.anonymousTokenError = null;
  }

  async requestAnonymousToken(): Promise<any> {
    if (this.accessToken == null) {
      return null;
    }

    this.anonymousTokenResponse = null;
    this.anonymousTokenError = null;

    var tokenRequest = {
      pAsHex: "0441d6f9552b03d9faf1a079b73f3d658f00879c5d3ceb3b49a4355defa6c70d280e285449c19ba3fe251e6d25d76d14c154d437ab21d42c06f6ceed548276b1ac"
    };
    
    try {
      var response = await this.http.post(
        environment.authIssuer + "/api/anonymoustokens",
        tokenRequest,
        {
          headers: {
            "Authorization": "Bearer " + this.accessToken
          }
        })
      .toPromise();
  
      this.anonymousTokenResponse = response;
      return this.anonymousTokenResponse;
    } catch (e) {
      this.anonymousTokenError = e && e.message ? e.message : e;
      return null;
    }
  }

  private initOauth(scopes: string = null, forceLoginPrompt: boolean = false) {
    this.initialised = true;
    let customParams = forceLoginPrompt ? {
      prompt: 'login'
    } : null;
    this.oauthService.configure(createAuthConfig(scopes, customParams));
    this.oauthService.loadDiscoveryDocumentAndTryLogin().then(_ => {
      if (this.oauthService.hasValidIdToken()) {
        this.loggedIn = true;
        this.idToken = this.oauthService.getIdToken();
        this.accessToken = this.oauthService.getAccessToken();
        this.grantedScopes = this.oauthService.getGrantedScopes();
        this.identityClaims = this.oauthService.getIdentityClaims();
      }
    });
  }
  
}
