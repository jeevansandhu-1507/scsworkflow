namespace SCSPortal.Models;

// ---------- Clients (FRD §4 / ETO) ----------

public enum InviteStatus { NotInvited, Invited, Accepted, Expired }

public class Client
{
    public string EtoId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Dob { get; set; } = "";
    public string Guardian { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string FundingStatus { get; set; } = "Permanent";

    public InviteStatus InviteStatus { get; set; } = InviteStatus.NotInvited;
    public DateTime? LastInvitedAt { get; set; }
    public int InviteCount { get; set; }
    public DateTime? InviteAcceptedAt { get; set; }
}

// ---------- Projects (FRD §5.2 project-based flow) ----------

public class Project
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Sponsor { get; set; } = "";
    public string Status { get; set; } = "Active";
    public string ManagerName { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
}

// ---------- Permanent Funding (FRD §6) ----------

public enum PermanentFundingStatus { Active, OnHold, Closed }

public class PermanentFunding
{
    public string Id { get; set; } = "";
    public string Reference { get; set; } = "";   // e.g. PF-2026-001
    public AssociationType Association { get; set; }
    public string LinkedEntityId { get; set; } = "";
    public string LinkedEntityName { get; set; } = "";   // denormalised display label
    public string FiscalYear { get; set; } = "FY2026";
    public string FundingSource { get; set; } = "MCCSS";
    public string Ministry { get; set; } = "Service Coordinator";
    public string Program { get; set; } = "Passport Funding";
    public decimal BudgetAmount { get; set; }
    public PermanentFundingStatus Status { get; set; } = PermanentFundingStatus.Active;
    public bool RolloverEligible { get; set; } = true;
    public bool AlreadyRolledOver { get; set; }
    public DateTime EffectiveStart { get; set; } = new(2026, 4, 1);
    public DateTime EffectiveEnd { get; set; } = new(2027, 3, 31);
    public string CreatedBy { get; set; } = "Finance";
    public DateTime CreatedAt { get; set; } = DateTime.Now.AddDays(-30);
    public string Notes { get; set; } = "";

    // ---- New fields for the prototype PF Edit form ----
    /// <summary>Source FC that triggered the PF creation (Add Permanent Funding from FC).</summary>
    public string? SourceFundingCommitmentId { get; set; }
    /// <summary>e.g. 'Draft', 'Pending Ministry Approval', 'Approved'.</summary>
    public string FundingStatusLabel { get; set; } = "Draft";
    public string ResidentialPlacementType { get; set; } = "";
    public string ProjectActiveStatus { get; set; } = "";

    public string FundingTargetType { get; set; } = "Client"; // Client, Vendor, Finance
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }

    /// <summary>Allocation types selected (Residential, CP, Operational, Passport).</summary>
    public List<string> AllocationTypes { get; set; } = new();

    public decimal MinistryApprovedAllocationResidential { get; set; }
    public decimal SCSPermanentPressureApprovalResidential { get; set; }
    public decimal TotalApprovedPermanentAllocationResidential => MinistryApprovedAllocationResidential + SCSPermanentPressureApprovalResidential;

    public decimal MinistryApprovedAllocationCP { get; set; }
    public decimal SCSPermanentPressureApprovalCP { get; set; }
    public decimal TotalApprovedPermanentAllocationCP => MinistryApprovedAllocationCP + SCSPermanentPressureApprovalCP;

    public decimal MinistryApprovedCMAllocationAmount { get; set; }
    public decimal MinistryApprovedACAAllocationAmount { get; set; }
    public decimal TotalMinistryApprovedOperationalLoad => MinistryApprovedCMAllocationAmount + MinistryApprovedACAAllocationAmount;

    public decimal ApprovedPassportAllocation { get; set; }
    public decimal HistoricalRespiteAllocation { get; set; }

    public string JustificationCategory { get; set; } = "";
    public string MinistryApprovalAttachment { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>Computed Total Approved Permanent Allocation across all allocation types.</summary>
    public decimal TotalApprovedAllocation =>
        TotalApprovedPermanentAllocationResidential +
        TotalApprovedPermanentAllocationCP +
        TotalMinistryApprovedOperationalLoad +
        ApprovedPassportAllocation +
        HistoricalRespiteAllocation;
}

public static class PfCatalog
{
    public static readonly string[] FundingStatusLabels =
    {
        "Draft",
        "Pending Ministry Approval",
        "Approved",
        "On Hold",
        "Closed",
        "CSN Youth"
    };

    public static readonly string[] AllocationTypes =
    {
        "Residential",
        "CP",
        "Operational",
        "Passport"
    };

    public static readonly string[] JustificationCategories =
    {
        "New client onboarding",
        "Permanent funding conversion",
        "Out-of-year supplement",
        "Renewal of existing commitment",
        "Ministry-directed allocation",
        "Other"
    };

