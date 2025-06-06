namespace Common.Exceptions;

public class ValidationException : Exception
{
    public List<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = [message];
    }

    public ValidationException(List<string> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}