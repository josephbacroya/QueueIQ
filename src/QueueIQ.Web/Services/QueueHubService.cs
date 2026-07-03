using Microsoft.AspNetCore.SignalR.Client;
using QueueIQ.Shared.DTOs;

namespace QueueIQ.Web.Services;

/// <summary>
/// A Blazor-friendly wrapper around the SignalR HubConnection.
/// 
/// This manages the connection lifecycle and exposes C# events that Blazor 
/// components can subscribe to. This pattern keeps SignalR infrastructure 
/// code out of the UI components.
/// </summary>
public class QueueHubService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<QueueHubService> _logger;
    private string? _currentBusinessSlug;

    // Events that components can subscribe to
    public event Action<IEnumerable<TicketDto>>? OnQueueUpdated;
    public event Action<TicketDto>? OnTicketCalled;
    public event Action<QueuePositionDto>? OnPositionUpdated;
    public event Action<TicketDto>? OnTicketAdded;
    public event Action<TicketDto>? OnTicketUpdated;

    public QueueHubService(IConfiguration config, ILogger<QueueHubService> logger)
    {
        _logger = logger;
        
        // The API URL should come from config, fallback to local dev default
        var apiUrl = config["ApiBaseUrl"] ?? "http://localhost:5088";
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiUrl}/hubs/queue")
            .WithAutomaticReconnect()
            .Build();

        // Wire up the strongly-typed methods defined in IQueueHubClient
        _hubConnection.On<IEnumerable<TicketDto>>("QueueUpdated", queue =>
        {
            OnQueueUpdated?.Invoke(queue);
        });

        _hubConnection.On<TicketDto>("TicketCalled", ticket =>
        {
            OnTicketCalled?.Invoke(ticket);
        });

        _hubConnection.On<QueuePositionDto>("PositionUpdated", position =>
        {
            OnPositionUpdated?.Invoke(position);
        });

        _hubConnection.On<TicketDto>("TicketAdded", ticket =>
        {
            OnTicketAdded?.Invoke(ticket);
        });

        _hubConnection.On<TicketDto>("TicketUpdated", ticket =>
        {
            OnTicketUpdated?.Invoke(ticket);
        });
    }

    /// <summary>
    /// Starts the connection and joins the business's group.
    /// Called by components in OnInitializedAsync.
    /// </summary>
    public async Task StartAsync(string businessSlug)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connected.");
        }

        if (_currentBusinessSlug != businessSlug)
        {
            if (_currentBusinessSlug is not null)
            {
                await _hubConnection.InvokeAsync("LeaveBusinessQueue", _currentBusinessSlug);
            }
            
            await _hubConnection.InvokeAsync("JoinBusinessQueue", businessSlug);
            _currentBusinessSlug = businessSlug;
            _logger.LogInformation("Joined queue group for {Slug}", businessSlug);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            if (_currentBusinessSlug is not null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("LeaveBusinessQueue", _currentBusinessSlug);
            }
            await _hubConnection.DisposeAsync();
        }
    }
}
