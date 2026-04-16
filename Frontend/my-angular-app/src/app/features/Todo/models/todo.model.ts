// Định nghĩa sẵn các trạng thái cho chuẩn
export type PriorityLevel = 'None' | 'Low' | 'Medium' | 'High' | 'Urgent';
export enum PriorityLevelEnum{
  None = 0,
  Low = 1,
  Medium = 2,
  High = 3,
  Urgent = 4
}
export type TodoStatus = 'Todo' | 'InProgress' | 'Done' | 'Blocked' | 'OnHold';

export interface TodoDto {
  id: string;
  title: string;
  description: string | null;
  priority: PriorityLevelEnum;
  status: TodoStatus;
  dueDate: string | null; // C# là DateTimeOffset? nhưng xuống JSON sẽ thành string chuẩn ISO
  createdAt: string;
  category: TodoCategoryDto| null;
  tags: TodoTagDto[] | null;
}


export interface CreateOrUpdateTodoDto {
  id?: string;
  title: string;
  description?: string | null;
  priority: PriorityLevelEnum;
  tags: number[]; // Mảng chứa ID của các tag được chọn
  dueDate?: string | null; // C# là DateTimeOffset? nhưng xuống JSON sẽ thành string chuẩn ISO
  categoryId?: string | null; // cái này là checkbox nên chỉ cần truyền ID của category được chọn, hoặc có thể là selector những chỉ được chonj 1 cái
}


export interface TodoCategoryDto {
  id: string;
  name: string;
  colorHex: string; // Ví dụ: "#FF5733"
}


export interface TodoTagDto {
  id: number;
  name: string;
}
