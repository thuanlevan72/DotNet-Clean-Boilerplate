import { Component, EventEmitter, inject, Input, Output, signal, OnInit, input } from '@angular/core';
import { CreateOrUpdateTodoDto, PriorityLevelEnum, TodoCategoryDto, TodoDto, TodoTagDto } from '../../models/todo.model';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { TodoApiService } from '../../services/todo-api.service';
import { LoadingService } from '../../../../shared/components/Loading/loading.service';

@Component({
  selector: 'app-create-or-update-todo',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './create-or-update-todo.html',
  styleUrl: './create-or-update-todo.css',
})
export class CreateOrUpdateTodo implements OnInit {

  // 1. SERVICES
  private readonly todoApiService = inject(TodoApiService);
  private readonly loadingService = inject(LoadingService);

  // 2. STATE & OUTPUTS
  isEditMode = signal<boolean>(false);
  @Output() close = new EventEmitter<void>();
  @Output() saveSuccess = new EventEmitter<void>(); // Bắn event khi lưu thành công để cha load lại Data Table
  @Input() stateTodo?: string; // Dữ liệu Todo được truyền vào từ Component Cha (List) khi user nhấn Edit, nếu không có nghĩa là đang ở chế độ Create
  // 3. INPUT (Tự động bật chế độ Edit nếu có data truyền vào)
  @Input() set todo(value: TodoDto | undefined) {
    this._todo = value;
    if (value && value.id) {
      this.isEditMode.set(true);
    }
  }
  get todo(): TodoDto | undefined { return this._todo; }
  private _todo?: TodoDto;

  // 4. DATA MASTER
  categories: TodoCategoryDto[] = [];
  availableTags: TodoTagDto[] = [];

  // 5. FORM GROUP (Khai báo Type chuẩn)
  todoForm = new FormGroup({
    title: new FormControl('', [Validators.required]),
    description: new FormControl(''),
    priority: new FormControl<PriorityLevelEnum>(PriorityLevelEnum.Low, [Validators.required]),
    dueDate: new FormControl(''),
    categoryId: new FormControl<string | null>(null, [Validators.required]),
    tag: new FormControl<number[]>([])
  });

  // ==================== LIFECYCLE ====================
  async ngOnInit() {
    try {
      this.loadingService.show(); // Bật màn hình mờ loading

      // A. Gọi API lấy dữ liệu tham chiếu
      const [categoryRes, tagRes] = await Promise.all([
        this.todoApiService.getCategories(),
        this.todoApiService.getTags()
      ]);

      if (categoryRes) this.categories = categoryRes;
      if (tagRes) this.availableTags = tagRes;

      // B. Phân luồng dữ liệu (Đổ vào Form)
      if (this.isEditMode() && this.todo) {

        // --- LUỒNG 1: EDIT ---
        // Xử lý cắt chuỗi ngày tháng để input HTML5 type="date" có thể hiểu (YYYY-MM-DD)
        let formattedDate = '';
        if (this.todo.dueDate) {
          formattedDate = this.todo.dueDate.split('T')[0];
        }

        // Bơm dữ liệu cũ vào Form
        this.todoForm.patchValue({
          title: this.todo.title,
          description: this.todo.description,
          priority: this.todo.priority,
          dueDate: formattedDate,
          categoryId: this.todo.category?.id || null,
        });

      } else {
        const now = new Date();
        const year = now.getFullYear();
        const month = (now.getMonth() + 1).toString().padStart(2, '0');
        const day = now.getDate().toString().padStart(2, '0');
        const hours = now.getHours().toString().padStart(2, '0');
        const minutes = now.getMinutes().toString().padStart(2, '0');

        const currentDateTime = `${year}-${month}-${day}T${hours}:${minutes}`;

        // --- LUỒNG 2: CREATE ---
        // Gán Category đầu tiên làm mặc định nếu có danh mục
        if (this.categories.length > 0) {
          this.todoForm.patchValue({
            categoryId: this.categories[0].id,
            dueDate: currentDateTime
          });
        }
      }

    } catch (error) {
      console.error('Lỗi khi tải dữ liệu khởi tạo form:', error);
      this.loadingService.hide(); // Dù lỗi hay thành công cũng phải tắt loading
      this.close.emit(); // Đóng modal nếu có lỗi nghiêm trọng khi tải dữ liệu
      // Có thể gọi service Toast (thông báo góc màn hình) ở đây
    } finally {
      this.loadingService.hide(); // Dù lỗi hay thành công cũng phải tắt loading
    }
  }

  // ==================== TAGS LOGIC ====================
  onTagChange(event: Event, tagId: number) {
    const isChecked = (event.target as HTMLInputElement).checked;
    const currentTags = this.todoForm.get('tag')?.value || [];

    if (isChecked) {
      this.todoForm.patchValue({ tag: [...currentTags, tagId] }); // Thêm vào mảng
    } else {
      this.todoForm.patchValue({ tag: currentTags.filter(id => id !== tagId) }); // Lọc bỏ khỏi mảng
    }
  }

  isTagChecked(tagId: number): boolean {
    const currentTags = this.todoForm.get('tag')?.value || [];
    return currentTags.includes(tagId);
  }

  // ==================== SUBMIT LOGIC ====================
  async onSubmit() {
    // Nếu user cố tình lách luật nhấn Save khi form chưa điền đủ
    if (this.todoForm.invalid) {
      this.todoForm.markAllAsTouched();
      return;
    }

    try {
      this.loadingService.show(); // Hiện Loading trong lúc gửi Data lên Server
      // 1. Lấy dữ liệu thô (raw) từ form
      const rawValue = this.todoForm.value;
      rawValue.priority = Number(rawValue.priority); // Chuyển đổi priority từ string sang number để backend không chửi


      // 2. Chuyển đổi ngày tháng sang chuẩn UTC (có chữ Z ở cuối) để PostgreSQL không chửi
      const payload: CreateOrUpdateTodoDto = {
        ...rawValue,
        tags: rawValue.tag || [],
        dueDate: rawValue.dueDate
            ? new Date(rawValue.dueDate).toISOString()
            : null
      } as CreateOrUpdateTodoDto;

      if (this.isEditMode() && this.todo?.id) {
        // --- GỌI API UPDATE ---
        // Ví dụ: await this.todoApiService.updateTodo(this.todo.id, payload);
        console.log(`Đã gửi API UPDATE cho ID ${this.todo.id}:`, payload);
      } else {
        // --- GỌI API CREATE ---
        // Ví dụ: await this.todoApiService.createTodo(payload);
        console.log('Đã gửi API CREATE:', payload);
        await this.todoApiService.CreateOrUpdateTodo(payload as CreateOrUpdateTodoDto);
      }
      this.loadingService.hide(); // Hiện Loading trong lúc gửi Data lên Server
      // Nếu API thành công:
      this.saveSuccess.emit(); // Báo cho Component Cha (List) biết để gọi lại API getTodos()
      this.close.emit();       // Đóng Modal hiện tại

    } catch (error) {
      console.error('Lỗi khi lưu Form:', error);
      // Bắn Toast báo lỗi cho user ở đây
    } finally {
      this.loadingService.hide(); // Tắt Loading
    }
  }

  // ==================== UI ACTIONS ====================
  onCancel() { this.close.emit(); }
  onOverlayClick() { this.close.emit(); }
}
