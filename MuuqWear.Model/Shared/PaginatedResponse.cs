namespace MuuqWear.Model.Shared;
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }      // ← add setter
    public bool HasMore { get; set; }        // ← add setter
    public bool HasPreviousPage { get; set; } // ← add setter
    public bool HasNextPage { get; set; }     // ← add setter

    public static PaginatedResponse<T> Empty(int page = 1, int pageSize = 20) =>
        new()
        {
            Data = [],
            TotalCount = 0,
            Page = page < 1 ? 1 : page,
            PageSize = pageSize < 1 ? 20 : pageSize,
            TotalPages = 0
        };
}
