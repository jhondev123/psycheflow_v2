using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.Conflict("sample.conflict", "Conflito.");

    [Fact]
    public void ImplicitValue_CreatesSuccessWithValue()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void ImplicitError_CreatesFailure()
    {
        Result<int> result = SampleError;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Value_OnFailure_Throws()
    {
        Result<int> result = SampleError;

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void NonGenericResult_SuccessAndFailure()
    {
        Result.Success().IsSuccess.ShouldBeTrue();

        Result failure = SampleError;
        failure.IsFailure.ShouldBeTrue();
        failure.Error.ShouldBe(SampleError);
    }
}
