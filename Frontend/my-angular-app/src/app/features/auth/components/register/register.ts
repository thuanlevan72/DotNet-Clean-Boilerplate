import { Component, inject, Input } from '@angular/core';
import { AuthApiService } from '../../services/auth-api.service';
import { CustomInputComponent } from '../../../../shared/components/input/custom-input.component';
import { LoginRequest } from '../../models/login.model';
import { RouterLink } from "@angular/router";
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
@Component({
  selector: 'app-register',
  imports: [CustomInputComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private fb = inject(FormBuilder);
  loginRegisterForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    fullname: ['', [Validators.required]]
  });

  // Hàm Helper để lấy control ra truyền cho component con (tránh lỗi TypeScript khi ép kiểu)
  getControl(name: string): FormControl {
    return this.loginRegisterForm.get(name) as FormControl;
  }
   submit() {
    if (this.loginRegisterForm.invalid) {
      this.loginRegisterForm.markAllAsTouched();
      return;
    }

    // const payload: LoginRequest = this.loginForm.getRawValue() as LoginRequest;
    // console.log('Chuẩn bị gửi payload:', payload.email, payload.password);
    // this.api.login(payload);
    // // this.api.login(payload).subscribe(...)
  }
}
