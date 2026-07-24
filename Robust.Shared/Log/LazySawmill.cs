namespace Robust.Shared.Log;

/// <summary>
/// A lazily instantiated wrapper around ISawmill.
/// </summary>
/// <seealso cref="ISawmill"/>
/// <seealso cref="ILogManager"/>
public sealed class LazySawmill(string name)
{
    /// <summary>
    /// The deferred sawmill instance.
    /// </summary>
    private ISawmill? _sawmill;

    /// <summary>
    /// The name to use when getting the sawmill.
    /// </summary>
    private readonly string Name = name;

    public ISawmill Sawmill
    {
        get
        {
            _sawmill ??= Logger.GetSawmill(Name);
            return _sawmill;
        }
    }
}
