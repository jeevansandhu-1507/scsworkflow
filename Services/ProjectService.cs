using SCSPortal.Models;

namespace SCSPortal.Services;

public class ProjectService
{
    public List<Project> Projects { get; } = new();
    public event Action? OnChange;

    public ProjectService()
    {
        Add("p-001", "Northern Outreach", "PRJ-N-2026", "MCCSS", "Sara Khan",
            "Mobile outreach for rural northern communities.", new(2025, 11, 1));
        Add("p-002", "Family Respite Pilot", "PRJ-FR-2026", "United Way", "Devon Walsh",
            "Pilot family respite program across Halton.", new(2026, 1, 15));
        Add("p-003", "Vocational Bridge", "PRJ-VB-2026", "MCYS", "Priya Nadar",
            "Vocational training bridging program for transitioning youth.", new(2025, 9, 1));
        Add("p-004", "Inclusive Living", "PRJ-IL-2026", "Trillium Foundation", "Marcus Tan",
            "Independent-living skill clusters with peer mentorship.", new(2026, 2, 1));
        Add("p-005", "Behavioural Outcomes Lab", "PRJ-BL-2026", "MCCSS", "Lin Chen",
            "Outcomes lab measuring behavioural therapy efficacy.", new(2025, 12, 1));
    }

    private void Add(string id, string name, string code, string sponsor, string mgr, string desc, DateTime start)
    {
        Projects.Add(new Project
        {
            Id = id, Name = name, Code = code, Sponsor = sponsor,
            ManagerName = mgr, Description = desc, StartDate = start
        });
    }

    public Project? FindById(string id) => Projects.FirstOrDefault(p => p.Id == id);

    public Project Create(string name, string code, string sponsor, string manager, string description)
    {
        var p = new Project
        {
            Id = "p-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            Name = name, Code = code, Sponsor = sponsor,
            ManagerName = manager, Description = description,
            StartDate = DateTime.Today, Status = "Active"
        };
        Projects.Insert(0, p);
        OnChange?.Invoke();
        return p;
    }
}

public class ClientDirectoryService
{
    public List<Client> Clients { get; } = new();
    public event Action? OnChange;

    private readonly AuditService? _audit;
    private readonly UserContextService? _users;

    public ClientDirectoryService() { SeedAll(); }

    public ClientDirectoryService(AuditService audit, UserContextService users)
    {
        _audit = audit; _users = users;
        SeedAll();
    }

    private void SeedAll()
    {
        Seed("10342", "Aaron Mackenzie",  "May 12, 2010", "Erin Mackenzie",  "erin.m@example.com",     "Hamilton",     InviteStatus.Accepted);
        Seed("11587", "Priya Sharma",     "Mar 28, 2009", "Anil Sharma",     "anil.s@example.com",     "Mississauga",  InviteStatus.Invited);
        Seed("12894", "Marcus O'Brien",   "Dec 03, 2012", "Kate O'Brien",    "kate.o@example.com",     "Burlington",   InviteStatus.NotInvited);
        Seed("13405", "Léa Beaulieu",     "Jun 17, 2011", "Marc Beaulieu",   "marc.b@example.com",     "Ottawa",       InviteStatus.Accepted);
        Seed("20716", "Hazel Wong",       "Aug 03, 2014", "Mei Wong",        "mei.w@example.com",      "Toronto",      InviteStatus.Invited);
        Seed("23343", "bill test",        "Jun 15, 2000", "",                "billtest@example.com",   "Test City",    InviteStatus.NotInvited);
        Seed("84291", "Sarah Chen",       "Mar 04, 2012", "Annie Chen",      "annie.c@example.com",    "Markham",      InviteStatus.Accepted);
        Seed("67234", "Marcus Williams",  "Sep 22, 2008", "Tina Williams",   "tina.w@example.com",     "London",       InviteStatus.Expired);
        Seed("19584", "Aisha Patel",      "Jan 18, 2003", "Suresh Patel",    "suresh.p@example.com",   "Brampton",     InviteStatus.NotInvited);
        Seed("90122", "Liam O'Brien",     "Nov 07, 2010", "Saoirse O'Brien", "saoirse@example.com",    "Oakville",     InviteStatus.NotInvited);
        Seed("33871", "Daniela Rojas",    "Apr 12, 2015", "Camila Rojas",    "camila.r@example.com",   "Hamilton",     InviteStatus.NotInvited);
        Seed("56098", "Kenji Nakamura",   "Aug 30, 2005", "Yuki Nakamura",   "yuki.n@example.com",     "Toronto",      InviteStatus.Invited);
        Seed("77423", "Emma Larsson",     "Feb 14, 2007", "Karl Larsson",    "karl.l@example.com",     "Waterloo",     InviteStatus.Accepted);
        Seed("42915", "Tariq Hassan",     "Jul 03, 2011", "Layla Hassan",    "layla.h@example.com",    "Mississauga",  InviteStatus.NotInvited);
        Seed("88307", "Priya Sharma",     "Dec 09, 2009", "Anil Sharma",     "anil2.s@example.com",    "Brampton",     InviteStatus.NotInvited);
        Seed("30521", "Yasmin Hossain",   "Aug 11, 2008", "Rafiq Hossain",   "rafiq.h@example.com",    "Toronto",      InviteStatus.NotInvited);
        Seed("45673", "Connor Reilly",    "Apr 22, 2011", "Patricia Reilly", "pat.r@example.com",      "Kingston",     InviteStatus.NotInvited);
        Seed("58104", "Mei Lin",          "Jul 18, 2009", "Jin Lin",         "jin.l@example.com",      "Toronto",      InviteStatus.NotInvited);
        Seed("82640", "Amélie Tremblay",  "Nov 30, 2007", "Sophie Tremblay", "sophie.t@example.com",   "Ottawa",       InviteStatus.NotInvited);
        Seed("10245", "Avery Thompson",   "Feb 19, 2009", "Sam Thompson",    "sam.t@example.com",      "Hamilton",     InviteStatus.NotInvited);
    }

