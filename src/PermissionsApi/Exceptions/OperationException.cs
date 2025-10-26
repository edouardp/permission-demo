namespace PermissionsApi.Exceptions;

public class OperationException : Exception
{
    public OperationException(string message, Exception innerException) : base(message, innerException) { }
}
