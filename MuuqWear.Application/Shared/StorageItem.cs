namespace MuuqWear.Application.Shared;
public class StorageItem<T>
{
    public T? Value { get; set; }
    public DateTime Expiry { get; set; }
}
