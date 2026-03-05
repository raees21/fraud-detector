namespace FraudEngine.Domain.Common;

/// <summary>
/// Represents an error that occurred during the execution of an operation.
/// </summary>
public class Error
{
    /// <summary>
    /// Represents no error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents an error where a null value was provided or encountered.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the descriptive error message.
    /// </summary>
    public string Message { get; }
}

/// <summary>
/// Represents the result of an operation, indicating success or failure.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error that occurred, if any.</param>
    /// <exception cref="InvalidOperationException">Thrown if the state is invalid.</exception>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException();
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException();

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with a failure result.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success()
    {
        return new Result(true, Error.None);
    }

    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failure <see cref="Result"/>.</returns>
    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }
}

/// <summary>
/// Represents the result of an operation that returns a value on success.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the result.</param>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error that occurred, if any.</param>
    protected Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value of a successful result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value of the successful result.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, Error.None);
    }

    /// <summary>
    /// Creates a failure result with the specified error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failure <see cref="Result{T}"/>.</returns>
    public new static Result<T> Failure(Error error)
    {
        return new Result<T>(default, false, error);
    }
}
