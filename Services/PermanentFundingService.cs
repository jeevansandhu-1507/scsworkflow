using SCSPortal.Models;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCSPortal.Services;

public class PermanentFundingService
{
    private List<PermanentFunding> _records = new();
    public List<PermanentFunding> Records
    {
        get
        {
            EnsureLoaded();
            return _records;
        }
    }
    public event Action? OnChange;
    private readonly AuditService? _audit;
    private readonly UserContextService? _users;
    private readonly IJSRuntime? _js;

    public PermanentFundingService() { Seed(); }

    public PermanentFundingService(AuditService audit, UserContextService users, IJSRuntime js)
    {
        _audit = audit; _users = users; _js = js;
    }

    private bool _loaded = false;
    private void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        if (_js is IJSInProcessRuntime jsInProcess)
        {
            try
            {
                var json = jsInProcess.Invoke<string>("localStorage.getItem", "scs_permanent_fundings");
                if (!string.IsNullOrEmpty(json))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<PermanentFunding>>(json);
                    if (list != null)
                    {
                        _records = list;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading permanent fundings: " + ex.Message);
            }
        }

        Seed();
        Save();
    }

    private void Save()
    {
        if (_js is IJSInProcessRuntime jsInProcess)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_records);
                jsInProcess.InvokeVoid("localStorage.setItem", "scs_permanent_fundings", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving permanent fundings: " + ex.Message);
            }
        }
    }

    private void Seed()
    {
        // Seed PF anchored to the FC seed data so the relationships render.
        // Client PFs (ClientIds line up with seeded FundingCommitments)
        AddClient("pf-001", "PF-2026-001", "23343", "bill test",         604443, "Passport Funding");
        AddClient("pf-002", "PF-2026-002", "84291", "Sarah Chen",          150, "Passport Funding");
        AddClient("pf-003", "PF-2026-003", "67234", "Marcus Williams",     800, "Passport Funding");
        AddClient("pf-004", "PF-2026-004", "20716", "Hazel Wong",        14600, "Permanent Funding Allocation - Children's");
        AddClient("pf-005", "PF-2026-005", "10245", "Avery Thompson",    35300, "Passport Funding");
        AddClient("pf-006", "PF-2026-006", "82640", "Amélie Tremblay",    1650, "Passport Funding");

        // Project PFs
        AddProject("pf-101", "PF-PRJ-2026-001", "p-001", "Northern Outreach",        125000, "Special Services at Home (SSAH)");
        AddProject("pf-102", "PF-PRJ-2026-002", "p-002", "Family Respite Pilot",      85000, "Passport Funding");
        AddProject("pf-103", "PF-PRJ-2026-003", "p-004", "Inclusive Living",         150000, "Permanent Funding Allocation - Adult");

        // Mark one as already-rolled-over for demo
        var rolled = _records.FirstOrDefault(r => r.Id == "pf-006");
        if (rolled != null)
        {
            rolled.AlreadyRolledOver = true;
        }
    }

    private void AddClient(string id, string reference, string clientId, string clientName, decimal budget, string program)
    {
        _records.Add(new PermanentFunding
        {
            Id = id, Reference = reference,
            Association = AssociationType.Client,
            LinkedEntityId = clientId, LinkedEntityName = clientName,
            BudgetAmount = budget, Program = program,
            CreatedBy = "Fatima Finance",
            CreatedAt = DateTime.Now.AddDays(-45)
        });
    }

    private void AddProject(string id, string reference, string projectId, string projectName, decimal budget, string program)
    {
        _records.Add(new PermanentFunding
        {
            Id = id, Reference = reference,
            Association = AssociationType.Project,
            LinkedEntityId = projectId, LinkedEntityName = projectName,
            BudgetAmount = budget, Program = program,
            CreatedBy = "Fatima Finance",
            CreatedAt = DateTime.Now.AddDays(-60)
        });
    }

    public PermanentFunding? FindById(string id) => Records.FirstOrDefault(p => p.Id == id);

    public IEnumerable<PermanentFunding> EligibleForRollover() =>
        Records.Where(r => r.Status == PermanentFundingStatus.Active
                        && r.RolloverEligible
                        && !r.AlreadyRolledOver);

    /// <summary>Create a Permanent Funding record, optionally linked to a source FC.</summary>
    public PermanentFunding Create(PermanentFunding pf, bool submitForApproval)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(pf.Id))
            pf.Id = "pf-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        if (string.IsNullOrEmpty(pf.Reference))
            pf.Reference = $"PF-{DateTime.Now.Year}-{(Records.Count + 1):000}";
        pf.CreatedBy = _users?.Effective.FullName ?? "Finance";
        pf.CreatedAt = DateTime.Now;
        pf.UpdatedAt = DateTime.Now;
        pf.FundingStatusLabel = submitForApproval ? "Pending Ministry Approval" : "Draft";
        pf.Status = submitForApproval ? PermanentFundingStatus.OnHold : PermanentFundingStatus.Active;
        pf.BudgetAmount = pf.TotalApprovedAllocation;
        _records.Insert(0, pf);
        Save();

        _audit?.Log(
            action: submitForApproval ? "PF.SubmitForApproval" : "PF.Create",
            entityType: "PermanentFunding",
            entityId: pf.Id,
            entityLabel: $"{pf.Reference} · {pf.LinkedEntityName}",
            after: pf.FundingStatusLabel,
            detail: $"Created with {pf.AllocationTypes.Count} allocation type(s), total {pf.TotalApprovedAllocation:C}");

        OnChange?.Invoke();
        return pf;
    }

    public void Update(PermanentFunding pf)
    {
        EnsureLoaded();
        var existing = FindById(pf.Id);
        if (existing == null) return;
        var before = existing.FundingStatusLabel;
        pf.UpdatedAt = DateTime.Now;
        pf.BudgetAmount = pf.TotalApprovedAllocation;
        var ix = _records.IndexOf(existing);
        _records[ix] = pf;
        Save();

        _audit?.Log(
            action: "PF.Update",
            entityType: "PermanentFunding",
            entityId: pf.Id,
            entityLabel: $"{pf.Reference} · {pf.LinkedEntityName}",
            before: before,
            after: pf.FundingStatusLabel,
            detail: $"Total {pf.TotalApprovedAllocation:C}");
        OnChange?.Invoke();
    }

    public void Approve(string id)
    {
        EnsureLoaded();
        var pf = FindById(id);
        if (pf == null) return;
        var before = pf.FundingStatusLabel;
        pf.FundingStatusLabel = "Approved";
        pf.Status = PermanentFundingStatus.Active;
        pf.UpdatedAt = DateTime.Now;
        Save();
        _audit?.Log("PF.Approve", "PermanentFunding", pf.Id,
                    $"{pf.Reference} · {pf.LinkedEntityName}", before, "Approved");
        OnChange?.Invoke();
    }
}
