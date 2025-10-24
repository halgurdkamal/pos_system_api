namespace pos_system_api.Core.Application.Common.Models;

/// <summary>
/// Represents a paginated result set
/// </summary>
public class PagedResult<T>
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public List<T> Data { get; set; } = new();
    
    public PagedResult()
    {
    }
    
    public PagedResult(List<T> data, int page, int limit, int total)
    {
        Data = data;
        Page = page;
        Limit = limit;
        Total = total;
    }
}
