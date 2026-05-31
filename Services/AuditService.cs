using SCSPortal.Models;

namespace SCSPortal.Services;

public class AuditService
{
    public List<AuditEntry> Entries { get; } = new();
    public event Action? OnChange;

    public AuditService(UserContextService users)
    {
        _users = users;
        // Seed a few audit entries
        Entries.Add(new AuditEntry
        {
            Timestamp = DateTime.Now.AddHours(-26),
            Actor = "System", ActorRole = AppRole.Admin,
            Action = "System.Boot",
            EntityType = "System", EntityId = "-", EntityLabel = "Bootstrap",
            Detail = "Application started; seeded data loaded."
        });
    }

    private readonly UserContextService _users;

    public void Log(string action, string entityType, string entityId, string entityLabel,
                    string before = "", string after = "", string detail = "")
    {
        var u = _users.Effective;
        Entries.Insert(0, new AuditEntry
        {
            Actor = u.FullName,
            ActorRole = u.Role,
            ImpersonatedBy = _users.Impersonating != null ? _users.Current.FullName : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityLabel = entityLabel,
            Before = before,
            After = after,
            Detail = detail
        });
        if (Entries.Count > 500) Entries.RemoveAt(Entries.Count - 1);
        OnChange?.Invoke();
    }
}
