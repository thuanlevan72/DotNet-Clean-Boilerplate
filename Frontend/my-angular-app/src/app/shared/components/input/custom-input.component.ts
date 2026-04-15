import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ErrorMessageComponent } from '../error-message/error-message.component';

@Component({
  selector: 'app-custom-input',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ErrorMessageComponent],
  template: `
    <div class="mb-4">
      <label class="block text-gray-700 text-sm font-bold mb-2">
        {{ labelText }}
      </label>

      <input
        [type]="inputType"
        [placeholder]="placeholderText"
        [formControl]="control"
        class="w-full px-4 py-2 border border-gray-300 rounded-lg text-gray-700
               focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
               transition duration-200 ease-in-out"
        [class.border-red-500]="control.invalid && (control.dirty || control.touched)"
        [class.focus:ring-red-500]="control.invalid && (control.dirty || control.touched)"
      />

      <app-error-message [control]="control"></app-error-message>
    </div>
  `
})
export class CustomInputComponent {
  @Input() labelText: string = '';
  @Input() inputType: string = 'text';
  @Input() placeholderText: string = 'Vui lòng nhập...';

  // Phải dùng type FormControl ở đây để bind với [formControl]
  @Input({ required: true }) control!: FormControl;
}
