import { PagedResponse } from './../../../shared/models/paged-response.model';
import { inject, Injectable } from '@angular/core';
import { BaseApiService } from '../../../core/api/base-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TodoDto } from '../models/todo.model';

@Injectable({ providedIn: 'root' })
export class TodoApiService extends BaseApiService {
  // Định nghĩa đường dẫn gốc cho module này
  private readonly ENDPOINT = '/api/todos';
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);


 /**
   * Lấy danh sách Todo có phân trang
   * @param pageNumber Trang hiện tại (mặc định là 1)
   * @param pageSize Số lượng/trang (mặc định là 10)
   */
  public async getTodos(pageNumber: number = 1, pageSize: number = 10): Promise<PagedResponse<TodoDto> | null> {
    try {
      // Vì hàm get của BaseApiService hỗ trợ nhận 'params',
      // ta chỉ cần truyền thẳng object vào đây. BaseService sẽ tự map thành URL: /api/todos?pageNumber=1&pageSize=10
      const params = {
    'PaginationRequest.PageNumber': pageNumber,
    'PaginationRequest.PageSize': pageSize
  };

      const response = await this.get<PagedResponse<TodoDto>>(this.ENDPOINT, params);
      return response;

    } catch (error) {
      console.error('Lỗi khi lấy danh sách Todo:', error);
      return null; // Hoặc bạn có thể throw error để Component tự xử lý
    }
  }
}
