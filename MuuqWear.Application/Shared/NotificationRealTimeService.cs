using MuuqWear.Model.NotificationModel;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Ticket;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;

namespace MuuqWear.Application.Shared;

public class NotificationRealtimeService : IAsyncDisposable
{
    private Supabase.Client? _client;
    private readonly List<RealtimeChannel> _channels = new();
    private bool _initialized = false;
    private bool _isDisposed = false;
    private string _supabaseUrl = string.Empty;
    private string _anonKey = string.Empty;
    private readonly SemaphoreSlim _lock = new(1, 1);

    //  reconnect config
    private const int MaxRetries = 5;
    private const int RetryDelaySeconds = 5;
    private int _retryCount = 0;
    private System.Threading.Timer? _reconnectTimer;

    public event Action<NotificationModel>? OnNotification;

    // =============================================
    // INITIALIZE
    // =============================================
    public async Task InitializeAsync(string supabaseUrl, string anonKey)
    {
        await _lock.WaitAsync();
        try
        {
            if (_initialized || _isDisposed) return;

            //  store for reconnect use
            _supabaseUrl = supabaseUrl;
            _anonKey = anonKey;

            await ConnectAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    // =============================================
    // CONNECT — reusable for init + reconnect
    // =============================================
    private async Task ConnectAsync()
    {
        try
        {
            _client = new Supabase.Client(_supabaseUrl, _anonKey,
                new SupabaseOptions
                {
                    AutoRefreshToken = false,
                    AutoConnectRealtime = true
                });

            await _client.InitializeAsync();

            if (_client.Realtime == null)
            {
                Console.WriteLine(
                    "Realtime unavailable — scheduling reconnect");
                ScheduleReconnect();
                return;
            }

            //  clear old channels before resubscribing
            _channels.Clear();

            await Task.WhenAll(
                SubscribeTable("orders", HandleOrder),
                SubscribeTable("support_tickets", HandleTicket),
                SubscribeTable("order_returns", HandleReturn));

            //  reset retry count on success
            _retryCount = 0;
            _initialized = true;

            //  cancel any pending reconnect timer
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;

            Console.WriteLine(" Notification realtime connected");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Realtime connect failed: {ex.Message}");
            _initialized = false;
            ScheduleReconnect();
        }
    }

    // =============================================
    // SUBSCRIBE TABLE
    // =============================================
    private async Task SubscribeTable(
        string table,
        Action<PostgresChangesResponse> handler)
    {
        try
        {
            var channel = _client!.Realtime
                .Channel($"notifications-{table}");

            channel.Register(new PostgresChangesOptions(
                schema: "MuuqWear",
                table: table));

            channel.AddPostgresChangeHandler(
                PostgresChangesOptions.ListenType.Inserts,
                (_, change) =>
                {
                    try
                    {
                        handler(change);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"{table} handler error: {ex.Message}");
                    }
                });

            await channel.Subscribe();
            _channels.Add(channel);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Subscribe to {table} failed: {ex.Message}");

            //  one table failure → trigger full reconnect
            ScheduleReconnect();
        }
    }

    // =============================================
    // RECONNECT LOGIC
    // =============================================
    private void ScheduleReconnect()
    {
        if (_isDisposed) return;

        if (_retryCount >= MaxRetries)
        {
            Console.WriteLine(
                $"Max retries ({MaxRetries}) reached. " +
                "Notifications disabled until app restart.");
            return;
        }

        _retryCount++;

        //  exponential backoff
        // retry 1 → 5s, retry 2 → 10s, retry 3 → 20s etc
        var delaySeconds = RetryDelaySeconds
                           * (int)Math.Pow(2, _retryCount - 1);

        Console.WriteLine(
            $"Reconnecting in {delaySeconds}s " +
            $"(attempt {_retryCount}/{MaxRetries})...");

        _reconnectTimer?.Dispose();
        _reconnectTimer = new System.Threading.Timer(
            async _ =>
            {
                if (_isDisposed) return;

                await _lock.WaitAsync();
                try
                {
                    _initialized = false;

                    //  clean up old channels
                    foreach (var ch in _channels)
                    {
                        try { ch.Unsubscribe(); }
                        catch { }
                    }
                    _channels.Clear();
                    _client = null;

                    await ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Reconnect attempt failed: {ex.Message}");
                    ScheduleReconnect();
                }
                finally
                {
                    _lock.Release();
                }
            },
            null,
            TimeSpan.FromSeconds(delaySeconds),
            Timeout.InfiniteTimeSpan); // ← fire once only 
    }

    // =============================================
    // HANDLERS
    // =============================================
    private void HandleOrder(PostgresChangesResponse change)
    {
        try
        {
            var model = change.Model<OrderInsertModel>();
            if (model == null) return;

            OnNotification?.Invoke(new NotificationModel
            {
                Type = NotificationType.Order,
                Message = model.OrderNumber != null
                    ? $"New order #{model.OrderNumber} placed"
                    : "New order placed",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleOrder error: {ex.Message}");
        }
    }

    private void HandleTicket(PostgresChangesResponse change)
    {
        try
        {
            var model = change.Model<TicketInsertModel>();
            if (model == null) return;

            OnNotification?.Invoke(new NotificationModel
            {
                Type = NotificationType.Ticket,
                Message = model.Subject != null
                    ? $"New ticket: {model.Subject}"
                    : "New support ticket submitted",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleTicket error: {ex.Message}");
        }
    }

    private void HandleReturn(PostgresChangesResponse change)
    {
        try
        {
            var model = change.Model<OrderReturnInsertedModel>();
            if (model == null) return;

            OnNotification?.Invoke(new NotificationModel
            {
                Type = NotificationType.Return,
                Message = model.ReturnNumber != null
                    ? $"Return request #{model.ReturnNumber} submitted"
                    : "New return request submitted",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleReturn error: {ex.Message}");
        }
    }

    // =============================================
    // DISPOSE
    // =============================================
    public async ValueTask DisposeAsync()
    {
        _isDisposed = true;

        _reconnectTimer?.Dispose();
        _reconnectTimer = null;

        foreach (var channel in _channels)
        {
            try { channel.Unsubscribe(); }
            catch { }
        }

        _channels.Clear();
        _client = null;
        _initialized = false;

        await ValueTask.CompletedTask;
    }
}