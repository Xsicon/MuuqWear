using System.Xml.Linq;
using MuuqWear.Web.Services;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class FirestoreXmlRepositoryTests
{
    [Fact]
    public void Round_trip_keeps_the_key_xml()
    {
        var store = new MemoryKeyDocuments();
        var repo = new FirestoreXmlRepository(store);
        var key = XElement.Parse("""<key id="abc-1"><masterKey>c2VjcmV0</masterKey></key>""");

        repo.StoreElement(key, "key-abc");

        var loaded = Assert.Single(repo.GetAllElements());
        Assert.Equal("abc-1", loaded.Attribute("id")!.Value);
        Assert.Equal("c2VjcmV0", loaded.Element("masterKey")!.Value);
        Assert.Equal("abc-1", Assert.Single(store.Ids));
    }

    [Fact]
    public void Missing_key_id_still_round_trips()
    {
        var store = new MemoryKeyDocuments();
        var repo = new FirestoreXmlRepository(store);

        repo.StoreElement(XElement.Parse("<key><value>1</value></key>"), "generated");

        var loaded = Assert.Single(repo.GetAllElements());
        Assert.Equal("1", loaded.Element("value")!.Value);
        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(store.Ids)));
    }

    [Fact]
    public void Blank_documents_are_skipped()
    {
        var store = new MemoryKeyDocuments();
        store.Write("empty", "  ");
        var repo = new FirestoreXmlRepository(store);

        Assert.Empty(repo.GetAllElements());
    }

    private sealed class MemoryKeyDocuments : IDataProtectionKeyDocuments
    {
        private readonly Dictionary<string, string> _docs = new(StringComparer.Ordinal);

        public IReadOnlyCollection<string> Ids => _docs.Keys;

        public IReadOnlyList<string> ReadAllXml() => _docs.Values.ToList();

        public void Write(string documentId, string xml) => _docs[documentId] = xml;
    }
}
