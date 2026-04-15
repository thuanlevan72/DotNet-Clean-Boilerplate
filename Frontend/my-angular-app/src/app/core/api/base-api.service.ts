import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { API_CONFIG } from './api.config';
// Nhớ import environment của dự án bạn vào đây
// import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BaseApiService {
  // Inject HttpClient theo chuẩn Angular mới
  protected http = inject(HttpClient);

  // Lấy URL gốc từ file môi trường (ví dụ: 'https://api.domain.com')
  protected baseUrl: string = API_CONFIG.BASE_URL;

  /**
   * Phương thức GET với tự động map Query Params
   */
  protected async get<T>(endpoint: string, params?: any): Promise<T> {
    let httpParams = new HttpParams();

    // Nếu có truyền params (ví dụ: { page: 1, limit: 10 }), tự động map vào URL
    if (params) {
      Object.keys(params).forEach(key => {
        if (params[key] !== null && params[key] !== undefined) {
          httpParams = httpParams.append(key, params[key]);
        }
      });
    }

    return await firstValueFrom(this.http.get<T>(`${this.baseUrl}${endpoint}`, { params: httpParams }));
  }

  /**
   * Phương thức POST
   */
  protected async post<T>(endpoint: string, body: any): Promise<T> {
    return await firstValueFrom(this.http.post<T>(`${this.baseUrl}${endpoint}`, body));
  }

  /**
   * Phương thức PUT (Cập nhật toàn bộ)
   */
  protected async put<T>(endpoint: string, body: any): Promise<T> {
    return await firstValueFrom(this.http.put<T>(`${this.baseUrl}${endpoint}`, body));
  }

  /**
   * Phương thức PATCH (Cập nhật một phần)
   */
  protected async patch<T>(endpoint: string, body: any): Promise<T> {
    return await firstValueFrom(this.http.patch<T>(`${this.baseUrl}${endpoint}`, body));
  }

  /**
   * Phương thức DELETE
   */
  protected async delete<T>(endpoint: string): Promise<T> {
    return await firstValueFrom(this.http.delete<T>(`${this.baseUrl}${endpoint}`));
  }
}
