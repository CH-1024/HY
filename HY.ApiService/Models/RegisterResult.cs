namespace HY.ApiService.Models
{
    public sealed class RegisterResult
    {
        public bool IsSucc { get; init; }
        public string? Error { get; init; }

        public RegisterResult(bool isSucc, string? error = null)
        {
            IsSucc = isSucc;
            Error = error;
        }

    }
}
