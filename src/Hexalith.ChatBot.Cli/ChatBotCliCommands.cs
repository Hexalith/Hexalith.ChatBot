using System.CommandLine;

using Hexalith.ChatBot.Client;

namespace Hexalith.ChatBot.Cli;

using ApprovalDecision = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;

public static class ChatBotCliCommands
{
    public static Task<int> InvokeAsync(
        string[] args,
        IChatBotClient client,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RootCommand root = CreateRootCommand(client, output, error);
        return root.Parse(args).InvokeAsync();
    }

    public static RootCommand CreateRootCommand(
        IChatBotClient client,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var service = new ChatBotCliService(client, output, error);
        var root = new RootCommand("Hexalith ChatBot governed workflow CLI");

        root.Subcommands.Add(BuildAssociationCommand(service));
        root.Subcommands.Add(BuildConversationCommand(service));
        root.Subcommands.Add(BuildTaskCommand(service));
        root.Subcommands.Add(BuildOperationCommand(service));
        root.Subcommands.Add(BuildApprovalCommand(service));
        root.Subcommands.Add(BuildAiActionCommand(service));

        return root;
    }

    private static Command BuildAssociationCommand(ChatBotCliService service)
    {
        var association = new Command("association", "Inspect and decide email association workflow items.");

        Command status = Leaf("status", "Read association routing status.", out CliOptionSet statusOptions);
        Option<string> associationId = StringOption("--association-id", "Association workflow identifier.");
        status.Options.Add(associationId);
        status.SetAction((parse, cancellationToken) => service.ShowAssociationStatusAsync(
            Required(parse.GetValue(associationId), "--association-id"),
            Options(parse, statusOptions),
            cancellationToken));
        association.Subcommands.Add(status);

        Command associate = Leaf("associate", "Associate an email workflow item to a project.", out CliOptionSet associateOptions);
        Option<string> associateId = StringOption("--association-id", "Association workflow identifier.");
        Option<string> associateIntake = StringOption("--intake-id", "Mailbox intake identifier.");
        Option<string> associateProject = StringOption("--project-id", "Target project identifier.");
        Option<string> associateFingerprint = StringOption("--evidence-fingerprint", "Candidate evidence fingerprint.");
        Option<long> associateVersion = LongOption("--source-version", "Expected association source version.");
        Option<string> associateSchema = StringOption("--schema-version", "Command schema version.", "chatbot.association-decision.v1");
        Option<string> associateNote = StringOption("--note", "Metadata-only decision note.");
        Add(associate, associateId, associateIntake, associateProject, associateFingerprint, associateVersion, associateSchema, associateNote);
        associate.SetAction((parse, cancellationToken) => service.AssociateAsync(
            Required(parse.GetValue(associateId), "--association-id"),
            Required(parse.GetValue(associateIntake), "--intake-id"),
            Required(parse.GetValue(associateProject), "--project-id"),
            Required(parse.GetValue(associateFingerprint), "--evidence-fingerprint"),
            parse.GetValue(associateVersion),
            Required(parse.GetValue(associateSchema), "--schema-version"),
            parse.GetValue(associateNote),
            Options(parse, associateOptions),
            cancellationToken));
        association.Subcommands.Add(associate);

        Command reject = Leaf("reject", "Reject all current association candidates.", out CliOptionSet rejectOptions);
        Option<string> rejectId = StringOption("--association-id", "Association workflow identifier.");
        Option<string> rejectIntake = StringOption("--intake-id", "Mailbox intake identifier.");
        Option<string> rejectFingerprint = StringOption("--evidence-fingerprint", "Candidate evidence fingerprint.");
        Option<long> rejectVersion = LongOption("--source-version", "Expected association source version.");
        Option<string> rejectSchema = StringOption("--schema-version", "Command schema version.", "chatbot.association-decision.v1");
        Option<string> rejectNote = StringOption("--note", "Metadata-only decision note.");
        Add(reject, rejectId, rejectIntake, rejectFingerprint, rejectVersion, rejectSchema, rejectNote);
        reject.SetAction((parse, cancellationToken) => service.RejectAssociationAsync(
            Required(parse.GetValue(rejectId), "--association-id"),
            Required(parse.GetValue(rejectIntake), "--intake-id"),
            Required(parse.GetValue(rejectFingerprint), "--evidence-fingerprint"),
            parse.GetValue(rejectVersion),
            Required(parse.GetValue(rejectSchema), "--schema-version"),
            parse.GetValue(rejectNote),
            Options(parse, rejectOptions),
            cancellationToken));
        association.Subcommands.Add(reject);

        Command defer = Leaf("defer", "Defer the association workflow item.", out CliOptionSet deferOptions);
        Option<string> deferId = StringOption("--association-id", "Association workflow identifier.");
        Option<string> deferIntake = StringOption("--intake-id", "Mailbox intake identifier.");
        Option<string> deferFingerprint = StringOption("--evidence-fingerprint", "Candidate evidence fingerprint.");
        Option<long> deferVersion = LongOption("--source-version", "Expected association source version.");
        Option<string> deferSchema = StringOption("--schema-version", "Command schema version.", "chatbot.association-decision.v1");
        Option<string> deferNote = StringOption("--note", "Metadata-only decision note.");
        Add(defer, deferId, deferIntake, deferFingerprint, deferVersion, deferSchema, deferNote);
        defer.SetAction((parse, cancellationToken) => service.DeferAssociationAsync(
            Required(parse.GetValue(deferId), "--association-id"),
            Required(parse.GetValue(deferIntake), "--intake-id"),
            Required(parse.GetValue(deferFingerprint), "--evidence-fingerprint"),
            parse.GetValue(deferVersion),
            Required(parse.GetValue(deferSchema), "--schema-version"),
            parse.GetValue(deferNote),
            Options(parse, deferOptions),
            cancellationToken));
        association.Subcommands.Add(defer);

        Command correct = Leaf("correct", "Correct a prior association decision.", out CliOptionSet correctOptions);
        Option<string> correctId = StringOption("--association-id", "Association workflow identifier.");
        Option<string> correctIntake = StringOption("--intake-id", "Mailbox intake identifier.");
        Option<string> priorProject = StringOption("--prior-project-id", "Prior project identifier.");
        Option<string> targetProject = StringOption("--target-project-id", "Target project identifier.");
        Option<string> predecessor = StringOption("--predecessor-association-id", "Predecessor association identifier.");
        Option<string> correctFingerprint = StringOption("--evidence-fingerprint", "Candidate evidence fingerprint.");
        Option<long> correctVersion = LongOption("--source-version", "Expected association source version.");
        Option<string> correctSchema = StringOption("--schema-version", "Command schema version.", "chatbot.association-correction.v1");
        Option<string> rationale = StringOption("--rationale", "Metadata-only correction rationale.");
        Add(correct, correctId, correctIntake, priorProject, targetProject, predecessor, correctFingerprint, correctVersion, correctSchema, rationale);
        correct.SetAction((parse, cancellationToken) => service.CorrectAssociationAsync(
            Required(parse.GetValue(correctId), "--association-id"),
            Required(parse.GetValue(correctIntake), "--intake-id"),
            Required(parse.GetValue(priorProject), "--prior-project-id"),
            Required(parse.GetValue(targetProject), "--target-project-id"),
            Required(parse.GetValue(predecessor), "--predecessor-association-id"),
            Required(parse.GetValue(correctFingerprint), "--evidence-fingerprint"),
            parse.GetValue(correctVersion),
            Required(parse.GetValue(correctSchema), "--schema-version"),
            parse.GetValue(rationale),
            Options(parse, correctOptions),
            cancellationToken));
        association.Subcommands.Add(correct);

        return association;
    }

