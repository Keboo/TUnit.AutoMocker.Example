using System.Collections.Concurrent;
using System.ComponentModel;
using TUnit.Mocks;

namespace TUnit.AutoMocker;

/// <summary>
/// Static entry point for creating auto-mocked instances.
/// The source generator registers factories via <see cref="RegisterFactory{T}"/> at module initialization time.
/// </summary>
public static class AutoMocker
{
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, object>> _factories = new();
    private static readonly ConcurrentDictionary<Type, Func<MockBehavior, Dictionary<Type, object>, object>> _builderFactories = new();

    /// <summary>
    /// Registers a factory for creating auto-mocked instances of type T. Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterFactory<T>(Func<MockBehavior, AutoMocked<T>> factory) where T : class
    {
        _factories[typeof(T)] = behavior => factory(behavior);
    }

    /// <summary>
    /// Registers a builder factory for creating auto-mocked instances with overrides. Called by generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void RegisterBuilderFactory<T>(Func<MockBehavior, Dictionary<Type, object>, AutoMocked<T>> factory) where T : class
    {
        _builderFactories[typeof(T)] = (behavior, overrides) => factory(behavior, overrides);
    }

    /// <summary>
    /// Creates an auto-mocked instance of <typeparamref name="T"/> with all constructor dependencies mocked.
    /// </summary>
    /// <typeparam name="T">The system-under-test type. Must have a corresponding [assembly: AutoMock(typeof(T))] attribute.</typeparam>
    /// <param name="behavior">The mock behavior to use for generated mocks. Defaults to <see cref="MockBehavior.Loose"/>.</param>
    /// <returns>An <see cref="AutoMocked{T}"/> containing the instance and its dependency mocks.</returns>
    public static AutoMocked<T> Create<T>(MockBehavior behavior = MockBehavior.Loose) where T : class
    {
        if (_factories.TryGetValue(typeof(T), out var factory))
        {
            return (AutoMocked<T>)factory(behavior);
        }

        throw new InvalidOperationException(
            $"No auto-mock factory registered for type '{typeof(T).FullName}'. " +
            $"Add [assembly: AutoMock(typeof({typeof(T).Name}))] to your test project.");
    }

    /// <summary>
    /// Creates a builder for an auto-mocked instance, allowing specific dependencies to be overridden.
    /// </summary>
    /// <typeparam name="T">The system-under-test type.</typeparam>
    /// <returns>A builder that can be used to configure overrides before creating the instance.</returns>
    public static AutoMockerBuilder<T> Build<T>() where T : class
    {
        return new AutoMockerBuilder<T>();
    }

    internal static AutoMocked<T> CreateWithOverrides<T>(MockBehavior behavior, Dictionary<Type, object> overrides) where T : class
    {
        if (_builderFactories.TryGetValue(typeof(T), out var factory))
        {
            return (AutoMocked<T>)factory(behavior, overrides);
        }

        throw new InvalidOperationException(
            $"No auto-mock builder factory registered for type '{typeof(T).FullName}'. " +
            $"Add [assembly: AutoMock(typeof({typeof(T).Name}))] to your test project.");
    }
}
