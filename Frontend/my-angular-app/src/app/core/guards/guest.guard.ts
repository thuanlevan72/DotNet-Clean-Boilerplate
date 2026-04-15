import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const guestGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const token = localStorage.getItem('access_token');

  if (token) {
    // Nếu ĐÃ ĐĂNG NHẬP mà cố tình gõ URL vào trang /login
    // -> Bắt buộc đá về lại trang chủ (Todo)
    router.navigate(['/']);
    return false;
  } else {
    // Nếu CHƯA đăng nhập -> Hợp lệ -> Cho phép vào trang Login/Register
    return true;
  }
};
