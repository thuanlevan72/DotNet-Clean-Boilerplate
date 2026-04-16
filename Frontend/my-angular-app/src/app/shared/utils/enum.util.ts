// 1. Import Enum từ thư mục models (bạn nhớ check lại tên file chứa Enum nhé, mình đoán là todo.model.ts)

import { PriorityLevelEnum, TodoStatus } from "../../features/Todo/models/todo.model";


/**
 * Hàm chuyển đổi từ Enum (số) sang Nhãn (chữ) để hiển thị lên UI
 */
export function getPriorityLabel(priority: PriorityLevelEnum): string {
  switch (priority) {
    case PriorityLevelEnum.Low: return 'Low';
    case PriorityLevelEnum.Medium: return 'Medium';
    case PriorityLevelEnum.High: return 'High';
    case PriorityLevelEnum.Urgent: return 'Urgent';
    default: return '';
  }
}


export function getStatusColor(status: TodoStatus): string {
    switch (status) {
      case 'Done':
        return 'text-green-600 bg-green-50 border-green-200';
      case 'InProgress':
        return 'text-blue-600 bg-blue-50 border-blue-200';
      case 'Blocked':
        return 'text-red-600 bg-red-50 border-red-200';
      case 'OnHold':
        return 'text-gray-500 bg-gray-50 border-gray-200';
      default:
        return 'text-gray-600 bg-gray-100 border-gray-200';
    }
  }

/**
 * Hàm lấy class màu sắc tương ứng với mức độ ưu tiên
 */
export function getPriorityColor(priority: PriorityLevelEnum): string {
  switch (priority) {
    case PriorityLevelEnum.Low:
      return 'bg-blue-50 text-blue-700 border-blue-200';
    case PriorityLevelEnum.Medium:
      return 'bg-yellow-50 text-yellow-700 border-yellow-200';
    case PriorityLevelEnum.High:
      return 'bg-orange-50 text-orange-700 border-orange-200';
    case PriorityLevelEnum.Urgent:
      return 'bg-red-50 text-red-700 border-red-200';
    default:
      return 'bg-gray-50 text-gray-700 border-gray-200';
  }
}
