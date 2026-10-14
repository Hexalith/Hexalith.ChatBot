using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.UI.E2E.Tests;

// This test renderer inspects the rendered component tree and invokes the actual Fluent control callback.
#pragma warning disable BL0006
/// <summary>Mounts real UI components with event and after-render lifecycle support.</summary>
internal sealed class SafeOutcomeInteractiveRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    : Renderer(services, loggerFactory)
{
    private readonly Dictionary<IComponent, int> _componentIds = [];

    /// <inheritdoc/>
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>Mounts a root component on the renderer dispatcher.</summary>
    /// <typeparam name="T">The real component to mount.</typeparam>
    /// <param name="parameters">The component parameters.</param>
    /// <returns>The mounted component instance.</returns>
    internal Task<T> MountAsync<T>(Dictionary<string, object?>? parameters = null) where T : IComponent
        => Dispatcher.InvokeAsync(async () =>
        {
            T component = (T)InstantiateComponent(typeof(T));
            int id = AssignRootComponentId(component);
            _componentIds[component] = id;
            await RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters ?? [])).ConfigureAwait(true);
            return component;
        });

    /// <summary>Updates root parameters and completes the actual after-render lifecycle.</summary>
    /// <param name="component">The mounted root.</param>
    /// <param name="parameters">The next root parameters.</param>
    internal Task UpdateAsync(IComponent component, Dictionary<string, object?> parameters)
        => Dispatcher.InvokeAsync(() => RenderRootComponentAsync(_componentIds[component], ParameterView.FromDictionary(parameters)));

    /// <summary>Finds descendants through the actual rendered component tree.</summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="parent">The mounted parent.</param>
    /// <returns>All matching descendants.</returns>
    internal IEnumerable<T> Find<T>(IComponent parent) where T : IComponent
    {
        var frames = GetCurrentRenderTreeFrames(_componentIds[parent]);
        for (int i = 0; i < frames.Count; i++)
        {
            RenderTreeFrame frame = frames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Component) continue;
            _componentIds[frame.Component] = frame.ComponentId;
            if (frame.Component is T component) yield return component;
            foreach (T child in Find<T>(frame.Component)) yield return child;
        }
    }

    /// <summary>Reads a rendered attribute across a component's markup and descendants.</summary>
    /// <param name="component">The mounted component.</param>
    /// <param name="name">The attribute name.</param>
    /// <returns>The first rendered attribute value.</returns>
    internal string? Attribute(IComponent component, string name)
    {
        var frames = GetCurrentRenderTreeFrames(_componentIds[component]);
        for (int i = 0; i < frames.Count; i++)
        {
            RenderTreeFrame frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == name)
                return frame.AttributeValue?.ToString();
            if (frame.FrameType != RenderTreeFrameType.Component) continue;
            _componentIds[frame.Component] = frame.ComponentId;
            string? value = Attribute(frame.Component, name);
            if (value is not null) return value;
        }
        return null;
    }

    /// <summary>Reads text emitted by one component and its descendants.</summary>
    /// <param name="component">The mounted component.</param>
    /// <returns>The rendered text.</returns>
    internal string Text(IComponent component)
    {
        var frames = GetCurrentRenderTreeFrames(_componentIds[component]);
        System.Text.StringBuilder text = new();
        for (int i = 0; i < frames.Count; i++)
        {
            RenderTreeFrame frame = frames.Array[i];
            if (frame.FrameType == RenderTreeFrameType.Text) text.Append(frame.TextContent);
            if (frame.FrameType != RenderTreeFrameType.Component) continue;
            _componentIds[frame.Component] = frame.ComponentId;
            text.Append(Text(frame.Component));
        }
        return text.ToString();
    }

    /// <inheritdoc/>
    protected override void HandleException(Exception exception) => throw exception;

    /// <inheritdoc/>
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
}
#pragma warning restore BL0006
