namespace TgJobAdAnalytics.Utils;

/// <summary>
/// Console-based single-line status that renders using a carriage return and can suspend/resume cleanly.
/// </summary>
public sealed class ConsoleStatusLinePrinter
{
    /// <summary>
    /// Sets and renders the status line text on the console.
    /// </summary>
    /// <param name="text">Status text to render.</param>
    public void Set(string text)
    {
        lock (_lock)
        {
            _current = text ?? string.Empty;
            Render();
        }
    }


    /// <summary>
    /// Clears the status line from the console.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            Erase();
            _current = string.Empty;
            _lastWidth = 0;
        }
    }


    /// <summary>
    /// Suspends the status line while other output is written, then restores it upon dispose.
    /// </summary>
    /// <returns>Disposable handle restoring the status line.</returns>
    public IDisposable Suspend()
    {
        lock (_lock)
        {
            Erase();
            return new Resume(this);
        }
    }


    private void Render()
    {
        var text = _current ?? string.Empty;

        Console.Write('\r');
        Console.Write(text);

        var pad = Math.Max(0, _lastWidth - text.Length);
        if (pad > 0)
            Console.Write(new string(' ', pad));

        Console.Write('\r');
        _lastWidth = text.Length;
    }


    private void Erase()
    {
        Console.Write('\r');

        if (_lastWidth > 0)
        {
            Console.Write(new string(' ', _lastWidth));
            Console.Write('\r');
        }
    }


    private sealed class Resume : IDisposable
    {
        public Resume(ConsoleStatusLinePrinter owner) 
            => _owner = owner;


        public void Dispose()
        {
            lock (_owner._lock)
            {
                _owner.Render();
            }
        }


        private readonly ConsoleStatusLinePrinter _owner;
    }


    private string _current = string.Empty;
    private int _lastWidth;

    private readonly Lock _lock = new();
}