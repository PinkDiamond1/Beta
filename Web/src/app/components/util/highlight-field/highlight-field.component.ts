import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ValueDisplayPipe } from '../../../util/value-display.pipe';

@Component({
  selector: 'highlight-field',
  templateUrl: './highlight-field.component.html',
  styleUrls: ['./highlight-field.component.css']
})
export class HighlightFieldComponent implements OnChanges {
  @Input() value: number;
  @Input() decimalsQty?: number = null;
  @Input() suffix: string = '';
  @Input() prefix: string = '';
  @Input() blinkGray: boolean = false;
  bear: boolean = false;
  bull: boolean = false;
  dark: boolean = false;

  ngOnChanges(changes: SimpleChanges) {
    if (changes && !changes.value.isFirstChange()) {
      if (this.blinkGray) {
        this.setDarkColor();
      } else if (changes.value.previousValue > changes.value.currentValue) {
        this.setBearColor();
      } else if (changes.value.previousValue < changes.value.currentValue) {
        this.setBullColor();
      }
    }
  }

  getValue(): string {
    if (this.value == undefined || this.value == null) {
      return "";
    } else 
    {
      let preffixText = this.prefix;
      let suffixText = this.suffix;
      if (!preffixText) {
        preffixText = '';
      }
      if (!suffixText) {
        suffixText = '';
      }
      if (this.decimalsQty || this.decimalsQty == 0) {
        return preffixText + this.value.toLocaleString(undefined, { minimumFractionDigits: this.decimalsQty, maximumFractionDigits: this.decimalsQty }) + suffixText;
      } else {
        return new ValueDisplayPipe().transform(this.value, preffixText) + suffixText;
      }
    }
  }

  setDarkColor() {
    this.dark = true;
    setTimeout(() => { this.dark = false; }, 500);
  }

  setBearColor() {
    this.bear = true;
    setTimeout(() => { this.bear = false; }, 500);
  }

  setBullColor() {
    this.bull = true;
    setTimeout(() => { this.bull = false; }, 500);
  }
}