    private void Seed(string id, string name, string dob, string guardian, string email, string city, InviteStatus invite)
    {
        var c = new Client { EtoId = id, FullName = name, Dob = dob, Guardian = guardian, Email = email, City = city };
        c.InviteStatus = invite;
        if (invite == InviteStatus.Invited)
        {
            c.LastInvitedAt = DateTime.Now.AddDays(-Random.Shared.Next(1, 6));
            c.InviteCount = 1;
        }
        else if (invite == InviteStatus.Accepted)
        {
            c.LastInvitedAt = DateTime.Now.AddDays(-Random.Shared.Next(7, 30));
            c.InviteAcceptedAt = c.LastInvitedAt?.AddDays(Random.Shared.Next(1, 3));
            c.InviteCount = 1;
        }
        else if (invite == InviteStatus.Expired)
        {
            c.LastInvitedAt = DateTime.Now.AddDays(-Random.Shared.Next(35, 60));
            c.InviteCount = 1;
        }
        Clients.Add(c);
    }

    // Mocks an ETO lookup: returns null if not found.
    public Client? LookupEto(string etoId) =>
        Clients.FirstOrDefault(c => c.EtoId == etoId.Trim());

    /// <summary>Send / resend the portal invite for a client (mocked).</summary>
    public bool SendInvite(string etoId)
    {
        var c = LookupEto(etoId);
        if (c == null) return false;
        var was = c.InviteStatus;
        c.LastInvitedAt = DateTime.Now;
        c.InviteCount++;
        if (was != InviteStatus.Accepted) c.InviteStatus = InviteStatus.Invited;
        _audit?.Log(
            action: was == InviteStatus.NotInvited ? "Client.Invite" : "Client.ResendInvite",
            entityType: "Client",
            entityId: c.EtoId,
            entityLabel: c.FullName,
            before: was.ToString(),
            after: c.InviteStatus.ToString(),
            detail: string.IsNullOrEmpty(c.Email) ? "(no email on file)" : $"sent to {c.Email}");
        OnChange?.Invoke();
        return true;
    }
}

public class VendorService
{
    public List<Vendor> Vendors { get; } = new();
    public event Action? OnChange;

    private readonly AuditService? _audit;
    private readonly UserContextService? _users;

    public VendorService() { Seed(); }

    public VendorService(AuditService audit, UserContextService users)
    {
        _audit = audit; _users = users;
        Seed();
    }

