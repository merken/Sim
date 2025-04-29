namespace Sim.Api.Validators;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public interface IValidator<in T>
{
    Task Validate(T model, CancellationToken cancellationToken = default);
}