    private static Command BuildConversationCommand(ChatBotCliService service)
    {
        Command conversation = Leaf("conversation", "Read project conversation workflow state.", out CliOptionSet options);
        Option<string> projectId = StringOption("--project-id", "Project identifier.");
        Option<string> cursor = StringOption("--cursor", "Opaque page cursor.");
        Option<int> pageSize = new("--page-size") { Description = "Maximum item count.", DefaultValueFactory = _ => 25 };
        Add(conversation, projectId, cursor, pageSize);
        conversation.SetAction((parse, cancellationToken) => service.ShowConversationAsync(
            Required(parse.GetValue(projectId), "--project-id"),
            parse.GetValue(cursor),
            parse.GetValue(pageSize),
            Options(parse, options),
            cancellationToken));
        return conversation;
    }

    private static Command BuildTaskCommand(ChatBotCliService service)
    {
        var task = new Command("task", "Inspect task-intent workflow state.");
        Command review = Leaf("review", "Read task intent review.", out CliOptionSet options);
        Option<string> projectId = StringOption("--project-id", "Project identifier.");
        Option<string> taskIntentId = StringOption("--task-intent-id", "Task intent identifier.");
        Add(review, projectId, taskIntentId);
        review.SetAction((parse, cancellationToken) => service.ShowTaskReviewAsync(
            Required(parse.GetValue(projectId), "--project-id"),
            Required(parse.GetValue(taskIntentId), "--task-intent-id"),
            Options(parse, options),
            cancellationToken));
        task.Subcommands.Add(review);
        return task;
    }

