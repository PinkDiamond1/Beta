import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from './http.service';
import { ValidateSignatureRequest } from '../model/account/validateSignatureRequest';
import { LoginResponse } from '../model/account/loginResponse';
import { LoginRequest } from '../model/account/loginRequest';
import { LoginResult } from '../model/account/loginResult';
import { ConfirmEmailRequest } from '../model/account/confirmEmailRequest';
import { ForgotPasswordRequest } from '../model/account/forgotPasswordRequest';
import { RecoverPasswordRequest } from '../model/account/recoverPasswordRequest';
import { ChangePasswordRequest } from '../model/account/changePasswordRequest';
import { RegisterRequest } from '../model/account/registerRequest';
import { RegisterResponse } from '../model/account/registerResponse';
import { FeedResponse } from '../model/advisor/feedResponse';
import { ReferralProgramInfoResponse } from '../model/account/ReferralProgramInfoResponse';
import { SetReferralRequest } from '../model/account/setReferralRequest';
import { ConfigurationResponse } from '../model/account/configurationResponse';
import { ConfigurationRequest } from '../model/account/configurationRequest';
import { NavigationService } from './navigation.service';
import { DashboardResponse } from '../model/admin/dashboardresponse';
import { SearchResponse } from '../model/search/searchResponse';
import { SocialLoginRequest } from '../model/account/socialLoginRequest';
import { ValidReferralCodeResponse } from '../model/account/validReferralCodeResponse';
import { SetReferralCodeReponse } from '../model/account/setReferralCodeReponse';
import { WalletLoginInfoResponse } from '../model/account/walletLoginInfoResponse';
import { EarlyAccessRequest } from '../model/account/earlyAccessRequest';
import { EventsService } from 'angular-event-service/dist';
import { AssetResponse } from "../model/asset/assetResponse";
import { AdvisorResponse } from "../model/advisor/advisorResponse";

@Injectable()
export class AccountService {
  private validateSignatureUrl = this.httpService.apiUrl("/v1/accounts/me/signatures");
  private loginUrl = this.httpService.apiUrl("v1/accounts/login");
  private userDataUrl = this.httpService.apiUrl("v1/accounts/me");
  private socialLoginUrl = this.httpService.apiUrl("v1/accounts/social_login");
  private confirmationEmailUrl = this.httpService.apiUrl("v1/accounts/me/confirmations");
  private recoverPasswordUrl = this.httpService.apiUrl("v1/accounts/passwords/recover");
  private changePasswordUrl = this.httpService.apiUrl("v1/accounts/me/passwords");
  private registerUrl = this.httpService.apiUrl("v1/accounts");
  private meReferralsUrl = this.httpService.apiUrl("v1/accounts/me/referrals");
  private meWalletLoginUrl = this.httpService.apiUrl("v1/accounts/me/wallet_login");
  private referralsUrl = this.httpService.apiUrl("v1/accounts/referrals");
  private aucAmountUrl = this.httpService.apiUrl("v1/accounts/auc");
  private configurationUrl = this.httpService.apiUrl("v1/accounts/me/configuration");
  private dashboardUrl = this.httpService.apiUrl("v1/accounts/dashboard");
  private searchUrl = this.httpService.apiUrl("v1/accounts/search");
  private assetsFollowedUrl = this.httpService.apiUrl("v1/accounts/{id}/assets/following");
  private expertsFollowedUrl = this.httpService.apiUrl("v1/accounts/{id}/advisors/following");

  constructor(private httpService : HttpService, private navigationService: NavigationService, private eventsService: EventsService) { }

  validateSignature(validateSignatureRequest: ValidateSignatureRequest): Observable<LoginResponse> {
    return this.httpService.post(this.validateSignatureUrl, validateSignatureRequest);
  }

  login(loginRequest: LoginRequest) : Observable<LoginResult> {
    return this.httpService.post(this.loginUrl, loginRequest);
  }

  getUserData() : Observable<LoginResponse> {
    return this.httpService.get(this.userDataUrl);
  }

