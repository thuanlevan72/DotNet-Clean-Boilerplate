import { inject, Injectable } from '@angular/core';
import { AuthResponse, LoginRequest, RefreshTokenResponse } from '../models/login.model';
import { BaseApiService } from '../../../core/api/base-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthApiService extends BaseApiService {
  // Định nghĩa đường dẫn gốc cho module này
  private readonly ENDPOINT = '/api/Auth/login';
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // HÀM REFRESH TOKEN
  async refreshToken(): Promise<AuthResponse> {
    // Lấy refresh token hiện tại từ localStorage
    const currentRefreshToken = this.authService.getRefreshToken();

    if (!currentRefreshToken) {
       throw new Error('Không tìm thấy Refresh Token');
    }

    // Gọi API bằng phương thức post từ BaseApiService
    // Payload gửi lên thường chỉ cần { refreshToken: '...' }
    return await this.post<AuthResponse>('/api/Auth/logout', {
      refreshToken: currentRefreshToken
    });
  }

  async logout(): Promise<void> {
    try {
      const currentRefreshToken = this.authService.getRefreshToken();

      if (currentRefreshToken) {
        await this.post('/api/Auth/logout', { clientRefreshToken: currentRefreshToken, logoutAllDevices: false });
        this.authService.logout();
      }
    } catch (error) {
      console.error('Lỗi khi gọi API logout:', error);
    }
  }

  // Đổi kiểu trả về thành Promise<string> vì dùng hàm async
  async login(data: LoginRequest): Promise<string> {
    try {
      data.DeviceId = this.authService.getOrCreateDeviceId(); // Lấy DeviceId từ AuthService và gán vào payload trước khi gửi đi
      // 1. Dùng firstValueFrom để ép Observable thành Promise và chờ (await)
      const response = await this.post<AuthResponse>(this.ENDPOINT, data);

      // 2. Chạy đến đây nghĩa là API đã thành công (200 OK)
      console.log('Đăng nhập thành công:', response);
      this.authService.setToken(response.token, response.refreshToken);
      this.router.navigate(['/']);

      return ''; // Nếu thành công, trả về chuỗi rỗng (không có lỗi)
    } catch (error: any) {
      // 3. Nếu API lỗi (400, 401, 500...), code sẽ nhảy ngay vào khối catch này
      console.error('Lỗi đăng nhập:', error);
      return error.error?.message || 'Tài khoản hoặc mật khẩu không chính xác';
    }
  }
}
