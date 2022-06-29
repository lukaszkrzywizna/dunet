using Dunet;

using static Result<System.Exception, string>;

var output1 = Try(() => "hello " + "world")
    .Match(success => success.Value.ToString(), failure => failure.Error.Message);

var output2 = Try(() => throw new InvalidOperationException())
    .Match(success => success.Value, failure => failure.Error.Message);

var asyncWork = async () =>
{
    await Task.Delay(250);
    return "done!";
};

var result3 = await TryAsync(asyncWork);
var output3 = result3.Match(success => success.Value.ToString(), failure => failure.Error.Message);

var asyncBoom = async Task<string> () =>
{
    await Task.Delay(250);
    throw new TaskCanceledException();
};

var result4 = await TryAsync(asyncBoom);
var output4 = result4.Match(success => success.Value.ToString(), failure => failure.Error.Message);

Console.WriteLine(output1);
Console.WriteLine(output2);
Console.WriteLine(output3);
Console.WriteLine(output4);

[Union]
public partial record Result<TFailure, TSuccess> where TFailure : Exception
{
    partial record Success(TSuccess Value);

    partial record Failure(TFailure Error);

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
