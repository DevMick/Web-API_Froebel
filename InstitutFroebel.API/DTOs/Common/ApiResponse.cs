namespace InstitutFroebel.API.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public object? Metadata { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string message = "Opération réussie")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }

        public static ApiResponse<T> ErrorResult(List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Erreurs de validation",
                Errors = errors
            };
        }
    }

    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse SuccessResult(string message = "Opération réussie")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public new static ApiResponse ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}