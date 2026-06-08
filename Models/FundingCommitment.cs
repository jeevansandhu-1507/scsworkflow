using System.Text.RegularExpressions;

namespace SCSPortal.Models;

public enum CommitmentStatus
{
    Draft,
    Pending,
    Awaiting,
    Special,
    Finance,
    Ministry,
    Csn,
    Approved,
    Active,
    Rejected
}

public class StatusInfo
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string CssClass { get; set; } = "";
    public string Swatch { get; set; } = "";

    public static StatusInfo For(CommitmentStatus s) => s switch
    {
        CommitmentStatus.Draft    => new() { Key = "draft",    Label = "In Draft",                  CssClass = "st-draft",    Swatch = "#2e5b73" },
        CommitmentStatus.Pending  => new() { Key = "pending",  Label = "Pending Approval",          CssClass = "st-pending",  Swatch = "#8a5a00" },
        CommitmentStatus.Awaiting => new() { Key = "awaiting", Label = "Awaiting Approval",         CssClass = "st-awaiting", Swatch = "#1b4a8c" },
        CommitmentStatus.Special  => new() { Key = "special",  Label = "Pending Special Approval",  CssClass = "st-special",  Swatch = "#8a1e3f" },
        CommitmentStatus.Finance  => new() { Key = "finance",  Label = "Pending Finance Approval",  CssClass = "st-finance",  Swatch = "#0f5e4a" },
        CommitmentStatus.Ministry => new() { Key = "ministry", Label = "Pending Ministry Approval", CssClass = "st-ministry", Swatch = "#4a3796" },
        CommitmentStatus.Csn      => new() { Key = "csn",      Label = "CSN Youth",                 CssClass = "st-csn",      Swatch = "#34495e" },
        CommitmentStatus.Active   => new() { Key = "active",   Label = "Active",                    CssClass = "st-active",   Swatch = "#1f6f24" },
        CommitmentStatus.Approved => new() { Key = "approved", Label = "Approved",                  CssClass = "st-approved", Swatch = "#0a5a36" },
        CommitmentStatus.Rejected => new() { Key = "rejected", Label = "Not Approved",              CssClass = "st-rejected", Swatch = "#8b1f1a" },
        _ => new() { Key = "draft", Label = "Unknown", CssClass = "st-draft", Swatch = "#2e5b73" }
    };

    public static readonly CommitmentStatus[] All =
    {
        CommitmentStatus.Draft, CommitmentStatus.Pending, CommitmentStatus.Awaiting,
        CommitmentStatus.Special, CommitmentStatus.Finance, CommitmentStatus.Ministry,
        CommitmentStatus.Csn, CommitmentStatus.Active, CommitmentStatus.Approved,
        CommitmentStatus.Rejected
    };
}

public class ServiceLine
{
    public string Name { get; set; } = "";
    public string Plan { get; set; } = "";   // MCCSS plan type mapping (restricted only)
    public string Provider { get; set; } = "";
    public string Vendor { get; set; } = "";
    public string Unit { get; set; } = "Hour";
    public decimal Rate { get; set; }
    public decimal Units { get; set; }
    public string Note { get; set; } = "";

    // Subform additions (matches "Add Service Funding Detail" dialog)
    public bool HasMaxRate { get; set; } = true;
    public bool HasMaxUnits { get; set; } = true;
    public bool IncludeHst { get; set; }
    public decimal? PlanningBaseAmount { get; set; }
    public string InvoiceSupportNote { get; set; } = "";

    // GL Mapping fields
    public int GlCode { get; set; }
    public string GlName { get; set; } = "";
    public string AccountId { get; set; } = "";

    public decimal LineTotal => Rate * Units;
    public decimal HstMultiplier => 1.0394m;
    public decimal LineTotalWithHst => IncludeHst ? Math.Round(LineTotal * HstMultiplier, 2) : LineTotal;

    public ServiceLine Clone() => new()
    {
        Name = Name, Plan = Plan, Provider = Provider, Vendor = Vendor,
        Unit = Unit, Rate = Rate, Units = Units, Note = Note,
        HasMaxRate = HasMaxRate, HasMaxUnits = HasMaxUnits, IncludeHst = IncludeHst,
        PlanningBaseAmount = PlanningBaseAmount, InvoiceSupportNote = InvoiceSupportNote,
        GlCode = GlCode, GlName = GlName, AccountId = AccountId
    };
}

public enum RowView { Detailed, Compact, Ultra }

public enum AssociationType { Client, Project }

