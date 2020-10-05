import { Component } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './auth.config';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'test-client';

  loggedIn: boolean = false;

  idToken: string;
  accessToken: string;
  grantedScopes: any;
  identityClaims: any;

  constructor(private oauthService: OAuthService) {
    this.oauthService.configure(authConfig);
    this.oauthService.loadDiscoveryDocumentAndTryLogin().then(_ => {
      if (this.oauthService.hasValidIdToken()) {
        this.loggedIn = true;
        this.idToken = this.oauthService.getIdToken();
        this.accessToken = this.oauthService.getAccessToken();
        this.grantedScopes = this.oauthService.getGrantedScopes();
        this.identityClaims = this.oauthService.getIdentityClaims();
      }
    });

    
    // Automatically load user profile
    this.oauthService.events
      .pipe(filter(e => e.type === 'token_received'))
      .subscribe(_ => {
        console.debug('state', this.oauthService.state);
        this.oauthService.loadUserProfile();
      });
  }

  startLogin() {
    this.oauthService.initLoginFlow();
  }

  startLogout() {
    this.oauthService.revokeTokenAndLogout();
  }
  
}
