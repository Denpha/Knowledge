namespace KMS.Api.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }

    public ApiResponse()
    {
        Timestamp = DateTime.UtcNow;
    }

    public ApiResponse(T data, string? message = null) : this()
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public ApiResponse(string message, List<string>? errors = null) : this()
    {
        Success = false;
        Message = message;
        Errors = errors;
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; }

    public ApiResponse()
    {
        Timestamp = DateTime.UtcNow;
    }

    public ApiResponse(bool success, string? message = null) : this()
    {
        Success = success;
        Message = message;
    }

    public ApiResponse(string message, List<string>? errors = null) : this()
    {
        Success = false;
        Message = message;
        Errors = errors;
    }
}