    private void Seed()
    {
        // Approved (existing roster)
        Add("v-isw-01", "Ima Worker",          VendorType.Isw, "Ima Worker",  "ima@worker.test", VendorStatus.Approved);
        Add("v-isw-02", "Alan Smith",          VendorType.Isw, "Alan Smith",  "alan.s@worker.test", VendorStatus.Approved);
        Add("v-isw-03", "Agnes Testworker",    VendorType.Isw, "Agnes T.",    "agnes@worker.test", VendorStatus.Approved);
        Add("v-isw-04", "Jatin Test",          VendorType.Isw, "Jatin Test",  "j.test@worker.test", VendorStatus.Approved);
        Add("v-sp-01",  "Maple Care Inc.",     VendorType.Sp,  "Lauren M.",   "ops@maplecare.test", VendorStatus.Approved);
        Add("v-sp-02",  "BrightPath Services", VendorType.Sp,  "Devon W.",    "devon@brightpath.test", VendorStatus.Approved);
        Add("v-sp-03",  "Northern Light Co.",  VendorType.Sp,  "Sarah N.",    "sarah@northernlight.test", VendorStatus.Approved);
        Add("v-sp-04",  "Aurora Support Co-op",VendorType.Sp,  "Carlos R.",   "carlos@aurora.test", VendorStatus.Approved);

        // Pending — awaiting Finance approval (sample data so the queue isn't empty)
        AddDetailed("v-isw-05", "Priya Bhat",         VendorType.Isw, "Priya Bhat",     "priya.b@workers.test",
                    "BN-78441-IS", "421 King St W, Toronto",  VendorStatus.Pending,  daysAgo: 2);
        AddDetailed("v-isw-06", "Tomás Álvarez",       VendorType.Isw, "Tomás Álvarez",  "tomas.a@workers.test",
                    "BN-90112-IS", "9 Bayview Pkwy, Newmarket", VendorStatus.Pending, daysAgo: 5);
        AddDetailed("v-sp-05",  "Halton Support Hub", VendorType.Sp,  "Maria Volkov",   "intake@haltonsh.test",
                    "BN-55203-SP", "150 Steeles Ave E, Milton", VendorStatus.Pending,  daysAgo: 1);
        AddDetailed("v-sp-06",  "Caregivers United", VendorType.Sp,  "Joel Marchand",  "ops@caregiversu.test",
                    "BN-66007-SP", "85 Industrial Pkwy, Aurora", VendorStatus.Pending, daysAgo: 8);

        // Rejected (example)
        AddDetailed("v-sp-07",  "Brightstar Inc.",    VendorType.Sp, "M. Brightstar",   "info@brightstar.test",
                    "BN-12345-SP", "1 Main St, Mississauga", VendorStatus.Rejected, daysAgo: 14,
                    rejection: "Missing WSIB clearance and W9.");
    }

    private void Add(string id, string name, VendorType t, string contact, string email, VendorStatus status)
    {
        // Spread invite statuses across the approved roster so the Admin invite page has a mix
        var inviteStatus = status == VendorStatus.Approved
            ? (Vendors.Count % 3 == 0 ? InviteStatus.Accepted
                : Vendors.Count % 3 == 1 ? InviteStatus.Invited
                : InviteStatus.NotInvited)
            : InviteStatus.NotInvited;
        Vendors.Add(new Vendor
        {
            Id = id, Name = name, Type = t, Contact = contact, Email = email,
            Phone = "(905) 555-0100",
            Status = status,
            SubmittedAt = DateTime.Now.AddDays(-30),
            SubmittedBy = "system seed",
            ApprovedBy = status == VendorStatus.Approved ? "Fatima Finance" : null,
            ApprovedAt = status == VendorStatus.Approved ? DateTime.Now.AddDays(-25) : null,
            InviteStatus = inviteStatus,
            LastInvitedAt = inviteStatus == InviteStatus.NotInvited ? null : DateTime.Now.AddDays(-Random.Shared.Next(2, 20)),
            InviteAcceptedAt = inviteStatus == InviteStatus.Accepted ? DateTime.Now.AddDays(-Random.Shared.Next(1, 10)) : null,
            InviteCount = inviteStatus == InviteStatus.NotInvited ? 0 : 1,
            SupportTypes = new List<string> { "Personal Development Support", "Community Participation Support" }
        });
    }

