---
title: "Product Brief: Hexalith.ChatBot"
status: "complete"
created: "2026-05-10T19:08:22.7186431+02:00"
updated: "2026-05-10T19:12:59.5556418+02:00"
inputs:
  - "README.md"
  - "Hexalith.EventStore/README.md"
  - "Hexalith.EventStore/docs/concepts/architecture-overview.md"
  - "Hexalith.Parties/_bmad-output/planning-artifacts/product-brief-Hexalith.Parties-2026-03-01.md"
  - "Hexalith.Tenants/_bmad-output/planning-artifacts/product-brief-Hexalith.Tenants-2026-03-06.md"
  - "Hexalith.Folders/_bmad-output/planning-artifacts/product-brief-Hexalith.Folders.md"
  - "Hexalith.Memories/README.md"
  - "Hexalith.FrontComposer/src/Hexalith.FrontComposer.Cli/README.md"
---

# Product Brief: Hexalith.ChatBot

## Executive Summary

Hexalith.ChatBot is an enterprise AI collaboration application for project-centered work. It gives business teams, developers, and project managers a shared conversational workspace where internal and external participants can coordinate around a project, exchange files, trigger AI-assisted tasks, and keep project execution moving across channels.

The first high-value use case is communication between internal and external users through email, supporting both Microsoft 365/Exchange-style enterprise mailboxes and generic email integration. Instead of losing project decisions, files, and action items inside mailbox threads, Hexalith.ChatBot turns those conversations into project context: messages become conversations, attachments become managed project files, and requests can become trackable AI or human tasks. The same capabilities are exposed through the chatbot UI, mailbox integration, CLI console application, and MCP server so humans, automation, and AI agents can operate through one consistent command surface.

The product builds on the Hexalith platform rather than reinventing collaboration infrastructure. Conversations are managed by `Hexalith.Conversations`, projects by `Hexalith.Projects`, folders and files by `Hexalith.Folders`, tenants by `Hexalith.Tenants`, parties by `Hexalith.Parties`, and service communication flows through `Hexalith.EventStore`. Keycloak provides identity and security, Aspire composes local and service runtime topology, and `Hexalith.FrontComposer` provides the UI foundation.

## The Problem

Project collaboration is fragmented across chat, email, file shares, task lists, and individual AI assistants. Business teams still coordinate with external stakeholders through email because it is universal, but email is a weak project system: context is scattered, attachments are duplicated, actions are implicit, and project state is hard to audit. Developers and project managers then spend time reconstructing decisions, finding the latest document, and translating conversation into executable work.

AI assistants make this fragmentation more visible. They can draft, summarize, and automate, but they often lack a durable project boundary: which files are authoritative, who is allowed to participate, which task was requested, what changed, and whether the action was triggered by a schedule, a file arrival, a mailbox message, or a user instruction. Without a governed project workspace, AI stays useful but unreliable for enterprise execution.

## The Solution

Hexalith.ChatBot provides a project collaboration layer where conversations, files, participants, and AI-executed tasks are managed as one workspace.

Users can:

- Collaborate with internal and external participants around a project or subject.
- Continue project conversations through the chatbot UI or mailbox, with future channels such as Teams, WhatsApp, and other messengers.
- Add files to project folders and ask the AI to act on them.
- Trigger automated tasks by schedule, by adding a file, or by a request from a conversation actor.
- Use the same commands through the UI, CLI console app, and MCP server.
- Require human approval before an AI action changes project folder content or sends information to an external recipient.

The user-facing promise is simple: "I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context."

Every automated action should remain attributable: who or what requested it, which project and files it used, which command was sent, what result came back, and what follow-up is required. That traceability is what makes AI useful for enterprise project work rather than just convenient for individual productivity.

## What Makes This Different

Hexalith.ChatBot is not another generic chatbot or disconnected AI assistant. Its differentiation is the combination of conversational collaboration, project structure, file management, and governed automation.

- **Project-first AI context**: conversations, files, folders, tasks, participants, and triggers belong to a project boundary instead of floating across tools.
- **Email as a first-class collaboration channel**: the MVP starts where external collaboration already happens, then turns mailbox activity into durable project context and actionable work.
- **One command model across surfaces**: chatbot UI, CLI, and MCP expose the same operations, reducing drift between human workflows, scripts, and AI agents.
- **Built on Hexalith services**: tenants, parties, conversations, projects, folders, and EventStore already define the core bounded contexts needed for enterprise-grade collaboration.
- **Automation with auditability**: scheduled, file-triggered, and conversation-triggered work can be represented as commands, events, projections, and task outcomes instead of hidden chatbot side effects.
- **Governed external collaboration**: internal and external users can participate in the same project context while tenant, party, identity, and authorization boundaries remain explicit.
- **Human approval for higher-risk actions**: the AI can prepare changes and outbound responses, but project file mutations and external information sharing require explicit approval.

