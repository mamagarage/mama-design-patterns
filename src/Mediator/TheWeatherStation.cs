using System.Text.Json;

// ------------------------------------------------------------
// Pub/Sub infrastructure
// ------------------------------------------------------------

public interface IPubSubComponent
{
    string Name { get; }

    void SetMediator(PubSubMediator mediator);
}

public sealed class PubSubMediator
{
    private readonly Dictionary<string, IPubSubComponent> _components = new();
    private readonly Dictionary<string, List<Action<object>>> _subscriptions = new();

    public PubSubMediator Register(IPubSubComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        _components[component.Name] = component;
        component.SetMediator(this);

        return this;
    }

    public void Subscribe(string topic, Action<object> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_subscriptions.TryGetValue(topic, out var handlers))
        {
            handlers = new List<Action<object>>();
            _subscriptions[topic] = handlers;
        }

        handlers.Add(handler);
    }

    public void Publish(string topic, object data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (!_subscriptions.TryGetValue(topic, out var handlers))
        {
            return;
        }

        // Create a copy so subscribers can safely modify subscriptions
        // while a message is being published.
        foreach (var handler in handlers.ToArray())
        {
            try
            {
                handler(data);
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"Error while publishing topic '{topic}': {exception.Message}");
            }
        }
    }
}

public abstract class PubSubComponent : IPubSubComponent
{
    private PubSubMediator? _mediator;

    protected PubSubComponent(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public string Name { get; }

    public void SetMediator(PubSubMediator mediator)
    {
        _mediator = mediator
            ?? throw new ArgumentNullException(nameof(mediator));
    }

    protected void PublishTo<T>(string topic, T data)
        where T : notnull
    {
        EnsureMediator();
        _mediator!.Publish(topic, data);
    }

    protected void SubscribeTo<T>(string topic, Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        EnsureMediator();

        _mediator!.Subscribe(topic, message =>
        {
            if (message is T typedMessage)
            {
                handler(typedMessage);
            }
            else
            {
                Console.WriteLine(
                    $"Invalid message type for topic '{topic}'. " +
                    $"Expected {typeof(T).Name}, received {message.GetType().Name}.");
            }
        });
    }

    private void EnsureMediator()
    {
        if (_mediator is null)
        {
            throw new InvalidOperationException(
                $"Component '{Name}' is not registered with a mediator.");
        }
    }
}

// ------------------------------------------------------------
// Message models
// ------------------------------------------------------------

public sealed record WeatherData(
    int Temperature,
    int Humidity,
    int Pressure);

public sealed record DataUpdatedMessage(
    string Source,
    WeatherData Values);

public sealed record AlertMessage(
    int Value,
    int Threshold,
    string Message,
    string? Source = null);

public sealed record DashboardAlert(
    int Value,
    int Threshold,
    string Message,
    string? Source,
    DateTime Timestamp);

public sealed record LogEntry(
    string Type,
    string? Source,
    WeatherData? Values,
    int? Value,
    int? Threshold,
    string? Message,
    DateTime Timestamp);

public sealed record LogFilter(
    string? Type = null,
    string? Source = null);

public sealed record DashboardState(
    IReadOnlyDictionary<string, WeatherData> Data,
    IReadOnlyList<DashboardAlert> Alerts);

// ------------------------------------------------------------
// Data source
// ------------------------------------------------------------

public sealed class DataSource : PubSubComponent, IDisposable
{
    private readonly TimeSpan _updateInterval;
    private readonly object _syncRoot = new();

    private Timer? _timer;
    private WeatherData? _data;
    private bool _disposed;

    public DataSource(
        string name,
        TimeSpan? updateInterval = null)
        : base(name)
    {
        _updateInterval = updateInterval ?? TimeSpan.FromSeconds(5);
    }

    public DataSource Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_syncRoot)
        {
            if (_timer is not null)
            {
                return this;
            }

            Console.WriteLine($"{Name} started collecting data");

            _timer = new Timer(
                callback: _ => CollectAndPublishData(),
                state: null,
                dueTime: TimeSpan.Zero,
                period: _updateInterval);
        }

