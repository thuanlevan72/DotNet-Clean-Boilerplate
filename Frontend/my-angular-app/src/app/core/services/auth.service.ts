import { jwtDecode } from 'jwt-decode';
import { Injectable, signal } from '@angular/core';
import { User, UserLocalStorage } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private userSignal = signal<User | null>(null);

  user$ = this.userSignal.asReadonly();

  setUser(user: User) {
    this.userSignal.set(user);
    localStorage.setItem('user', JSON.stringify(user));
  }

  getUser(): UserLocalStorage | null {
    const userData = localStorage.getItem('user_profile');
    if (userData) {
      const user: UserLocalStorage = JSON.parse(userData);
      return user;
    }
    return null;
  }

  setToken(token: string, refreshToken: string) {
    // 1. Vẫn phải lưu token gốc để dành cho Interceptor gắn vào Header (Bearer token)
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', refreshToken); // Nếu có refresh token, lưu tương tự

    // 2. Phân tích (Decode) token để lấy Payload
    try {
      // jwtDecode sẽ tự động cắt phần payload ở giữa và parse ra dạng Object
      const decodedPayload: any = jwtDecode(token);

      // 3. Chuẩn hóa dữ liệu (Mapping)
      // Vì key của .NET sinh ra có chứa dấu '/', ':' nên ta bắt buộc phải dùng ngoặc vuông [] để truy xuất.
      const userProfile = {
        id: decodedPayload.sub,
        email: decodedPayload.email,
        fullName: decodedPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
        role: decodedPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],

        // Có thể lưu thêm thời gian hết hạn để xử lý tự động log out sau này
        expirationTime: decodedPayload.exp,
      };

      // 4. Lưu Object vào LocalStorage
      // LocalStorage CHỈ lưu được chuỗi (string), nên bắt buộc phải ép Object về chuỗi JSON
      localStorage.setItem('user_profile', JSON.stringify(userProfile));

      // LƯU Ý: Nếu bạn đang dùng Signal (như mình đã thiết lập ở câu trước),
      // bạn có thể cập nhật trạng thái User ngay tại đây luôn:
      // this.currentUser.set(userProfile);
    } catch (error) {
      console.error('Lỗi khi phân tích Token:', error);
    }
  }
  getAssceToken(): string | null {
    return this.userSignal()?.token || localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return this.userSignal()?.refreshToken || localStorage.getItem('refresh_token');
  }

  private getDeviceInfo(): string {
    const ua = navigator.userAgent;
    let browser = 'Unknown Browser';
    let os = 'Unknown OS';

    // 1. Phân tích tên Trình duyệt
    // Lưu ý: Phải check Edge và Opera trước vì chuỗi của nó thường chứa cả chữ "Chrome"
    if (ua.indexOf('Edg') > -1) {
      browser = 'Edge';
    } else if (ua.indexOf('OPR') > -1 || ua.indexOf('Opera') > -1) {
      browser = 'Opera';
    } else if (ua.indexOf('Chrome') > -1) {
      browser = 'Chrome';
    } else if (ua.indexOf('Firefox') > -1) {
      browser = 'Firefox';
    } else if (ua.indexOf('Safari') > -1) {
      browser = 'Safari';
    }

    // 2. Phân tích Hệ điều hành (OS)
    if (ua.indexOf('Win') > -1) os = 'Windows';
    else if (ua.indexOf('Android') > -1) os = 'Android';
    else if (ua.indexOf('Mac') > -1) {
      // Check thêm xem là Macbook hay iPhone/iPad
      os = ua.indexOf('Mobile') > -1 ? 'iOS' : 'MacOS';
    } else if (ua.indexOf('Linux') > -1) os = 'Linux';

    return `${browser}-${os}`; // Trả về dạng: "Chrome-Windows" hoặc "Safari-iOS"
  }

  // ==========================================
  // THÊM MỚI: Hàm lấy hoặc tạo Device ID
  // ==========================================
  getOrCreateDeviceId(): string {
    const DEVICE_ID_KEY = 'app_device_id';
    let deviceId = localStorage.getItem(DEVICE_ID_KEY);

    if (!deviceId) {
      // Nếu chưa có, tạo một chuỗi ngẫu nhiên.
      // Ưu tiên dùng crypto.randomUUID() của trình duyệt hiện đại
      if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        deviceId = crypto.randomUUID() + '-' + this.getDeviceInfo() + '-' + new Date().getTime(); // Thêm thông tin thiết bị và timestamp để tăng độ duy nhất
      } else {
        // Fallback tự chế nếu trình duyệt quá cũ
        deviceId =
          'device-' + this.getDeviceInfo() +  '-' + new Date().getTime() + '-' + Math.random().toString(36).substring(2, 9);
      }

      // Lưu lại để các lần đăng nhập sau trên trình duyệt này vẫn dùng chung 1 ID
      localStorage.setItem(DEVICE_ID_KEY, deviceId);
    }

    return deviceId;
  }

  logout() {
    this.userSignal.set(null);
    localStorage.clear();
  }

  isAuthenticated(): boolean {
    return !!this.getAssceToken();
  }
}
