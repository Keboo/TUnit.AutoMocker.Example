using TUnit.Mocks;

namespace TUnit.AutoMocker;

/// <summary>
/// Builder for creating auto-mocked instances with specific dependency overrides.
/// </summary>
/// <typeparam name="T">The system-under-test type.</typeparam>
public sealed class AutoMockerBuilder<T> where T : class
{
    private readonly Dictionary<Type, object> _overrides = new();
    private MockBehavior _behavior = MockBehavior.Loose;

    internal AutoMockerBuilder() { }

    /// <summary>
    /// Sets the mock behavior for all generated mocks.
    /// </summary>
    public AutoMockerBuilder<T> WithBehavior(MockBehavior behavior)
    {
        _behavior = behavior;
        return this;
    }

    /// <summary>
    /// Overrides the auto-mocked dependency with a specific instance.
    /// </summary>
    /// <typeparam name="TDep">The dependency type to override.</typeparam>
    /// <param name="instance">The instance to use instead of an auto-generated mock.</param>
    public AutoMockerBuilder<T> Use<TDep>(TDep instance) where TDep : class
    {
        _overrides[typeof(TDep)] = instance;
        return this;
    }

    /// <summary>
    /// Overrides the auto-mocked dependency with a specific mock.
    /// The mock's Object will be used as the dependency, and the mock will be accessible via GetMock.
    /// </summary>
    /// <typeparam name="TDep">The dependency type to override.</typeparam>
    /// <param name="mock">The mock to use.</param>
    public AutoMockerBuilder<T> Use<TDep>(Mock<TDep> mock) where TDep : class
    {
        _overrides[typeof(TDep)] = mock;
        return this;
    }

    /// <summary>
    /// Creates the auto-mocked instance with all configured overrides applied.
    /// </summary>
    public AutoMocked<T> Create()
    {
        return AutoMocker.CreateWithOverrides<T>(_behavior, _overrides);
    }
}