  socialLogin(socialLoginRequest: SocialLoginRequest) : Observable<LoginResult> {
    return this.httpService.post(this.socialLoginUrl, socialLoginRequest);
  }

  setLoginData(loginData: LoginResponse): void {
    this.httpService.setLoginData(loginData);
    this.eventsService.broadcast("onLogin", loginData);
  }

  getLoginData() : LoginResponse {
    return this.httpService.getLoginData();
  }

  getAccessToken() : string {
    return this.httpService.getAccessToken();
  }

  getUserEmail() : string {
    return this.httpService.getUserEmail();
  }

  logout() : void {
    this.httpService.logout();
    this.navigationService.goToHome();
  }

  logoutWithoutRedirect() : void {
    this.httpService.logout();
  }

  isLoggedIn() : boolean {
    return this.httpService.isLoggedIn();
  }

  resendEmailConfirmation() : Observable<LoginResult> {
    return this.httpService.put(this.confirmationEmailUrl, null);
  }

  confirmEmail(confirmEmailRequest: ConfirmEmailRequest) : Observable<LoginResult> {
    return this.httpService.post(this.confirmationEmailUrl, confirmEmailRequest);
  }

  forgotPassword(forgotPasswordRequest: ForgotPasswordRequest) : Observable<void> {
    return this.httpService.post(this.recoverPasswordUrl, forgotPasswordRequest);
  }
  
  recoverPassword(recoverPasswordRequest: RecoverPasswordRequest) : Observable<LoginResult> {
    return this.httpService.put(this.recoverPasswordUrl, recoverPasswordRequest);
  }

  changePassword(changePasswordRequest: ChangePasswordRequest) : Observable<void> {
    return this.httpService.put(this.changePasswordUrl, changePasswordRequest);
  }

  register(registerRequest: RegisterRequest) : Observable<RegisterResponse> {
    return this.httpService.post(this.registerUrl, registerRequest)
  }

  getReferralProgramInfo() : Observable<ReferralProgramInfoResponse> {
    return this.httpService.get(this.meReferralsUrl);
  }

  setReferralCode(referralCode: string) : Observable<SetReferralCodeReponse> {
    let request = new SetReferralRequest();
    request.referralCode = referralCode;
    return this.httpService.post(this.meReferralsUrl, request);
  }

  getWalletLoginInfo() : Observable<WalletLoginInfoResponse> {
    return this.httpService.get(this.meWalletLoginUrl);
  }

  isValidReferralCode(referralCode: string) : Observable<ValidReferralCodeResponse> {
    return this.httpService.get(this.referralsUrl + "?referralCode=" + referralCode);
  }

  getAUCAmount(address: string) : Observable<number> {
    return this.httpService.get(this.aucAmountUrl + "/" + address);
  }

  getConfiguration() : Observable<ConfigurationResponse>{
    return this.httpService.get(this.configurationUrl);
  }

  setConfiguration(configurationRequest: ConfigurationRequest) : Observable<void> {
    return this.httpService.post(this.configurationUrl, configurationRequest);
  }

  getDashboard() : Observable<DashboardResponse> {
    return this.httpService.get(this.dashboardUrl);
  }

  search(searchTerm: string) : Observable<SearchResponse> {
    return this.httpService.get(this.searchUrl + "?term=" + searchTerm);
  }
  
  hasInvestmentToCallLoggedAction():boolean{
    let loginData = this.getLoginData();
    if(!loginData){
      this.navigationService.goToLogin();
      return false;
    }
    else if (!!loginData && !loginData.isAdvisor) {
      this.navigationService.goToCompleteRegistration();
      return false;
    }
    return true;
  }

  getAssetsFollowedByUser(id: number): Observable<AssetResponse[]> {
    return this.httpService.get(this.assetsFollowedUrl.replace("{id}", id.toString()));
  }

  getExpertsFollowedByUser(id: number): Observable<AdvisorResponse[]> {
    return this.httpService.get(this.expertsFollowedUrl.replace("{id}", id.toString()));
  }
}
