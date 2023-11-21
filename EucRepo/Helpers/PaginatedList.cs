using Microsoft.EntityFrameworkCore;

namespace EucRepo.Helpers;

 public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int PageSize { get; private set; }
        public new int Count { get; private set; }
        public int StartRecord { get; private set; }
        public int EndRecord { get; private set; }
        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            Count = count;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            StartRecord = (pageIndex - 1) * pageSize + 1;
            EndRecord = StartRecord + PageSize > count ? count : StartRecord + PageSize - 1;

            this.AddRange(items);
        }

        public bool HasPreviousPage => (PageIndex > 1);
        public bool HasNextPage => (PageIndex < TotalPages);

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
        public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
        public static PaginatedList<T> Recreate(List<T> source, int pageIndex, int pageSize, int count = 0)
        {

            var items = source;
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

    }