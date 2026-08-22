using System.Data;
using System.Text.Json;
using Dapper;
using HomeDiary_api.Data;
using HomeDiary_api.Models;
using HomeDiary_api.Security;

namespace HomeDiary_api.Repositories;

public class EmailTriageRepository(DbConnectionFactory db, ErrorLogRepository errorLog, ClientContext clientContext)
    : IEmailTriageRepository
{
    public async Task<EmailTriagePage> GetAsync(string? status, int limit, int offset)
    {
        try
        {
            using var conn = db.Create();
            const string where = "WHERE ei.client_id=@clientId AND (@status IS NULL OR ei.status = @status)";
            var parameters = new { status, limit, offset, clientId = clientContext.RequireClientId() };
            var items = (await conn.QueryAsync<EmailIntakeSummary>(
                $$"""
                SELECT ei.email_intake_id, ei.provider, ei.provider_message_id,
                       ei.envelope_recipient, ei.sender_email, ei.sender_name, ei.subject,
                       ei.received_at, ei.status, ei.triage_summary, ei.triage_reason,
                       ei.triage_confidence, ei.attempt_count, ei.reviewed_at,
                       ei.ai_status, ei.ai_processed_at,
                       COUNT(ets.email_task_suggestion_id)::int AS suggestion_count,
                       COUNT(ets.email_task_suggestion_id)
                           FILTER (WHERE ets.status = 'pending')::int AS pending_suggestion_count
                  FROM email_intake ei
                  LEFT JOIN email_task_suggestion ets
                    ON ets.email_intake_id = ei.email_intake_id
                {{where}}
                 GROUP BY ei.email_intake_id
                 ORDER BY ei.received_at DESC, ei.email_intake_id DESC
                 LIMIT @limit OFFSET @offset
                """, parameters)).AsList();

            var total = await conn.QuerySingleAsync<int>(
                $"SELECT COUNT(*)::int FROM email_intake ei {where}", parameters);

            return new EmailTriagePage
            {
                Items = items,
                Total = total,
                Limit = limit,
                Offset = offset
            };
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EmailTriageRepository));
            throw;
        }
    }

    public async Task<EmailIntakeDetail?> GetByIdAsync(long id)
    {
        try
        {
            using var conn = db.Create();
            var row = await conn.QuerySingleOrDefaultAsync<EmailIntakeDetailRow>(
                """
                SELECT ei.email_intake_id, ei.provider, ei.provider_message_id,
                       ei.storage_bucket, ei.storage_key, ei.envelope_recipient,
                       ei.sender_email, ei.sender_name, ei.reply_to_email, ei.subject,
                       ei.received_at, ei.status, ei.triage_summary, ei.triage_reason,
                       ei.triage_confidence, ei.authentication_result::text AS authentication_result_json,
                       ei.extracted_text, ei.extraction_result::text AS extraction_result_json,
                       ei.attempt_count, ei.next_attempt_at, ei.processing_started_at,
                       ei.processed_at, ei.last_error, ei.reviewed_by_id, ei.reviewed_at,
                       ei.created_at, ei.updated_at, ei.ai_status, ei.ai_processed_at,
                       ei.ai_last_error,
                       COUNT(ets.email_task_suggestion_id)::int AS suggestion_count,
                       COUNT(ets.email_task_suggestion_id)
                           FILTER (WHERE ets.status = 'pending')::int AS pending_suggestion_count
                  FROM email_intake ei
                  LEFT JOIN email_task_suggestion ets
                    ON ets.email_intake_id = ei.email_intake_id
                 WHERE ei.email_intake_id = @id AND ei.client_id=@clientId
                 GROUP BY ei.email_intake_id
                """, new { id, clientId = clientContext.RequireClientId() });

            if (row is null) return null;

            var suggestionRows = await conn.QueryAsync<EmailTaskSuggestionRow>(
                """
                SELECT email_task_suggestion_id, email_intake_id, suggestion_number,
                       title, description, target_completion_date, event_type_id, area_id,
                       priority_id, project_id, confidence,
                       missing_information::text AS missing_information_json,
                       extraction_data::text AS extraction_data_json,
                       status, review_notes, reviewed_by_id, reviewed_at, event_id,
                       created_at, updated_at, action_type, target_event_id,
                       source_ai_run_id, reason, evidence::text AS evidence_json,
                       proposed_changes::text AS proposed_changes_json
                  FROM email_task_suggestion
                 WHERE email_intake_id = @id AND client_id=@clientId
                 ORDER BY suggestion_number
                """, new { id, clientId = clientContext.RequireClientId() });

            var detail = row.ToModel();
            detail.Suggestions = suggestionRows.Select(item => item.ToModel()).ToList();
            var runRows = await conn.QueryAsync<EmailAiRunRow>(
                """
                SELECT email_ai_run_id, email_intake_id, run_role, provider, model,
                       prompt_version, status, response_json::text AS response_json_text,
                       input_tokens, output_tokens, duration_ms, error_message,
                       started_at, completed_at
                  FROM email_ai_run
                 WHERE email_intake_id = @id AND client_id=@clientId
                 ORDER BY started_at DESC, email_ai_run_id DESC
                """, new { id, clientId = clientContext.RequireClientId() });
            detail.AiRuns = runRows.Select(item => item.ToModel()).ToList();
            return detail;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EmailTriageRepository));
            throw;
        }
    }

    public async Task<bool> ReviewIntakeAsync(long id, EmailIntakeReviewRequest review, int reviewerId)
    {
        try
        {
            using var conn = db.Create();
            return await conn.ExecuteAsync(
                """
                UPDATE email_intake
                   SET status = @Status,
                       triage_summary = @TriageSummary,
                       triage_reason = @TriageReason,
                       reviewed_by_id = @reviewerId,
                       reviewed_at = now(),
                       updated_at = now()
                 WHERE email_intake_id = @id AND client_id=@clientId
                """, new
                {
                    id,
                    review.Status,
                    review.TriageSummary,
                    review.TriageReason,
                    reviewerId,
                    clientId = clientContext.RequireClientId()
                }) > 0;
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EmailTriageRepository));
            throw;
        }
    }

    public async Task<EmailSuggestionReviewResult?> ReviewSuggestionAsync(
        long intakeId,
        long suggestionId,
        EmailSuggestionReviewRequest review,
        int reviewerId)
    {
        try
        {
            using var conn = db.Create();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            var row = await conn.QuerySingleOrDefaultAsync<EmailTaskSuggestionRow>(
                """
                SELECT email_task_suggestion_id, email_intake_id, suggestion_number,
                       title, description, target_completion_date, event_type_id, area_id,
                       priority_id, project_id, confidence,
                       missing_information::text AS missing_information_json,
                       extraction_data::text AS extraction_data_json,
                       status, review_notes, reviewed_by_id, reviewed_at, event_id,
                       created_at, updated_at, action_type, target_event_id,
                       source_ai_run_id, reason, evidence::text AS evidence_json,
                       proposed_changes::text AS proposed_changes_json
                  FROM email_task_suggestion
                 WHERE email_task_suggestion_id = @suggestionId
                   AND email_intake_id = @intakeId
                   AND client_id = @clientId
                 FOR UPDATE
                """, new { intakeId, suggestionId, clientId = clientContext.RequireClientId() }, transaction);
            if (row is null) return null;

            var suggestion = row.ToModel();
            if (suggestion.Status is "created" or "applied")
            {
                if (review.Status != "approved")
                    throw new EmailSuggestionApplicationException(
                        "This suggestion has already been applied and cannot be changed.");

                await CompleteIntakeIfReviewedAsync(
                    conn, transaction, intakeId, reviewerId, clientContext.RequireClientId());
                transaction.Commit();
                return new EmailSuggestionReviewResult
                {
                    Status = suggestion.Status,
                    EventId = suggestion.EventId ?? suggestion.TargetEventId,
                    Applied = true
                };
            }

            int? appliedEventId = null;
            var finalStatus = review.Status;
            if (review.Status == "approved")
            {
                suggestion.EventTypeId = review.EventTypeId ?? suggestion.EventTypeId;
                suggestion.AreaId = review.AreaId ?? suggestion.AreaId;
                suggestion.PriorityId = review.PriorityId ?? suggestion.PriorityId;
                suggestion.ProjectId = review.ProjectId ?? suggestion.ProjectId;

                if (suggestion.ActionType is "create_task" or "create_project_task")
                {
                    appliedEventId = await CreateTaskFromSuggestionAsync(
                        conn, transaction, suggestion, reviewerId, clientContext.RequireClientId());
                    finalStatus = "created";
                }
                else if (suggestion.ActionType == "update_existing_task")
                {
                    appliedEventId = await UpdateTaskFromSuggestionAsync(
                        conn, transaction, suggestion, reviewerId, clientContext.RequireClientId());
                    finalStatus = "applied";
                }
                else
                {
                    throw new EmailSuggestionApplicationException(
                        $"Unsupported suggestion action '{suggestion.ActionType}'.");
                }
            }

            await conn.ExecuteAsync(
                """
                UPDATE email_task_suggestion
                   SET status = @finalStatus,
                       review_notes = @ReviewNotes,
                       reviewed_by_id = @reviewerId,
                       reviewed_at = now(),
                       event_id = @storedEventId,
                       event_type_id = @appliedEventTypeId,
                       area_id = @appliedAreaId,
                       priority_id = @appliedPriorityId,
                       project_id = @appliedProjectId,
                       updated_at = now()
                 WHERE email_task_suggestion_id = @suggestionId
                   AND email_intake_id = @intakeId
                   AND client_id = @clientId
                """, new
                {
                    intakeId,
                    suggestionId,
                    finalStatus,
                    review.ReviewNotes,
                    reviewerId,
                    storedEventId = finalStatus == "created" ? appliedEventId : null,
                    appliedEventTypeId = suggestion.EventTypeId,
                    appliedAreaId = suggestion.AreaId,
                    appliedPriorityId = suggestion.PriorityId,
                    appliedProjectId = suggestion.ProjectId,
                    clientId = clientContext.RequireClientId()
                }, transaction);

            await CompleteIntakeIfReviewedAsync(
                conn, transaction, intakeId, reviewerId, clientContext.RequireClientId());
            transaction.Commit();
            return new EmailSuggestionReviewResult
            {
                Status = finalStatus,
                EventId = appliedEventId,
                Applied = finalStatus is "created" or "applied"
            };
        }
        catch (Exception ex)
        {
            await errorLog.LogAsync(ex.Message, ex.StackTrace, nameof(EmailTriageRepository));
            throw;
        }
    }

    private static async Task<int> CreateTaskFromSuggestionAsync(
        IDbConnection conn,
        IDbTransaction transaction,
        EmailTaskSuggestion suggestion,
        int reviewerId,
        int clientId)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(suggestion.Title)) missing.Add("title");
        if (suggestion.ActionType == "create_project_task" && !suggestion.ProjectId.HasValue)
            missing.Add("project");
        if (missing.Count > 0)
            throw new EmailSuggestionApplicationException(
                $"Complete the missing {string.Join(", ", missing)} before creating this task.");

        suggestion.EventTypeId ??= await LookupIdAsync(
            conn, transaction, "event_type", "event_type_id", "maintenance", clientId);
        suggestion.AreaId ??= await LookupIdAsync(
            conn, transaction, "area", "area_id", "whole property", clientId);
        suggestion.AreaId ??= await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT area_id FROM area WHERE client_id=@clientId ORDER BY title LIMIT 1",
            new { clientId }, transaction);
        suggestion.PriorityId ??= await LookupIdAsync(
            conn, transaction, "event_priority", "event_priority_id", "medium", clientId);

        if (!suggestion.EventTypeId.HasValue)
            throw new EmailSuggestionApplicationException(
                "The default Maintenance event type is not configured.");
        if (!suggestion.AreaId.HasValue)
            throw new EmailSuggestionApplicationException(
                "At least one area must be configured before creating a task.");
        if (!suggestion.PriorityId.HasValue)
            throw new EmailSuggestionApplicationException(
                "The default Medium event priority is not configured.");

        var pendingStatusId = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT event_status_id FROM event_status WHERE LOWER(title) = 'pending' LIMIT 1",
            transaction: transaction);
        if (!pendingStatusId.HasValue)
            throw new EmailSuggestionApplicationException(
                "The Pending event status is not configured.");

        return await conn.QuerySingleAsync<int>(
            """
            INSERT INTO home_event
                   (client_id, title, description, event_date, created_date, created_by_id,
                    updated_date, event_type_id, area_id, event_status_id, priority_id,
                    target_completion_date, actual_completion_date, is_recurring,
                    recurrence_interval, recurrence_unit, project_id)
            VALUES (@clientId, @Title, @Description, CURRENT_DATE, CURRENT_DATE, @reviewerId,
                    CURRENT_DATE, @EventTypeId, @AreaId, @pendingStatusId, @PriorityId,
                    @TargetCompletionDate, NULL, false, NULL, NULL, @ProjectId)
            RETURNING event_id
            """, new
            {
                suggestion.Title,
                suggestion.Description,
                reviewerId,
                suggestion.EventTypeId,
                suggestion.AreaId,
                pendingStatusId,
                suggestion.PriorityId,
                suggestion.TargetCompletionDate,
                suggestion.ProjectId,
                clientId
            }, transaction);
    }

    private static Task<int?> LookupIdAsync(
        IDbConnection conn,
        IDbTransaction transaction,
        string table,
        string idColumn,
        string title,
        int clientId)
    {
        var tenantClause = table is "event_type" or "area" ? " AND client_id=@clientId" : string.Empty;
        var sql = $"SELECT {idColumn} FROM {table} WHERE LOWER(title) = @title{tenantClause} LIMIT 1";
        return conn.QuerySingleOrDefaultAsync<int?>(sql, new { title, clientId }, transaction);
    }

    private static async Task<int> UpdateTaskFromSuggestionAsync(
        IDbConnection conn,
        IDbTransaction transaction,
        EmailTaskSuggestion suggestion,
        int reviewerId,
        int clientId)
    {
        if (!suggestion.TargetEventId.HasValue)
            throw new EmailSuggestionApplicationException(
                "The existing task selected by this suggestion is missing.");

        var eventId = await conn.QuerySingleOrDefaultAsync<int?>(
            """
            UPDATE home_event
               SET title = COALESCE(NULLIF(@Title, ''), title),
                   description = COALESCE(@Description, description),
                   target_completion_date = COALESCE(@TargetCompletionDate, target_completion_date),
                   event_type_id = COALESCE(@EventTypeId, event_type_id),
                   area_id = COALESCE(@AreaId, area_id),
                   priority_id = COALESCE(@PriorityId, priority_id),
                   updated_date = CURRENT_DATE
             WHERE event_id = @TargetEventId AND client_id=@clientId
            RETURNING event_id
            """, new
            {
                suggestion.Title,
                suggestion.Description,
                suggestion.TargetCompletionDate,
                suggestion.EventTypeId,
                suggestion.AreaId,
                suggestion.PriorityId,
                suggestion.TargetEventId,
                clientId
            }, transaction);
        if (!eventId.HasValue)
            throw new EmailSuggestionApplicationException(
                $"Task #{suggestion.TargetEventId} no longer exists.");

        var proposedNote = ProposedNote(suggestion.ProposedChanges);
        if (!string.IsNullOrWhiteSpace(proposedNote))
        {
            var subject = $"Email triage: {suggestion.Title}";
            if (subject.Length > 255) subject = subject[..255];
            await conn.ExecuteAsync(
                """
                INSERT INTO note
                       (client_id, link_object_type_id, link_object_id, subject, note_text,
                        created_date, created_by_id)
                VALUES (@clientId, 2, @eventId, @subject, @proposedNote, CURRENT_DATE, @reviewerId)
                """, new { clientId, eventId, subject, proposedNote, reviewerId }, transaction);
        }

        return eventId.Value;
    }

    private static string? ProposedNote(JsonElement? proposedChanges)
    {
        if (proposedChanges is not { ValueKind: JsonValueKind.Object } changes)
            return null;
        if (changes.TryGetProperty("ProposedNote", out var value) ||
            changes.TryGetProperty("proposedNote", out value))
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return null;
    }

    private static async Task CompleteIntakeIfReviewedAsync(
        IDbConnection conn,
        IDbTransaction transaction,
        long intakeId,
        int reviewerId,
        int clientId)
    {
        await conn.ExecuteAsync(
            """
            UPDATE email_intake ei
               SET status = 'completed',
                   reviewed_by_id = @reviewerId,
                   reviewed_at = now(),
                   updated_at = now()
             WHERE ei.email_intake_id = @intakeId
               AND ei.client_id = @clientId
               AND EXISTS
                   (
                       SELECT 1
                         FROM email_task_suggestion ets
                        WHERE ets.email_intake_id = ei.email_intake_id
                   )
               AND NOT EXISTS
                   (
                       SELECT 1
                         FROM email_task_suggestion ets
                        WHERE ets.email_intake_id = ei.email_intake_id
                          AND ets.status NOT IN ('rejected', 'created', 'applied')
                   )
            """, new { intakeId, reviewerId, clientId }, transaction);
    }

    private static JsonElement? ParseOptionalJson(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : JsonSerializer.Deserialize<JsonElement>(value);

    private sealed class EmailIntakeDetailRow : EmailIntakeDetail
    {
        public string? AuthenticationResultJson { get; set; }
        public string? ExtractionResultJson { get; set; }

        public EmailIntakeDetail ToModel()
        {
            AuthenticationResult = ParseOptionalJson(AuthenticationResultJson);
            ExtractionResult = ParseOptionalJson(ExtractionResultJson);
            return this;
        }
    }

    private sealed class EmailTaskSuggestionRow : EmailTaskSuggestion
    {
        public string MissingInformationJson { get; set; } = "[]";
        public string? ExtractionDataJson { get; set; }
        public string EvidenceJson { get; set; } = "[]";
        public string? ProposedChangesJson { get; set; }

        public EmailTaskSuggestion ToModel()
        {
            MissingInformation = JsonSerializer.Deserialize<JsonElement>(MissingInformationJson);
            ExtractionData = ParseOptionalJson(ExtractionDataJson);
            Evidence = JsonSerializer.Deserialize<JsonElement>(EvidenceJson);
            ProposedChanges = ParseOptionalJson(ProposedChangesJson);
            return this;
        }
    }

    private sealed class EmailAiRunRow : EmailAiRun
    {
        public string? ResponseJsonText { get; set; }

        public EmailAiRun ToModel()
        {
            Response = ParseOptionalJson(ResponseJsonText);
            return this;
        }
    }
}
