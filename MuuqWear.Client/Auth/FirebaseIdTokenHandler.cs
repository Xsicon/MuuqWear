namespace MuuqWear.Client.Auth;

public sealed class FirebaseIdTokenHandler : DelegatingHandler
{
    private readonly IFirebaseIdTokenSource _tokens;

    public FirebaseIdTokenHandler(IFirebaseIdTokenSource tokens)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        FirebaseAuthorization.Apply(request, await _tokens.GetIdTokenAsync());
        return await base.SendAsync(request, cancellationToken);
    }
}
