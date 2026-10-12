using System.Security.Claims;
using System.Text.Json;
using KnowledgeAssistant.Domain.Entities.Audit;
using KnowledgeAssistant.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KnowledgeAssistant.Infrastructure.Data.Interceptors
{
    // This will automatically run when a SaveChanges or SaveChangesAsync is called on the DbContext, and it will create an ActivityAudit entry for each entity that has been added, modified, or deleted.
    public class ActivityAuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly List<(EntityEntry Entry, ActivityAudit Audit)> pendingAudits = [];
        private bool isSavingAudits;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "Token",
        "AccessToken",
        "RefreshToken",
        "Secret",
        "ApiKey"
    };

        public ActivityAuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (!isSavingAudits && eventData.Context is { } context)
            {
                AddAuditEntries(context);
            }

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (!isSavingAudits && eventData.Context is { } context)
            {
                AddAuditEntries(context);
            }

            return base.SavingChangesAsync(eventData, result, ct);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            if (!isSavingAudits && eventData.Context is { } context)
            {
                UpdateGeneratedEntityIds(context);
            }

            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
        {
            if (!isSavingAudits && eventData.Context is { } context)
            {
                await UpdateGeneratedEntityIdsAsync(context, ct);
            }

            return await base.SavedChangesAsync(eventData, result, ct);
        }

        private void AddAuditEntries(DbContext context)
        {
            context.ChangeTracker.DetectChanges();

            var entries = context.ChangeTracker.Entries()
                .Where(entry =>
                    entry.Entity is not ActivityAudit &&
                    entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                var audit = CreateAuditEntry(entry);

                if (audit is null)
                {
                    continue;
                }

                context.Set<ActivityAudit>().Add(audit);
                pendingAudits.Add((entry, audit));
            }
        }

        private ActivityAudit? CreateAuditEntry(EntityEntry entry)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => (AuditAction?)null
            };

            if (action is null)
            {
                return null;
            }

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var changedProperties = new List<string>();

            foreach (var property in entry.Properties)
            {
                if (SensitivePropertyNames.Contains(property.Metadata.Name))
                {
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[property.Metadata.Name] = property.CurrentValue;
                        changedProperties.Add(property.Metadata.Name);
                        break;

                    case EntityState.Modified when property.IsModified:
                        oldValues[property.Metadata.Name] = property.OriginalValue;
                        newValues[property.Metadata.Name] = property.CurrentValue;
                        changedProperties.Add(property.Metadata.Name);
                        break;

                    case EntityState.Deleted:
                        oldValues[property.Metadata.Name] = property.OriginalValue;
                        changedProperties.Add(property.Metadata.Name);
                        break;
                }
            }

            // Skip updates where no properties have actually changed.
            if (entry.State == EntityState.Modified && changedProperties.Count == 0)
            {
                return null;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var principal = httpContext?.User;

            var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal?.FindFirst("sub")?.Value;

            var userName = principal?.Identity?.Name
                ?? principal?.FindFirst(ClaimTypes.Email)?.Value;

            return new ActivityAudit
            {
                DateTime = DateTime.UtcNow,
                UserId = userId,
                UserName = userName,
                Action = action.Value,
                Description = $"{action.Value} {entry.Metadata.ClrType.Name}",
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = GetEntityId(entry),
                OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues, JsonOptions) : null,
                NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues, JsonOptions) : null,
                ChangedProperties = changedProperties.Count > 0 ? JsonSerializer.Serialize(changedProperties, JsonOptions) : null
            };
        }

        private static string? GetEntityId(EntityEntry entry)
        {
            var primaryKey = entry.Metadata.FindPrimaryKey();

            if (primaryKey is null)
            {
                return null;
            }

            var keyValues = new Dictionary<string, object?>();

            foreach (var keyProperty in primaryKey.Properties)
            {
                var property = entry.Property(keyProperty.Name);

                if (entry.State == EntityState.Added && property.IsTemporary)
                {
                    return null;
                }

                keyValues[keyProperty.Name] = entry.State == EntityState.Deleted
                    ? property.OriginalValue
                    : property.CurrentValue;
            }

            return keyValues.Count == 1
                ? keyValues.Values.First()?.ToString()
                : JsonSerializer.Serialize(keyValues, JsonOptions);
        }

        private void UpdateGeneratedEntityIds(DbContext context)
        {
            var changed = false;

            foreach (var (entry, audit) in pendingAudits)
            {
                var entityId = GetEntityId(entry);

                if (audit.EntityId != entityId)
                {
                    audit.EntityId = entityId;
                    changed = true;
                }
            }

            pendingAudits.Clear();

            if (!changed)
            {
                return;
            }

            isSavingAudits = true;

            try
            {
                context.SaveChanges();
            }
            finally
            {
                isSavingAudits = false;
            }
        }

        private async Task UpdateGeneratedEntityIdsAsync(DbContext context, CancellationToken ct)
        {
            var changed = false;

            foreach (var (entry, audit) in pendingAudits)
            {
                var entityId = GetEntityId(entry);

                if (audit.EntityId != entityId)
                {
                    audit.EntityId = entityId;
                    changed = true;
                }
            }

            pendingAudits.Clear();

            if (!changed)
            {
                return;
            }

            isSavingAudits = true;

            try
            {
                await context.SaveChangesAsync(ct);
            }
            finally
            {
                isSavingAudits = false;
            }
        }
    }

}