    public static readonly string[] ProjectActiveStatuses =
    {
        "Active", "On Hold", "Closed", "N/A"
    };
}

// ---------- Vendors ----------

public enum VendorType { Isw, Sp }
public enum VendorStatus { Pending, Approved, Rejected, Inactive }

public class Vendor
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public VendorType Type { get; set; }
    public string Contact { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public VendorStatus Status { get; set; } = VendorStatus.Approved;
    public string? BusinessNumber { get; set; }
    public string? Address { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.Now;
    public string SubmittedBy { get; set; } = "";
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    public InviteStatus InviteStatus { get; set; } = InviteStatus.NotInvited;
    public DateTime? LastInvitedAt { get; set; }
    public int InviteCount { get; set; }
    public DateTime? InviteAcceptedAt { get; set; }

    public List<string> AssignedClientIds { get; set; } = new();
    public List<string> AssignedProjectIds { get; set; } = new();
    public List<string> SupportTypes { get; set; } = new();
}

// ---------- Users & Roles (FRD §3, §11) ----------

public enum AppRole
{
    Admin,
    Finance,
    CaseManager,
    SpecialApprover,
    Client,
    VendorIsw,
    VendorSp
}

public static class RoleInfo
{
    public static string Label(AppRole r) => r switch
    {
        AppRole.Admin => "SCS Admin",
        AppRole.Finance => "SCS Finance",
        AppRole.CaseManager => "Case Manager",
        AppRole.SpecialApprover => "Special Approver",
        AppRole.Client => "Client",
        AppRole.VendorIsw => "Vendor · ISW",
        AppRole.VendorSp => "Vendor · SP",
        _ => r.ToString()
    };

    public static string Code(AppRole r) => r switch
    {
        AppRole.Admin => "AD",
        AppRole.Finance => "FN",
        AppRole.CaseManager => "CM",
        AppRole.SpecialApprover => "SA",
        AppRole.Client => "CL",
        AppRole.VendorIsw => "IS",
        AppRole.VendorSp => "SP",
        _ => "??"
    };

    public static bool IsInternal(AppRole r) =>
        r is AppRole.Admin or AppRole.Finance or AppRole.CaseManager or AppRole.SpecialApprover;
}

public class AppUser
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public AppRole Role { get; set; }
    public string Email { get; set; } = "";
    public bool Active { get; set; } = true;
    public string? LinkedClientId { get; set; }     // for Client role
    public string? LinkedVendorId { get; set; }     // for Vendor / ISW
}

// ---------- Audit (FRD §14) ----------

public class AuditEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Actor { get; set; } = "";
    public AppRole ActorRole { get; set; }
    public string? ImpersonatedBy { get; set; }
    public string Action { get; set; } = "";        // e.g. "FC.Submit"
    public string EntityType { get; set; } = "";    // e.g. "FundingCommitment"
    public string EntityId { get; set; } = "";
    public string EntityLabel { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string? Ip { get; set; }
    public string Detail { get; set; } = "";
}

// ---------- Notifications (FRD §13) ----------

public enum NotificationLevel { Info, Success, Warning, Danger }

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public DateTime When { get; set; } = DateTime.Now;
    public NotificationLevel Level { get; set; } = NotificationLevel.Info;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? TargetUserId { get; set; }       // null = broadcast
    public AppRole? TargetRole { get; set; }
    public string? Link { get; set; }
    public bool Read { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

// ---------- Financial Pressure Requests (FRD §10) ----------

public enum FpStatus { Draft, Submitted, CmReview, FinanceReview, Approved, Rejected }
public enum FpPriority { Low, Normal, High, Urgent }

public class FinancialPressureRequest
{
    public string Id { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string FcId { get; set; } = "";
    public AssociationType Association { get; set; }
    public string LinkedEntityName { get; set; } = "";
    public decimal RequestedAmount { get; set; }
    public string SupportType { get; set; } = "";
    public string Justification { get; set; } = "";
    public FpPriority Priority { get; set; } = FpPriority.Normal;
    public FpStatus Status { get; set; } = FpStatus.Submitted;
    public DateTime Created { get; set; } = DateTime.Now;
    public List<string> Attachments { get; set; } = new();
    public string? CmComment { get; set; }
    public string? FinanceComment { get; set; }
}

// ---------- Scheduling (FRD §8) ----------

public enum ShiftStatus { Planned, Completed, Submitted, Invoiced, Cancelled }

public class Shift
{
    public string Id { get; set; } = "";
    public string FcId { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string VendorName { get; set; } = "";
    public string ClientOrProjectName { get; set; } = "";
    public string SupportType { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public TimeSpan Start { get; set; } = new(9, 0, 0);
    public TimeSpan End { get; set; } = new(11, 0, 0);
    public decimal Hours => (decimal)(End - Start).TotalHours;
    public decimal Rate { get; set; } = 45;
    public decimal Total => Hours * Rate;
    public ShiftStatus Status { get; set; } = ShiftStatus.Planned;
    public string Note { get; set; } = "";
}

// ---------- Invoices (FRD §9) ----------

public enum InvoiceStatus { Draft, Submitted, Approved, Paid, Rejected }

public class InvoiceLine
{
    public string Description { get; set; } = "";
    public string SupportType { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount => Quantity * Rate;
}

public class Invoice
{
    public string Id { get; set; } = "";
    public string Number { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string VendorName { get; set; } = "";
    public VendorType VendorType { get; set; }
    public string FcId { get; set; } = "";
    public AssociationType Association { get; set; }
    public string LinkedEntityName { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Today;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public List<InvoiceLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Amount);
    public string? RejectionReason { get; set; }
}

// ---------- Locale (FRD §13 bilingual) ----------

public enum AppLocale { En, Fr }
