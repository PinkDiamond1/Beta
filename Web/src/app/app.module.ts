import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { SimpleNotificationsModule } from 'angular2-notifications';
import { ChartModule,HIGHCHARTS_MODULES } from 'angular-highcharts';
import stock from 'highcharts/modules/stock.src';
import more from 'highcharts/highcharts-more.src';

export function highchartsModules() {
  // apply Highcharts Modules to this array
  return [stock, more];
}

import { AppComponent } from './app.component';

import { Web3Service } from './services/web3.service';
import { LocalStorageService } from './services/local-storage.service';
import { HttpService } from './services/http.service';
import { AssetHistoryChartComponent } from './components/asset/asset-history-chart/asset-history-chart.component';
import { RecommendationDistributionComponent } from './components/recommendation-distribution/recommendation-distribution.component';
import { AdvisorCardComponent } from './components/advisor/advisor-card/advisor-card.component';
import { TopAdvisorsComponent } from './components/advisor/top-advisors/top-advisors.component';
import { AppRoutingModule } from './app-routing.module';
import { AdvisorService } from './services/advisor.service';
import { HttpModule } from '../../node_modules/@angular/http';
import { HttpClientModule } from '../../node_modules/@angular/common/http';
import { TopAssetsComponent } from './components/asset/top-assets/top-assets.component';
import { AssetService } from './services/asset.service';
import { MessageSignatureComponent } from './components/account/message-signature/message-signature.component';

@NgModule({
  declarations: [
    AppComponent,
    AssetHistoryChartComponent,
    RecommendationDistributionComponent,
    AdvisorCardComponent,
    TopAdvisorsComponent,
    TopAssetsComponent,
    MessageSignatureComponent
  ],
  imports: [
    BrowserModule,
    HttpModule,
    HttpClientModule,
    AppRoutingModule,
    SimpleNotificationsModule.forRoot(),
    ChartModule
  ],
  providers: [
    HttpService,
    LocalStorageService,
    Web3Service,
    AdvisorService,
    AssetService,
    {provide:HIGHCHARTS_MODULES, useFactory:highchartsModules} 
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
