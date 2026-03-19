namespace HY.ApiService.Models
{
    public sealed class UploadResult
    {
        public bool IsSucc { get; init; }
        public string? Error { get; init; }
        public string? FileId { get; init; }

        public UploadResult(bool isSucc, string? error = null, string? fileId = null)
        {
            IsSucc = isSucc;
            Error = error;
            FileId = fileId;
        }
    }
}
