using System.Xml.Linq;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace MuuqWear.Web.Services;

public interface IDataProtectionKeyDocuments
{
    IReadOnlyList<string> ReadAllXml();
    void Write(string documentId, string xml);
}

public sealed class FirestoreXmlRepository : IXmlRepository
{
    public const string CollectionName = "_dataProtectionKeys";
    private readonly IDataProtectionKeyDocuments _documents;

    public FirestoreXmlRepository(FirestoreDb database)
        : this(new FirestoreKeyDocuments(database))
    {
    }

    public FirestoreXmlRepository(IDataProtectionKeyDocuments documents)
    {
        _documents = documents;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();
        foreach (var xml in _documents.ReadAllXml())
        {
            if (!string.IsNullOrWhiteSpace(xml))
                elements.Add(XElement.Parse(xml));
        }
        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        _documents.Write(DocumentId(element), element.ToString(SaveOptions.DisableFormatting));
    }

    public static string DocumentId(XElement element)
    {
        var id = element.Attribute("id")?.Value?.Trim();
        if (string.IsNullOrEmpty(id))
            id = Guid.NewGuid().ToString("N");
        return id.Replace("/", "_", StringComparison.Ordinal);
    }
}

internal sealed class FirestoreKeyDocuments : IDataProtectionKeyDocuments
{
    private readonly FirestoreDb _database;

    public FirestoreKeyDocuments(FirestoreDb database)
    {
        _database = database;
    }

    public IReadOnlyList<string> ReadAllXml()
    {
        var snapshot = _database.Collection(FirestoreXmlRepository.CollectionName)
            .GetSnapshotAsync()
            .GetAwaiter()
            .GetResult();
        var xml = new List<string>();
        foreach (var document in snapshot.Documents)
        {
            if (document.TryGetValue("xml", out string value))
                xml.Add(value);
        }
        return xml;
    }

    public void Write(string documentId, string xml)
    {
        _database.Collection(FirestoreXmlRepository.CollectionName)
            .Document(documentId)
            .SetAsync(new Dictionary<string, object>
            {
                ["xml"] = xml,
                ["createdAt"] = Timestamp.GetCurrentTimestamp()
            })
            .GetAwaiter()
            .GetResult();
    }
}
