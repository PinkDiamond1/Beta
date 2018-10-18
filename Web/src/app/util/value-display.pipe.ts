import { Pipe, PipeTransform } from '@angular/core';

@Pipe({name: 'valueDisplay'})
export class ValueDisplayPipe implements PipeTransform {
  transform(value: number): string {
    if (value == 0) {
      return '$' + value.toFixed(2);
    } else if(!value){
      return null;
    }
    var decimals = 1;
    var currentValue = 0;
    while(currentValue <= 1 && decimals < 8){
        decimals++; 
        currentValue = value * Math.pow(10, decimals - 2);
    }
    return '$' + value.toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
  }


}