        return this;
    }

    public DataSource Stop()
    {
        lock (_syncRoot)
        {
            if (_timer is null)
            {
                return this;
            }

            _timer.Dispose();
            _timer = null;

            Console.WriteLine($"{Name} stopped collecting data");
        }

        return this;
    }

    private void CollectAndPublishData()
    {
        try
        {
            _data = new WeatherData(
                Temperature: Random.Shared.Next(10, 41),
                Humidity: Random.Shared.Next(30, 91),
                Pressure: Random.Shared.Next(980, 1001));

            PublishTo(
                "data.updated",
                new DataUpdatedMessage(
                    Source: Name,
                    Values: _data));

            if (_data.Temperature > 35)
            {
                PublishTo(
                    "alert.temperature",
                    new AlertMessage(
                        Value: _data.Temperature,
                        Threshold: 35,
                        Message: "Temperature too high!",
                        Source: Name));
            }

            if (_data.Humidity > 80)
            {
                PublishTo(
                    "alert.humidity",
                    new AlertMessage(
                        Value: _data.Humidity,
                        Threshold: 80,
                        Message: "Humidity too high!",
                        Source: Name));
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"{Name} failed to collect data: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}

// ------------------------------------------------------------
// Dashboard
// ------------------------------------------------------------

public sealed class Dashboard : PubSubComponent
{
    private readonly object _syncRoot = new();

    private readonly Dictionary<string, WeatherData> _latestData = new();
    private readonly List<DashboardAlert> _alerts = new();

    public Dashboard(string name)
        : base(name)
    {
    }

    public Dashboard SubscribeToDataUpdated()
    {
        SubscribeTo<DataUpdatedMessage>(
            "data.updated",
            UpdateDisplay);

        return this;
    }

    public Dashboard SubscribeToTemperatureAlerts()
    {
        SubscribeTo<AlertMessage>(
            "alert.temperature",
            DisplayAlert);

        return this;
    }

    public Dashboard SubscribeToHumidityAlerts()
    {
        SubscribeTo<AlertMessage>(
            "alert.humidity",
            DisplayAlert);

        return this;
    }

    private void UpdateDisplay(DataUpdatedMessage data)
    {
        lock (_syncRoot)
        {
            _latestData[data.Source] = data.Values;
        }

        Console.WriteLine(
            $"{Name} updated with data from {data.Source}: " +
            $"temperature={data.Values.Temperature}°C, " +
            $"humidity={data.Values.Humidity}%, " +
            $"pressure={data.Values.Pressure} hPa");
    }

    private void DisplayAlert(AlertMessage alert)
    {
        lock (_syncRoot)
        {
            _alerts.Add(
                new DashboardAlert(
                    Value: alert.Value,
                    Threshold: alert.Threshold,
                    Message: alert.Message,
                    Source: alert.Source,
                    Timestamp: DateTime.Now));
        }

        Console.WriteLine($"{Name} ALERT: {alert.Message}");
    }

    public DashboardState GetState()
    {
        lock (_syncRoot)
        {
            return new DashboardState(
                Data: new Dictionary<string, WeatherData>(_latestData),
                Alerts: _alerts.ToList());
        }
    }
}

// ------------------------------------------------------------
// Logger
// ------------------------------------------------------------

public sealed class Logger : PubSubComponent
{
    private readonly object _syncRoot = new();
    private readonly List<LogEntry> _logs = new();

    public Logger(string name)
        : base(name)
    {
    }

    public Logger SubscribeToDataUpdated()
    {
        SubscribeTo<DataUpdatedMessage>(
            "data.updated",
            LogData);

        return this;
    }

    public Logger SubscribeToTemperatureAlerts()
    {
        SubscribeTo<AlertMessage>(
            "alert.temperature",
            LogAlert);

        return this;
    }

    public Logger SubscribeToHumidityAlerts()
    {
        SubscribeTo<AlertMessage>(
            "alert.humidity",
            LogAlert);

        return this;
    }

    private void LogData(DataUpdatedMessage data)
    {
        lock (_syncRoot)
        {
            _logs.Add(
                new LogEntry(
                    Type: "data",
                    Source: data.Source,
                    Values: data.Values,
                    Value: null,
                    Threshold: null,
                    Message: null,
                    Timestamp: DateTime.Now));
        }

        Console.WriteLine(
            $"{Name} logged data update from {data.Source}");
    }

    private void LogAlert(AlertMessage alert)
    {
        lock (_syncRoot)
        {
            _logs.Add(
                new LogEntry(
                    Type: "alert",
                    Source: alert.Source,
                    Values: null,
                    Value: alert.Value,
                    Threshold: alert.Threshold,
                    Message: alert.Message,
                    Timestamp: DateTime.Now));
        }

        Console.WriteLine(
            $"{Name} logged alert: {alert.Message}");
    }

    public IReadOnlyList<LogEntry> GetLogs(LogFilter? filter = null)
    {
        filter ??= new LogFilter();

        lock (_syncRoot)
        {
            IEnumerable<LogEntry> filteredLogs = _logs;

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                filteredLogs = filteredLogs.Where(log =>
                    string.Equals(
                        log.Type,
                        filter.Type,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Source))
            {
                filteredLogs = filteredLogs.Where(log =>
                    string.Equals(
                        log.Source,
                        filter.Source,
                        StringComparison.OrdinalIgnoreCase));
            }

            return filteredLogs.ToList();
        }
    }
}

// ------------------------------------------------------------
// Application
// ------------------------------------------------------------

public static partial class Program
{
    public static async Task Main()
    {
        var mediator = new PubSubMediator();

        using var weatherStation = new DataSource(
            "weatherStation",
            TimeSpan.FromSeconds(2));

        var mainDashboard = new Dashboard("mainDashboard");
        var systemLogger = new Logger("systemLogger");

        // Register components with the mediator.
        mediator
            .Register(weatherStation)
            .Register(mainDashboard)
            .Register(systemLogger);

        // Set up dashboard subscriptions.
        mainDashboard
            .SubscribeToDataUpdated()
            .SubscribeToTemperatureAlerts()
            .SubscribeToHumidityAlerts();

        // Set up logger subscriptions.
        systemLogger
            .SubscribeToDataUpdated()
            .SubscribeToTemperatureAlerts()
            .SubscribeToHumidityAlerts();

        // Start the data source.
        weatherStation.Start();

        // Equivalent of JavaScript setTimeout(..., 10000).
        await Task.Delay(TimeSpan.FromSeconds(10));

        weatherStation.Stop();

        Console.WriteLine();
        Console.WriteLine("Dashboard State:");

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        Console.WriteLine(
            JsonSerializer.Serialize(
                mainDashboard.GetState(),
                serializerOptions));

        Console.WriteLine();
        Console.WriteLine("Logger Alerts:");

        var alertLogs = systemLogger.GetLogs(
            new LogFilter(Type: "alert"));

        foreach (var log in alertLogs)
        {
            Console.WriteLine(
                $"- {log.Timestamp.ToLongTimeString()}: {log.Message}");
        }
    }
}