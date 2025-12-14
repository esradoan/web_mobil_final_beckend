namespace SmartCampus.Business.DTOs
{
    /// <summary>
    /// Genel pagination response wrapper
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int Limit { get; set; }
        public int Total { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)Total / Limit);
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;
    }

    /// <summary>
    /// Standart API error response
    /// </summary>
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
