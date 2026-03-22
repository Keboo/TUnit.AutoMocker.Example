using TUnit.Mocks;

namespace TUnit.AutoMocker;

/// <summary>
/// Wraps a system-under-test instance along with the mocks created for its constructor dependencies.
/// </summary>
/// <typeparam name="T">The system-under-test type.</typeparam>
public sealed class AutoMocked<T> where T : class
{
    private readonly Dictionary<Type, IMock> _mocks;

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public AutoMocked(T instance, Dictionary<Type, IMock> mocks)
    {
        Instance = instance;
        _mocks = mocks;
    }

    /// <summary>
    /// The created instance of <typeparamref name="T"/> with all dependencies injected.
    /// </summary>
    public T Instance { get; }

    /// <summary>
    /// Retrieves the <see cref="Mock{TDep}"/> that was injected for the specified dependency type.
    /// </summary>
    /// <typeparam name="TDep">The dependency type (must be an interface or abstract class).</typeparam>
    /// <returns>The mock wrapper for the dependency.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no mock was registered for the specified type.</exception>
    public Mock<TDep> GetMock<TDep>() where TDep : class
    {
        if (_mocks.TryGetValue(typeof(TDep), out var mock))
        {
            return (Mock<TDep>)mock;
        }

        throw new InvalidOperationException(
            $"No mock registered for type '{typeof(TDep).FullName}'. " +
            $"Ensure '{typeof(TDep).Name}' is a constructor parameter of '{typeof(T).Name}'.");
    }

    /// <summary>
    /// Checks whether a mock was registered for the specified dependency type.
    /// </summary>
    public bool HasMock<TDep>() where TDep : class
        => _mocks.ContainsKey(typeof(TDep));

    /// <summary>
    /// Gets all mock wrappers that were created for the constructor dependencies.
    /// </summary>
    public IReadOnlyCollection<IMock> AllMocks => _mocks.Values;
}
