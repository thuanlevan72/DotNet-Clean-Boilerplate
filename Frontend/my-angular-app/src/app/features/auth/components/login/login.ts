import { Component, inject, Input, signal } from '@angular/core';
import { AuthApiService } from '../../services/auth-api.service';
import { Router, RouterLink } from "@angular/router";
import { CustomInputComponent } from '../../../../shared/components/input/custom-input.component';
import { LoginRequest } from '../../models/login.model';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LoadingService } from '../../../../shared/components/Loading/loading.service';


@Component({
  selector: 'app-login',
  imports: [CustomInputComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private fb = inject(FormBuilder);
  private api = inject(AuthApiService);
  private router = inject(Router); // Inject Router
  // 1. Inject LoadingService
  private loadingService = inject(LoadingService);
  // Các biến trạng thái UI
  errorMessage = signal<string>(''); // Lưu câu báo lỗi

  loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  // Hàm Helper để lấy control ra truyền cho component con (tránh lỗi TypeScript khi ép kiểu)
  getControl(name: string): FormControl {
    return this.loginForm.get(name) as FormControl;
  }

  async submit() {

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    // 2. BẬT LOADING TOÀN CỤC
    this.loadingService.show();

    const payload: LoginRequest =  this.loginForm.getRawValue() as LoginRequest;
    console.log('Chuẩn bị gửi payload:', payload.email, payload.password);
    let mess = await this.api.login(payload);
    this.errorMessage.set(mess); // Xóa câu thông báo lỗi cũ đi (nếu có)
    this.loadingService.hide();
    // this.api.login(payload).subscribe(...)
  }

}
