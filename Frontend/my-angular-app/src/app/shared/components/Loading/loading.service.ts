import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root' // <-- Điểm mấu chốt để thành "toàn cục"
})
export class LoadingService {
  // Biến lưu trạng thái loading chung
  isLoading = signal<boolean>(false);

  // Hàm bật loading
  show() {
    this.isLoading.set(true);
  }

  // Hàm tắt loading
  hide() {
    this.isLoading.set(false);
  }
}