## Who This Serves

**Business teams** need to collaborate with customers, partners, suppliers, and internal specialists without forcing everyone into the same tool. Success means email discussions, documents, and action items become organized project work instead of scattered correspondence.

**Project managers** need visibility into what was requested, what the AI did, which files changed, and which tasks are complete. Success means less coordination overhead and fewer manual follow-ups.

**Developers and automation builders** need a reliable command surface for integrating project collaboration into scripts, services, and AI agents. Success means the CLI and MCP server can perform the same operations as the UI without custom one-off integrations.

**Platform operators and administrators** need tenant isolation, identity, access control, service composition, and operational evidence. Success means external collaboration and AI automation can be governed with the same rigor as other enterprise services.

## Success Criteria

Hexalith.ChatBot is working when teams complete project work faster because communication, files, and AI actions stay connected.

Primary success measures:

- **Task completion rate**: percentage of project tasks completed successfully after being created from conversation, file, or schedule triggers.
- **Reduced project coordination time**: measurable reduction in time spent finding context, following up, and translating email threads into work.
- **Document management automation**: percentage of incoming files classified, stored, linked to the right project folder, and made available to AI workflows without manual handling.
- **Cross-surface command parity**: core chatbot UI operations are also available through CLI and MCP with consistent authorization, validation, and outcomes.
- **External collaboration reliability**: mailbox-driven conversations correctly preserve participants, attachments, project association, and task requests.
- **Mailbox-to-project accuracy**: incoming messages and attachments are associated with the intended project, with clear fallback when automatic matching is uncertain.

## MVP Scope

The first version should focus on proving the project collaboration loop:

- Chatbot UI for project conversations and task requests.
- Mailbox integration for internal and external project communication.
- Support for both enterprise mailbox integration and generic email integration.
- Project participation model for internal and external users, backed by parties linked to email addresses and tenant-aware security.
- File ingestion into project folders from user upload and mailbox attachments.
- AI task requests from conversation actors.
- Automated task triggers from scheduled time and file addition.
- Human approval gates before changing project folder content or sending information to an external recipient.
- CLI console app exposing the same core project, conversation, file, and task commands as the UI.
- MCP server exposing the same core capabilities for AI agents.
- EventStore-backed command/query flow for services, with audit-friendly events and projections.
- Aspire-composed local development/runtime topology and Keycloak-backed authentication.

Explicitly out of scope for the first version:

- Teams, WhatsApp, and additional messenger channels beyond mailbox.
- Advanced workflow designer or visual automation builder.
- Broad enterprise project management replacement features such as portfolio planning, budgeting, or resource management.
- Deep document co-authoring UI.
- Full repair/operations console beyond essential status and traceability.

The MVP should prove a narrow operational path before expanding: create or select a project, invite or identify participants as parties linked to email addresses, receive project email, store attachments in the project folder, create or request an AI task from the conversation, request approval when the task will modify folder content or send external information, execute the task through EventStore-backed commands, and show the outcome consistently in the UI, CLI, and MCP surfaces.

## Technical Approach

Hexalith.ChatBot should remain an orchestrating application over dedicated Hexalith services, not a monolith that owns every domain concern. Project state belongs in `Hexalith.Projects`; conversations in `Hexalith.Conversations`; files and folders in `Hexalith.Folders`; people and organizations in `Hexalith.Parties`; tenant facts and authorization context in `Hexalith.Tenants`; and commands, queries, events, projections, and service communication in `Hexalith.EventStore`.

This architecture keeps the ChatBot product focused on user experience and orchestration: channel adapters, project context assembly, AI task intent capture, command routing, and cross-surface consistency. It also creates a clear path for future channels and automation modes without changing the core collaboration model.

Two design risks deserve early validation. First, mailbox messages need a reliable project association strategy so external email does not pollute the wrong workspace. Second, external participant access must be enforced before task execution or file access, not only at the UI layer.

## Vision

If successful, Hexalith.ChatBot becomes the AI-native project collaboration layer for the Hexalith ecosystem. It starts by making email-based collaboration manageable and actionable, then expands into a multi-channel workspace where people and AI agents collaborate around durable project context.

Over the next two to three years, the product can grow into a governed agentic collaboration platform: project-aware AI workers, richer scheduled and event-driven automations, multi-channel conversations, task and document intelligence, audit-ready execution history, and reusable MCP tools that let external AI assistants participate safely in enterprise project work.
