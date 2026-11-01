using System.Security.Claims;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>Supplies an explicit principal at the hub method boundary without admitting an HTTP connection.</summary>
/// <param name="principal">The principal evaluated by the domain guard.</param>
internal sealed class ProjectionHubCallerContext(ClaimsPrincipal principal) : HubCallerContext
{
    /// <inheritdoc />
    public override string ConnectionId => "direct-hub-invocation";
    /// <inheritdoc />
    public override string? UserIdentifier => null;
    /// <inheritdoc />
    public override ClaimsPrincipal User => principal;
    /// <inheritdoc />
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    /// <inheritdoc />
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    /// <inheritdoc />
    public override CancellationToken ConnectionAborted => TestContext.Current.CancellationToken;
    /// <inheritdoc />
    public override void Abort() => throw new NotSupportedException();
}
