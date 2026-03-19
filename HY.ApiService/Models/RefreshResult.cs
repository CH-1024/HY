namespace HY.ApiService.Models
{
    public sealed class RefreshResult
    {
        public bool IsSucc { get; init; }
        public string? Error { get; init; }
        public TokenResult? TokenResult { get; init; }

        public RefreshResult(bool isSucc, string? error = null, TokenResult? tokenResult = null)
        {
            IsSucc = isSucc;
            Error = error;
            TokenResult = tokenResult;
        }
    }
}