    private static Command BuildOperationCommand(ChatBotCliService service)
    {
        var operation = new Command("operation", "Inspect and retry governed operations.");

        Command status = Leaf("status", "Read operation status.", out CliOptionSet statusOptions);
        Option<string> statusOperationId = StringOption("--operation-id", "Operation identifier.");
        status.Options.Add(statusOperationId);
        status.SetAction((parse, cancellationToken) => service.ShowOperationStatusAsync(
            Required(parse.GetValue(statusOperationId), "--operation-id"),
            Options(parse, statusOptions),
            cancellationToken));
        operation.Subcommands.Add(status);

        Command audit = Leaf("audit", "Read operation audit history.", out CliOptionSet auditOptions);
        Option<string> auditOperationId = StringOption("--operation-id", "Operation identifier.");
        audit.Options.Add(auditOperationId);
        audit.SetAction((parse, cancellationToken) => service.ShowOperationAuditAsync(
            Required(parse.GetValue(auditOperationId), "--operation-id"),
            Options(parse, auditOptions),
            cancellationToken));
        operation.Subcommands.Add(audit);

        Command retry = Leaf("retry", "Request retry for failed workflow work.", out CliOptionSet retryOptions);
        Option<string> retryId = StringOption("--retry-id", "Retry request identifier.");
        Option<string> failedEventId = StringOption("--failed-event-id", "Failed event or operation identifier.");
        Option<string> failedClass = StringOption("--failed-operation-class", "Metadata-only failed operation class.");
        Option<string> failureReason = StringOption("--failure-reason-code", "Failure reason code.");
        Option<long> failedVersion = LongOption("--expected-failed-source-version", "Expected failed source version.");
        Option<string> rationale = StringOption("--rationale", "Metadata-only retry rationale.");
        Add(retry, retryId, failedEventId, failedClass, failureReason, failedVersion, rationale);
        retry.SetAction((parse, cancellationToken) => service.RetryOperationAsync(
            Required(parse.GetValue(retryId), "--retry-id"),
            Required(parse.GetValue(failedEventId), "--failed-event-id"),
            Required(parse.GetValue(failedClass), "--failed-operation-class"),
            Required(parse.GetValue(failureReason), "--failure-reason-code"),
            parse.GetValue(failedVersion),
            parse.GetValue(rationale),
            Options(parse, retryOptions),
            cancellationToken));
        operation.Subcommands.Add(retry);

        return operation;
    }

    private static Command BuildApprovalCommand(ChatBotCliService service)
    {
        var approval = new Command("approval", "Decide governed AI action approvals.");
        Command decide = Leaf("decide", "Record an approval decision.", out CliOptionSet options);
        Option<string> projectId = StringOption("--project-id", "Project identifier.");
        Option<string> approvalId = StringOption("--approval-id", "Approval identifier.");
        Option<string> proposalId = StringOption("--proposal-id", "Proposal identifier.");
        Option<string> sourceMessageId = StringOption("--source-message-id", "Source message identifier.");
        Option<string> decision = StringOption("--decision", "Decision: approve, reject, request-revision, or cancel.");
        Option<long> expectedVersion = LongOption("--expected-approval-source-version", "Expected approval source version.");
        Option<string> commandCorrelationId = StringOption("--command-correlation-id", "Command correlation identifier.");
        Option<string> decisionId = StringOption("--decision-id", "Decision identifier.");
        Add(decide, projectId, approvalId, proposalId, sourceMessageId, decision, expectedVersion, commandCorrelationId, decisionId);
        decide.SetAction((parse, cancellationToken) => service.DecideApprovalAsync(
            Required(parse.GetValue(projectId), "--project-id"),
            Required(parse.GetValue(approvalId), "--approval-id"),
            Required(parse.GetValue(proposalId), "--proposal-id"),
            Required(parse.GetValue(sourceMessageId), "--source-message-id"),
            ParseApprovalDecision(Required(parse.GetValue(decision), "--decision")),
            parse.GetValue(expectedVersion),
            Required(parse.GetValue(commandCorrelationId), "--command-correlation-id"),
            Required(parse.GetValue(decisionId), "--decision-id"),
            Options(parse, options),
            cancellationToken));
        approval.Subcommands.Add(decide);
        return approval;
    }

