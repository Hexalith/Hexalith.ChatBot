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
}
