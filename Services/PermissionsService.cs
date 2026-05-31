using SCSPortal.Models;

namespace SCSPortal.Services;

/// <summary>
/// In-memory permission matrix that Admin can edit. For the prototype this is
/// a visual configuration; the sidebar still gates on role checks directly.
/// </summary>
public class PermissionsService
{
    public event Action? OnChange;
    private readonly AuditService? _audit;
    private readonly UserContextService? _users;

    /// <summary>Roles × features → allowed?</summary>
    public Dictionary<AppRole, HashSet<string>> Matrix { get; } = new();

    public static readonly (string Key, string Label, string Group)[] Features =
    {
        ("dashboard",         "Dashboard",                "Common"),
        ("profile",           "Profile (client/project)", "Common"),
        ("notifications",     "Notifications",            "Common"),
        ("audit",             "Audit log",                "Common"),

        ("fc.list",           "Funding Commitments — list",   "Funding"),
        ("fc.create",         "Funding Commitments — create", "Funding"),
        ("fc.revise",         "Funding Commitments — revise", "Funding"),
        ("fc.associate-vendor","FC associate vendor",         "Funding"),
        ("fc.add-pf",         "FC add Permanent Funding",     "Funding"),
        ("pf.list",           "Permanent Funding — list",     "Funding"),
        ("pf.create",         "Permanent Funding — create",   "Funding"),
        ("rollover",          "Budget Rollover",              "Funding"),
        ("financial-pressure",          "Financial Pressure — list",  "Funding"),
        ("financial-pressure.submit",   "Financial Pressure — submit","Funding"),
        ("approvals",         "Approvals queue",              "Funding"),

        ("scheduling",        "Scheduling — calendar",        "Operations"),
        ("scheduling.new",    "Scheduling — new shift",       "Operations"),
        ("scheduling.bulk",   "Scheduling — bulk add",        "Operations"),
        ("invoices.list",     "Invoices — list",              "Operations"),
        ("invoices.convert",  "Invoices — convert shifts",    "Operations"),
        ("invoices.manual",   "Invoices — manual builder",    "Operations"),
        ("my-clients",        "My Clients (vendor view)",     "Operations"),
        ("my-vendors",        "My Vendors (client view)",     "Operations"),

        ("admin.users",       "Admin — Users & Roles",        "Admin"),
        ("admin.permissions", "Admin — Permissions matrix",   "Admin"),
        ("admin.invitations", "Admin — Clients & Vendors invitations", "Admin"),
        ("vendor-approval",   "Finance — Vendor approval",    "Admin")
    };

    public PermissionsService(AuditService? audit = null, UserContextService? users = null)
    {
        _audit = audit; _users = users;
        // Seed sensible defaults per role
        Set(AppRole.Admin,
            "dashboard", "notifications", "audit",
            "admin.users", "admin.permissions", "admin.invitations");
        Set(AppRole.Finance,
            "dashboard", "notifications", "audit", "profile",
            "fc.list", "fc.create", "fc.revise", "fc.associate-vendor", "fc.add-pf",
            "pf.list", "pf.create", "rollover",
            "financial-pressure", "approvals", "vendor-approval",
            "invoices.list", "invoices.convert", "invoices.manual");
        Set(AppRole.CaseManager,
            "dashboard", "notifications", "profile",
            "fc.list", "fc.create", "fc.revise", "fc.associate-vendor");
        Set(AppRole.SpecialApprover,
            "dashboard", "notifications", "audit", "fc.list", "approvals");
        Set(AppRole.VendorIsw,
            "dashboard", "notifications",
            "my-clients", "scheduling", "scheduling.new", "scheduling.bulk",
            "invoices.list", "invoices.convert");
        Set(AppRole.VendorSp,
            "dashboard", "notifications",
            "my-clients", "financial-pressure", "financial-pressure.submit",
            "invoices.list", "invoices.manual");
        Set(AppRole.Client,
            "dashboard", "notifications",
            "my-vendors", "fc.list", "scheduling", "scheduling.new",
            "invoices.list", "invoices.manual");
    }

    private void Set(AppRole role, params string[] features)
    {
        Matrix[role] = features.ToHashSet();
    }

    public bool Allows(AppRole role, string feature) =>
        Matrix.TryGetValue(role, out var set) && set.Contains(feature);

    public void Toggle(AppRole role, string feature)
    {
        if (!Matrix.TryGetValue(role, out var set))
        {
            set = new HashSet<string>();
            Matrix[role] = set;
        }
        var before = set.Contains(feature) ? "allow" : "deny";
        if (!set.Add(feature)) set.Remove(feature);
        var after = set.Contains(feature) ? "allow" : "deny";
        _audit?.Log(
            action: "Permissions.Toggle",
            entityType: "Permissions",
            entityId: $"{role}:{feature}",
            entityLabel: $"{RoleInfo.Label(role)} · {feature}",
            before: before,
            after: after,
            detail: $"Changed by {_users?.Effective.FullName ?? "Admin"}");
        OnChange?.Invoke();
    }
}
