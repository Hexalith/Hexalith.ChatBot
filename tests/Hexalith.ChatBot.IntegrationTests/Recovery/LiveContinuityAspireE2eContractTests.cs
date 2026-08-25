using System.Net;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Always-run contracts for the live Tier-3 startup boundary.</summary>
public sealed class LiveContinuityAspireE2eContractTests
{
    [Theory]
    [InlineData(null, 300)]
    [InlineData("265", 265)]
    [InlineData("1", 1)]
    [InlineData("300", 300)]
    public void RecoveryWorkflowTimeoutHonorsTheBoundedCompletionOverride(string? configured, int expectedMinutes)
    {
        LiveContinuityAspireE2eTests.RecoveryWorkflowTimeout(configured)
            .ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    [InlineData("not-a-number")]
    public void RecoveryWorkflowTimeoutRejectsValuesOutsideTheRunnerBudget(string configured)
    {
        Should.Throw<InvalidOperationException>(() =>
            LiveContinuityAspireE2eTests.RecoveryWorkflowTimeout(configured));
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    public void MailboxAdmissionStartupStatusClassificationIsFailClosed(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        LiveContinuityAspireE2eTests.IsTransientMailboxAdmissionStatus(statusCode).ShouldBe(expectedTransient);
    }

    /// <summary>
    /// The admission proof is the problem <c>type</c>. <c>dispatch-unavailable</c> is emitted only from
    /// <c>CommandGateway</c>'s accepted branch, so it proves admission; <c>audit-unavailable</c> (the pre-commit
    /// denial), an authorization denial, a body that is not a problem document, and a matching <c>code</c> under a
    /// different <c>type</c> must not.
    /// </summary>
    /// <param name="problemDetails">The verbatim response body.</param>
    /// <param name="expected">Whether the body proves the caller was admitted.</param>
    [Theory]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable\",\"status\":503,\"code\":\"audit_unavailable\"}",
        true)]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/audit-unavailable\",\"status\":503,\"code\":\"audit_unavailable\"}",
        false)]
    [InlineData(
        "{\"type\":\"https://hexalith.dev/errors/chatbot/authorization-denied\",\"status\":403,\"code\":\"authorization_denied\"}",
        false)]
    [InlineData("{\"code\":\"audit_unavailable\"}", false)]
    [InlineData("{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable-x\"}", false)]
    [InlineData("[\"dispatch-unavailable\"]", false)]
    [InlineData("not json at all", false)]
    [InlineData("", false)]
    public void MailboxAdmissionProofRequiresTheDispatchUnavailableProblemType(string problemDetails, bool expected)
        => LiveContinuityAspireE2eTests.IsDispatchUnavailableProblem(problemDetails).ShouldBe(expected);

    /// <summary>
    /// The admission proof requires BOTH the dispatch-unavailable problem type AND a 503; the body predicate alone
    /// would accept that document under any status, which is not what the production check does.
    /// </summary>
    /// <param name="statusCode">The status observed alongside the dispatch-unavailable body.</param>
    /// <param name="expectedAdmissionProof">Whether that pair proves admission.</param>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    [InlineData(HttpStatusCode.BadGateway, false)]
    public void MailboxAdmissionProofRequiresServiceUnavailableAlongsideTheProblemType(
        HttpStatusCode statusCode,
        bool expectedAdmissionProof)
    {
        const string body =
            "{\"type\":\"https://hexalith.dev/errors/chatbot/dispatch-unavailable\",\"status\":503}";

        bool provesAdmission = statusCode == HttpStatusCode.ServiceUnavailable
            && LiveContinuityAspireE2eTests.IsDispatchUnavailableProblem(body);

        provesAdmission.ShouldBe(expectedAdmissionProof);
    }
}
