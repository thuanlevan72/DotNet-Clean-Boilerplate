export function formatDate(date: Date): string {
  return new Intl.DateTimeFormat('vi-VN').format(date);
}
