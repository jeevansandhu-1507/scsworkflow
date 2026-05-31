using SCSPortal.Models;

namespace SCSPortal.Services;

public class SchedulingService
{
    public List<Shift> Shifts { get; } = new();
    public event Action? OnChange;

    private readonly UserContextService _users;
    private readonly AuditService _audit;
    private readonly NotificationService _notify;

    public SchedulingService(UserContextService users, AuditService audit, NotificationService notify,
                             FundingCommitmentService fc)
    {
        _users = users; _audit = audit; _notify = notify;

        var rand = new Random(42);
        var supports = new[] { "Personal Development Support", "Community Participation Support", "In-Home Respite Services" };
        var today = DateTime.Today;

        // Generic seed shifts on a few real FCs spread across this and last month
        var sampleFcs = fc.Rows.Where(r => r.Status is CommitmentStatus.Approved or CommitmentStatus.Active or CommitmentStatus.Pending)
                               .Take(8).ToList();
        foreach (var f in sampleFcs)
        {
            for (int i = 0; i < 4; i++)
            {
                var d = today.AddDays(rand.Next(-15, 15));
                var startH = 8 + rand.Next(0, 8);
                Shifts.Add(new Shift
                {
                    Id = "sh-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    FcId = f.CommitId,
                    VendorId = "v-isw-01",
                    VendorName = f.Services.FirstOrDefault()?.Vendor ?? "Ima Worker",
                    ClientOrProjectName = f.Association == AssociationType.Project ? f.ClientName : f.ClientName,
                    SupportType = supports[rand.Next(supports.Length)],
                    Date = d, Start = new(startH, 0, 0), End = new(startH + 2, 0, 0),
                    Rate = f.Services.FirstOrDefault()?.Rate ?? 45,
                    Status = d < today ? (i % 2 == 0 ? ShiftStatus.Completed : ShiftStatus.Submitted) : ShiftStatus.Planned
                });
            }
        }

        // ---- Targeted ISW sample shifts so 'Ima Worker' can immediately test the convert flow ----
        // Tied to the two ISW-vendor FCs added in FundingCommitmentService.Seed().
        var iswFcs = new[] { "iswvendr1", "iswvendr2" };
        foreach (var commitId in iswFcs)
        {
            var f = fc.FindByCommitId(commitId);
            if (f == null) continue;
            foreach (var sv in f.Services)
            {
                // 2 Submitted (ready to convert) + 1 Completed + 1 Planned per service line
                AddIswShift(commitId, f.ClientName, sv.Name, sv.Rate, today.AddDays(-rand.Next(2, 8)),  ShiftStatus.Submitted);
                AddIswShift(commitId, f.ClientName, sv.Name, sv.Rate, today.AddDays(-rand.Next(2, 8)),  ShiftStatus.Submitted);
                AddIswShift(commitId, f.ClientName, sv.Name, sv.Rate, today.AddDays(-rand.Next(2, 8)),  ShiftStatus.Completed);
                AddIswShift(commitId, f.ClientName, sv.Name, sv.Rate, today.AddDays(rand.Next(1, 10)), ShiftStatus.Planned);
            }
        }
    }

    private void AddIswShift(string fcId, string clientName, string svc, decimal rate, DateTime date, ShiftStatus status)
    {
        var startH = 9;
        Shifts.Add(new Shift
        {
            Id = "sh-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            FcId = fcId,
            VendorId = "v-isw-01",
            VendorName = "Ima Worker",
            ClientOrProjectName = clientName,
            SupportType = svc,
            Date = date, Start = new(startH, 0, 0), End = new(startH + 2, 0, 0),
            Rate = rate,
            Status = status
        });
    }

    public IEnumerable<Shift> ForCurrentUser()
    {
        var u = _users.Effective;
        return u.Role switch
        {
            AppRole.VendorIsw or AppRole.VendorSp =>
                Shifts.Where(s => s.VendorName.Equals(u.FullName, StringComparison.OrdinalIgnoreCase)
                               || s.VendorName.Contains(u.FullName, StringComparison.OrdinalIgnoreCase)),
            AppRole.Client when u.LinkedClientId != null =>
                Shifts.Where(s => s.ClientOrProjectName.Contains(u.LinkedClientId)),
            _ => Shifts
        };
    }

    public void Submit(string shiftId)
    {
        var s = Shifts.FirstOrDefault(x => x.Id == shiftId);
        if (s == null || s.Status != ShiftStatus.Completed) return;
        s.Status = ShiftStatus.Submitted;
        _audit.Log("Shift.Submit", "Shift", s.Id, $"Shift {s.Date:MMM dd} {s.VendorName}",
                   detail: $"{s.SupportType} · {s.Total:C}");
        _notify.Push("Shift submitted", $"{s.VendorName} submitted shift on {s.Date:MMM dd}.",
                     NotificationLevel.Info, targetRole: AppRole.CaseManager);
        OnChange?.Invoke();
    }

    public void Add(Shift shift)
    {
        shift.Id = "sh-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        Shifts.Add(shift);
        _audit.Log("Shift.Create", "Shift", shift.Id, $"Shift {shift.Date:MMM dd} {shift.VendorName}");
        OnChange?.Invoke();
    }
}