public class FundingCommitment
{
    public string CommitId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Dob { get; set; } = "";
    public string Year { get; set; } = "2026";

    // Unified funding linkage (FRD §2.2)
    public AssociationType Association { get; set; } = AssociationType.Client;
    public string LinkedEntityId { get; set; } = "";   // Client.EtoId or Project.Id
    public string? PermanentFundingId { get; set; }
    public string? ParentFundingCommitmentId { get; set; }   // for rollover chain
    public string FiscalYear { get; set; } = "FY2026";
    public string FundingSource { get; set; } = "MCCSS";

    // FC version / lock state (FRD §5.6)
    public int Version { get; set; } = 1;
    public bool IsLocked => Status is CommitmentStatus.Approved or CommitmentStatus.Active or CommitmentStatus.Rejected;
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? SpecialApprovedAt { get; set; }
    public DateTime? FinanceApprovedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public string? SpecialApprovedBy { get; set; }
    public string? FinanceApprovedBy { get; set; }

    public CommitmentStatus Status { get; set; } = CommitmentStatus.Draft;

    public string FundingType { get; set; } = "Individual Funding";
    public string Placement { get; set; } = "Family Home";
    public string CsnLivingArrangement { get; set; } = "";
    public string Ministry { get; set; } = "Service Coordinator";
    public string PlanType { get; set; } = "Restricted";

    public string MainFunder { get; set; } = "MCCSS";
    public string AddlFunder { get; set; } = "— None —";
    public decimal AddlFunderAmount { get; set; }
    public decimal AddlFunderVendor { get; set; }

    public string PlanStart { get; set; } = "2026-04-01";
    public string PlanEnd { get; set; } = "2027-03-31";

    public List<string> MccssTypes { get; set; } = new();
    public Dictionary<string, decimal> PlanAmounts { get; set; } = new();

    public string Outcome { get; set; } = "";
    public string AltSources { get; set; } = "";

    public int Attachments { get; set; }
    public int Messages { get; set; }
    public int History { get; set; } = 1;

    public string Creator { get; set; } = "Jatin";
    public string Date { get; set; } = "May 12, 2026";
    public string Time { get; set; } = "10:30 AM";

    public List<ServiceLine> Services { get; set; } = new();

    public bool Selected { get; set; }
    public RowView? ViewOverride { get; set; }

    public decimal Total => Services.Sum(s => s.LineTotal);
    public decimal TotalPlan => PlanAmounts.Values.Sum();
    public bool OverBudget => TotalPlan > 0 && Total > TotalPlan;

    public string MccssLabel => string.Join(", ", MccssTypes);

    public FundingCommitment Clone()
    {
        return new FundingCommitment
        {
            CommitId = CommitId, ClientId = ClientId, ClientName = ClientName,
            Dob = Dob, Year = Year, Status = Status,
            Association = Association,
            LinkedEntityId = LinkedEntityId,
            PermanentFundingId = PermanentFundingId,
            ParentFundingCommitmentId = ParentFundingCommitmentId,
            FiscalYear = FiscalYear,
            FundingSource = FundingSource,
            Version = Version,
            FundingType = FundingType, Placement = Placement, CsnLivingArrangement = CsnLivingArrangement, Ministry = Ministry, PlanType = PlanType,
            MainFunder = MainFunder, AddlFunder = AddlFunder,
            AddlFunderAmount = AddlFunderAmount, AddlFunderVendor = AddlFunderVendor,
            PlanStart = PlanStart, PlanEnd = PlanEnd,
            MccssTypes = new List<string>(MccssTypes),
            PlanAmounts = new Dictionary<string, decimal>(PlanAmounts),
            Outcome = Outcome, AltSources = AltSources,
            Attachments = Attachments, Messages = Messages, History = History,
            Creator = Creator, Date = Date, Time = Time,
            Services = Services.Select(s => s.Clone()).ToList(),
            Selected = false, ViewOverride = null
        };
    }
}

public static class Catalog
{
    // Wizard step 1 — funding type (matches dev.portal screenshot)
    public static readonly string[] FundingTypeChoices =
    {
        "In-Year MYSLP Residential",
        "Regional / TPR transfer",
        "CSN Youth",
        "None of the above"
    };

    // Wizard step 1 — residential placement type
    public static readonly string[] ResidentialPlacements =
    {
        "Group Living",
        "Specialized Accommodations",
        "Host Family",
        "Supported Independent Living",
        "Lives with caregiver or guardian",
        "None of the above"
    };

