namespace TUnit.AutoMocker;

/// <summary>
/// Instructs the TUnit.AutoMocker source generator to analyze the specified type's constructor
/// and generate factory code that creates an instance with all dependencies auto-mocked.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AutoMockAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance with the type to auto-mock.
    /// </summary>
    /// <param name="type">The system-under-test type whose constructor dependencies should be auto-mocked.</param>
    public AutoMockAttribute(Type type) { }

    /// <summary>
    /// Optional: specify explicit constructor parameter types to select a specific constructor.
    /// When omitted, the greediest (most parameters) public constructor is used.
    /// </summary>
    public Type[]? ConstructorTypes { get; set; }
}
