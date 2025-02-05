import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AccountService } from '../services/account.service';
import { LoginResponse} from '../model/account/loginResponse';
import { LocalStorageService } from '../services/local-storage.service';
import { NavigationService } from '../services/navigation.service';
import { Observable } from 'rxjs';

@Injectable()
export class AuthRedirect implements CanActivate {

  constructor(private accountService: AccountService, private navigationService: NavigationService, private localStorageService: LocalStorageService) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) : Observable<boolean> {
    let loginData = this.accountService.getLoginData();
    if (!!loginData) {
      return new Observable (observer => 
        {
          this.redirect(loginData, state.url).subscribe(ret => observer.next(!ret));
        });
    } else {
      this.redirectToLogin(state.url);
    }
    return new Observable (observer => observer.next(false));
  }

  redirectAfterLoginAction(loginResponse?: LoginResponse) {
    let loginData;
    if (!!loginResponse) {
      loginData = loginResponse;
    } else {
      loginData = this.accountService.getLoginData();
    }
    if (!!loginData) {
      this.redirect(loginData).subscribe(redirected =>
        {
          if (!redirected) {
            var redirectUrl = this.localStorageService.getLocalStorage("redirectUrl");
            this.localStorageService.removeLocalStorage("redirectUrl");
            if (redirectUrl && !redirectUrl.includes("login=true")) {
              this.navigationService.goToUrl(redirectUrl);
            } else {
              if(loginData.hasInvestment){
                this.navigationService.goToPortfolio();
              }
              else{
                this.navigationService.goToTradeMarkets();
              }
            }
          }
        });
    }
  }

  redirectToLogin(currentUrl?: string) {
    if (currentUrl && !currentUrl.includes("login=true")) {
      this.localStorageService.setLocalStorage("redirectUrl", currentUrl);
    }
    this.navigationService.goToLogin();
  }

  private redirect(loginResponse: LoginResponse, currentUrl?: string) : Observable<boolean> {
    if(this.navigationService.isSameRoute('wallet-login', currentUrl)) {
      return new Observable (observer => observer.next(false));
    } else if (!loginResponse.isAdvisor) {
        return new Observable (observer => 
          {
            this.accountService.getUserData().subscribe(ret =>
            {
              this.accountService.setLoginData(ret);
              if (!ret.isAdvisor) {
                this.navigationService.goToCompleteRegistration();
                return observer.next(true);
              } else {
                return observer.next(false);
              }
            }, () => observer.next(false));
          });
    } else {
      return new Observable (observer => observer.next(false));
    }
  }
}
