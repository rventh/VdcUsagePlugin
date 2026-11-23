using System;

namespace MyPlugin.Service.Vspc;

/// <summary>An error talking to VSPC, with a stable code the UI can show a clear message for.</summary>
public sealed class VspcApiException : Exception
{
    public int Status { get; }
    public string Code { get; }

    public VspcApiException(int status, string code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Status = status;
        Code = code;
    }
}
