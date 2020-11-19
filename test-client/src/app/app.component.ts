import { Component } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { createAuthConfig } from './auth.config';
import { filter } from 'rxjs/operators';

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

  constructor(private oauthService: OAuthService) {
    // Automatically load user profile
    this.oauthService.events
      .pipe(filter(e => e.type === 'token_received'))
      .subscribe(_ => {
        console.debug('state', this.oauthService.state);
        this.oauthService.loadUserProfile();
      });
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