    private void AddDetailed(string id, string name, VendorType t, string contact, string email,
                             string businessNumber, string address, VendorStatus status, int daysAgo,
                             string? rejection = null)
    {
        Vendors.Add(new Vendor
        {
            Id = id, Name = name, Type = t, Contact = contact, Email = email,
            Phone = "(905) 555-0" + (100 + Vendors.Count % 99),
            Status = status,
            BusinessNumber = businessNumber,
            Address = address,
            SubmittedAt = DateTime.Now.AddDays(-daysAgo),
            SubmittedBy = name,
            RejectionReason = rejection,
            SupportTypes = new List<string> { "Personal Development Support" }
        });
    }

    public Vendor? FindById(string id) => Vendors.FirstOrDefault(v => v.Id == id);

    public IEnumerable<Vendor> Pending() => Vendors.Where(v => v.Status == VendorStatus.Pending)
                                                   .OrderByDescending(v => v.SubmittedAt);
    public IEnumerable<Vendor> Approved() => Vendors.Where(v => v.Status == VendorStatus.Approved)
                                                    .OrderBy(v => v.Name);
    public IEnumerable<Vendor> Rejected() => Vendors.Where(v => v.Status == VendorStatus.Rejected)
                                                    .OrderByDescending(v => v.SubmittedAt);

    public void Approve(string id)
    {
        var v = FindById(id);
        if (v == null) return;
        var before = v.Status.ToString();
        v.Status = VendorStatus.Approved;
        v.ApprovedAt = DateTime.Now;
        v.ApprovedBy = _users?.Effective.FullName ?? "Finance";
        v.RejectionReason = null;
        _audit?.Log("Vendor.Approve", "Vendor", v.Id, v.Name, before, "Approved",
                    $"Approved by {v.ApprovedBy}");
        OnChange?.Invoke();
    }

    public void Reject(string id, string reason)
    {
        var v = FindById(id);
        if (v == null) return;
        var before = v.Status.ToString();
        v.Status = VendorStatus.Rejected;
        v.RejectionReason = reason;
        v.ApprovedBy = null;
        v.ApprovedAt = null;
        _audit?.Log("Vendor.Reject", "Vendor", v.Id, v.Name, before, "Rejected", reason);
        OnChange?.Invoke();
    }

    public void Reactivate(string id)
    {
        var v = FindById(id);
        if (v == null) return;
        var before = v.Status.ToString();
        v.Status = VendorStatus.Pending;
        _audit?.Log("Vendor.Reactivate", "Vendor", v.Id, v.Name, before, "Pending");
        OnChange?.Invoke();
    }

    public Vendor Create(string name, VendorType type, string contact, string email, string? bn, string? address)
    {
        var v = new Vendor
        {
            Id = "v-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            Name = name, Type = type, Contact = contact, Email = email,
            Phone = "(905) 555-" + Random.Shared.Next(1000, 9999),
            BusinessNumber = bn,
            Address = address,
            Status = VendorStatus.Pending,
            SubmittedAt = DateTime.Now,
            SubmittedBy = _users?.Effective.FullName ?? "Finance"
        };
        Vendors.Insert(0, v);
        _audit?.Log("Vendor.Create", "Vendor", v.Id, v.Name, after: "Pending",
                    detail: $"Submitted for approval by {v.SubmittedBy}");
        OnChange?.Invoke();
        return v;
    }

    /// <summary>Send / resend the portal invite for a vendor (mocked).</summary>
    public bool SendInvite(string id)
    {
        var v = FindById(id);
        if (v == null) return false;
        var was = v.InviteStatus;
        v.LastInvitedAt = DateTime.Now;
        v.InviteCount++;
        if (was != InviteStatus.Accepted) v.InviteStatus = InviteStatus.Invited;
        _audit?.Log(
            action: was == InviteStatus.NotInvited ? "Vendor.Invite" : "Vendor.ResendInvite",
            entityType: "Vendor",
            entityId: v.Id,
            entityLabel: v.Name,
            before: was.ToString(),
            after: v.InviteStatus.ToString(),
            detail: string.IsNullOrEmpty(v.Email) ? "(no email on file)" : $"sent to {v.Email}");
        OnChange?.Invoke();
        return true;
    }

    /// <summary>Lookup approved vendor by display name. Used by FC resend-invite.</summary>
    public Vendor? FindByName(string name) =>
        Vendors.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
}
