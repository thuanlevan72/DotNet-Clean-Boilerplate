import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="control?.invalid && (control?.dirty || control?.touched)"
         class="text-red-500 text-xs mt-1 ml-1 font-medium animate-pulse">

      <span *ngIf="control?.hasError('required')">Trường này không được để trống.</span>
      <span *ngIf="control?.hasError('email')">Email không đúng định dạng.</span>
      <span *ngIf="control?.hasError('minlength')">
        Mật khẩu phải có ít nhất {{ control?.errors?.['minlength'].requiredLength }} ký tự.
      </span>

    </div>
  `
})
export class ErrorMessageComponent {
  // Nhận vào một control từ cha truyền xuống
  @Input({ required: true }) control!: AbstractControl | null;
}
