import { FormGroup } from "@angular/forms";

export interface LoginRequest extends FormGroup {
  email: string;
  password: string;
  DeviceId: string; // Thêm trường DeviceId vào request
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken: string;
}

// Tương đương với AuthResponseDTO ở Backend
export interface AuthResponse {
  token: string;
  refreshToken: string;
  message: string;
}
