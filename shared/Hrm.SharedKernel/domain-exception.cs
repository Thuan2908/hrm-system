namespace Hrm.SharedKernel;

public sealed class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Domain error code is required.", nameof(code));
        }

        Code = code;
    }

    public string Code { get; }
}
