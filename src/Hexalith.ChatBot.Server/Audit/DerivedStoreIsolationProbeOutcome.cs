using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a derived-store cross-tenant isolation sweep: ordered pairs probed, breached, and alerted.</summary>
/// <param name="PartitionsProbed">Ordered <c>(owner, intruder)</c> pairs actually probed.</param>
/// <param name="Breaches">Pairs where the intruder observed the owner's sentinel.</param>
/// <param name="Alerted">Breaches for which an operator alert was successfully delivered.</param>
/// <param name="TenantsEnumerated">
/// How many known production tenants were available to probe. The set is the union of tenants already represented in
/// the derived store and tenants independently observed in the WORM audit store, so an empty or misbound derived store
/// cannot erase the population and turn missing coverage into a vacuous pass.
/// </param>
internal sealed record DerivedStoreIsolationProbeOutcome(int PartitionsProbed, int Breaches, int Alerted, int TenantsEnumerated = 0);
