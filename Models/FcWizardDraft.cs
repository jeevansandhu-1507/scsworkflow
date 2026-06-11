namespace SCSPortal.Models;

/// <summary>
/// Mutable working state for the 9-step New Funding Commitment wizard.
/// Each step in the UI binds to fields here; on the final step the wizard
/// converts this into a FundingCommitment.
/// </summary>
public class FcWizardDraft
{
    // Context (inherited from ActiveContextService when the wizard opens)
    public AssociationType Association { get; set; } = AssociationType.Client;
    public string LinkedEntityId { get; set; } = "";
    public string LinkedEntityDisplay { get; set; } = "";
    public string? PermanentFundingId { get; set; }

    // Step 1 — Funding type + placement + plan dates + outcome + impact
    public string FundingTypeChoice { get; set; } = "";
    public string ResidentialPlacement { get; set; } = "";
    public string CsnLivingArrangement { get; set; } = "";
    public DateTime? PlanStart { get; set; }
    public DateTime? PlanEnd { get; set; }
    public string CurrentSituationOutcome { get; set; } = "";
    public string ImpactAndAlternatives { get; set; } = "";

    // Step 2 — Funders
    public string MainFunder { get; set; } = "MCCSS";
    public List<string> AdditionalFunders { get; set; } = new();
    public decimal AddlFunderAmount { get; set; }
    public bool AssignAddlAmountToVendor { get; set; }
    public decimal AddlFunderVendor { get; set; }

    // Step 3 — SCS team accountable
    public string ScsTeam { get; set; } = "";

    // Step 4 — MCCSS funding types + per-type plan amounts + per-type restriction
    public List<string> McsssTypes { get; set; } = new();
    public Dictionary<string, decimal> McsssAmounts { get; set; } = new();
    /// <summary>Per-funding-type restriction: "Restricted" or "Unrestricted".</summary>
    public Dictionary<string, string> McsssRestrictions { get; set; } = new();

    /// <summary>Back-compat: returns "Restricted" if any selected type is restricted, else "Unrestricted".</summary>
    public string McsssPlanType
    {
        get => McsssTypes.Any(t => RestrictionFor(t) == "Restricted") ? "Restricted" : "Unrestricted";
        set
        {
            // For legacy callers — apply this restriction to every selected type
            foreach (var t in McsssTypes) McsssRestrictions[t] = value ?? "Restricted";
        }
    }

    public string RestrictionFor(string fundingType) =>
        McsssRestrictions.TryGetValue(fundingType, out var r) && !string.IsNullOrEmpty(r) ? r : "Restricted";

    // Step 5 — Vendors
    public List<string> AssignedVendors { get; set; } = new();

    // Step 6 — Service lines
    public List<ServiceLine> Services { get; set; } = new();

    // Step 7 — Attachments (filenames mocked)
    public List<string> Attachments { get; set; } = new();
    public string Notes { get; set; } = "";

    // Step 8 — Review (no fields, derived view)

    // Step 9 — Confirm / submit-vs-save-draft choice
    public bool SubmitOnFinish { get; set; } = true;

    public decimal TotalPlan => McsssAmounts.Values.Sum();
    public decimal TotalCommitted => Services.Sum(s => s.LineTotal);
    public decimal RemainingPlan => TotalPlan - TotalCommitted;
}
