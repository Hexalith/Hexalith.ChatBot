namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>The deployed artifact and evaluation identity, supplied only by the trusted runtime registry.</summary>
/// <param name="ArtifactVersion">Exact deployed classifier version.</param>
/// <param name="ArtifactHash">Exact deployed classifier digest.</param>
/// <param name="CandidateRevision">Exact candidate revision.</param>
/// <param name="DependencyRevisions">Every consumed dependency and revision.</param>
/// <param name="EnvironmentProfile">Approved runtime environment/profile.</param>
/// <param name="DatasetVersion">Required A9a dataset version.</param>
/// <param name="DatasetHash">Required A9a dataset digest.</param>
internal sealed record ChatBotClassifierRuntimeIdentity(
    string ArtifactVersion,
    string ArtifactHash,
    string CandidateRevision,
    IReadOnlyDictionary<string, string> DependencyRevisions,
    string EnvironmentProfile,
    string DatasetVersion,
    string DatasetHash);