    // CSN Living Arrangements
    public static readonly string[] CsnLivingArrangements =
    {
        "Lives with caregiver or guardian",
        "Lives with service provider (operator)"
    };

    // Wizard step 2 — main funder choice (matches dev.portal screenshot)
    public static readonly string[] MainFunderChoices =
    {
        "MCCSS", "Passport One (Family Services Ontario)", "SSAH", "CHEO", "ODSP"
    };

    // Wizard step 2 — additional funders (multi-select chips)
    public static readonly string[] AddlFunderChoices =
    {
        "MCCSS", "Passport One (Family Services Ontario)", "SSAH", "CHEO", "ODSP"
    };

    // Wizard step 3 — SCS team accountable (conditional on funder)
    public static readonly string[] ScsTeams =
    {
        "Children's Case Management (CCM)",
        "Adult Case Management (ACM)",
        "Residential And Community Services (RCS)",
        "Program Supervisor (Special Approver)",
        "Finance/Admin"
    };

    // Wizard step 4 — MCCSS funding type chips (matches screenshot)
    public static readonly string[] MccssFundingTypeChoices =
    {
        "Temporary Funding Allocation - Children's",
        "Temporary Funding - Autism Spectrum Disorder (ASD) Allocation",
        "Temporary Funding Allocation - Community Enhancement (CEF)",
        "Temporary Funding Allocation (Adult)",
        "Temporary Flexible Funding Allocation (Adult)",
        "Historical Respite Funding Allocation",
        "MCCSS Fiscal Community Participation Funding Allocation",
        "MCCSS Fiscal Residential Funding Allocation",
        "Passport Funding"
    };

    // Wizard step 4 — MCCSS plan type dropdown
    public static readonly string[] McsssPlanTypes =
    {
        "Restricted", "Unrestricted"
    };

    // Wizard step 5 — service names (richer set matching prototype subform)
    public static readonly string[] WizardServiceNames =
    {
        "Individualized Staffing",
        "Personal Development Support",
        "Behavioural Support Services",
        "Community Participation Support",
        "Caregiver Support / Education",
        "In-Home Respite Services",
        "Out of Home Caregiver Respite Services and Support",
        "Therapy Services",
        "Transportation Support",
        "Skills Development Coaching"
    };

    // Provider types (matches prototype)
    public static readonly string[] WizardProviderTypes =
    {
        "Independent Support Worker",
        "Service Provider Organization",
        "Direct Family",
        "Self-managed",
        "Agency-managed"
    };

    // Unit types in subform
    public static readonly string[] WizardUnits =
    {
        "Hour", "Day", "Session", "Visit", "Item", "Week", "Month", "One-time"
    };

    // --- Legacy lists used by the busy queue card view; left in place for back-compat ---
    public static readonly string[] FundingTypes =
    {
        "None of the above", "Individual Funding", "Group Funding", "Pooled Funding", "Cross-Service Funding"
    };

    public static readonly string[] Placements =
    {
        "Group Living", "Family Home", "Independent Living", "Supported Living", "Foster Care", "Other"
    };

    public static readonly string[] MainFunders =
    {
        "MCCSS", "MOH (Health)", "MOE (Education)", "Indigenous Services", "Municipal", "Other"
    };

    public static readonly string[] AddlFunders =
    {
        "— None —", "Passport One (Family Services Ontario)", "Trillium Foundation",
        "United Way", "Local Municipality", "Family Contribution"
    };

    public static readonly string[] Ministries =
    {
        "Finance / Admin", "Service Coordinator", "Intake Team",
        "Clinical Team", "Resource Allocation", "MCCSS Liaison"
    };

    public static readonly string[] MccssFunding =
    {
        "Temporary Funding Allocation - Children's",
        "Permanent Funding Allocation - Children's",
        "Temporary Funding Allocation - Adult",
        "Special Services at Home (SSAH)",
        "Passport Funding"
    };

    public static readonly string[] PlanTypes = { "Restricted", "Unrestricted" };

    // PROVIDERS
    public static readonly string[] Providers =
    {
        "Independent Support Worker", "Service Provider Organization",
        "Direct Family", "Self-managed"
    };

    // SERVICES
    public static readonly string[] Services =
    {
        "Out of Home Caregiver Respite Services and Support",
        "In-Home Respite Services",
        "Personal Development Support",
        "Behavioural Support Services",
        "Community Participation Support",
        "Caregiver Support / Education"
    };

    // UNITS
    public static readonly string[] Units = { "Session", "Hour", "Day", "Visit", "Item" };

