import { Component, Input, OnInit, OnDestroy, OnChanges, EventEmitter, Output } from '@angular/core';
import { TickerService } from '../../../services/ticker.service';
import { Subscription } from 'rxjs';
import { BinanceTickerPayload } from '../../../model/binanceTickerPayload';
import { PairResponse } from '../../../model/asset/assetResponse';
import { Constants } from '../../../util/constants';
import { CONFIG } from '../../../services/config.service';
import { Util } from '../../../util/util';

@Component({
  selector: 'ticker-percentage-field',
  templateUrl: './ticker-percentage-field.component.html',
  styleUrls: ['./ticker-percentage-field.component.css']
})
export class TickerPercentageFieldComponent implements OnInit, OnDestroy, OnChanges {
  Math = Math;
  @Input() pair: PairResponse;
  @Input() priceValue?: any = null;
  @Input() quantityValue?: any = null;
  @Input() feeValue?: any = null;
  @Input() orderType?: number = null;
  @Input() startValue?: number = null;
  value: number;
  mainTickerSubscription: Subscription;
  multiplierTickerSubscription: Subscription;
  quoteVariation?: number = null;
  baseVariation?: number = null;
  quoteValue?: number = null;
  baseValue?: number = null;
  initialized: boolean = false;
  @Output() onNewValue = new EventEmitter<number>();

  constructor(private tickerService: TickerService) { }

  ngOnChanges() {
    if (this.initialized) {
      this.ngOnDestroy();
      this.ngOnInit();
    }
  }
  
  ngOnInit() {
    if (this.startValue != undefined) {
      this.value = this.startValue;
    }
    if (this.priceValue && this.quantityValue) {
      this.priceValue = parseFloat(this.priceValue);
      this.quantityValue = parseFloat(this.quantityValue);
      this.feeValue = parseFloat(this.feeValue);
    }
    if (this.pair) {
      if (this.pair.symbol) {
      this.mainTickerSubscription = this.tickerService.binanceTicker(this.pair.symbol).subscribe(ret =>
        {
          this.setValue(ret, true);
        });
      }
      if (this.pair.multipliedSymbol) {
        this.multiplierTickerSubscription = this.tickerService.binanceTicker(this.pair.multipliedSymbol).subscribe(ret =>
          {
            this.setValue(ret, false);
          });
      }
    }
    this.initialized = true;
  }

  setValue(ticker: BinanceTickerPayload, isMainPair: boolean) {
    if (!this.pair.multipliedSymbol) {
      if (this.priceValue && this.quantityValue) {
        this.value = Util.GetProfit(this.orderType, this.priceValue, this.feeValue, this.quantityValue, this.quantityValue, this.getConsideredPrice(ticker));
      } else {
        this.value = ticker.priceChangePercentage / 100;
      }
    } else {
      if (isMainPair) {
        this.baseValue = this.getConsideredPrice(ticker);
        this.baseVariation = ticker.priceChangePercentage;
      } else {
        this.quoteValue = this.getConsideredPrice(ticker);
        this.quoteVariation = ticker.priceChangePercentage;
      }
      if (this.priceValue && this.quantityValue) {
        if (this.baseValue && this.quoteValue) {
          this.value = Util.GetProfit(this.orderType, this.priceValue, this.feeValue, this.quantityValue, this.quantityValue, (this.baseValue * this.quoteValue));
        }
      } else if (this.baseValue && this.quoteValue && (this.baseVariation || this.baseVariation == 0) 
        && (this.quoteVariation || this.quoteVariation == 0)) {
          let previousBase = this.baseValue / (1 + (this.baseVariation / 100));
          let previousQuote = this.quoteValue / (1 + (this.quoteVariation / 100));
          this.value = ((this.baseValue * this.quoteValue) / (previousBase * previousQuote)) - 1;
      }
    }
    if(this.onNewValue) {
      this.onNewValue.emit(this.value);
    }
  }

  getReferenceValueMultiplier() : number {
    return this.orderType === Constants.OrderType.Sell ? -1.0 : 1.0;
  }

  getConsideredPrice(ticker: BinanceTickerPayload) : number {
    return this.priceValue && this.quantityValue ? this.orderType === Constants.OrderType.Sell ? ticker.bestAskPrice : ticker.bestBidPrice : ticker.currentClosePrice;
  }

  ngOnDestroy() {
    this.initialized = false;
    if (this.mainTickerSubscription) {
      this.mainTickerSubscription.unsubscribe();
    }
    if (this.multiplierTickerSubscription) {
      this.multiplierTickerSubscription.unsubscribe();
    }
  }
}
