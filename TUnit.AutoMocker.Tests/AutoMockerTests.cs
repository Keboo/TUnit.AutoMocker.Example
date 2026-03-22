using TUnit.AutoMocker;
using TUnit.AutoMocker.Tests.Services;

[assembly: AutoMock(typeof(OrderService))]
[assembly: AutoMock(typeof(NotifyingOrderService))]
[assembly: AutoMock(typeof(SimpleService))]
[assembly: AutoMock(typeof(MixedService))]

// TUnit.Mocks requires GenerateMock for each interface to create source-generated mocks
// Each [GenerateMock] must be in a separate file due to TUnit.Mocks source generator limitation
[assembly: TUnit.Mocks.GenerateMock(typeof(IOrderRepository))]

namespace TUnit.AutoMocker.Tests;

public class AutoMockerCreateTests
{
    [Test]
    public async Task Create_ReturnsNonNullAutoMocked()
    {
        var sut = AutoMocker.Create<OrderService>();

        await Assert.That(sut).IsNotNull();
        await Assert.That(sut.Instance).IsNotNull();
    }

    [Test]
    public async Task Create_InstanceIsUsable()
    {
        var sut = AutoMocker.Create<OrderService>();

        // Unconfigured mock returns default (false for bool)
        var result = sut.Instance.PlaceOrder(new Order { ProductName = "Widget" });

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Create_GetMock_ReturnsMockForDependency()
    {
        var sut = AutoMocker.Create<OrderService>();

        var repoMock = sut.GetMock<IOrderRepository>();
        var loggerMock = sut.GetMock<ILogger>();

        await Assert.That(repoMock).IsNotNull();
        await Assert.That(loggerMock).IsNotNull();
    }

    [Test]
    public async Task Create_ConfiguredMock_AffectsInstance()
    {
        var sut = AutoMocker.Create<OrderService>();

        sut.GetMock<IOrderRepository>().Save(Arg.Any<Order>()).Returns(true);

        var result = sut.Instance.PlaceOrder(new Order { ProductName = "Widget" });

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Create_VerifyMockCalls()
    {
        var sut = AutoMocker.Create<OrderService>();
        sut.GetMock<IOrderRepository>().Save(Arg.Any<Order>()).Returns(true);

        sut.Instance.PlaceOrder(new Order { ProductName = "Gadget" });

        sut.GetMock<ILogger>().Log(Arg.Any<string>()).WasCalled(Times.Once);
        sut.GetMock<IOrderRepository>().Save(Arg.Any<Order>()).WasCalled(Times.Once);
    }

    [Test]
    public async Task Create_WithThreeDependencies()
    {
        var sut = AutoMocker.Create<NotifyingOrderService>();

        await Assert.That(sut.HasMock<IOrderRepository>()).IsTrue();
        await Assert.That(sut.HasMock<ILogger>()).IsTrue();
        await Assert.That(sut.HasMock<INotificationService>()).IsTrue();
    }

    [Test]
    public async Task Create_ThreeDependencies_AllWork()
    {
        var sut = AutoMocker.Create<NotifyingOrderService>();
        sut.GetMock<IOrderRepository>().Save(Arg.Any<Order>()).Returns(true);

        var result = sut.Instance.PlaceOrder(new Order { ProductName = "Item" }, "test@example.com");

        await Assert.That(result).IsTrue();
        sut.GetMock<INotificationService>()
            .SendNotification(Arg.Any<string>(), Arg.Any<string>())
            .WasCalled(Times.Once);
    }

    [Test]
    public async Task Create_NoConstructorDependencies()
    {
        var sut = AutoMocker.Create<SimpleService>();

        await Assert.That(sut.Instance).IsNotNull();
        await Assert.That(sut.Instance.Add(2, 3)).IsEqualTo(5);
    }

    [Test]
    public async Task Create_MixedDependencies_ValueTypeGetsDefault()
    {
        var sut = AutoMocker.Create<MixedService>();

        await Assert.That(sut.Instance).IsNotNull();
        await Assert.That(sut.Instance.RetryCount).IsEqualTo(0);
        await Assert.That(sut.HasMock<ILogger>()).IsTrue();
    }

    [Test]
    public async Task Create_StrictBehavior_ThrowsOnUnconfiguredCalls()
    {
        var sut = AutoMocker.Create<OrderService>(MockBehavior.Strict);

        Assert.Throws<Exception>(() =>
        {
            sut.Instance.PlaceOrder(new Order { ProductName = "Widget" });
        });
    }

    [Test]
    public async Task GetMock_ForNonDependency_ThrowsInvalidOperation()
    {
        var sut = AutoMocker.Create<OrderService>();

        Assert.Throws<InvalidOperationException>(() =>
        {
            sut.GetMock<INotificationService>();
        });
    }
}

public class AutoMockerBuilderTests
{
    [Test]
    public async Task Build_WithMockOverride()
    {
        var customRepo = Mock.Of<IOrderRepository>();
        customRepo.Save(Arg.Any<Order>()).Returns(true);

        var sut = AutoMocker.Build<OrderService>()
            .Use(customRepo)
            .Create();

        var result = sut.Instance.PlaceOrder(new Order { ProductName = "Custom" });

        await Assert.That(result).IsTrue();
        // The overridden mock should be accessible via GetMock
        await Assert.That(sut.HasMock<IOrderRepository>()).IsTrue();
    }

    [Test]
    public async Task Build_WithInstanceOverride()
    {
        var sut = AutoMocker.Build<OrderService>()
            .Use<IOrderRepository>(Mock.Of<IOrderRepository>().Object)
            .Create();

        await Assert.That(sut.Instance).IsNotNull();
        // Instance override is not tracked as a mock
        await Assert.That(sut.HasMock<IOrderRepository>()).IsFalse();
    }

    [Test]
    public async Task Build_WithBehavior()
    {
        var sut = AutoMocker.Build<OrderService>()
            .WithBehavior(MockBehavior.Strict)
            .Create();

        Assert.Throws<Exception>(() =>
        {
            sut.Instance.PlaceOrder(new Order { ProductName = "Widget" });
        });
    }

    [Test]
    public async Task Build_PartialOverride_OtherDepsStillMocked()
    {
        var customRepo = Mock.Of<IOrderRepository>();
        customRepo.Save(Arg.Any<Order>()).Returns(true);

        var sut = AutoMocker.Build<OrderService>()
            .Use(customRepo)
            .Create();

        // Logger should still be auto-mocked
        await Assert.That(sut.HasMock<ILogger>()).IsTrue();
        // Repository was overridden
        await Assert.That(sut.HasMock<IOrderRepository>()).IsTrue();

        var result = sut.Instance.PlaceOrder(new Order { ProductName = "Test" });
        await Assert.That(result).IsTrue();
    }
}
