namespace Application.Common.Pagination;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    public PaginatedResponse()
    {
    }

    public PaginatedResponse(IEnumerable<T> data, int pageNumber, int pageSize, int totalItems)
    {
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItems = totalItems;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }
}

public class PaginationParameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    private const int MaxPageSize = 50;

    public PaginationParameters()
    {
    }

    public PaginationParameters(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize < 1 ? 10 : pageSize;
    }
}
