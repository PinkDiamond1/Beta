import { Component, OnInit, Input } from '@angular/core';
import { AssetService } from '../../../services/asset.service';
import { AssetResponse } from '../../../model/asset/assetResponse';
import { AccountService } from '../../../services/account.service';
import { ModalService } from '../../../services/modal.service';

@Component({
  selector: 'trending-assets',
  templateUrl: './trending-assets.component.html',
  styleUrls: ['./trending-assets.component.css']
})
export class TrendingAssetsComponent implements OnInit {
  @Input() isWalletLogin: boolean = false;
  assets : AssetResponse[];
  
  dummyData =
  [{"assetId":1,"name":"Coin 1","code":"XXX","lastValue":0,"variation24h":0.0,"mode":0,
  "recommendationDistribution":[
    {"type":0,"total":2.0},
    {"type":1,"total":2.0},
    {"type":2,"total":2.0}]},
    {"assetId":1,"name":"Coin 2","code":"XXX","lastValue":0,"variation24h":0.0,"mode":0,
    "recommendationDistribution":[
      {"type":0,"total":2.0},
      {"type":1,"total":2.0},
      {"type":2,"total":2.0}]},
      {"assetId":1,"name":"Coin 3","code":"XXX","lastValue":0,"variation24h":0.0,"mode":0,
  "recommendationDistribution":[
    {"type":0,"total":2.0},
    {"type":1,"total":2.0},
    {"type":2,"total":2.0}]}]

  constructor(private accountService: AccountService, 
    private assetService: AssetService, private modalService: ModalService) { }

  ngOnInit() {
    if(this.canView()){
      this.assetService.getAssetsDetails().subscribe(result => {
        if(result!= null && result.length > 3){
          this.assets = result.slice(0,3);
        }
        else{
          this.assets = result;
        }
      });
    }
    else{
      this.assets = this.dummyData;
    }
  }

  canView() {
    let loginData = this.accountService.getLoginData();
    return !!loginData && loginData.hasInvestment;
  }

  onActionClick() {
    if (!this.isWalletLogin) {
      this.modalService.setLogin();
    }
  }

  getActionText() {
    if (!this.isWalletLogin) {
      return "Login to access";
    } else {
      return "Hold AUC";
    }
  }
}
