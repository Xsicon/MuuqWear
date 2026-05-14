using MuuqWear.Model.Vote;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.Interfaces;
using Supabase.Realtime.PostgresChanges;


namespace MuuqWear.Application.Services.VoteService;

public class VoteRealtimeSubscription : IAsyncDisposable
{
    private Supabase.Client? _client;
    private RealtimeChannel? _channel;

    public event Action<Guid, int>? OnVoteUpdated;

    public async Task SubscribeAsync(string supabaseUrl, string anonKey)
    {
        try
        {
            _client = new Supabase.Client(
                supabaseUrl,
                anonKey,
                new SupabaseOptions
                {
                    AutoRefreshToken = false,
                    AutoConnectRealtime = true
                });

            await _client!.InitializeAsync();

            _channel = _client!.Realtime.Channel("vote-items-changes");

            _channel!.Register(new PostgresChangesOptions(
                schema: "MuuqWear",
                table: "vote_items"));

            _channel!.AddPostgresChangeHandler(
                PostgresChangesOptions.ListenType.Updates,
                HandleUpdate);

            await _channel!.Subscribe();

            Console.WriteLine(" Vote realtime subscribed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Realtime subscribe failed: {ex.Message}");
        }
    }

    //  add this new method to VoteRealtimeSubscription.cs
    private void HandleUpdate(
  IRealtimeChannel _,
  PostgresChangesResponse change)
    {
        try
        {
            //  VoteItemRealtimeModel inherits BaseModel
            var updated = change.Model<VoteItemRealtimeModel>();
            if (updated == null) return;

            var id = updated.Id;
            if (id == Guid.Empty) return;

            OnVoteUpdated?.Invoke(id, updated.VoteCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Realtime handle failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            //  Unsubscribe returns IRealtimeChannel not Task
            // don't await it
            _channel?.Unsubscribe();

            //  Client has no Dispose — just set null
            _client = null;
        }
        catch
        {
            // silent
        }

        await ValueTask.CompletedTask;
    }
}