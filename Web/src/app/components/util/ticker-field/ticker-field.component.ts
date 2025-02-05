import { Component, Input, OnInit, OnDestroy, OnChanges, EventEmitter, Output } from '@angular/core';
import { TickerService } from '../../../services/ticker.service';
import { Subscription } from 'rxjs';
import { PairResponse } from '../../../model/asset/assetResponse';

@Component({
  selector: 'ticker-field',
  templateUrl: './ticker-field.component.html',
  styleUrls: ['./ticker-field.component.css']
})
export class TickerFieldComponent implements OnInit, OnDestroy, OnChanges {
  @Input() pair: PairResponse;
  @Input() tickerProperty: string = "currentClosePrice";
  @Input() tickerToMultiplierProperty: string = "currentClosePrice";
  @Input() startValue?: number = null;
  @Input() valueMultiplier?: number = null;
  @Input() blinkGray: boolean = false;
  value: number;
  mainTickerSubscription: Subscription;
  multiplierTickerSubscription: Subscription;
  multiplierValue?: number = null;
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
    if (this.startValue && !this.value) {
      this.value = this.startValue;
    }
    if (this.pair) {
      if (this.pair.symbol) {
      this.mainTickerSubscription = this.tickerService.binanceTicker(this.pair.symbol).subscribe(ret =>
        {
          if (!this.pair.multipliedSymbol) {
            this.setValue(ret[this.tickerProperty], 1);
          } else {
            this.baseValue = ret[this.tickerProperty];
            if (this.multiplierValue || this.multiplierValue == 0) {
              this.setValue(this.baseValue, this.multiplierValue);
            }
          }
        });
      }
      if (this.pair.multipliedSymbol) {
        this.multiplierTickerSubscription = this.tickerService.binanceTicker(this.pair.multipliedSymbol).subscribe(ret =>
          {
            this.multiplierValue = ret[this.tickerToMultiplierProperty];
            if (this.baseValue || this.baseValue == 0) {
              this.setValue(this.baseValue, this.multiplierValue);
            }
          });
      }
    }
    this.initialized = true;
  }

  setValue(base: number, multiplier: number) {
    this.value = base * multiplier * (this.valueMultiplier ? this.valueMultiplier : 1);
    this.onNewValue.emit(this.value);
  }

  getPreffix(): string {
    return this.pair ? this.pair.preffix ? this.pair.preffix : '' : '$';
  }

  getSuffix(): string {
    return this.pair && this.pair.suffix ? this.pair.suffix : '';
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
