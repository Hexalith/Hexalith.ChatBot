using Grpc.Core;

using Hexalith.ChatBot.Server.Lifecycle.Workflows;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class DaprWorkflowDuplicateInstanceDetectorTests
{
    [Fact]
    public void RpcAlreadyExistsIsDuplicate()
        => DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(
                new RpcException(new Status(StatusCode.AlreadyExists, "workflow already exists")))
            .ShouldBeTrue();

    [Fact]
    public void MessageBasedAlreadyExistsIsDuplicate()
        => DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(
                new InvalidOperationException("workflow instance already exists"))
            .ShouldBeTrue();

    [Fact]
    public void UnrelatedFailureIsNotDuplicate()
        => DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(
                new InvalidOperationException("connection refused"))
            .ShouldBeFalse();
}
