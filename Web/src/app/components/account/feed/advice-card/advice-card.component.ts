import { Component, OnInit, Input } from '@angular/core';
import { FeedResponse } from '../../../../model/advisor/feedResponse';
import { CONFIG } from '../../../../services/config.service';
import { AssetService } from '../../../../services/asset.service';
import { Util } from '../../../../util/Util';
import { NavigationService } from '../../../../services/navigation.service';
import { Subscription } from 'rxjs';
import { AccountService } from '../../../../services/account.service';
import { ValueDisplayPipe } from 'src/app/util/value-display.pipe';

@Component({
  selector: 'advice-card',
  templateUrl: './advice-card.component.html',
  styleUrls: ['./advice-card.component.css']
})
export class AdviceCardComponent implements OnInit {
  @Input() adviceFeed : FeedResponse;
  promise : Subscription;
  
  constructor(private assetService : AssetService,
    private navigationService: NavigationService,
    private accountService: AccountService) { }

  ngOnInit() {
  }
  
  getAdvisorImgUrl(){
    return CONFIG.profileImgUrl.replace("{id}", this.adviceFeed.advice.advisorUrlGuid);
  }

  getAssetImgUrl(){
    return CONFIG.assetImgUrl.replace("{id}", this.adviceFeed.assetId.toString());
  }
  
  onFollowClick(event: Event){
    if(this.accountService.hasInvestmentToCallLoggedAction()){
      this.promise = this.assetService.followAsset(this.adviceFeed.assetId).subscribe(result =>
          this.adviceFeed.followingAsset = true
      );
    }
    event.stopPropagation();
  }
  
  onUnfollowClick(event: Event){
    this.promise = this.assetService.unfollowAsset(this.adviceFeed.assetId).subscribe(result =>this.adviceFeed.followingAsset = false);
    event.stopPropagation();
  }

  getAdviceTypeDescription(){
    return Util.GetRecommendationTypeDescription(this.adviceFeed.advice.adviceType);
  }

  getAdviceParametersDescription() {
    if (!this.adviceFeed.advice.targetPrice && !this.adviceFeed.advice.stopLoss && this.adviceFeed.advice.operationType == 0) {
      return '';
    } else {
      if (this.adviceFeed.advice.targetPrice && this.adviceFeed.advice.stopLoss) {
        return 'Target value: ' +  new ValueDisplayPipe().transform(this.adviceFeed.advice.targetPrice) + ' - Stop loss: ' + new ValueDisplayPipe().transform(this.adviceFeed.advice.stopLoss);
      } else if (this.adviceFeed.advice.targetPrice) {
        return 'Target value: ' +  new ValueDisplayPipe().transform(this.adviceFeed.advice.targetPrice);
      } else if (this.adviceFeed.advice.stopLoss) {
        return 'Stop loss: ' +  new ValueDisplayPipe().transform(this.adviceFeed.advice.stopLoss);
      } else if (this.adviceFeed.advice.operationType != 0 && this.adviceFeed.advice.adviceType == 2) {
        return 'Triggered by ' + Util.GetCloseReasonDescription(this.adviceFeed.advice.operationType);
      } else {
        return '';
      }
    }
  }

  getAdviceTypeColor(){
    return Util.GetRecommendationTypeColor(this.adviceFeed.advice.adviceType);
  }

  goToAssetDetails(){
    this.navigationService.goToAssetDetails(this.adviceFeed.assetId);
  }
  
  goToExpertDetails(){
    this.navigationService.goToExpertDetails(this.adviceFeed.advice.advisorId);
  }
}
