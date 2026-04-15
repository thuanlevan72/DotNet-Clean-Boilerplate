using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Domain.Shared
{
    /// <summary>
    /// Lớp tiện ích hỗ trợ xây dựng biểu thức lọc dữ liệu động (Dynamic LINQ Expression).
    /// Giúp tự động chuyển đổi Dictionary từ Frontend thành câu lệnh WHERE trong SQL.
    /// </summary>
    public static class DynamicFilterBuilder
    {
        /// <summary>
        /// Tạo tự động Expression lọc dữ liệu dựa trên danh sách Key-Value
        /// </summary>
        /// <typeparam name="T">Entity cần lọc (VD: TodoItem)</typeparam>
        /// <param name="filters">Dictionary chứa tên thuộc tính và giá trị cần lọc</param>
        /// <returns>Biểu thức Lambda (x => x.Prop == Value)</returns>
        public static Expression<Func<T, bool>> Build<T>(Dictionary<string, object>? filters)
        {
            // 1. Khởi tạo tham số đại diện cho Entity (tương đương biến 'x' trong x => ...)
            var parameter = Expression.Parameter(typeof(T), "x");

            // Nếu không có filter, trả về biểu thức luôn đúng (x => true)
            if (filters == null || !filters.Any())
            {
                return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), parameter);
            }

            Expression? combinedExpression = null;

            foreach (var filter in filters)
            {
                // 2. Sử dụng Reflection để tìm Property trong Entity
                // BindingFlags.IgnoreCase giúp khớp 'status' (frontend) với 'Status' (backend)
                var propInfo = typeof(T).GetProperty(
                    filter.Key,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (propInfo == null) continue;

                var property = Expression.Property(parameter, propInfo);
                var value = filter.Value;

                Expression condition;

                // 3. Xử lý logic so sánh dựa trên kiểu dữ liệu của thuộc tính
                if (property.Type == typeof(string) && value != null)
                {
                    // --- LOGIC XỬ LÝ LIKE LINH HOẠT ---
                    string val = value.ToString()!;
                    MethodInfo method;
                    string searchPattern;

                    if (val.StartsWith("%") && val.EndsWith("%"))
                    {
                        // %keyword% -> Tương đương LIKE '%value%'
                        method = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                        searchPattern = val.Trim('%');
                    }
                    else if (val.StartsWith("%"))
                    {
                        // %keyword -> Tương đương LIKE '%value'
                        method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!;
                        searchPattern = val.TrimStart('%');
                    }
                    else if (val.EndsWith("%"))
                    {
                        // keyword% -> Tương đương LIKE 'value%'
                        method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!;
                        searchPattern = val.TrimEnd('%');
                    }
                    else
                    {
                        // Mặc định không có % thì dùng Contains (LIKE '%value%') cho tiện dụng
                        method = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                        searchPattern = val;
                    }

                    var constant = Expression.Constant(searchPattern, typeof(string));
                    condition = Expression.Call(property, method, constant);
                }
                else if (value == null)
                {
                    // Lọc giá trị NULL
                    condition = Expression.Equal(property, Expression.Constant(null, property.Type));
                }
                else
                {
                    // Các kiểu dữ liệu khác (int, Guid, Enum, bool, DateTime)
                    var convertedValue = ConvertValue(value, property.Type);
                    var constant = Expression.Constant(convertedValue, property.Type);
                    condition = Expression.Equal(property, constant);
                }

                // 4. Ghép nối các điều kiện bằng toán tử AND (&&)
                combinedExpression = combinedExpression == null
                    ? condition
                    : Expression.AndAlso(combinedExpression, condition);
            }

            combinedExpression ??= Expression.Constant(true);

            return Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        }

        /// <summary>
        /// Hàm hỗ trợ chuyển đổi kiểu dữ liệu an toàn, đặc biệt là Guid và Enum
        /// </summary>
        private static object? ConvertValue(object value, Type targetType)
        {
            var realType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (realType == typeof(Guid))
            {
                return Guid.Parse(value.ToString()!);
            }

            if (realType.IsEnum)
            {
                return Enum.Parse(realType, value.ToString()!);
            }

            return Convert.ChangeType(value, realType);
        }
    }
}