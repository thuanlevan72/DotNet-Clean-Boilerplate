import { CreateOrUpdateTodo } from './../components/create-or-update-todo/create-or-update-todo';
import { PagedResponse } from './../../../shared/models/paged-response.model';
import { inject, Injectable } from '@angular/core';
import { BaseApiService } from '../../../core/api/base-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CreateOrUpdateTodoDto, TodoCategoryDto, TodoDto, TodoTagDto } from '../models/todo.model';

@Injectable({ providedIn: 'root' })
export class TodoApiService extends BaseApiService {
  // Định nghĩa đường dẫn gốc cho module này
  private readonly ENDPOINTG = {
    GET_TODOS: '/api/todos',
    GET_TAGS: '/api/tags',
    GET_CATEGORIES: '/api/categories',
    CREATE_TODO: '/api/todos',
    UPDATE_TODO: (id: string) => `/api/todos/${id}`,
    CHANGE_STATUS: (id: string) => `/api/todos/${id}/status`,
    DELETE_TODO: (id: string) => `/api/todos/${id}`,
  };
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  public async DeleteTodo(id: string): Promise<void> {
    try {
      const response = await this.delete<void>(this.ENDPOINTG.DELETE_TODO(id));
    } catch (error) {
      console.error('Lỗi khi xóa Todo:', error);
      throw error; // Quăng lỗi lên tầng trên
    }
  }

  public async CreateOrUpdateTodo(todo: CreateOrUpdateTodoDto): Promise<string | TodoDto | null> {
    try {
      const response = await this.post<string>(this.ENDPOINTG.CREATE_TODO, todo);
      return response;
    } catch (error) {
      console.error('Lỗi khi tạo/cập nhật Todo:', error);
      throw error; // Quăng lỗi lên tầng trên
    }
  }

  public async changeStatus(id: string, newStatus: number): Promise<void> {
    try {
      await this.patch<void>(this.ENDPOINTG.CHANGE_STATUS(id), { status: newStatus });
      console.log('Trạng thái đã được cập nhật thành công');
    } catch (error) {
      console.error('Lỗi khi thay đổi trạng thái Todo:', error);
      throw error; // Quăng lỗi lên tầng trên
    }
  }
  /// Lấy danh sách loại danh mục của todo app
  public async getCategories(): Promise<TodoCategoryDto[] | null> {
    try {
      const user = this.authService.getUser(); // ✅ Lấy thông tin user từ AuthService (nếu cần)
      const response = await this.get<TodoCategoryDto[]>(this.ENDPOINTG.GET_CATEGORIES);
      return response;
    } catch (error) {
      console.error('Lỗi khi lấy danh sách Todo:', error);
      throw error; // Quăng lỗi lên tầng trên
    }
  }
  // lấy danh sách danh sách tag của todo app
  //#region Lấy danh sách danh sách tag của todo app
  public async getTags(): Promise<TodoTagDto[] | null> {
    try {
      const user = this.authService.getUser(); // ✅ Lấy thông tin user từ AuthService (nếu cần)
      const response = await this.get<TodoTagDto[]>(this.ENDPOINTG.GET_TAGS);
      return response;
    } catch (error) {
      console.error('Lỗi khi lấy danh sách Todo:', error);
      throw error; // Quăng lỗi lên tầng trên
    }
  }
  //#endregion
  /**
   * Lấy danh sách Todo có phân trang
   * @param pageNumber Trang hiện tại (mặc định là 1)
   * @param pageSize Số lượng/trang (mặc định là 10)
   */
  public async getTodos(
    pageNumber: number = 1,
    pageSize: number = 10,
  ): Promise<PagedResponse<TodoDto> | null> {
    try {
      // Vì hàm get của BaseApiService hỗ trợ nhận 'params',
      // ta chỉ cần truyền thẳng object vào đây. BaseService sẽ tự map thành URL: /api/todos?pageNumber=1&pageSize=10
      const params = {
        'PaginationRequest.PageNumber': pageNumber,
        'PaginationRequest.PageSize': pageSize,
      };

      const response = await this.get<PagedResponse<TodoDto>>(this.ENDPOINTG.GET_TODOS, params);
      return response;
    } catch (error) {
      console.error('Lỗi khi lấy danh sách Todo:', error);
      throw error; // Quăng lỗi lên tầng trên
    }
  }
}
