import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TodoApiService } from '../../services/todo-api.service';
import { LoadingService } from '../../../../shared/components/Loading/loading.service';
import { TodoDto, TodoStatus, PriorityLevel } from '../../models/todo.model';
import { PagedResponse } from '../../../../shared/models/paged-response.model';
import { CreateOrUpdateTodo } from '../create-or-update-todo/create-or-update-todo';
import {
  getPriorityColor,
  getPriorityLabel,
  getStatusColor,
} from '../../../../shared/utils/enum.util';

@Component({
  selector: 'app-todo',
  standalone: true,
  imports: [CommonModule, RouterLink, CreateOrUpdateTodo],
  templateUrl: './todo.html',
  styleUrl: './todo.css',
})
export class Todo implements OnInit {
  todoData = signal<PagedResponse<TodoDto> | null>(null);
  openCreateModal = signal<boolean>(false);
  // "Bắt cầu" 2 hàm này thành thuộc tính của class để HTML có thể nhìn thấy
  readonly getPriorityLabel = getPriorityLabel;
  readonly getPriorityColor = getPriorityColor;
  readonly getStatusColor = getStatusColor;

  currentPage = signal<number>(1);
  pageSize = signal<number>(10);

  isModalOpen = false;
  selectedTodo?: TodoDto;
  text: string = '';

  openModal(todo?: TodoDto) {
    this.selectedTodo = todo;
    this.text = 'task tạo mới todo';
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
  }

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
    if (
      page === this.currentPage() ||
      page < 1 ||
      (this.todoData() && page > this.todoData()!.totalPages)
    ) {
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

  async delete(id: string) {
    if (!confirm('Bạn có chắc muốn xóa công việc này?')) return;
    this.loadingService.show();
    try {
      this.loadingService.show();
      await this.todoApiService.DeleteTodo(id);
      // Sau khi xóa thành công, tải lại danh sách để cập nhật giao diện
      this.loadTodos();
    } catch (error) {
      console.error('Lỗi khi xóa Todo:', error);
      // Có thể gọi service Toast
    } finally {
      this.loadingService.hide();
    }
  }
}
