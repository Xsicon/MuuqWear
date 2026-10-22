namespace MuuqWear.Application.Shared;
public class Response<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static Response<T> SuccessResponse(T data, string message = "")
    {
        return new Response<T>
        {
            Data = data,
            Success = true,
            Message = message
        };
    }

    public static Response<T> Fail(string message)
    {
        return new Response<T>
        {
            Success = false,
            Message = message
        };
    }

    public static bool TryEmptySuccess(out Response<T> response)
    {
        response = Fail("Unavailable");
        var type = typeof(T);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            response = SuccessResponse((T)Activator.CreateInstance(type)!);
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(MuuqWear.Model.Shared.PaginatedResponse<>))
        {
            response = SuccessResponse((T)Activator.CreateInstance(type)!);
            return true;
        }

        return false;
    }
}
