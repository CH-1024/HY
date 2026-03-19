using HY.ApiService.Dtos;

namespace HY.ApiService.Models
{
    public sealed class LoginResult
    {
        public bool IsSucc { get; init; }
        public string? Error { get; init; }
        public UserDto? User { get; init; }
        public TokenResult? TokenResult { get; init; }

        public LoginResult(bool isSucc, string? error = null, UserDto? user = null, TokenResult? tokenResult = null)
        {
            IsSucc = isSucc;
            Error = error;
            User = user;
            TokenResult = tokenResult;
        }
    }
}
