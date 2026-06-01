namespace OtpAuth.Application.Common;

/// <summary>
/// Servis katmanından dönen, exception fırlatmadan başarı/başarısızlık taşıyan basit sonuç tipi.
/// </summary>
public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static Result Success() => new() { Succeeded = true };
    public static Result Fail(string error) => new() { Succeeded = false, Error = error };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static new Result<T> Fail(string error) => new() { Succeeded = false, Error = error };
}
