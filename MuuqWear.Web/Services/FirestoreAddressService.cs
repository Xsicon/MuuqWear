using System.Security.Claims;
using Google.Cloud.Firestore;
using MuuqWear.Application.Services.AddressService;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Address;

namespace MuuqWear.Web.Services;

public sealed class FirestoreAddressService : IAddressService
{
    private readonly FirestoreDb _db;
    private readonly IHttpContextAccessor _http;

    public FirestoreAddressService(IConfiguration config, IHttpContextAccessor http)
    {
        _db = FirestoreDb.Create(config["Firebase:ProjectId"] ?? "muuqwear-6e2e0");
        _http = http;
    }

    public async Task<Response<List<AddressModel>>> GetAll()
    {
        var col = Addresses();
        if (col is null)
            return Response<List<AddressModel>>.Fail("Sign in to manage addresses.");
        var snap = await col.GetSnapshotAsync();
        return Response<List<AddressModel>>.SuccessResponse(
            snap.Documents.Select(ToAddress).OrderByDescending(a => a.IsDefault).ToList());
    }

    public async Task<Response<AddressModel>> GetById(Guid id)
    {
        var col = Addresses();
        if (col is null)
            return Response<AddressModel>.Fail("Sign in to manage addresses.");
        var snap = await col.Document(id.ToString()).GetSnapshotAsync();
        return snap.Exists
            ? Response<AddressModel>.SuccessResponse(ToAddress(snap))
            : Response<AddressModel>.Fail("Address not found");
    }

    public async Task<Response<AddressModel>> Create(CreateAddressModel request)
    {
        var col = Addresses();
        if (col is null)
            return Response<AddressModel>.Fail("Sign in to manage addresses.");
        var id = Guid.NewGuid();
        if (request.IsDefault)
            await ClearDefaults(col);
        await col.Document(id.ToString()).SetAsync(Fields(
            request.Label, request.FullName, request.Street1, request.Street2,
            request.City, request.State, request.PostalCode, request.Country, request.IsDefault));
        return await GetById(id);
    }

    public async Task<Response<AddressModel>> Update(Guid id, UpdateAddressModel request)
    {
        var col = Addresses();
        if (col is null)
            return Response<AddressModel>.Fail("Sign in to manage addresses.");
        if (request.IsDefault)
            await ClearDefaults(col);
        await col.Document(id.ToString()).SetAsync(Fields(
            request.Label, request.FullName, request.Street1, request.Street2,
            request.City, request.State, request.PostalCode, request.Country, request.IsDefault), SetOptions.MergeAll);
        return await GetById(id);
    }

    public async Task<Response<bool>> Delete(Guid id)
    {
        var col = Addresses();
        if (col is null)
            return Response<bool>.Fail("Sign in to manage addresses.");
        await col.Document(id.ToString()).DeleteAsync();
        return Response<bool>.SuccessResponse(true);
    }

    public async Task<Response<AddressModel>> SetDefault(Guid id)
    {
        var col = Addresses();
        if (col is null)
            return Response<AddressModel>.Fail("Sign in to manage addresses.");
        await ClearDefaults(col);
        await col.Document(id.ToString()).UpdateAsync("isDefault", true);
        return await GetById(id);
    }

    private CollectionReference? Addresses()
    {
        var uid = _http.HttpContext?.User.FindFirst("UserId")?.Value
            ?? _http.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return null;
        return _db.Collection("customers").Document(uid).Collection("addresses");
    }

    private static async Task ClearDefaults(CollectionReference col)
    {
        var snap = await col.GetSnapshotAsync();
        foreach (var doc in snap.Documents.Where(d => d.ContainsField("isDefault") && d.GetValue<bool>("isDefault")))
            await doc.Reference.UpdateAsync("isDefault", false);
    }

    private static Dictionary<string, object> Fields(
        string? label, string? fullName, string? street1, string? street2,
        string? city, string? state, string? postalCode, string? country, bool isDefault) => new()
    {
        ["label"] = label ?? "",
        ["fullName"] = fullName ?? "",
        ["street1"] = street1 ?? "",
        ["street2"] = street2 ?? "",
        ["city"] = city ?? "",
        ["state"] = state ?? "",
        ["postalCode"] = postalCode ?? "",
        ["country"] = country ?? "US",
        ["isDefault"] = isDefault,
        ["createdAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
    };

    private static AddressModel ToAddress(DocumentSnapshot doc)
    {
        Guid.TryParse(doc.Id, out var id);
        return new AddressModel
        {
            Id = id,
            Label = Str(doc, "label"),
            FullName = Str(doc, "fullName"),
            Street1 = Str(doc, "street1"),
            Street2 = Str(doc, "street2"),
            City = Str(doc, "city"),
            State = Str(doc, "state"),
            PostalCode = Str(doc, "postalCode"),
            Country = string.IsNullOrWhiteSpace(Str(doc, "country")) ? "US" : Str(doc, "country"),
            IsDefault = doc.ContainsField("isDefault") && doc.GetValue<bool>("isDefault")
        };
    }

    private static string Str(DocumentSnapshot doc, string field) =>
        doc.ContainsField(field) ? doc.GetValue<string>(field) ?? "" : "";
}
