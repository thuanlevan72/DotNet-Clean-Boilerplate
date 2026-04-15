// Định nghĩa sẵn các trạng thái cho chuẩn
export type PriorityLevel = 'None' | 'Low' | 'Medium' | 'High' | 'Urgent';
export type TodoStatus = 'Todo' | 'InProgress' | 'Done' | 'Blocked' | 'OnHold';

export interface TodoDto {
  id: string;
  title: string;
  description: string | null;
  priority: PriorityLevel;
  status: TodoStatus;
  dueDate: string | null; // C# là DateTimeOffset? nhưng xuống JSON sẽ thành string chuẩn ISO
  createdAt: string;
  categoryName: string | null;
}