    // VENDORS
    public static readonly string[] Vendors =
    {
        "Jatin Test", "Maple Care Inc.", "BrightPath Services",
        "Northern Light Co.", "Aurora Support Co-op", "Unassigned"
    };

    // CREATORS for default attribution
    public static readonly string[] Creators =
    {
        "Fatoumata Diallo", "Bhavesh Mishra", "Parth Mehta", "Jati K", "Aisha Khan", "Lin Chen"
    };

    // 12-entry palette matching JS CLIENT_COLORS exactly
    public static readonly (string, string)[] ClientColors =
    {
        ("#1f6a87","#2c87a3"), ("#0e4a64","#266f8a"), ("#155670","#5da0b5"),
        ("#1f6f24","#3aa040"), ("#8a5a00","#cf9a2a"), ("#4a3796","#6a52c2"),
        ("#0a5a36","#147a4d"), ("#8b1f1a","#b53028"), ("#1b4a8c","#3a73c4"),
        ("#34495e","#566a7f"), ("#0f5e4a","#2a8a6f"), ("#155670","#2c87a3")
    };

    public static (string c1, string c2) ClientColor(string id)
    {
        // Mirrors JS: h = ((h<<5) - h + ch) | 0
        int h = 0;
        foreach (var ch in id)
        {
            // Use 32-bit signed int arithmetic to match JS bitwise behavior
            unchecked
            {
                h = ((h << 5) - h + ch) | 0;
            }
        }
        var idx = Math.Abs(h) % ClientColors.Length;
        return ClientColors[idx];
    }

    public static string NormalizePlanType(string? pt)
    {
        if (pt == "Restricted" || pt == "Unrestricted") return pt;
        if (pt == "Capped") return "Restricted";
        return "Unrestricted";
    }

    // shortFundingLabel exactly mirrors prototype regex chain
    public static string ShortFundingLabel(string t)
    {
        if (string.IsNullOrEmpty(t)) return "—";
        if (Regex.IsMatch(t, @"ASD|Autism", RegexOptions.IgnoreCase)) return "ASD";
        if (Regex.IsMatch(t, @"Community Enhancement|CEF", RegexOptions.IgnoreCase)) return "CEF";
        if (Regex.IsMatch(t, @"SSAH|Special Services at Home", RegexOptions.IgnoreCase)) return "SSAH";
        if (Regex.IsMatch(t, @"Passport", RegexOptions.IgnoreCase)) return "Passport";
        if (Regex.IsMatch(t, @"Children", RegexOptions.IgnoreCase) && Regex.IsMatch(t, @"Temporary", RegexOptions.IgnoreCase))
            return "Children's · Temp";
        if (Regex.IsMatch(t, @"Children", RegexOptions.IgnoreCase) && Regex.IsMatch(t, @"Permanent", RegexOptions.IgnoreCase))
            return "Children's · Perm";
        if (Regex.IsMatch(t, @"Adult", RegexOptions.IgnoreCase) && Regex.IsMatch(t, @"Temporary", RegexOptions.IgnoreCase))
            return "Adult · Temp";
        if (Regex.IsMatch(t, @"Adult", RegexOptions.IgnoreCase) && Regex.IsMatch(t, @"Permanent", RegexOptions.IgnoreCase))
            return "Adult · Perm";
        return string.Join(" ", t.Split(' ').Take(3));
    }
}

public enum FilterKey { All, Draft, Pending, Approved, Rejected }

public enum BulkMccssMode { Replace, Add, Remove }

public class BulkChanges
{
    public bool EnableStatus { get; set; }
    public CommitmentStatus Status { get; set; } = CommitmentStatus.Draft;

    public bool EnablePlanPeriod { get; set; }
    public string PlanStart { get; set; } = "";
    public string PlanEnd { get; set; } = "";

    public bool EnableMccss { get; set; }
    public BulkMccssMode MccssMode { get; set; } = BulkMccssMode.Replace;
    public List<string> MccssTypes { get; set; } = new();

    public bool EnablePlanType { get; set; }
    public string PlanType { get; set; } = "Restricted";

    public bool EnableMinistry { get; set; }
    public string Ministry { get; set; } = "Service Coordinator";

    public bool EnableOutcome { get; set; }
    public string Outcome { get; set; } = "";

    public bool HasAnyChange =>
        EnableStatus || EnablePlanPeriod ||
        (EnableMccss && MccssTypes.Count > 0) ||
        EnablePlanType || EnableMinistry || EnableOutcome;
}
