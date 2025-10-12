using System;
using System.IO;
using System.Text;

namespace TgJobAdAnalytics.Utils;

/// <summary>
/// TextWriter that clears the status line before writing and restores it after, so logs never interleave with the status.
/// </summary>
public sealed class StatusAwareTextWriter : TextWriter
{
    public StatusAwareTextWriter(TextWriter inner, ConsoleStatusLinePrinter statusPrinter)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _statusPrinter = statusPrinter ?? throw new ArgumentNullException(nameof(statusPrinter));
    }


    public override Encoding Encoding 
        => _inner.Encoding;


    public override void Write(char value)
    {
        using (_statusPrinter.Suspend())
            _inner.Write(value);
    }


    public override void Write(string? value)
    {
        using (_statusPrinter.Suspend())
            _inner.Write(value);
    }


    public override void WriteLine(string? value)
    {
        using (_statusPrinter.Suspend())
            _inner.WriteLine(value);
    }


    public override void Flush()
    {
        using (_statusPrinter.Suspend())
            _inner.Flush();
    }


    private readonly TextWriter _inner;
    private readonly ConsoleStatusLinePrinter _statusPrinter;
}