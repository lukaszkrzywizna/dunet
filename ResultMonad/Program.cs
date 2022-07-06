using Dunet;

using Res = Result<System.Exception, string>;
using static Result<System.Exception, string>;

while (true)
{
    Res hello = "Hello";
    var world = static Res (Success s) => $"{s.Value} world";
    var emphasis = static Res (Success s) => $"{s.Value}!";

    var result = hello | world | emphasis | emphasis | emphasis;

    var output = result.Match(success => success.Value, failure => failure.Error.Message);

    Console.WriteLine(output);
    Console.ReadLine();
}

[Union]
public partial record Result<TFailure, TSuccess> where TFailure : Exception
{
    partial record Success(TSuccess Value);

    partial record Failure(TFailure Error);

    public static Result<TFailure, TSuccess> operator |(
        Result<TFailure, TSuccess> left,
        Func<Result<TFailure, TSuccess>.Success, Result<TFailure, TSuccess>> right
    )
    {
        return left.Match(success => right(success), failure => failure);
    }

    public static implicit operator Result<TFailure, TSuccess>(TSuccess Value) =>
        new Result<TFailure, TSuccess>.Success(Value);

    public static Result<Exception, TSuccess> Try(Func<TSuccess> func)
    {
        try
        {
            var value = func();
            return new Result<Exception, TSuccess>.Success(value);
        }
        catch (Exception ex)
        {
            return new Result<Exception, TSuccess>.Failure(ex);
        }
    }

    public static async Task<Result<Exception, TSuccess>> TryAsync(Func<Task<TSuccess>> func)
    {
        try
        {
            var result = await func();
            return new Result<Exception, TSuccess>.Success(result);
        }
        catch (Exception ex)
        {
            return new Result<Exception, TSuccess>.Failure(ex);
        }
    }
}

public static class ResultExtensions
{
    public static Result<Exception, TResult> SelectMany<TFirst, TSecond, TResult>(
        this Result<Exception, TFirst> first,
        Func<TFirst, Result<Exception, TSecond>> getSecond,
        Func<TFirst, TSecond, TResult> getResult
    )
    {
        return first.Match(
            firstSuccess =>
            {
                var secondResult = getSecond(firstSuccess.Value);

                return secondResult.Match<Result<Exception, TResult>>(
                    secondSuccess =>
                        new Result<Exception, TResult>.Success(
                            getResult(firstSuccess.Value, secondSuccess.Value)
                        ),
                    secondFailure => new Result<Exception, TResult>.Failure(secondFailure.Error)
                );
            },
            firstFailure => new Result<Exception, TResult>.Failure(firstFailure.Error)
        );
    }
}

public static class TaskExtensions
{
    public static async Task<Result<Exception, TResult>> SelectMany<TFirst, TSecond, TResult>(
        this Task<Result<Exception, TFirst>> first,
        Func<TFirst, Task<Result<Exception, TSecond>>> getSecond,
        Func<TFirst, TSecond, TResult> getResult
    )
    {
        var firstResult = await first;

        return await firstResult.Match<Task<Result<Exception, TResult>>>(
            async firstSuccess =>
            {
                var secondResult = await getSecond(firstSuccess.Value);

                return secondResult.Match<Result<Exception, TResult>>(
                    secondSuccess =>
                        new Result<Exception, TResult>.Success(
                            getResult(firstSuccess.Value, secondSuccess.Value)
                        ),
                    secondFailure => new Result<Exception, TResult>.Failure(secondFailure.Error)
                );
            },
            firstFailure =>
            {
                var res = new Result<Exception, TResult>.Failure(firstFailure.Error);
                return Task.FromResult<Result<Exception, TResult>>(res);
            }
        );
    }
}
