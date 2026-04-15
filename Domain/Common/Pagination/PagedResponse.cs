using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Common.Pagination
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        // Tự động tính toán tổng số trang
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        // Constructor tiện ích
        public PagedResponse(List<T> items, int totalItems, int currentPage, int pageSize)
        {
            Items = items;
            TotalItems = totalItems;
            CurrentPage = currentPage;
            PageSize = pageSize;
        }
    }
}
