namespace HY.ApiService.Enums
{
    [Flags]
    public enum VariantType
    {
        Thumbnail = 1 << 0, // 1
        Compress = 1 << 1,  // 2
        Cover = 1 << 2,     // 4
        LowVideo = 1 << 3,  // 8
    }
}
