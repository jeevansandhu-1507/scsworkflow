using SCSPortal.Models;

namespace SCSPortal.Services;

public class FinancialPressureService
{
    public List<FinancialPressureRequest> Requests { get; } = new();
    public event Action? OnChange;
    private int _seq = 100;

    private readonly UserContextService _users;
    private readonly AuditService _audit;
    private readonly NotificationService _notify;

    public FinancialPressureService(UserContextService users, AuditService audit, NotificationService notify)
    {
        _users = users; _audit = audit; _notify = notify;

        // Seed a few sample requests at different stages
        Add("BrightPath Services", "f8065e44", AssociationType.Client, "Sarah Chen",
            1200, "In-Home Respite Services", "Family requires extra weekend coverage in May.",
            FpPriority.Normal, FpStatus.CmReview);
        Add("Maple Care Inc.", "1c8e92aa", AssociationType.Client, "Priya Sharma",
            2400, "Behavioural Support Services", "Behaviour plan escalation; specialist hours required.",
            FpPriority.High, FpStatus.FinanceReview);
        Add("Northern Light Co.", "prj78a01", AssociationType.Project, "Northern Outreach",
            8500, "Community Participation Support", "Add summer programming for two new cohorts.",
            FpPriority.Urgent, FpStatus.Submitted);
    }

    private void Add(string vendor, string fcId, AssociationType assoc, string linked,
                     decimal amount, string support, string justify, FpPriority prio, FpStatus status)
    {
        Requests.Add(new FinancialPressureRequest
        {
            Id = "fp-" + (++_seq),
            VendorId = "v-sp-01",
            VendorName = vendor,
            FcId = fcId,
            Association = assoc,
            LinkedEntityName = linked,
            RequestedAmount = amount,
            SupportType = support,
            Justification = justify,
            Priority = prio,
            Status = status,
            Created = DateTime.Now.AddDays(-(_seq - 100))
        });
    }

    public FinancialPressureRequest Submit(string vendor, string fcId, AssociationType assoc, string linked,
                                          decimal amount, string support, string justify, FpPriority prio)
    {
        var fp = new FinancialPressureRequest
        {
            Id = "fp-" + (++_seq),
            VendorId = _users.Effective.LinkedVendorId ?? "v-sp-01",
            VendorName = vendor, FcId = fcId,
            Association = assoc, LinkedEntityName = linked,
            RequestedAmount = amount, SupportType = support,
            Justification = justify, Priority = prio,
            Status = FpStatus.Submitted
        };
        Requests.Insert(0, fp);
        _audit.Log("FP.Submit", "FinancialPressure", fp.Id, $"FP {fp.Id}",
                   detail: $"{vendor} requests {amount:C} for {linked}");
        _notify.Push("Financial Pressure request",
                     $"{vendor} requested {amount:C} for {linked}.",
                     NotificationLevel.Warning,
                     targetRole: AppRole.CaseManager);
        OnChange?.Invoke();
        return fp;
    }

    public void CmReview(string id, string comment)
    {
        var r = Find(id); if (r == null) return;
        r.CmComment = comment;
        r.Status = FpStatus.FinanceReview;
        _audit.Log("FP.CmReview", "FinancialPressure", r.Id, $"FP {r.Id}", detail: comment);
        _notify.Push("FP cleared by Case Manager",
                     $"{r.VendorName} request for {r.LinkedEntityName} awaits Finance review.",
                     NotificationLevel.Info, targetRole: AppRole.Finance);
        OnChange?.Invoke();
    }

    public void FinanceApprove(string id, string comment)
    {
        var r = Find(id); if (r == null) return;
        r.FinanceComment = comment;
        r.Status = FpStatus.Approved;
        _audit.Log("FP.Approve", "FinancialPressure", r.Id, $"FP {r.Id}", detail: comment);
        _notify.Push("Financial Pressure approved",
                     $"{r.VendorName}: {r.RequestedAmount:C} approved.",
                     NotificationLevel.Success);
        OnChange?.Invoke();
    }

    public void Reject(string id, string reason)
    {
        var r = Find(id); if (r == null) return;
        r.Status = FpStatus.Rejected;
        r.FinanceComment = reason;
        _audit.Log("FP.Reject", "FinancialPressure", r.Id, $"FP {r.Id}", detail: reason);
        _notify.Push("Financial Pressure rejected",
                     $"{r.VendorName}: {reason}", NotificationLevel.Danger);
        OnChange?.Invoke();
    }

    private FinancialPressureRequest? Find(string id) => Requests.FirstOrDefault(x => x.Id == id);
}
