import { Component, EventEmitter, inject, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingService } from './loading.service';

@Component({
  selector: 'loading-component',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (loadingService.isLoading()) {
      <div class="fixed inset-0 z-[9999] flex items-center justify-center bg-black/40 backdrop-blur-sm">

        <button
          (click)="cancelLoading()"
          class="absolute top-6 right-6 text-white hover:text-gray-300 text-4xl font-light transition-colors focus:outline-none"
          aria-label="Cancel loading">
          &times;
        </button>

        <div class="flex flex-col items-center gap-4">
          <div class="relative w-16 h-16">
            <div class="absolute inset-0 rounded-full border-4 border-blue-200"></div>
            <div class="absolute inset-0 rounded-full border-4 border-blue-600 border-t-transparent animate-spin"></div>
          </div>

          <p class="text-white text-sm tracking-wide animate-pulse">
            Loading, please wait...
          </p>
        </div>
      </div>
    }
  `
})
export class LoadingComponent {
  // Inject Service vào
  loadingService = inject(LoadingService);

  // Hàm này được gọi khi click vào dấu X
  cancelLoading() {
    this.loadingService.hide(); // Gọi hàm tắt từ service
  }
}
