using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// AC1 — the nine-actor negative matrix exists and is non-vacuous. The matrix must declare exactly the nine
/// required personas, each with an explicitly declared surface origin, each must produce at least one EXECUTABLE
/// fail-closed negative case against a foreign tenant, and every required leakage channel must be represented.
/// The suite fails if any persona or guarded channel has zero cases (the non-vacuity guard the prior stories
/// flagged as the single biggest risk).
/// </summary>
public sealed class CrossTenantActorMatrixTests
{
    [Fact]
    public void MatrixShouldDeclareExactlyTheNineRequiredPersonasEachWithADeclaredOrigin()
    {
        IsolationActorMatrix.Personas.Count.ShouldBe(9);
        IsolationActorMatrix.Personas.Select(static persona => persona.Label).ShouldBe(IsolationActorMatrix.RequiredPersonaLabels, ignoreOrder: false);
        IsolationActorMatrix.Personas.Select(static persona => persona.Label).Distinct(StringComparer.Ordinal).Count().ShouldBe(9);

        foreach (IsolationActorPersona persona in IsolationActorMatrix.Personas)
        {
            persona.Label.ShouldNotBeNullOrWhiteSpace();
            persona.AdapterPosture.ShouldNotBeNullOrWhiteSpace();
            // Every case declares its surface origin explicitly (the only cross-persona delta the gateway observes).
            persona.DeclaredOrigin.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task EveryPersonaShouldHaveAtLeastOneExecutableFailClosedNegativeCase()
    {
        Dictionary<string, int> casesByPersona = new(StringComparer.Ordinal);

        foreach (IsolationActorPersona persona in IsolationActorMatrix.Personas)
        {
            CrossTenantIsolationHarness.MutatingDenialOutcome outcome = await CrossTenantIsolationHarness
                .RunMutatingDenialAsync(persona, CrossTenantIsolationHarness.TenantContextVariant.ForeignTargetCommandBody, TestContext.Current.CancellationToken);

            // The negative case actually executed and the gateway denied it fail-closed.
            outcome.IsAccepted.ShouldBeFalse();
            outcome.AuthorizationFailureCount.ShouldBe(1);
            outcome.DispatchCount.ShouldBe(0);
            casesByPersona[persona.Label] = casesByPersona.GetValueOrDefault(persona.Label) + 1;
        }

        // The suite fails if ANY required persona has zero executable cases.
        foreach (string required in IsolationActorMatrix.RequiredPersonaLabels)
        {
            casesByPersona.TryGetValue(required, out int count).ShouldBeTrue($"persona '{required}' has no executable negative case");
            count.ShouldBeGreaterThanOrEqualTo(1);
        }

        casesByPersona.Count.ShouldBe(9);
    }

    [Fact]
    public void EveryRequiredLeakageChannelShouldBeRepresentedByAtLeastOneSentinel()
    {
        CrossTenantLeakageCorpus.RequiredChannels.ShouldNotBeEmpty();
        CrossTenantLeakageCorpus.Sentinels.ShouldNotBeEmpty();

        foreach (string channel in CrossTenantLeakageCorpus.RequiredChannels)
        {
            CrossTenantLeakageCorpus.Sentinels
                .Count(sentinel => string.Equals(sentinel.Channel, channel, StringComparison.Ordinal))
                .ShouldBeGreaterThanOrEqualTo(1);
        }

        // The candidate/evidence/file/cursor channels must exist NOW even though M0 has no such endpoints — so
        // future Epic 2/3 endpoints plug into this same gate rather than inventing a parallel test style (AC4).
        foreach (string futureChannel in new[] { "candidate", "evidence", "file", "cursor" })
        {
            CrossTenantLeakageCorpus.RequiredChannels.ShouldContain(futureChannel);
        }
    }
}
