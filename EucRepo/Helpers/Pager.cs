using Microsoft.AspNetCore.Mvc.Rendering;

namespace EucRepo.Helpers;
  public class Pager
    {
        public Pager(
            int totalItems,
            int currentPage = 1,
            int pageSize = 10,
            int maxPages = 10,
            IEnumerable<int>? pageSizeOptions  = null
            )
        {
            // calculate total pages
            var totalPages = (int)Math.Ceiling((decimal)totalItems / (decimal)pageSize);

            // ensure current page isn't out of range
            if (currentPage < 1)
            {
                currentPage = 1;
            }
            else if (currentPage > totalPages)
            {
                currentPage = totalPages;
            }

            int startPage, endPage;
            if (totalPages <= maxPages) 
            {
                // total pages less than max so show all pages
                startPage = 1;
                endPage = totalPages;
            }
            else 
            {
                // total pages more than max so calculate start and end pages
                var maxPagesBeforeCurrentPage = (int)Math.Floor((decimal)maxPages / (decimal)2);
                var maxPagesAfterCurrentPage = (int)Math.Ceiling((decimal)maxPages / (decimal)2) - 1;
                if (currentPage <= maxPagesBeforeCurrentPage) 
                {
                    // current page near the start
                    startPage = 1;
                    endPage = maxPages;
                } 
                else if (currentPage + maxPagesAfterCurrentPage >= totalPages) 
                {
                    // current page near the end
                    startPage = totalPages - maxPages + 1;
                    endPage = totalPages;
                }
                else 
                {
                    // current page somewhere in the middle
                    startPage = currentPage - maxPagesBeforeCurrentPage;
                    endPage = currentPage + maxPagesAfterCurrentPage;
                }
            }

            // calculate start and end item indexes
            var startIndex = (currentPage - 1) * pageSize;
            var endIndex = Math.Min(startIndex + pageSize - 1, totalItems - 1);

            // create an array of pages that can be looped over
            var pages = Enumerable.Range(startPage, (endPage + 1) - startPage);
            
            // create a select list from the array of pages
            var selectListPages = Enumerable.Range(startPage, (endPage + 1) - startPage)
                .Select(x => new SelectListItem() { Text = x.ToString(),Selected = x==currentPage});

            // create a select list for the page sizes
            pageSizeOptions ??= new List<int>() { 5, 10, 25, 50 };
            var sizeOptions = pageSizeOptions.ToList();
            var selectListPageSizes = sizeOptions.Select(x => new SelectListItem()
                { Text = x.ToString(), Selected = x == pageSize });
            
            // create a string for displaying the current page record counts
            var pageItems = $"{startIndex+1}-{endIndex+1} of {totalItems}";
            
            // update object instance with all pager properties required by the view
            TotalItems = totalItems;
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalPages = totalPages;
            StartPage = startPage;
            EndPage = endPage;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Pages = pages;
            PageSizes = sizeOptions;
            SelectListPages = selectListPages;
            SelectListPageSizes = selectListPageSizes;
            PageItems = pageItems;
        }

        public int TotalItems { get; private set; }
        public int CurrentPage { get; private set; }
        public int PageSize { get; private set; }
        public int TotalPages { get; private set; }
        public int StartPage { get; private set; }
        public int EndPage { get; private set; }
        public int StartIndex { get; private set; }
        public int EndIndex { get; private set; }
        public IEnumerable<int> Pages { get; private set; }
        public IEnumerable<int> PageSizes { get; private set; }
        public IEnumerable<SelectListItem> SelectListPages { get; private set; }
        public IEnumerable<SelectListItem> SelectListPageSizes { get; private set; }
        public string PageItems { get; private set; }
    }