namespace FileService.Domain;

public enum AssetType
{
    VIDEO,
    PREVIEW,
    AVATAR,
}

public static class AssetTypeExtensions
{
    public static AssetType ToAssetType(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "video" => AssetType.VIDEO,
            "preview" => AssetType.PREVIEW,
            "avatar" => AssetType.AVATAR,
            _ => throw new ArgumentException($"Invalid asset type: {value}")
        };
    }
}
