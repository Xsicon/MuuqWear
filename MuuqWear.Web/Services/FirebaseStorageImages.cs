using Google.Cloud.Storage.V1;
using MuuqWear.Application.Catalog;
using MuuqWear.Application.Shared;

namespace MuuqWear.Web.Services;

internal static class FirebaseStorageImages
{
    public static Task<Response<string>> UploadAsync(
        IConfiguration config,
        string folder,
        string fileName,
        byte[] bytes,
        string contentType) =>
        UploadAsync(config, folder, fileName, bytes, contentType, AllowImage);

    public static async Task<Response<string>> UploadAsync(
        IConfiguration config,
        string folder,
        string fileName,
        byte[] bytes,
        string contentType,
        Func<string, string, bool> isAllowed)
    {
        if (bytes.Length == 0)
            return Response<string>.Fail("Empty file");

        contentType ??= "";
        if (!isAllowed(contentType, fileName ?? ""))
            return Response<string>.Fail("File type not allowed");

        var bucket = config["Firebase:StorageBucket"] ?? "muuqwear-6e2e0.firebasestorage.app";
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = $"{Guid.NewGuid():N}.bin";

        var objectName = $"{folder.Trim('/')}/{Guid.NewGuid():N}/{safeName}";
        var client = await StorageClient.CreateAsync();
        await using var stream = new MemoryStream(bytes);
        await client.UploadObjectAsync(
            bucket,
            objectName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            stream);
        return Response<string>.SuccessResponse(StorefrontCatalogMap.ImageUrl(objectName));
    }

    public static bool AllowImage(string contentType, string _) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public static bool AllowResume(string contentType, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".pdf" or ".doc" or ".docx")
            return true;

        return contentType is "application/pdf"
            or "application/msword"
            or "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    }

    public static bool AllowAffiliateSample(string contentType, string _) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
}