    private static Command BuildAiActionCommand(ChatBotCliService service)
    {
        var aiAction = new Command("ai-action", "Execute approved AI actions.");
        Command execute = Leaf("execute", "Execute an approved AI action.", out CliOptionSet options);
        Option<string> projectId = StringOption("--project-id", "Project identifier.");
        Option<string> proposalId = StringOption("--proposal-id", "Proposal identifier.");
        Option<string> approvalId = StringOption("--approval-id", "Approval identifier.");
        Option<string> taskIntentId = StringOption("--task-intent-id", "Task intent identifier.");
        Option<string> sourceMessageId = StringOption("--source-message-id", "Source message identifier.");
        Option<string> requesterId = StringOption("--requester-id", "Requester identifier.");
        Option<string> commandName = StringOption("--command-name", "Approved command name.");
        Option<string> allowlistVersion = StringOption("--command-allowlist-version", "Command allowlist version.");
        Option<long> expectedApprovalVersion = LongOption("--expected-approval-source-version", "Expected approval source version.");
        Option<long> expectedProposalVersion = LongOption("--expected-proposal-source-version", "Expected proposal source version.");
        Option<string> commandCorrelationId = StringOption("--command-correlation-id", "Command correlation identifier.");
        Option<string> executionId = StringOption("--execution-id", "Execution identifier.");
        Option<string> transitionId = StringOption("--transition-id", "Transition identifier.");
        Option<string[]> sourceEvidence = StringArrayOption("--source-evidence", "Source evidence reference.");
        Option<string[]> affectedResources = StringArrayOption("--affected-resource", "Affected resource reference.");
        Option<string[]> recipients = StringArrayOption("--recipient", "Recipient reference.");
        Add(
            execute,
            projectId,
            proposalId,
            approvalId,
            taskIntentId,
            sourceMessageId,
            requesterId,
            commandName,
            allowlistVersion,
            expectedApprovalVersion,
            expectedProposalVersion,
            commandCorrelationId,
            executionId,
            transitionId,
            sourceEvidence,
            affectedResources,
            recipients);
        execute.SetAction((parse, cancellationToken) => service.ExecuteAiActionAsync(
            Required(parse.GetValue(projectId), "--project-id"),
            Required(parse.GetValue(proposalId), "--proposal-id"),
            Required(parse.GetValue(approvalId), "--approval-id"),
            Required(parse.GetValue(taskIntentId), "--task-intent-id"),
            Required(parse.GetValue(sourceMessageId), "--source-message-id"),
            Required(parse.GetValue(requesterId), "--requester-id"),
            Required(parse.GetValue(commandName), "--command-name"),
            Required(parse.GetValue(allowlistVersion), "--command-allowlist-version"),
            parse.GetValue(expectedApprovalVersion),
            parse.GetValue(expectedProposalVersion),
            Required(parse.GetValue(commandCorrelationId), "--command-correlation-id"),
            Required(parse.GetValue(executionId), "--execution-id"),
            Required(parse.GetValue(transitionId), "--transition-id"),
            parse.GetValue(sourceEvidence) ?? [],
            parse.GetValue(affectedResources) ?? [],
            parse.GetValue(recipients) ?? [],
            Options(parse, options),
            cancellationToken));
        aiAction.Subcommands.Add(execute);
        return aiAction;
    }

    private static Command Leaf(string name, string description, out CliOptionSet options)
    {
        var command = new Command(name, description);
        options = new CliOptionSet(
            new Option<bool>("--json") { Description = "Write JSON output." },
            StringOption("--correlation-id", "Caller-supplied correlation identifier."),
            StringOption("--task-id", "Caller-supplied task identifier."),
            StringOption("--tenant", "Display/filter intent or configuration selector; never tenant authority."));
        command.Options.Add(options.Json);
        command.Options.Add(options.CorrelationId);
        command.Options.Add(options.TaskId);
        command.Options.Add(options.Tenant);
        return command;
    }

    private static ChatBotCliOptions Options(ParseResult parse, CliOptionSet options)
        => new(
            parse.GetValue(options.Json),
            parse.GetValue(options.CorrelationId),
            parse.GetValue(options.TaskId),
            parse.GetValue(options.Tenant));

    private static Option<string> StringOption(string name, string description, string? defaultValue = null)
        => defaultValue is null
            ? new Option<string>(name) { Description = description }
            : new Option<string>(name) { Description = description, DefaultValueFactory = _ => defaultValue };

    private static Option<long> LongOption(string name, string description)
        => new(name) { Description = description };

    private static Option<string[]> StringArrayOption(string name, string description)
        => new(name) { Description = description, AllowMultipleArgumentsPerToken = true, DefaultValueFactory = _ => [] };

    private static void Add(Command command, params Option[] options)
    {
        foreach (Option option in options)
        {
            command.Options.Add(option);
        }
    }

    private static string Required(string? value, string optionName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Missing required option {optionName}.", optionName)
            : value;

    private static ApprovalDecision ParseApprovalDecision(string value)
        => value switch
        {
            "approve" => ApprovalDecision.Approve,
            "reject" => ApprovalDecision.Reject,
            "request-revision" => ApprovalDecision.RequestRevision,
            "cancel" => ApprovalDecision.Cancel,
            _ => throw new ArgumentException("Approval decision must be approve, reject, request-revision, or cancel.", nameof(value)),
        };

    private sealed record CliOptionSet(
        Option<bool> Json,
        Option<string> CorrelationId,
        Option<string> TaskId,
        Option<string> Tenant);
}
