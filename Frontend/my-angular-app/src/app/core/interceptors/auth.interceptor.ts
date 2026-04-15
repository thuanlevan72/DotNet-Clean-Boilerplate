import { Injectable, inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject, from } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { AuthApiService } from '../../features/auth/services/auth-api.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private authService = inject(AuthService);
  private authApiService = inject(AuthApiService); // Gọi API refresh

  // Cờ đánh dấu xem có đang gọi API refresh token không
  private isRefreshing = false;

  // Nơi lưu trữ trạng thái chờ của các Request bị 401
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // 1. Gắn Token hiện tại vào mọi Request gửi đi
    let authReq = this.addTokenHeader(request, this.authService.getAssceToken());
    
    // 2. Bắt lỗi trả về từ Backend
    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        // 1. In ra log để bắt bệnh xem có đúng bị lỗi CORS (status = 0) không
        console.log('🔥 Interceptor bắt được HTTP Error:', error.status, request.url);
        // Bỏ qua nếu lỗi 401 đến từ chính API Login hoặc API Refresh Token (để tránh lặp vô hạn)
        if (error.status === 401 && !request.url.includes('/Auth/login') && !request.url.includes('/Auth/refresh')) {
          return this.handle401Error(request, next);
        }

        // Các lỗi khác (400, 403, 500...) thì ném ra ngoài bình thường
        return throwError(() => error);
      })
    );
  }

  // Hàm xử lý khi gặp 401
  private handle401Error(request: HttpRequest<any>, next: HttpHandler) {
  if (!this.isRefreshing) {
    this.isRefreshing = true;
    this.refreshTokenSubject.next(null);

    // SỬA Ở ĐÂY: Dùng from() để bọc cái Promise lại thành Observable
    return from(this.authApiService.refreshToken()).pipe(
      switchMap((response: any) => {
        this.isRefreshing = false;

        // Lưu token mới
        this.authService.setToken(response.accessToken, response.refreshToken);
        this.refreshTokenSubject.next(response.accessToken);

        // Chạy tiếp request đang bị lỗi ban đầu
        return next.handle(this.addTokenHeader(request, response.accessToken));
      }),
      catchError((err) => {
        this.isRefreshing = false;
        this.authService.logout();
        return throwError(() => err);
      })
    );
  } else {
    // Đoạn Else xếp hàng chờ thì giữ nguyên không đổi
    return this.refreshTokenSubject.pipe(
      filter(token => token !== null),
      take(1),
      switchMap((token) => next.handle(this.addTokenHeader(request, token)))
    );
  }
}
  // Hàm phụ trợ gắn Token vào Header
  private addTokenHeader(request: HttpRequest<any>, token: string | null) {
    if (token) {
      return request.clone({
        headers: request.headers.set('Authorization', `Bearer ${token}`)
      });
    }
    return request;
  }
}
