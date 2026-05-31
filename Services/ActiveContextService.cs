using SCSPortal.Models;

namespace SCSPortal.Services;

public enum ActiveContextKind { None, Client, Project }

/// <summary>
/// Holds the currently-active working context (Client or Project) for internal staff.
/// All client/project-scoped actions read from this. Client-role users auto-bind to
/// their own linked client.
/// </summary>
public class ActiveContextService
{
    public ActiveContextKind Kind { get; private set; } = ActiveContextKind.None;
    public Client? ActiveClient { get; private set; }
    public Project? ActiveProject { get; private set; }

    public event Action? OnChange;

    private readonly UserContextService _users;
    private readonly ClientDirectoryService _clients;
    private readonly ProjectService _projects;
    private readonly AuditService _audit;
    private readonly PermanentFundingService _pf;
    private readonly FundingCommitmentService _fc;
    private readonly VendorService _vendors;

    public ActiveContextService(UserContextService users,
                                ClientDirectoryService clients,
                                ProjectService projects,
                                AuditService audit,
                                PermanentFundingService pf,
                                FundingCommitmentService fc,
                                VendorService vendors)
    {
        _users = users; _clients = clients; _projects = projects;
        _audit = audit; _pf = pf; _fc = fc; _vendors = vendors;

        users.OnChange += AutoBind;
        AutoBind();
    }

    /// <summary>Resolve the current user's vendor name (or display name fallback).</summary>
    private string? CurrentVendorName()
    {
        var u = _users.Effective;
        if (u.LinkedVendorId != null)
        {
            var v = _vendors.FindById(u.LinkedVendorId);
            if (v != null) return v.Name;
        }
        return u.FullName;
    }

    private void AutoBind()
    {
        var u = _users.Effective;
        if (u.Role == AppRole.Client && u.LinkedClientId != null)
        {
            var c = _clients.LookupEto(u.LinkedClientId);
            if (c != null && (Kind != ActiveContextKind.Client || ActiveClient?.EtoId != c.EtoId))
            {
                Kind = ActiveContextKind.Client;
                ActiveClient = c;
                ActiveProject = null;
                OnChange?.Invoke();
            }
        }
    }

    public bool TrySetClient(string etoId, out string message)
    {
        var c = _clients.LookupEto((etoId ?? "").Trim());
        if (c == null)
        {
            message = $"No client found for ETO ID “{etoId}”.";
            return false;
        }
        if (IsRestrictedToAssociated && !AssociatedClientsForCurrentVendor().Any(x => x.EtoId == c.EtoId))
        {
            message = $"You are not associated with {c.FullName}. Please pick from your assigned clients.";
            return false;
        }
        ActiveClient = c;
        ActiveProject = null;
        Kind = ActiveContextKind.Client;
        _audit.Log("ActiveContext.SetClient", "Client", c.EtoId, c.FullName);
        message = $"Active client set: {c.FullName} (#{c.EtoId}).";
        OnChange?.Invoke();
        return true;
    }

    public bool TrySetProject(string projectId, out string message)
    {
        var p = _projects.FindById((projectId ?? "").Trim());
        if (p == null)
        {
            message = "No matching project.";
            return false;
        }
        if (IsRestrictedToAssociated && !AssociatedProjectsForCurrentVendor().Any(x => x.Id == p.Id))
        {
            message = $"You are not associated with project {p.Name}. Please pick from your assigned projects.";
            return false;
        }
        ActiveProject = p;
        ActiveClient = null;
        Kind = ActiveContextKind.Project;
        _audit.Log("ActiveContext.SetProject", "Project", p.Id, p.Name);
        message = $"Active project set: {p.Name} ({p.Code}).";
        OnChange?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (Kind == ActiveContextKind.None) return;
        var prev = DisplayName;
        Kind = ActiveContextKind.None;
        ActiveClient = null;
        ActiveProject = null;
        _audit.Log("ActiveContext.Clear", "Context", "-", prev);
        OnChange?.Invoke();
    }

    public bool RequiresActive =>
        _users.Effective.Role is AppRole.Admin or AppRole.Finance
                              or AppRole.CaseManager or AppRole.SpecialApprover
                              or AppRole.VendorIsw or AppRole.VendorSp;

