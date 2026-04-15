import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TodoApiService } from '../../services/todo-api.service';
import { LoadingService } from '../../../../shared/components/Loading/loading.service';
import { TodoDto, TodoStatus, PriorityLevel } from '../../models/todo.model';
import { PagedResponse } from '../../../../shared/models/paged-response.model';

@Component({
  selector: 'app-todo',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './todo.html',
  styleUrl: './todo.css',
})
export class Todo implements OnInit {
  todoData = signal<PagedResponse<TodoDto> | null>(null);

  currentPage = signal<number>(1);
  pageSize = signal<number>(10);

  visiblePages = computed(() => {
    const data = this.todoData();
    if (!data) return [];

    const total = data.totalPages;
    let start = Math.max(1, this.currentPage() - 2);
    let end = Math.min(total, start + 4);

    if (end - start < 4) {
      start = Math.max(1, end - 4);
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  private readonly todoApiService = inject(TodoApiService);
  private loadingService = inject(LoadingService);

  ngOnInit() {
    this.loadTodos();
  }

  // --- ĐÃ FIX: Thêm if (data) để tránh lỗi 'possibly null' ---
  async loadTodos(page: number = this.currentPage(), size: number = this.pageSize()) {
    setTimeout(() => this.loadingService.show(), 0);

    try {
      const data = await this.todoApiService.getTodos(page, size);
      this.todoData.set(data);

      // Nếu API trả về data hợp lệ thì mới cập nhật số trang
      if (data) {
        this.currentPage.set(data.currentPage);
        this.pageSize.set(data.pageSize);
      }
    } catch (error) {
      console.error('Lỗi tải dữ liệu', error);
    } finally {
      setTimeout(() => this.loadingService.hide(), 0);
    }
  }

  goToPage(page: number) {
    if (page === this.currentPage() || page < 1 || (this.todoData() && page > this.todoData()!.totalPages)) {
      return;
    }
    this.loadTodos(page, this.pageSize());
  }

  onPageSizeChange(event: Event) {
    const newSize = Number((event.target as HTMLSelectElement).value);
    this.loadTodos(1, newSize);
  }

  // --- ĐÃ FIX: Thêm hàm mathMin để HTML gọi được thư viện Math ---
  mathMin(a: number, b: number): number {
    return Math.min(a, b);
  }

  toggleStatus(todo: TodoDto) {
    const newStatus: TodoStatus = todo.status === 'Done' ? 'Todo' : 'Done';
    todo.status = newStatus;
  }

  delete(id: string) {
    if (!confirm('Bạn có chắc muốn xóa công việc này?')) return;
    this.todoData.update(currentData => {
      if (!currentData) return null;
      return {
        ...currentData,
        items: currentData.items.filter((t) => t.id !== id),
        totalItems: currentData.totalItems - 1
      };
    });
  }

  getPriorityColor(priority: PriorityLevel): string {
    switch (priority) {
      case 'Urgent': return 'bg-red-100 text-red-700 border-red-200';
      case 'High': return 'bg-orange-100 text-orange-700 border-orange-200';
      case 'Medium': return 'bg-yellow-100 text-yellow-700 border-yellow-200';
      case 'Low': return 'bg-blue-100 text-blue-700 border-blue-200';
      default: return 'bg-gray-100 text-gray-700 border-gray-200';
    }
  }

  getStatusColor(status: TodoStatus): string {
    switch (status) {
      case 'Done': return 'text-green-600 bg-green-50 border-green-200';
      case 'InProgress': return 'text-blue-600 bg-blue-50 border-blue-200';
      case 'Blocked': return 'text-red-600 bg-red-50 border-red-200';
      case 'OnHold': return 'text-gray-500 bg-gray-50 border-gray-200';
      default: return 'text-gray-600 bg-gray-100 border-gray-200';
    }
  }
}
