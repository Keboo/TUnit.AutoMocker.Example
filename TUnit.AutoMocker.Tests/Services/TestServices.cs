namespace TUnit.AutoMocker.Tests.Services;

public class Order
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public interface IOrderRepository
{
    bool Save(Order order);
    Order? GetById(int id);
}

public interface ILogger
{
    void Log(string message);
    string LastMessage { get; }
}

public interface INotificationService
{
    void SendNotification(string to, string message);
}

public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger _logger;

    public OrderService(IOrderRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public bool PlaceOrder(Order order)
    {
        _logger.Log($"Placing order for {order.ProductName}");
        return _repository.Save(order);
    }

    public Order? GetOrder(int id)
    {
        _logger.Log($"Getting order {id}");
        return _repository.GetById(id);
    }
}

public class NotifyingOrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;

    public NotifyingOrderService(IOrderRepository repository, ILogger logger, INotificationService notificationService)
    {
        _repository = repository;
        _logger = logger;
        _notificationService = notificationService;
    }

    public bool PlaceOrder(Order order, string notifyEmail)
    {
        _logger.Log($"Placing order for {order.ProductName}");
        var result = _repository.Save(order);
        if (result)
        {
            _notificationService.SendNotification(notifyEmail, $"Order {order.ProductName} placed successfully");
        }
        return result;
    }
}

public class SimpleService
{
    public int Add(int a, int b) => a + b;
}

public class MixedService
{
    private readonly ILogger _logger;
    private readonly int _retryCount;

    public MixedService(ILogger logger, int retryCount)
    {
        _logger = logger;
        _retryCount = retryCount;
    }

    public int RetryCount => _retryCount;

    public void DoWork()
    {
        _logger.Log($"Doing work with retry count {_retryCount}");
    }
}
