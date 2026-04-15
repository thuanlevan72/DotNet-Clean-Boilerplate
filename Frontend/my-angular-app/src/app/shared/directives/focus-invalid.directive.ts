import { Directive, ElementRef, AfterViewInit } from '@angular/core';

@Directive({
  selector: '[focusInvalid]',
  standalone: true
})
export class FocusInvalidDirective implements AfterViewInit {
  constructor(private el: ElementRef) {}

  ngAfterViewInit() {
    const invalid = this.el.nativeElement.querySelector('.ng-invalid');
    if (invalid) invalid.focus();
  }
}
