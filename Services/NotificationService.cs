using SCSPortal.Models;

namespace SCSPortal.Services;

public class NotificationService
{
    public List<Notification> Items { get; } = new();
    public event Action? OnChange;

    private readonly UserContextService _users;

    public NotificationService(UserContextService users)
    {
        _users = users;
        Items.Add(new Notification
        {
            Title = "Welcome to SCS Portal",
            Body  = "Switch roles via the top-right pill to see each persona's view.",
            Level = NotificationLevel.Info,
            When  = DateTime.Now.AddHours(-3)
        });
        Items.Add(new Notification
        {
            Title = "Budget rollover queue ready",
            Body  = "20 draft FCs have been auto-generated from FY2026 permanent funding.",
            Level = NotificationLevel.Info,
            TargetRole = AppRole.Finance,
            When  = DateTime.Now.AddHours(-1),
            Link  = "/rollover"
        });
    }

    public IEnumerable<Notification> ForCurrent()
    {
        var u = _users.Effective;
        return Items.Where(n =>
            n.TargetUserId == null && n.TargetRole == null
            || n.TargetUserId == u.Id
            || n.TargetRole == u.Role);
    }

    public int UnreadCountForCurrent() => ForCurrent().Count(n => !n.Read);

    public void Push(string title, string body,
                     NotificationLevel level = NotificationLevel.Info,
                     AppRole? targetRole = null,
                     string? targetUserId = null,
                     string? link = null,
                     string? entityType = null,
                     string? entityId = null)
    {
        Items.Insert(0, new Notification
        {
            Title = title, Body = body, Level = level,
            TargetRole = targetRole, TargetUserId = targetUserId, Link = link,
            EntityType = entityType, EntityId = entityId
        });
        if (Items.Count > 200) Items.RemoveAt(Items.Count - 1);
        OnChange?.Invoke();
    }

    public void MarkRead(string id)
    {
        var n = Items.FirstOrDefault(x => x.Id == id);
        if (n != null) { n.Read = true; OnChange?.Invoke(); }
    }

    public void MarkAllRead()
    {
        foreach (var n in ForCurrent()) n.Read = true;
        OnChange?.Invoke();
    }
}
