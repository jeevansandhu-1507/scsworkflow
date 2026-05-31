using SCSPortal.Models;

namespace SCSPortal.Services;

public class InvoicingService
{
    public List<Invoice> Invoices { get; } = new();
    public event Action? OnChange;
    private int _seq = 1000;

    private readonly UserContextService _users;
    private readonly AuditService _audit;
    private readonly NotificationService _notify;

    public InvoicingService(UserContextService users, AuditService audit, NotificationService notify,
                            SchedulingService scheduling, FundingCommitmentService fc)
    {
        _users = users; _audit = audit; _notify = notify;

        // Seed a few invoices
        var fcs = fc.Rows.Where(r => r.Status == CommitmentStatus.Approved).Take(3).ToList();
        var ix = 0;
        foreach (var f in fcs)
        {
            var inv = new Invoice
            {
                Id = "inv-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                Number = "INV-" + (++_seq),
                VendorId = "v-isw-01",
                VendorName = f.Services.FirstOrDefault()?.Vendor ?? "Maple Care Inc.",
                VendorType = ix % 2 == 0 ? VendorType.Isw : VendorType.Sp,
                FcId = f.CommitId,
                Association = f.Association,
                LinkedEntityName = f.ClientName,
                Status = ix == 0 ? InvoiceStatus.Submitted : ix == 1 ? InvoiceStatus.Approved : InvoiceStatus.Paid,
                Date = DateTime.Today.AddDays(-ix * 3),
                Lines = new List<InvoiceLine>
                {
                    new() { Description = "Support hours", SupportType = "Personal Development Support",
                            Quantity = 10 + ix * 2, Rate = 45 }
                }
            };
            Invoices.Add(inv);
            ix++;
        }
    }

    public IEnumerable<Invoice> ForCurrentUser()
    {
        var u = _users.Effective;
        return u.Role switch
        {
            AppRole.VendorIsw or AppRole.VendorSp =>
                Invoices.Where(i => i.VendorName.Equals(u.FullName, StringComparison.OrdinalIgnoreCase)
                                 || i.VendorName.Contains(u.FullName, StringComparison.OrdinalIgnoreCase)),
            AppRole.Client when u.LinkedClientId != null =>
                Invoices.Where(i => i.LinkedEntityName.Contains(u.LinkedClientId)),
            _ => Invoices
        };
    }

    public Invoice CreateFromShifts(IEnumerable<Shift> shifts, string fcId, string vendorName)
    {
        var inv = new Invoice
        {
            Id = "inv-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            Number = "INV-" + (++_seq),
            VendorId = _users.Effective.LinkedVendorId ?? "v-isw-01",
            VendorName = vendorName,
            VendorType = VendorType.Isw,
            FcId = fcId,
            Status = InvoiceStatus.Draft,
            Lines = shifts.Select(s => new InvoiceLine
            {
                Description = $"{s.SupportType} · {s.Date:MMM dd}",
                SupportType = s.SupportType,
                Quantity = s.Hours,
                Rate = s.Rate
            }).ToList()
        };
        Invoices.Insert(0, inv);
        _audit.Log("Invoice.Create", "Invoice", inv.Id, inv.Number, detail: $"From {inv.Lines.Count} shifts");
        OnChange?.Invoke();
        return inv;
    }

    public Invoice CreateManual(string vendorName, string fcId, IEnumerable<InvoiceLine> lines)
    {
        var inv = new Invoice
        {
            Id = "inv-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            Number = "INV-" + (++_seq),
            VendorName = vendorName, VendorType = VendorType.Sp,
            FcId = fcId, Status = InvoiceStatus.Draft,
            Lines = lines.ToList()
        };
        Invoices.Insert(0, inv);
        _audit.Log("Invoice.Create", "Invoice", inv.Id, inv.Number, detail: "Manual SP entry");
        OnChange?.Invoke();
        return inv;
    }

    public void Submit(string id)
    {
        var i = Invoices.FirstOrDefault(x => x.Id == id);
        if (i == null || i.Status != InvoiceStatus.Draft) return;
        i.Status = InvoiceStatus.Submitted;
        _audit.Log("Invoice.Submit", "Invoice", i.Id, i.Number);
        _notify.Push("Invoice submitted", $"{i.Number} ({i.Total:C}) submitted by {i.VendorName}.",
                     NotificationLevel.Info, targetRole: AppRole.Finance);
        OnChange?.Invoke();
    }

    public void Approve(string id)
    {
        var i = Invoices.FirstOrDefault(x => x.Id == id);
        if (i == null || i.Status != InvoiceStatus.Submitted) return;
        i.Status = InvoiceStatus.Approved;
        _audit.Log("Invoice.Approve", "Invoice", i.Id, i.Number);
        OnChange?.Invoke();
    }

    public void Reject(string id, string reason)
    {
        var i = Invoices.FirstOrDefault(x => x.Id == id);
        if (i == null) return;
        i.Status = InvoiceStatus.Rejected;
        i.RejectionReason = reason;
        _audit.Log("Invoice.Reject", "Invoice", i.Id, i.Number, detail: reason);
        OnChange?.Invoke();
    }

    public void MarkPaid(string id)
    {
        var i = Invoices.FirstOrDefault(x => x.Id == id);
        if (i == null || i.Status != InvoiceStatus.Approved) return;
        i.Status = InvoiceStatus.Paid;
        _audit.Log("Invoice.Pay", "Invoice", i.Id, i.Number);
        OnChange?.Invoke();
    }
}
