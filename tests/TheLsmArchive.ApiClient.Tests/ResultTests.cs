namespace TheLsmArchive.ApiClient.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessWithData()
    {
        var result = Result<string>.Ok("hello");

        Result<string>.Success success = Assert.IsType<Result<string>.Success>(result);
        Assert.Equal("hello", success.Data);
    }

    [Fact]
    public void None_ReturnsNoContent()
    {
        var result = Result<int>.None();

        Assert.IsType<Result<int>.NoContent>(result);
    }

    [Fact]
    public void Fail_ReturnsFailureWithMessage()
    {
        var result = Result<string>.Fail("something broke");

        Result<string>.Failure failure = Assert.IsType<Result<string>.Failure>(result);
        Assert.Equal("something broke", failure.Message);
    }

    [Fact]
    public void Success_RecordEquality_WorksCorrectly()
    {
        var a = Result<int>.Ok(42);
        var b = Result<int>.Ok(42);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Success_RecordEquality_DifferentData_NotEqual()
    {
        var a = Result<int>.Ok(1);
        var b = Result<int>.Ok(2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void NoContent_RecordEquality_WorksCorrectly()
    {
        var a = Result<string>.None();
        var b = Result<string>.None();

        Assert.Equal(a, b);
    }

    [Fact]
    public void Failure_RecordEquality_WorksCorrectly()
    {
        var a = Result<string>.Fail("error");
        var b = Result<string>.Fail("error");

        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentResultTypes_AreNotEqual()
    {
        var ok = Result<string>.Ok("data");
        var none = Result<string>.None();
        var fail = Result<string>.Fail("error");

        Assert.NotEqual(ok, none);
        Assert.NotEqual(ok, fail);
        Assert.NotEqual(none, fail);
    }

    [Fact]
    public void PatternMatching_WorksForAllVariants()
    {
        var success = Result<int>.Ok(1);
        var none = Result<int>.None();
        var failure = Result<int>.Fail("err");

        Assert.Equal("1", Match(success));
        Assert.Equal("empty", Match(none));
        Assert.Equal("err", Match(failure));

        static string Match(Result<int> result)
        {
            return result switch
            {
                Result<int>.Success s => s.Data.ToString(),
                Result<int>.NoContent => "empty",
                Result<int>.Failure f => f.Message,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
