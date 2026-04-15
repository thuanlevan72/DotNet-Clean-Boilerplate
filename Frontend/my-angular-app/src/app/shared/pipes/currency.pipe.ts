import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'currencyVn',
  standalone: true
})
export class CurrencyPipeVn implements PipeTransform {
  transform(value: number): string {
    return value.toLocaleString('vi-VN') + ' đ';
  }
}
