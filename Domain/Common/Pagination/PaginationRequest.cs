using Domain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Common.Pagination
{
    public class PaginationRequest
    {
        const int maxPageSize = 50; // Giới hạn tối đa 50 record/trang

        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10; // Mặc định là 10
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }

        public Dictionary<string, object>? DynamicFilters { get; set; }
        // Sau này có thể thêm:
        // public string? SearchTerm { get; set; }
        // public string? SortBy { get; set; }
    }
}