    /// <summary>
    /// True when the current user is restricted to associated entities only (vendors).
    /// Internal staff can pick any client/project; vendors only see their associations.
    /// </summary>
    public bool IsRestrictedToAssociated => _users.Effective.Role is AppRole.VendorIsw or AppRole.VendorSp;

    public bool HasActive => Kind != ActiveContextKind.None;

    // ----- Associated clients/projects for vendors -----

    /// <summary>
    /// Clients the current vendor is associated with — derived from FC service lines
    /// whose Vendor matches the vendor's display name.
    /// </summary>
    public IEnumerable<Client> AssociatedClientsForCurrentVendor()
    {
        var u = _users.Effective;
        if (u.Role is not (AppRole.VendorIsw or AppRole.VendorSp)) return Array.Empty<Client>();

        var name = CurrentVendorName();
        if (string.IsNullOrEmpty(name)) return Array.Empty<Client>();

        var clientIds = _fc.Rows
            .Where(r => r.Association == AssociationType.Client &&
                        r.Services.Any(s => s.Vendor.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.ClientId)
            .Distinct()
            .ToHashSet();
        return _clients.Clients.Where(c => clientIds.Contains(c.EtoId));
    }

    public IEnumerable<Project> AssociatedProjectsForCurrentVendor()
    {
        var u = _users.Effective;
        if (u.Role is not (AppRole.VendorIsw or AppRole.VendorSp)) return Array.Empty<Project>();

        var name = CurrentVendorName();
        if (string.IsNullOrEmpty(name)) return Array.Empty<Project>();

        var projectIds = _fc.Rows
            .Where(r => r.Association == AssociationType.Project &&
                        r.Services.Any(s => s.Vendor.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.LinkedEntityId)
            .Distinct()
            .ToHashSet();
        return _projects.Projects.Where(p => projectIds.Contains(p.Id));
    }

    public string DisplayName => Kind switch
    {
        ActiveContextKind.Client when ActiveClient != null => ActiveClient.FullName,
        ActiveContextKind.Project when ActiveProject != null => ActiveProject.Name,
        _ => ""
    };

    public string DisplayId => Kind switch
    {
        ActiveContextKind.Client when ActiveClient != null => ActiveClient.EtoId,
        ActiveContextKind.Project when ActiveProject != null => ActiveProject.Code,
        _ => ""
    };

    public string DisplaySubtitle => Kind switch
    {
        ActiveContextKind.Client when ActiveClient != null => $"DOB {ActiveClient.Dob}",
        ActiveContextKind.Project when ActiveProject != null => $"{ActiveProject.Sponsor} · started {ActiveProject.StartDate:MMM yyyy}",
        _ => ""
    };

    // ----- Live financial roll-up for the strip -----

    /// <summary>Sum of all PF budgets attached to the active context.</summary>
    public decimal FundingTotal
    {
        get
        {
            return Kind switch
            {
                ActiveContextKind.Client when ActiveClient != null =>
                    _pf.Records.Where(p => p.Association == AssociationType.Client && p.LinkedEntityId == ActiveClient.EtoId)
                               .Sum(p => p.BudgetAmount),
                ActiveContextKind.Project when ActiveProject != null =>
                    _pf.Records.Where(p => p.Association == AssociationType.Project && p.LinkedEntityId == ActiveProject.Id)
                               .Sum(p => p.BudgetAmount),
                _ => 0
            };
        }
    }

    /// <summary>Sum of FC line totals on approved/active commitments for this entity.</summary>
    public decimal ActualSpent
    {
        get
        {
            return Kind switch
            {
                ActiveContextKind.Client when ActiveClient != null =>
                    _fc.Rows.Where(r => r.Association == AssociationType.Client
                                     && r.ClientId == ActiveClient.EtoId
                                     && r.Status is CommitmentStatus.Approved or CommitmentStatus.Active)
                            .Sum(r => r.Total),
                ActiveContextKind.Project when ActiveProject != null =>
                    _fc.Rows.Where(r => r.Association == AssociationType.Project
                                     && r.LinkedEntityId == ActiveProject.Id
                                     && r.Status is CommitmentStatus.Approved or CommitmentStatus.Active)
                            .Sum(r => r.Total),
                _ => 0
            };
        }
    }

    public decimal AvailableToSpend => FundingTotal - ActualSpent;

    // Convenience for back-compat with code still using ActiveClientService
    public bool RequiresActiveClient => RequiresActive;
}
