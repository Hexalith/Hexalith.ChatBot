using Grpc.Core;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Detects Dapr Workflow duplicate-instance scheduling conflicts for deterministic ids.</summary>
internal static class DaprWorkflowDuplicateInstanceDetector
{
    public static bool IsDuplicateInstance(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is RpcException { StatusCode: StatusCode.AlreadyExists })
        {
            return true;
        }

        if (exception is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Any(IsDuplicateInstance);
        }

        if (exception.InnerException is not null && IsDuplicateInstance(exception.InnerException))
        {
            return true;
        }

        string message = exception.Message;
        if (!Contains(message, "workflow"))
        {
            return false;
        }

        if (Contains(message, "already exists")
            || Contains(message, "already started")
            || Contains(message, "already running")
            || Contains(message, "duplicate"))
        {
            return true;
        }

        return Contains(message, "instance")
            && (Contains(message, "409") || Contains(message, "conflict"));
    }

    private static bool Contains(string value, string fragment)
        => value.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
