namespace TheLsmArchive.ApiClient.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessWithData()
    {
        Result<string> result = Result<string>.Ok("hello");

        Result<string>.Success success = Assert.IsType<Result<string>.Success>(result);
        Assert.Equal("hello", success.Data);
    }

    [Fact]
    public void None_ReturnsNoContent()
    {
        Result<int> result = Result<int>.None();

        Assert.IsType<Result<int>.NoContent>(result);
    }

    [Fact]
    public void Fail_ReturnsFailureWithMessage()
    {
        Result<string> result = Result<string>.Fail("something broke");

        Result<string>.Failure failure = Assert.IsType<Result<string>.Failure>(result);
        Assert.Equal("something broke", failure.Message);
    }

    [Fact]
    public void Success_RecordEquality_WorksCorrectly()
    {
        Result<int> a = Result<int>.Ok(42);
        Result<int> b = Result<int>.Ok(42);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Success_RecordEquality_DifferentData_NotEqual()
    {
        Result<int> a = Result<int>.Ok(1);
        Result<int> b = Result<int>.Ok(2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void NoContent_RecordEquality_WorksCorrectly()
    {
        Result<string> a = Result<string>.None();
        Result<string> b = Result<string>.None();

        Assert.Equal(a, b);
    }

    [Fact]
    public void Failure_RecordEquality_WorksCorrectly()
    {
        Result<string> a = Result<string>.Fail("error");
        Result<string> b = Result<string>.Fail("error");

        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentResultTypes_AreNotEqual()
    {
        Result<string> ok = Result<string>.Ok("data");
        Result<string> none = Result<string>.None();
        Result<string> fail = Result<string>.Fail("error");

        Assert.NotEqual(ok, none);
        Assert.NotEqual(ok, fail);
        Assert.NotEqual(none, fail);
    }

    [Fact]
    public void PatternMatching_WorksForAllVariants()
    {
        Result<int> success = Result<int>.Ok(1);
        Result<int> none = Result<int>.None();
        Result<int> failure = Result<int>.Fail("err");

        Assert.Equal("1", Match(success));
        Assert.Equal("empty", Match(none));
        Assert.Equal("err", Match(failure));

        static string Match(Result<int> result) => result switch
        {
            Result<int>.Success s => s.Data.ToString(),
            Result<int>.NoContent => "empty",
            Result<int>.Failure f => f.Message,
            _ => throw new InvalidOperationException()
        };
    }
}
