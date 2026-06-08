using SCSPortal.Models;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCSPortal.Services;

public class FundingCommitmentService
{
    private List<FundingCommitment> _rows = new();
    public List<FundingCommitment> Rows
    {
        get
        {
            EnsureLoaded();
            return _rows;
        }
    }

    public FilterKey Filter { get; set; } = FilterKey.Draft;
    public string Search { get; set; } = "";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int ApprovedThisSession { get; private set; }
    public int FocusedIndex { get; set; }
    public RowView View { get; set; } = RowView.Compact;
    public bool DraftNoticeDismissed { get; set; }
    public bool HintsVisible { get; set; }

    public bool LastApprovePop { get; private set; }

    public event Action? OnChange;
    public event Action? OnApprovePop;
    public event Action<string>? OnRequestScroll;

    public void NotifyChanged()
    {
        Save();
        OnChange?.Invoke();
    }
    public void RequestScrollToCommit(string commitId) => OnRequestScroll?.Invoke(commitId);

    private readonly UserContextService? _users;
    private readonly AuditService? _audit;
    private readonly NotificationService? _notify;
    private readonly VendorService? _vendors;
    private readonly PermanentFundingService? _pf;
    private readonly IJSRuntime? _js;

    public FundingCommitmentService() { Seed(); }

    public FundingCommitmentService(UserContextService users, AuditService audit, NotificationService notify, VendorService vendors, PermanentFundingService pf, IJSRuntime js)
    {
        _users = users;
        _audit = audit;
        _notify = notify;
        _vendors = vendors;
        _pf = pf;
        _js = js;
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
                var json = jsInProcess.Invoke<string>("localStorage.getItem", "scs_funding_commitments");
                if (!string.IsNullOrEmpty(json))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<FundingCommitment>>(json);
                    if (list != null)
                    {
                        _rows = list;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading commitments: " + ex.Message);
            }
        }

        Seed();
        Save();
    }

    public void Save()
    {
        if (_js is IJSInProcessRuntime jsInProcess)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_rows);
                jsInProcess.InvokeVoid("localStorage.setItem", "scs_funding_commitments", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving commitments: " + ex.Message);
            }
        }
    }

    /// <summary>Names the current user can be matched against on a service line's Vendor field.</summary>
    private List<string> CurrentVendorNames()
    {
        var names = new List<string>();
        var u = _users?.Effective;
        if (u == null) return names;
        names.Add(u.FullName);
        if (u.LinkedVendorId != null && _vendors != null)
        {
            var v = _vendors.FindById(u.LinkedVendorId);
            if (v != null && !names.Contains(v.Name, StringComparer.OrdinalIgnoreCase))
                names.Add(v.Name);
        }
        return names;
    }

    // ---------- Visibility (FRD §11) ----------

    public IEnumerable<FundingCommitment> VisibleToCurrent()
    {
        if (_users == null) return Rows;
        var u = _users.Effective;
        if (u.Role == AppRole.Client && u.LinkedClientId != null)
        {
            return Rows.Where(r => r.Association == AssociationType.Client && r.ClientId == u.LinkedClientId);
        }
        if (u.Role is AppRole.VendorIsw or AppRole.VendorSp && u.LinkedVendorId != null)
        {
            var candidates = CurrentVendorNames();
            return Rows.Where(r => r.Services.Any(s =>
                candidates.Any(c => string.Equals(c, s.Vendor, StringComparison.OrdinalIgnoreCase))));
        }
        return Rows;
    }

    // ---------- Filtering ----------

    public IEnumerable<FundingCommitment> GetFiltered()
    {
        var q = (Search ?? "").Trim().ToLowerInvariant();

        return Rows.Where(r =>
        {
            var matchesStatus = Filter switch
            {
                FilterKey.All      => true,
                FilterKey.Draft    => r.Status == CommitmentStatus.Draft,
                FilterKey.Pending  => r.Status is CommitmentStatus.Pending or CommitmentStatus.Awaiting
                                              or CommitmentStatus.Special or CommitmentStatus.Finance
                                              or CommitmentStatus.Ministry,
                FilterKey.Approved => r.Status is CommitmentStatus.Approved or CommitmentStatus.Active,
                FilterKey.Rejected => r.Status == CommitmentStatus.Rejected,
                _ => true
            };
            if (!matchesStatus) return false;

            if (string.IsNullOrEmpty(q)) return true;

            var hay = string.Join(' ', new[]
            {
                r.CommitId, r.ClientId, r.ClientName, r.Ministry,
                r.MccssLabel, r.MainFunder, r.AddlFunder, r.Creator,
                string.Join(' ', r.Services.Select(s => s.Vendor + " " + s.Name))
            }).ToLowerInvariant();

            return hay.Contains(q);
        });
    }

    public IReadOnlyDictionary<FilterKey, int> GetCounts() => new Dictionary<FilterKey, int>
    {
        [FilterKey.All]      = Rows.Count,
        [FilterKey.Draft]    = Rows.Count(r => r.Status == CommitmentStatus.Draft),
        [FilterKey.Pending]  = Rows.Count(r => r.Status is CommitmentStatus.Pending or CommitmentStatus.Awaiting
                                                       or CommitmentStatus.Special or CommitmentStatus.Finance
                                                       or CommitmentStatus.Ministry),
        [FilterKey.Approved] = Rows.Count(r => r.Status is CommitmentStatus.Approved or CommitmentStatus.Active),
        [FilterKey.Rejected] = Rows.Count(r => r.Status == CommitmentStatus.Rejected)
    };

    public RowView ViewFor(FundingCommitment r) => r.ViewOverride ?? View;

    // ---------- Actions: status ----------

    private static bool IsApprovable(CommitmentStatus s) =>
        s is CommitmentStatus.Draft or CommitmentStatus.Pending
          or CommitmentStatus.Awaiting or CommitmentStatus.Special
          or CommitmentStatus.Finance or CommitmentStatus.Ministry;

    public FundingCommitment? FindByCommitId(string commitId) =>
        Rows.FirstOrDefault(x => x.CommitId == commitId);

    public void Approve(string commitId)
    {
        // Legacy single-step approve — kept for compatibility, but it now
        // advances through the proper workflow stages depending on current state.
        var r = FindByCommitId(commitId);
        if (r == null) return;
        if (!IsApprovable(r.Status)) return;

        var before = r.Status;
        r.Status = CommitmentStatus.Approved;
        r.Selected = false;
        r.FinanceApprovedAt ??= DateTime.Now;
        r.FinanceApprovedBy ??= _users?.Effective.FullName;
        ApprovedThisSession++;
        OnApprovePop?.Invoke();
        Audit("FC.Approve", r, before, $"{r.ClientName ?? r.LinkedEntityId}: {before} → Approved");
        Notify(NotificationLevel.Success, "Commitment approved",
            $"{Display(r)} approved by {_users?.Effective.FullName ?? "user"}.",
            link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        NotifyChanged();
    }

    // ---- Multi-stage workflow (FRD §5.5) -----------------------------

    public void Submit(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null || r.Status != CommitmentStatus.Draft) return;
        var before = r.Status;
        
        if (r.FundingType == "In-Year MYSLP Residential" || r.FundingType == "Regional / TPR transfer")
        {
            r.Status = CommitmentStatus.Ministry;
            r.SubmittedAt = DateTime.Now;
            r.SubmittedBy = _users?.Effective.FullName;
            Audit("FC.Submit", r, before, $"{Display(r)} submitted for Ministry Approval");
            Notify(NotificationLevel.Info, "FC submitted",
                $"{Display(r)} awaits Ministry Approval.",
                targetRole: AppRole.Finance,
                link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        }
        else if (r.FundingType == "CSN Youth")
        {
            r.Status = CommitmentStatus.Csn;
            r.SubmittedAt = DateTime.Now;
            r.SubmittedBy = _users?.Effective.FullName;
            Audit("FC.Submit", r, before, $"{Display(r)} submitted for CSN Youth Gate");
            Notify(NotificationLevel.Info, "FC submitted",
                $"{Display(r)} awaits CSN Youth Gate review.",
                targetRole: AppRole.SpecialApprover,
                link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        }
        else
        {
            r.Status = CommitmentStatus.Special;
            r.SubmittedAt = DateTime.Now;
            r.SubmittedBy = _users?.Effective.FullName;
            Audit("FC.Submit", r, before, $"{Display(r)} submitted for Special Approval");
            Notify(NotificationLevel.Info, "FC submitted",
                $"{Display(r)} awaits Special Approver review.",
                targetRole: AppRole.SpecialApprover,
                link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        }
        
        NotifyChanged();
    }

    public void SpecialApprove(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null || r.Status != CommitmentStatus.Special) return;
        var before = r.Status;
        r.Status = CommitmentStatus.Finance;
        r.SpecialApprovedAt = DateTime.Now;
        r.SpecialApprovedBy = _users?.Effective.FullName;
        Audit("FC.SpecialApprove", r, before, $"{Display(r)} cleared by Special Approver");
        Notify(NotificationLevel.Info, "FC awaits Finance review",
            $"{Display(r)} cleared by {_users?.Effective.FullName ?? "Special Approver"}.",
            targetRole: AppRole.Finance,
            link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        NotifyChanged();
    }

    public string? FinanceApprove(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return "Commitment not found.";
        if (r.Status is not (CommitmentStatus.Finance or CommitmentStatus.Special
                          or CommitmentStatus.Awaiting or CommitmentStatus.Ministry
                          or CommitmentStatus.Pending or CommitmentStatus.Draft))
            return "Invalid commitment status for approval.";

        if (r.FundingType == "In-Year MYSLP Residential" || r.FundingType == "Regional / TPR transfer")
        {
            if (_pf == null)
            {
                return "Internal Error: Permanent Funding Service not available.";
            }

            var pfRecord = _pf.Records.FirstOrDefault(pf => 
                (pf.SourceFundingCommitmentId == r.CommitId || 
                 (pf.LinkedEntityId == r.LinkedEntityId && pf.Association == r.Association && pf.FiscalYear == r.FiscalYear)) 
                && pf.FundingStatusLabel == "Approved");

            if (pfRecord == null)
            {
                return $"Approval blocked: No corresponding Approved Permanent Funding record exists for this {r.Association}.";
            }

            if (pfRecord.BudgetAmount < r.Total)
            {
                return $"Approval blocked: Mapped Permanent Funding budget envelope amount (${pfRecord.BudgetAmount:N2}) is less than the commitment total amount (${r.Total:N2}).";
            }
        }

        var before = r.Status;
        r.Status = CommitmentStatus.Approved;
        r.Selected = false;
        r.FinanceApprovedAt = DateTime.Now;
        r.FinanceApprovedBy = _users?.Effective.FullName;
        ApprovedThisSession++;
        OnApprovePop?.Invoke();
        Audit("FC.FinanceApprove", r, before, $"{Display(r)} approved by Finance");
        Notify(NotificationLevel.Success, "FC approved",
            $"{Display(r)} approved.",
            link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        NotifyChanged();
        return null;
    }

    public void RejectWithReason(string commitId, string reason)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        var before = r.Status;
        r.Status = CommitmentStatus.Rejected;
        r.RejectionReason = reason;
        Audit("FC.Reject", r, before, $"{Display(r)} rejected: {reason}");
        Notify(NotificationLevel.Warning, "FC rejected",
            $"{Display(r)}: {reason}",
            link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        NotifyChanged();
    }

    private void Audit(string action, FundingCommitment r, CommitmentStatus before, string detail)
    {
        _audit?.Log(action, "FundingCommitment", r.CommitId, Display(r),
                    before: before.ToString(), after: r.Status.ToString(), detail: detail);
    }

    private void Notify(NotificationLevel lvl, string title, string body,
                        AppRole? targetRole = null, string? targetUserId = null,
                        string? link = null, string? entityType = null, string? entityId = null)
    {
        _notify?.Push(title, body, lvl, targetRole, targetUserId, link, entityType, entityId);
    }

    private static string Display(FundingCommitment r)
    {
        var who = r.Association == AssociationType.Client
            ? r.ClientName
            : r.LinkedEntityId;   // project name resolved at call sites; fallback to id
        return string.IsNullOrWhiteSpace(who) ? r.CommitId : $"FC {r.CommitId} ({who})";
    }

    public string? ApproveAndAdvance(string commitId)
    {
        var filteredBefore = GetFiltered().ToList();
        var idx = filteredBefore.FindIndex(r => r.CommitId == commitId);
        var prevView = filteredBefore.ElementAtOrDefault(idx) is { } row ? ViewFor(row) : View;

        Approve(commitId);

        var filteredAfter = GetFiltered().ToList();
        if (filteredAfter.Count == 0) { NotifyChanged(); return null; }

        var nextIx = Math.Min(idx, filteredAfter.Count - 1);
        if (nextIx < 0) nextIx = 0;
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredAfter.Count / (double)PageSize));
        Page = Math.Min(totalPages, (nextIx / PageSize) + 1);
        FocusedIndex = nextIx % PageSize;

        var nextRow = filteredAfter[nextIx];
        if (prevView != RowView.Ultra) nextRow.ViewOverride = prevView;
        NotifyChanged();
        return nextRow.CommitId;
    }

    public void ApproveFocused()
    {
        var filtered = GetFiltered().ToList();
        var row = filtered.ElementAtOrDefault(FocusedIndex);
        if (row == null) return;
        if (!IsApprovable(row.Status)) return;
        Approve(row.CommitId);
    }

    public void Reject(string commitId)
    {
        RejectWithReason(commitId, "Returned with feedback");
    }

    public void Revise(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        var before = r.Status;
        r.Status = CommitmentStatus.Draft;
        r.Version++;
        r.SubmittedAt = null;
        r.SpecialApprovedAt = null;
        r.FinanceApprovedAt = null;
        Audit("FC.Revise", r, before, $"{Display(r)} sent back to draft (v{r.Version})");
        Notify(NotificationLevel.Info, "FC sent back to draft", $"{Display(r)} sent back to draft.",
            link: "/", entityType: "FundingCommitment", entityId: r.CommitId);
        NotifyChanged();
    }

    public void SetStatus(string commitId, CommitmentStatus newStatus)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        r.Status = newStatus;
        NotifyChanged();
    }

    public void Duplicate(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        var copy = r.Clone();
        copy.CommitId = Guid.NewGuid().ToString("N").Substring(0, 8);
        copy.Status = CommitmentStatus.Draft;
        copy.Selected = false;
        Rows.Insert(0, copy);
        NotifyChanged();
    }

    public void Delete(string commitId)
    {
        Rows.RemoveAll(r => r.CommitId == commitId);
        NotifyChanged();
    }

    // ---------- Selection ----------

    public void ToggleSelect(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        r.Selected = !r.Selected;
        NotifyChanged();
    }

    public void ToggleSelectFocused()
    {
        var filtered = GetFiltered().ToList();
        var row = filtered.ElementAtOrDefault(FocusedIndex);
        if (row == null) return;
        row.Selected = !row.Selected;
        NotifyChanged();
    }

    public bool SelectAllVisible()
    {
        var filtered = GetFiltered().ToList();
        var allSelected = filtered.Count > 0 && filtered.All(r => r.Selected);
        foreach (var r in filtered) r.Selected = !allSelected;
        NotifyChanged();
        return !allSelected;
    }

    public void ClearSelection()
    {
        foreach (var r in Rows) r.Selected = false;
        NotifyChanged();
    }

    public int SelectedCount => Rows.Count(r => r.Selected);
    public IEnumerable<FundingCommitment> SelectedRows => Rows.Where(r => r.Selected);

    public void BulkApproveSelected()
    {
        var selected = Rows.Where(r => r.Selected && IsApprovable(r.Status)).ToList();
        if (selected.Count == 0) return;
        foreach (var r in selected)
        {
            r.Status = CommitmentStatus.Approved;
            r.Selected = false;
            ApprovedThisSession++;
        }
        OnApprovePop?.Invoke();
        NotifyChanged();
    }

    public int BulkApprovableSelectedCount => Rows.Count(r => r.Selected && IsApprovable(r.Status));

    // ---------- Focus / nav ----------

    public void MoveFocus(int delta)
    {
        var filtered = GetFiltered().ToList();
        if (filtered.Count == 0) return;
        FocusedIndex = Math.Max(0, Math.Min(filtered.Count - 1, FocusedIndex + delta));

        var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        Page = Math.Min(totalPages, (FocusedIndex / PageSize) + 1);
        var localIdx = FocusedIndex % PageSize;

        NotifyChanged();
        var item = filtered.ElementAtOrDefault(FocusedIndex);
        if (item != null) RequestScrollToCommit(item.CommitId);
    }

    public void GoToPage(int p)
    {
        var filtered = GetFiltered().ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        if (p < 1 || p > totalPages) return;
        Page = p;
        FocusedIndex = 0;
        NotifyChanged();
    }

    public void SetFilter(FilterKey f)
    {
        Filter = f;
        FocusedIndex = 0;
        Page = 1;
        if (f != FilterKey.Draft) DraftNoticeDismissed = true;
        NotifyChanged();
    }

    public void SetView(RowView v)
    {
        View = v;
        foreach (var r in Rows) r.ViewOverride = null;
        NotifyChanged();
    }

    public void CycleRowView(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        var cur = ViewFor(r);
        r.ViewOverride = cur switch
        {
            RowView.Ultra => RowView.Compact,
            RowView.Compact => RowView.Detailed,
            _ => RowView.Ultra
        };
        NotifyChanged();
    }

    public void CollapseRowToUltra(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        r.ViewOverride = RowView.Ultra;
        NotifyChanged();
    }

    public void ExpandRowFromUltra(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        if (ViewFor(r) == RowView.Ultra) r.ViewOverride = RowView.Compact;
        NotifyChanged();
    }

    // ---------- Field updates ----------

    public void UpdateField(string commitId, string key, string value)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        switch (key)
        {
            case "planStart":        r.PlanStart = value; break;
            case "planEnd":          r.PlanEnd = value; break;
            case "mainFunder":       r.MainFunder = value; break;
            case "ministry":         r.Ministry = value; break;
            case "fundingType":      r.FundingType = value; break;
            case "placement":        r.Placement = value; break;
            case "addlFunder":       r.AddlFunder = value; break;
            case "outcome":          r.Outcome = value; break;
            case "altSources":       r.AltSources = value; break;
            case "planType":         r.PlanType = Catalog.NormalizePlanType(value); break;
        }
        NotifyChanged();
    }

    public void UpdateFieldNumber(string commitId, string key, decimal value)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        switch (key)
        {
            case "addlFunderAmount": r.AddlFunderAmount = value; break;
            case "addlFunderVendor": r.AddlFunderVendor = value; break;
        }
        NotifyChanged();
    }

    public void ToggleMccss(string commitId, string type, Func<string, bool>? onWarn = null)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        if (r.MccssTypes.Contains(type))
        {
            if (r.MccssTypes.Count == 1)
            {
                onWarn?.Invoke("A commitment needs at least one MCCSS funding type");
                return;
            }
            r.MccssTypes.Remove(type);
            r.PlanAmounts.Remove(type);
        }
        else
        {
            r.MccssTypes.Add(type);
            if (!r.PlanAmounts.ContainsKey(type)) r.PlanAmounts[type] = 0;
        }
        NotifyChanged();
    }

    public void SetPlanAmount(string commitId, string type, decimal amount)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        r.PlanAmounts[type] = amount;
        NotifyChanged();
    }

    // ---------- Services ----------

    public void AddService(string commitId)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        r.Services.Add(new ServiceLine
        {
            Name = "",
            Plan = r.MccssTypes.FirstOrDefault() ?? Catalog.MccssFunding[0],
            Provider = Catalog.Providers[0],
            Vendor = "Unassigned",
            Unit = "Session",
            Rate = 0,
            Units = 0,
            Note = ""
        });
        NotifyChanged();
    }

    public bool DeleteService(string commitId, int idx, Func<string, bool>? onWarn = null)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return false;
        if (r.Services.Count <= 1)
        {
            onWarn?.Invoke("A commitment needs at least one service line");
            return false;
        }
        if (idx < 0 || idx >= r.Services.Count) return false;
        r.Services.RemoveAt(idx);
        NotifyChanged();
        return true;
    }

    public void UpdateServiceField(string commitId, int idx, string key, string value)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        if (idx < 0 || idx >= r.Services.Count) return;
        var s = r.Services[idx];
        switch (key)
        {
            case "name":     s.Name = value; break;
            case "plan":     s.Plan = value; break;
            case "provider": s.Provider = value; break;
            case "vendor":   s.Vendor = value; break;
            case "unit":     s.Unit = value; break;
            case "note":     s.Note = value; break;
        }
        NotifyChanged();
    }

    public void UpdateServiceNumber(string commitId, int idx, string key, decimal value)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        if (idx < 0 || idx >= r.Services.Count) return;
        var s = r.Services[idx];
        if (key == "rate") s.Rate = value;
        else if (key == "units") s.Units = value;
        NotifyChanged();
    }

    public void SetServiceNote(string commitId, int idx, string note)
    {
        var r = FindByCommitId(commitId);
        if (r == null) return;
        if (idx < 0 || idx >= r.Services.Count) return;
        r.Services[idx].Note = note;
        NotifyChanged();
    }

    // ---------- Bulk edit ----------

    public int ApplyBulkChanges(BulkChanges c)
    {
        var sel = Rows.Where(r => r.Selected).ToList();
        if (sel.Count == 0) return 0;
        foreach (var r in sel)
        {
            if (c.EnableStatus) r.Status = c.Status;
            if (c.EnablePlanPeriod)
            {
                if (!string.IsNullOrEmpty(c.PlanStart)) r.PlanStart = c.PlanStart;
                if (!string.IsNullOrEmpty(c.PlanEnd)) r.PlanEnd = c.PlanEnd;
            }
            if (c.EnableMccss && c.MccssTypes.Count > 0)
            {
                if (c.MccssMode == BulkMccssMode.Replace)
                {
                    r.MccssTypes = new List<string>(c.MccssTypes);
                    var next = new Dictionary<string, decimal>();
                    foreach (var t in r.MccssTypes)
                        next[t] = r.PlanAmounts.TryGetValue(t, out var v) ? v : 0;
                    r.PlanAmounts = next;
                }
                else if (c.MccssMode == BulkMccssMode.Add)
                {
                    foreach (var t in c.MccssTypes)
                    {
                        if (!r.MccssTypes.Contains(t))
                        {
                            r.MccssTypes.Add(t);
                            if (!r.PlanAmounts.ContainsKey(t)) r.PlanAmounts[t] = 0;
                        }
                    }
                }
                else if (c.MccssMode == BulkMccssMode.Remove)
                {
                    var keep = r.MccssTypes.Where(t => !c.MccssTypes.Contains(t)).ToList();
                    if (keep.Count > 0)
                    {
                        r.MccssTypes = keep;
                        var next = new Dictionary<string, decimal>();
                        foreach (var t in keep)
                            next[t] = r.PlanAmounts.TryGetValue(t, out var v) ? v : 0;
                        r.PlanAmounts = next;
                    }
                    // else: don't strip last type
                }
            }
            if (c.EnablePlanType) r.PlanType = Catalog.NormalizePlanType(c.PlanType);
            if (c.EnableMinistry) r.Ministry = c.Ministry;
            if (c.EnableOutcome) r.Outcome = c.Outcome;
        }
        NotifyChanged();
        return sel.Count;
    }

    // ---------- New commitment ----------

    public string CreateFromWizard(FcWizardDraft d, bool submitOnFinish)
    {
        var id = Guid.NewGuid().ToString("N").Substring(0, 8);
        var today = DateTime.Now;

        var fc = new FundingCommitment
        {
            CommitId = id,
            ClientId = d.Association == AssociationType.Client ? d.LinkedEntityId : "",
            ClientName = d.LinkedEntityDisplay,
            Dob = "—",
            Year = today.Year.ToString(),
            Status = CommitmentStatus.Draft,
            Association = d.Association,
            LinkedEntityId = d.LinkedEntityId,
            PermanentFundingId = d.PermanentFundingId,
            FiscalYear = "FY" + today.Year,
            FundingSource = d.MainFunder,
            Creator = _users?.Effective.FullName ?? "User",
            Date = today.ToString("MMM dd, yyyy"),
            Time = today.ToString("h:mm tt"),
            PlanStart = d.PlanStart?.ToString("yyyy-MM-dd") ?? today.ToString("yyyy-MM-dd"),
            PlanEnd = d.PlanEnd?.ToString("yyyy-MM-dd") ?? today.AddYears(1).ToString("yyyy-MM-dd"),
            FundingType = string.IsNullOrEmpty(d.FundingTypeChoice) ? "Individual Funding" : d.FundingTypeChoice,
            Placement = string.IsNullOrEmpty(d.ResidentialPlacement) ? "Family Home" : d.ResidentialPlacement,
            CsnLivingArrangement = d.CsnLivingArrangement,
            MainFunder = d.MainFunder,
            AddlFunder = d.AdditionalFunders.Count > 0 ? string.Join(", ", d.AdditionalFunders) : "— None —",
            Ministry = string.IsNullOrEmpty(d.ScsTeam) ? "Service Coordinator" : d.ScsTeam,
            MccssTypes = new List<string>(d.McsssTypes),
            PlanAmounts = new Dictionary<string, decimal>(d.McsssAmounts),
            PlanType = Catalog.NormalizePlanType(d.McsssPlanType),
            Outcome = d.CurrentSituationOutcome,
            AltSources = d.ImpactAndAlternatives,
            Attachments = d.Attachments.Count,
            Services = d.Services.Count > 0 ? d.Services.Select(s => s.Clone()).ToList()
                : new List<ServiceLine>
                {
                    new() { Name = "", Plan = d.McsssTypes.FirstOrDefault() ?? Catalog.MccssFunding[0],
                            Provider = Catalog.Providers[0], Vendor = "Unassigned",
                            Unit = "Session", Rate = 0, Units = 0 }
                }
        };
        Rows.Insert(0, fc);
        Filter = FilterKey.Draft;
        Page = 1;
        FocusedIndex = 0;

        Audit("FC.Create", fc, fc.Status,
            $"{Display(fc)} created via wizard ({d.Association} → {d.LinkedEntityDisplay})");

        if (submitOnFinish)
        {
            Submit(id); // immediately advance to Special Approver
        }
        else
        {
            Notify(NotificationLevel.Success, "Draft FC saved",
                   $"{Display(fc)} saved as draft.",
                   entityType: "FundingCommitment", entityId: id);
        }

        NotifyChanged();
        return id;
    }

    public string CreateUnified(AssociationType assoc, string linkedEntityId, string linkedDisplayName,
                                string? permanentFundingId, string fiscalYear, string fundingSource,
                                string? parentFcId = null)
    {
        var id = Guid.NewGuid().ToString("N").Substring(0, 8);
        var today = DateTime.Now;
        var ymd = today.ToString("yyyy-MM-dd");
        var endYmd = today.AddYears(1).ToString("yyyy-MM-dd");

        var draft = new FundingCommitment
        {
            CommitId = id,
            ClientId = assoc == AssociationType.Client ? linkedEntityId : "",
            ClientName = linkedDisplayName,
            Dob = "—",
            Year = today.Year.ToString(),
            Status = CommitmentStatus.Draft,
            Association = assoc,
            LinkedEntityId = linkedEntityId,
            PermanentFundingId = permanentFundingId,
            ParentFundingCommitmentId = parentFcId,
            FiscalYear = fiscalYear,
            FundingSource = fundingSource,
            Creator = _users?.Effective.FullName ?? "Jatin K",
            Date = today.ToString("MMM dd, yyyy"),
            Time = today.ToString("h:mm tt"),
            PlanStart = ymd,
            PlanEnd = endYmd,
            FundingType = assoc == AssociationType.Project ? "Group Funding" : "Individual Funding",
            Placement = "Family Home",
            MainFunder = fundingSource,
            AddlFunder = "— None —",
            Ministry = "Service Coordinator",
            MccssTypes = new List<string> { Catalog.MccssFunding[0] },
            PlanAmounts = new Dictionary<string, decimal> { [Catalog.MccssFunding[0]] = 0 },
            PlanType = "Restricted",
            Services = new List<ServiceLine>
            {
                new() { Name = "", Plan = Catalog.MccssFunding[0], Provider = Catalog.Providers[0],
                        Vendor = "Unassigned", Unit = "Session", Rate = 0, Units = 0 }
            }
        };
        Rows.Insert(0, draft);
        Filter = FilterKey.Draft;
        Search = "";
        Page = 1;
        FocusedIndex = 0;
        Audit("FC.Create", draft, draft.Status,
            $"{Display(draft)} created ({assoc} → {linkedDisplayName})");
        Notify(NotificationLevel.Success, "Draft FC created",
            $"{Display(draft)} created. Add services and submit when ready.",
            entityType: "FundingCommitment", entityId: draft.CommitId);
        NotifyChanged();
        return id;
    }

    // ---------- Rollover (FRD §7) ----------

    public List<string> GenerateRolloverDrafts(IEnumerable<PermanentFunding> sources, string newFiscalYear)
    {
        var created = new List<string>();
        foreach (var pf in sources)
        {
            // Pick an existing parent FC for linkage if one exists
            var parent = Rows.FirstOrDefault(r => r.PermanentFundingId == pf.Id)
                       ?? Rows.FirstOrDefault(r => r.LinkedEntityId == pf.LinkedEntityId
                                                && r.Association == pf.Association);

            var id = CreateUnified(
                assoc: pf.Association,
                linkedEntityId: pf.LinkedEntityId,
                linkedDisplayName: pf.LinkedEntityName,
                permanentFundingId: pf.Id,
                fiscalYear: newFiscalYear,
                fundingSource: pf.FundingSource,
                parentFcId: parent?.CommitId
            );

            // Carry forward vendors / restrictions / budgets from parent if available
            if (parent != null)
            {
                var draft = FindByCommitId(id);
                if (draft != null)
                {
                    draft.PlanType = parent.PlanType;
                    draft.MccssTypes = new List<string>(parent.MccssTypes);
                    draft.PlanAmounts = new Dictionary<string, decimal>(parent.PlanAmounts);
                    draft.Services = parent.Services.Select(s => s.Clone()).ToList();
                    foreach (var s in draft.Services) s.Note = "Carried forward from " + parent.CommitId;
                    draft.Ministry = parent.Ministry;
                }
            }

            pf.AlreadyRolledOver = true;
            created.Add(id);
        }
        NotifyChanged();
        return created;
    }

    public string NewCommitment()
    {
        var id = Guid.NewGuid().ToString("N").Substring(0, 8);
        var cid = (10000 + Random.Shared.Next(89999)).ToString();
        var today = DateTime.Now;
        var ymd = today.ToString("yyyy-MM-dd");
        var fy = today.Year.ToString();
        var fmtDate = today.ToString("MMM dd, yyyy");
        var fmtTime = today.ToString("h:mm tt");

        var draft = new FundingCommitment
        {
            CommitId = id,
            ClientId = cid,
            ClientName = "New client",
            Dob = "—",
            Year = fy,
            Status = CommitmentStatus.Draft,
            Creator = "Jatin K",
            Date = fmtDate,
            Time = fmtTime,
            PlanStart = ymd,
            PlanEnd = ymd,
            FundingType = "None of the above",
            Placement = "Family Home",
            MainFunder = "MCCSS",
            AddlFunder = "— None —",
            Ministry = "Service Coordinator",
            MccssTypes = new List<string> { Catalog.MccssFunding[0] },
            PlanAmounts = new Dictionary<string, decimal> { [Catalog.MccssFunding[0]] = 0 },
            PlanType = "Restricted",
            Services = new List<ServiceLine>
            {
                new() { Name = "", Plan = Catalog.MccssFunding[0], Provider = Catalog.Providers[0],
                        Vendor = "Unassigned", Unit = "Session", Rate = 0, Units = 0 }
            }
        };
        Rows.Insert(0, draft);
        Filter = FilterKey.Draft;
        Search = "";
        Page = 1;
        FocusedIndex = 0;
        NotifyChanged();
        return id;
    }

    // ---------- Seed (mirrors prototype) ----------

    private static int CreatorIndex(string cid) => cid[0] % Catalog.Creators.Length;

    private FundingCommitment Row(
        string id, string cid, string name, string dob,
        string year = "2026",
        CommitmentStatus status = CommitmentStatus.Draft,
        string? creator = null,
        string date = "May 12, 2026", string time = "10:30 AM",
        string ps = "2026-04-01", string pe = "2027-03-31",
        string ft = "Individual Funding",
        string place = "Family Home",
        string addl = "— None —",
        string min = "Service Coordinator",
        string[]? mccss = null,
        Dictionary<string, decimal>? planAmounts = null,
        string pt = "Restricted",
        decimal pa = 500,
        string outcome = "", string alt = "",
        decimal aa = 0, decimal av = 0,
        int att = 0, int msg = 0, int hist = 1,
        ServiceLine[]? services = null,
        string svcName = "",
        string vendor = "BrightPath Services",
        string unit = "Hour",
        decimal rate = 45,
        decimal units = 10,
        string note = "")
    {
        var mccssTypes = (mccss ?? new[] { Catalog.MccssFunding[0] }).ToList();
        var planAmts = new Dictionary<string, decimal>();
        for (int i = 0; i < mccssTypes.Count; i++)
        {
            var t = mccssTypes[i];
            if (planAmounts != null && planAmounts.TryGetValue(t, out var v))
                planAmts[t] = v;
            else
                planAmts[t] = i == 0 ? pa : 0;
        }

        return new FundingCommitment
        {
            CommitId = id,
            ClientId = cid,
            ClientName = name,
            Dob = dob,
            Year = year,
            Status = status,
            Association = AssociationType.Client,
            LinkedEntityId = cid,
            FiscalYear = "FY" + (year == "—" ? "2026" : year),
            FundingSource = "MCCSS",
            Creator = creator ?? Catalog.Creators[CreatorIndex(cid)],
            Date = date,
            Time = time,
            PlanStart = ps,
            PlanEnd = pe,
            FundingType = ft,
            Placement = place,
            MainFunder = "MCCSS",
            AddlFunder = addl,
            Ministry = min,
            MccssTypes = mccssTypes,
            PlanAmounts = planAmts,
            PlanType = Catalog.NormalizePlanType(pt),
            Outcome = outcome,
            AltSources = alt,
            AddlFunderAmount = aa,
            AddlFunderVendor = av,
            Attachments = att,
            Messages = msg,
            History = hist,
            Services = services?.ToList() ?? new List<ServiceLine>
            {
                new()
                {
                    Name = string.IsNullOrEmpty(svcName) ? Catalog.Services[0] : svcName,
                    Plan = mccssTypes[0],
                    Provider = Catalog.Providers[0],
                    Vendor = vendor,
                    Unit = unit,
                    Rate = rate,
                    Units = units,
                    Note = note
                }
            }
        };
    }

    private void Seed()
    {
        // Original 12 — varied statuses
        Rows.Add(Row("45f4368f", "23343", "bill test", "Jun 15, 2000", year: "—", status: CommitmentStatus.Draft,
            creator: "Fatoumata Diallo", date: "May 13, 2026", time: "05:14 PM",
            ps: "2026-05-01", pe: "2026-05-31",
            ft: "None of the above", place: "Family Home", min: "Finance / Admin",
            addl: "Passport One (Family Services Ontario)", outcome: "Test", alt: "Test",
            mccss: new[] { "Temporary Funding Allocation - Children's", "Temporary Funding Allocation - Adult", "Permanent Funding Allocation - Children's" },
            planAmounts: new Dictionary<string, decimal>
            {
                ["Temporary Funding Allocation - Children's"] = 45555,
                ["Temporary Funding Allocation - Adult"] = 555555,
                ["Permanent Funding Allocation - Children's"] = 3333
            },
            pt: "Restricted", pa: 45555, aa: 122, av: 122, att: 0, msg: 0, hist: 1,
            services: new[]
            {
                new ServiceLine { Name = Catalog.Services[1], Plan = "Temporary Funding Allocation - Children's", Provider = Catalog.Providers[0], Vendor = "Ima Worker", Unit = "Hour", Rate = 50, Units = 10 },
                new ServiceLine { Name = Catalog.Services[4], Plan = "Temporary Funding Allocation - Adult", Provider = Catalog.Providers[0], Vendor = "Alan Smith", Unit = "Day", Rate = 455, Units = 1 },
                new ServiceLine { Name = Catalog.Services[2], Plan = "Permanent Funding Allocation - Children's", Provider = Catalog.Providers[0], Vendor = "Agnes Testworker", Unit = "Hour", Rate = 455, Units = 1 }
            }));

        Rows.Add(Row("f8065e44", "84291", "Sarah Chen", "Mar 04, 2012", year: "2027", status: CommitmentStatus.Draft,
            creator: "Bhavesh Mishra", date: "May 13, 2026", time: "12:03 PM",
            ps: "2026-05-08", pe: "2026-05-31",
            ft: "None of the above", place: "Group Living", min: "Finance / Admin",
            pa: 50, addl: "Passport One (Family Services Ontario)", outcome: "Test", alt: "Test",
            aa: 25, av: 25, att: 1, hist: 1,
            services: new[]
            {
                new ServiceLine { Name = Catalog.Services[0], Plan = Catalog.MccssFunding[0], Provider = Catalog.Providers[0], Vendor = "Jatin Test", Unit = "Session", Rate = 20, Units = 1, Note = "Confirmed with caregiver" },
                new ServiceLine { Name = "", Plan = Catalog.MccssFunding[0], Provider = Catalog.Providers[1], Vendor = "Unassigned", Unit = "Session", Rate = 30, Units = 1 }
            }));

        Rows.Add(Row("a5eb1251", "67234", "Marcus Williams", "Sep 22, 2008", year: "—", status: CommitmentStatus.Draft,
            ft: "Individual Funding", pa: 400, outcome: "Weekly respite covered",
            att: 2, msg: 3, hist: 5,
            vendor: "BrightPath Services", rate: 42, units: 8,
            svcName: Catalog.Services[1], note: "Weekly schedule TBD with family"));

        Rows.Add(Row("5e7e5f51", "19584", "Aisha Patel", "Jan 18, 2003", status: CommitmentStatus.Draft,
            ft: "Individual Funding", place: "Independent Living", min: "Clinical Team",
            mccss: new[] { "Passport Funding" }, pt: "Capped", pa: 480,
            att: 1, msg: 1, hist: 3,
            vendor: "Maple Care Inc.", unit: "Session", rate: 120, units: 4,
            svcName: Catalog.Services[3]));

        Rows.Add(Row("f5d6725e", "90122", "Liam O'Brien", "Nov 07, 2010", status: CommitmentStatus.Draft,
            ft: "Pooled Funding", place: "Group Living", min: "Resource Allocation",
            mccss: new[] { Catalog.MccssFunding[1] }, pa: 1500, addl: "Trillium Foundation",
            outcome: "Community participation", alt: "Family contribution",
            aa: 300, av: 300, att: 3, msg: 2, hist: 7,
            services: new[]
            {
                new ServiceLine { Name = Catalog.Services[4], Plan = Catalog.MccssFunding[1], Provider = Catalog.Providers[1], Vendor = "Northern Light Co.", Unit = "Day", Rate = 185, Units = 5, Note = "Summer program M-F" },
                new ServiceLine { Name = Catalog.Services[2], Plan = Catalog.MccssFunding[1], Provider = Catalog.Providers[0], Vendor = "Jatin Test", Unit = "Hour", Rate = 45, Units = 6, Note = "After-school sessions" }
            }));

        Rows.Add(Row("bc418688", "33871", "Daniela Rojas", "Apr 12, 2015", status: CommitmentStatus.Draft,
            ft: "None of the above", min: "Intake Team",
            mccss: new[] { "Special Services at Home (SSAH)" }, pt: "Unrestricted", pa: 0,
            vendor: "Unassigned", unit: "Session", rate: 0, units: 0,
            svcName: Catalog.Services[5], note: "Awaiting rate confirmation"));

        Rows.Add(Row("b63c2b22", "56098", "Kenji Nakamura", "Aug 30, 2005", status: CommitmentStatus.Draft,
            ft: "Individual Funding", place: "Supported Living", min: "Clinical Team",
            mccss: new[] { "Passport Funding" }, pt: "Capped", pa: 2000, addl: "United Way",
            outcome: "Behavioural plan rollout", aa: 100, av: 100,
            att: 2, msg: 4, hist: 9,
            vendor: "Maple Care Inc.", rate: 95, units: 20, svcName: Catalog.Services[3]));

        Rows.Add(Row("b51633ce", "77423", "Emma Larsson", "Feb 14, 2007", status: CommitmentStatus.Approved,
            ft: "Individual Funding", place: "Independent Living",
            mccss: new[] { "Temporary Funding Allocation - Adult" }, pt: "Flexible", pa: 1140,
            att: 1, msg: 2, hist: 6,
            vendor: "BrightPath Services", rate: 38, units: 30, svcName: Catalog.Services[1]));

        Rows.Add(Row("d6246691", "42915", "Tariq Hassan", "Jul 03, 2011", status: CommitmentStatus.Active,
            ft: "Pooled Funding", place: "Group Living", min: "Resource Allocation",
            mccss: new[] { Catalog.MccssFunding[1] }, pa: 2400, addl: "Local Municipality",
            outcome: "Summer day program", aa: 400, av: 400,
            att: 4, msg: 1, hist: 12,
            vendor: "Northern Light Co.", unit: "Day", rate: 200, units: 12, svcName: Catalog.Services[4]));

        Rows.Add(Row("1c8e92aa", "88307", "Priya Sharma", "Dec 09, 2009", status: CommitmentStatus.Rejected,
            ft: "Individual Funding", min: "Finance / Admin",
            mccss: new[] { "Special Services at Home (SSAH)" }, pt: "Capped", pa: 550,
            alt: "Family declined", att: 1, msg: 5, hist: 8,
            vendor: "Aurora Support Co-op", rate: 55, units: 10, svcName: Catalog.Services[2],
            note: "Exceeds annual cap — see Finance"));

        Rows.Add(Row("7a3f01bd", "15402", "Noah Martin", "May 25, 2013", status: CommitmentStatus.Draft,
            ft: "Individual Funding", min: "Intake Team", pa: 600, msg: 1, hist: 2,
            vendor: "BrightPath Services", rate: 50, units: 12, svcName: Catalog.Services[2],
            note: "Pending intake assessment"));

        Rows.Add(Row("2d9c12ee", "62018", "Olivia Brown", "Oct 17, 2006", status: CommitmentStatus.Pending,
            ft: "Group Funding", place: "Supported Living", min: "Resource Allocation",
            mccss: new[] { Catalog.MccssFunding[2] }, pt: "Capped", pa: 1800,
            addl: "Passport One (Family Services Ontario)", outcome: "Independent skills program",
            aa: 200, av: 200, att: 2, hist: 4,
            services: new[]
            {
                new ServiceLine { Name = Catalog.Services[2], Plan = "Temporary Funding Allocation - Adult", Provider = Catalog.Providers[1], Vendor = "Aurora Support Co-op", Unit = "Hour", Rate = 60, Units = 20 },
                new ServiceLine { Name = Catalog.Services[5], Plan = "Temporary Funding Allocation - Adult", Provider = Catalog.Providers[0], Vendor = "Jatin Test", Unit = "Session", Rate = 75, Units = 8, Note = "Group sessions" }
            }));

        // Worst-case multi-type drafts
        Rows.Add(Row("9d2ae741", "10245", "Avery Thompson", "Feb 19, 2009", status: CommitmentStatus.Draft,
            ft: "Pooled Funding", place: "Group Living", min: "Resource Allocation",
            mccss: new[] { "Temporary Funding Allocation - Children's", "Special Services at Home (SSAH)", "Passport Funding" },
            planAmounts: new Dictionary<string, decimal>
            {
                ["Temporary Funding Allocation - Children's"] = 12500,
                ["Special Services at Home (SSAH)"] = 4800,
                ["Passport Funding"] = 18000
            },
            pa: 12500, pt: "Restricted", addl: "Trillium Foundation",
            outcome: "Community + respite combo", aa: 1500, av: 1500,
            att: 3, msg: 2, hist: 6,
            services: new[]
            {
                new ServiceLine { Name = Catalog.Services[1], Plan = "Temporary Funding Allocation - Children's", Provider = Catalog.Providers[1], Vendor = "Maple Care Inc.", Unit = "Hour", Rate = 55, Units = 60 },
                new ServiceLine { Name = Catalog.Services[4], Plan = "Passport Funding", Provider = Catalog.Providers[0], Vendor = "Northern Light Co.", Unit = "Day", Rate = 180, Units = 40, Note = "Weekday program" },
                new ServiceLine { Name = Catalog.Services[0], Plan = "Special Services at Home (SSAH)", Provider = Catalog.Providers[2], Vendor = "Jatin Test", Unit = "Hour", Rate = 40, Units = 30, Note = "Family-directed" }
            }));

        Rows.Add(Row("4f81d3a2", "20716", "Hazel Wong", "Aug 03, 2014", status: CommitmentStatus.Pending,
            ft: "Individual Funding", place: "Family Home", min: "Clinical Team",
            mccss: new[] { "Temporary Funding Allocation - Children's", "Permanent Funding Allocation - Children's" },
            planAmounts: new Dictionary<string, decimal>
            {
                ["Temporary Funding Allocation - Children's"] = 8200,
                ["Permanent Funding Allocation - Children's"] = 6400
            },
            pa: 8200, pt: "Unrestricted", att: 2, msg: 1, hist: 3,
            services: new[]
            {
                new ServiceLine { Name = Catalog.Services[2], Plan = "Permanent Funding Allocation - Children's", Provider = Catalog.Providers[0], Vendor = "BrightPath Services", Unit = "Hour", Rate = 55, Units = 80 },
                new ServiceLine { Name = Catalog.Services[3], Plan = "Temporary Funding Allocation - Children's", Provider = Catalog.Providers[1], Vendor = "Aurora Support Co-op", Unit = "Session", Rate = 95, Units = 30 }
            }));

        // Additional 25 — mostly drafts for the rollover queue
        Rows.Add(Row("a82b41d9", "30521", "Yasmin Hossain",   "Aug 11, 2008", status: CommitmentStatus.Draft, pa: 720, vendor: "Maple Care Inc.", rate: 60, units: 12, svcName: Catalog.Services[1]));
        Rows.Add(Row("c4f29b18", "45673", "Connor Reilly",    "Apr 22, 2011", status: CommitmentStatus.Draft, ft: "Individual Funding", pa: 850, att: 1, msg: 1, vendor: "BrightPath Services", rate: 55, units: 16, svcName: Catalog.Services[2]));
        Rows.Add(Row("e7d51a04", "58104", "Mei Lin",          "Jul 18, 2009", status: CommitmentStatus.Draft, ft: "Individual Funding", place: "Supported Living", mccss: new[] { Catalog.MccssFunding[2] }, pa: 1200, vendor: "Northern Light Co.", unit: "Day", rate: 175, units: 7, svcName: Catalog.Services[4], hist: 3));
        Rows.Add(Row("9b3e7c52", "71925", "David Cohen",      "Mar 09, 2014", status: CommitmentStatus.Draft, min: "Intake Team", pa: 340, att: 2, vendor: "BrightPath Services", rate: 42, units: 8));
        Rows.Add(Row("5fac3d91", "82640", "Amélie Tremblay",  "Nov 30, 2007", status: CommitmentStatus.Draft, ft: "Individual Funding", mccss: new[] { "Passport Funding" }, pt: "Flexible", pa: 1650, att: 1, msg: 2, vendor: "Maple Care Inc.", rate: 85, units: 18, svcName: Catalog.Services[3]));
        Rows.Add(Row("1ed4628b", "93487", "Hiroshi Tanaka",   "Jan 25, 2010", status: CommitmentStatus.Draft, ft: "Pooled Funding", place: "Group Living", mccss: new[] { Catalog.MccssFunding[1] }, pa: 2100, addl: "United Way", aa: 250, av: 250, att: 3, vendor: "Aurora Support Co-op", unit: "Day", rate: 165, units: 12, svcName: Catalog.Services[4]));
        Rows.Add(Row("8c91f426", "14758", "Zainab Ali",       "Jun 04, 2012", status: CommitmentStatus.Draft, pa: 480, vendor: "BrightPath Services", rate: 48, units: 10, svcName: Catalog.Services[2]));
        Rows.Add(Row("6da72e58", "26839", "Mateo Garcia",     "Oct 14, 2005", status: CommitmentStatus.Draft, ft: "Individual Funding", place: "Independent Living", min: "Clinical Team", mccss: new[] { "Passport Funding" }, pt: "Capped", pa: 920, att: 2, msg: 1, vendor: "Maple Care Inc.", rate: 78, units: 12, svcName: Catalog.Services[3]));
        Rows.Add(Row("b2417f6a", "37980", "Luna Petrov",      "Feb 28, 2013", status: CommitmentStatus.Draft, pa: 550, vendor: "BrightPath Services", rate: 50, units: 11));
        Rows.Add(Row("4eba2c89", "49102", "Samuel Adeyemi",   "Sep 08, 2009", status: CommitmentStatus.Draft, ft: "Group Funding", mccss: new[] { Catalog.MccssFunding[2] }, pa: 1340, addl: "Trillium Foundation", aa: 180, av: 180, att: 1, hist: 3, vendor: "Northern Light Co.", rate: 67, units: 20, svcName: Catalog.Services[2]));
        Rows.Add(Row("7f3d918e", "51243", "Olga Volkov",      "Dec 21, 2008", status: CommitmentStatus.Draft, min: "Intake Team", pa: 380, att: 1, vendor: "Aurora Support Co-op", rate: 42, units: 9));
        Rows.Add(Row("2c8540ab", "62385", "Ravi Krishnan",    "May 17, 2006", status: CommitmentStatus.Draft, ft: "Individual Funding", place: "Supported Living", mccss: new[] { "Passport Funding" }, pt: "Capped", pa: 1450, att: 2, msg: 3, vendor: "Maple Care Inc.", rate: 82, units: 18, svcName: Catalog.Services[3]));
        Rows.Add(Row("eb1973c7", "74527", "Sophie Laurent",   "Apr 02, 2011", status: CommitmentStatus.Draft, pa: 620, vendor: "BrightPath Services", rate: 55, units: 11, svcName: Catalog.Services[2], hist: 2));
        Rows.Add(Row("93af6258", "85669", "Idris Mohamed",    "Aug 14, 2007", status: CommitmentStatus.Draft, ft: "Individual Funding", mccss: new[] { "Temporary Funding Allocation - Adult" }, pa: 980, addl: "Local Municipality", aa: 140, av: 140, att: 1, vendor: "Maple Care Inc.", rate: 62, units: 16));
        Rows.Add(Row("a5827e3d", "96804", "Camila Vega",      "Jul 25, 2013", status: CommitmentStatus.Draft, min: "Intake Team", pa: 450, vendor: "BrightPath Services", rate: 45, units: 10));
        Rows.Add(Row("6c038f1b", "17946", "Jonas Bergström",  "Nov 11, 2010", status: CommitmentStatus.Draft, ft: "Pooled Funding", place: "Group Living", mccss: new[] { Catalog.MccssFunding[1] }, pa: 1820, att: 2, msg: 1, vendor: "Northern Light Co.", unit: "Day", rate: 195, units: 9, svcName: Catalog.Services[4]));
        Rows.Add(Row("d49a172e", "29087", "Aaliyah Brown",    "Mar 19, 2012", status: CommitmentStatus.Draft, pa: 520, att: 1, vendor: "BrightPath Services", rate: 48, units: 11));
        Rows.Add(Row("f8b62a04", "31228", "Anders Holm",      "Jun 06, 2008", status: CommitmentStatus.Draft, ft: "Individual Funding", mccss: new[] { "Passport Funding" }, pt: "Flexible", pa: 1280, att: 3, msg: 2, vendor: "Maple Care Inc.", rate: 76, units: 17, svcName: Catalog.Services[3]));
        Rows.Add(Row("27e5d8c9", "42369", "Nia Okafor",       "Oct 30, 2009", status: CommitmentStatus.Draft, pa: 680, addl: "Trillium Foundation", aa: 90, av: 90, vendor: "BrightPath Services", rate: 54, units: 13, hist: 2));
        Rows.Add(Row("5b09e173", "53401", "Pavel Novák",      "Feb 12, 2014", status: CommitmentStatus.Draft, min: "Intake Team", pa: 410, vendor: "Aurora Support Co-op", rate: 44, units: 9));
        Rows.Add(Row("aef38c61", "64542", "Linnea Andersson", "Sep 23, 2007", status: CommitmentStatus.Draft, ft: "Individual Funding", place: "Independent Living", mccss: new[] { "Temporary Funding Allocation - Adult" }, pa: 1180, att: 2, vendor: "Maple Care Inc.", rate: 70, units: 17, svcName: Catalog.Services[2]));
        Rows.Add(Row("1d4f29ba", "75683", "Diego Fernandes",  "May 08, 2011", status: CommitmentStatus.Draft, pa: 740, att: 1, msg: 1, vendor: "BrightPath Services", rate: 58, units: 13));
        Rows.Add(Row("8042bcf5", "86824", "Sana Iqbal",       "Aug 17, 2013", status: CommitmentStatus.Draft, pa: 490, vendor: "Aurora Support Co-op", rate: 47, units: 10));
        Rows.Add(Row("b71e6f30", "97965", "Ethan Roberts",    "Dec 04, 2010", status: CommitmentStatus.Draft, ft: "Individual Funding", mccss: new[] { "Passport Funding" }, pt: "Capped", pa: 1380, att: 2, msg: 2, hist: 4, vendor: "Maple Care Inc.", rate: 75, units: 18, svcName: Catalog.Services[3]));
        Rows.Add(Row("3c8a05de", "19207", "Beatrice Müller",  "Apr 14, 2008", status: CommitmentStatus.Draft, pa: 870, att: 1, vendor: "Northern Light Co.", rate: 64, units: 14, svcName: Catalog.Services[2]));

        // A few more in other statuses
        Rows.Add(Row("f1d28e93", "21349", "Owen Foster", "Jul 06, 2009", status: CommitmentStatus.Pending, pa: 920, att: 1, vendor: "BrightPath Services", rate: 60, units: 15, svcName: Catalog.Services[1]));
        Rows.Add(Row("92ba174c", "32480", "Hana Suzuki", "Jan 19, 2011", status: CommitmentStatus.Finance, pa: 1600, att: 2, msg: 1, vendor: "Maple Care Inc.", rate: 80, units: 20, svcName: Catalog.Services[3]));
        Rows.Add(Row("5e7c12af", "43621", "Lucas Silva", "Sep 02, 2013", status: CommitmentStatus.Approved, pa: 560, att: 1, vendor: "BrightPath Services", rate: 50, units: 11));

        // ---- Project-based FCs (FRD §2.1, §5.2) ----
        var prj1 = Row("prj78a01", "p-001", "Northern Outreach", "—", status: CommitmentStatus.Pending,
            ft: "Group Funding", place: "Family Home", min: "Resource Allocation",
            mccss: new[] { "Special Services at Home (SSAH)" }, pt: "Restricted", pa: 28000,
            att: 4, msg: 2, hist: 6, vendor: "Northern Light Co.", unit: "Day", rate: 220, units: 90, svcName: Catalog.Services[4]);
        prj1.Association = AssociationType.Project;
        prj1.LinkedEntityId = "p-001";
        prj1.PermanentFundingId = "pf-101";
        Rows.Add(prj1);

        var prj2 = Row("prjfb273", "p-002", "Family Respite Pilot", "—", status: CommitmentStatus.Draft,
            ft: "Pooled Funding", min: "Service Coordinator",
            mccss: new[] { "Passport Funding" }, pa: 18000,
            att: 2, msg: 1, hist: 3, vendor: "Maple Care Inc.", rate: 75, units: 220, svcName: Catalog.Services[1]);
        prj2.Association = AssociationType.Project;
        prj2.LinkedEntityId = "p-002";
        prj2.PermanentFundingId = "pf-102";
        Rows.Add(prj2);

        var prj3 = Row("prjcc940", "p-004", "Inclusive Living", "—", status: CommitmentStatus.Awaiting,
            ft: "Pooled Funding", place: "Supported Living", min: "Clinical Team",
            mccss: new[] { "Permanent Funding Allocation - Adult" }, pa: 38000,
            att: 3, msg: 2, hist: 5, vendor: "BrightPath Services", rate: 95, units: 380, svcName: Catalog.Services[3]);
        prj3.Association = AssociationType.Project;
        prj3.LinkedEntityId = "p-004";
        prj3.PermanentFundingId = "pf-103";
        Rows.Add(prj3);

        // ---- Wire a few client FCs to PermanentFunding for the relationship demo ----
        WirePf("45f4368f", "pf-001");   // bill test          → PF-2026-001
        WirePf("f8065e44", "pf-002");   // Sarah Chen         → PF-2026-002
        WirePf("a5eb1251", "pf-003");   // Marcus Williams    → PF-2026-003
        WirePf("4f81d3a2", "pf-004");   // Hazel Wong         → PF-2026-004
        WirePf("9d2ae741", "pf-005");   // Avery Thompson     → PF-2026-005
        WirePf("5fac3d91", "pf-006");   // Amélie Tremblay    → PF-2026-006 (already-rolled)
        WirePf("b51633ce", "pf-002");   // Emma Larsson       → renewal of PF-2026-002
        WirePf("d6246691", "pf-004");   // Tariq Hassan       → PF-2026-004
        WirePf("2d9c12ee", "pf-003");   // Olivia Brown       → PF-2026-003

        // ---- Vendor-side sample FCs (so ISW / SP logins have data to test) ----
        // Ima Worker (ISW) — Approved FC for Hazel Wong with multiple service types
        Rows.Add(new FundingCommitment
        {
            CommitId = "iswvendr1",
            ClientId = "20716", ClientName = "Hazel Wong", Dob = "Aug 03, 2014", Year = "2026",
            Association = AssociationType.Client, LinkedEntityId = "20716",
            PermanentFundingId = "pf-004", FiscalYear = "FY2026", FundingSource = "MCCSS",
            Status = CommitmentStatus.Approved,
            FinanceApprovedAt = DateTime.Now.AddDays(-12), FinanceApprovedBy = "Fatima Finance",
            FundingType = "Individual Funding", Placement = "Family Home",
            MainFunder = "MCCSS", AddlFunder = "— None —", Ministry = "Children's Case Management (CCM)",
            MccssTypes = new() { "Passport Funding", "Special Services at Home (SSAH)" },
            PlanAmounts = new()
            {
                ["Passport Funding"] = 12000,
                ["Special Services at Home (SSAH)"] = 4800
            },
            PlanType = "Restricted",
            Creator = "Jatin Kakrey", Date = "May 10, 2026", Time = "09:30 AM",
            PlanStart = "2026-04-01", PlanEnd = "2027-03-31",
            Services = new()
            {
                new ServiceLine { Name = "Personal Development Support", Plan = "Passport Funding",
                    Provider = "Independent Support Worker", Vendor = "Ima Worker",
                    Unit = "Hour", Rate = 48, Units = 120 },
                new ServiceLine { Name = "In-Home Respite Services", Plan = "Passport Funding",
                    Provider = "Independent Support Worker", Vendor = "Ima Worker",
                    Unit = "Hour", Rate = 55, Units = 80 },
                new ServiceLine { Name = "Community Participation Support", Plan = "Special Services at Home (SSAH)",
                    Provider = "Independent Support Worker", Vendor = "Ima Worker",
                    Unit = "Session", Rate = 75, Units = 30 }
            }
        });

        // Ima Worker — second Approved FC for Sarah Chen
        Rows.Add(new FundingCommitment
        {
            CommitId = "iswvendr2",
            ClientId = "84291", ClientName = "Sarah Chen", Dob = "Mar 04, 2012", Year = "2026",
            Association = AssociationType.Client, LinkedEntityId = "84291",
            PermanentFundingId = "pf-002", FiscalYear = "FY2026", FundingSource = "MCCSS",
            Status = CommitmentStatus.Active,
            FundingType = "Individual Funding", Placement = "Family Home",
            MainFunder = "MCCSS", AddlFunder = "— None —", Ministry = "Children's Case Management (CCM)",
            MccssTypes = new() { "Passport Funding" },
            PlanAmounts = new() { ["Passport Funding"] = 6000 },
            PlanType = "Restricted",
            Creator = "Jatin Kakrey", Date = "Apr 22, 2026", Time = "10:15 AM",
            PlanStart = "2026-04-01", PlanEnd = "2027-03-31",
            Services = new()
            {
                new ServiceLine { Name = "Personal Development Support", Plan = "Passport Funding",
                    Provider = "Independent Support Worker", Vendor = "Ima Worker",
                    Unit = "Hour", Rate = 50, Units = 100 },
                new ServiceLine { Name = "Behavioural Support Services", Plan = "Passport Funding",
                    Provider = "Independent Support Worker", Vendor = "Ima Worker",
                    Unit = "Session", Rate = 95, Units = 12 }
            }
        });

        // Maple Care Inc. (SP) — Approved FC for Aaron Mackenzie
        Rows.Add(new FundingCommitment
        {
            CommitId = "spvendor1",
            ClientId = "10342", ClientName = "Aaron Mackenzie", Dob = "May 12, 2010", Year = "2026",
            Association = AssociationType.Client, LinkedEntityId = "10342",
            FiscalYear = "FY2026", FundingSource = "MCCSS",
            Status = CommitmentStatus.Approved,
            FinanceApprovedAt = DateTime.Now.AddDays(-8), FinanceApprovedBy = "Fatima Finance",
            FundingType = "Individual Funding", Placement = "Family Home",
            MainFunder = "MCCSS", AddlFunder = "— None —", Ministry = "Children's Case Management (CCM)",
            MccssTypes = new() { "Passport Funding" },
            PlanAmounts = new() { ["Passport Funding"] = 15000 },
            PlanType = "Restricted",
            Creator = "Jatin Kakrey", Date = "May 02, 2026", Time = "02:00 PM",
            PlanStart = "2026-04-01", PlanEnd = "2027-03-31",
            Services = new()
            {
                new ServiceLine { Name = "Behavioural Support Services", Plan = "Passport Funding",
                    Provider = "Service Provider Organization", Vendor = "Maple Care Inc.",
                    Unit = "Hour", Rate = 85, Units = 100 },
                new ServiceLine { Name = "Caregiver Support / Education", Plan = "Passport Funding",
                    Provider = "Service Provider Organization", Vendor = "Maple Care Inc.",
                    Unit = "Session", Rate = 120, Units = 20 }
            }
        });

        // Maple Care Inc. — second FC for Tariq Hassan (already in PF-004)
        Rows.Add(new FundingCommitment
        {
            CommitId = "spvendor2",
            ClientId = "42915", ClientName = "Tariq Hassan", Dob = "Jul 03, 2011", Year = "2026",
            Association = AssociationType.Client, LinkedEntityId = "42915",
            FiscalYear = "FY2026", FundingSource = "MCCSS",
            Status = CommitmentStatus.Active,
            FundingType = "Individual Funding", Placement = "Family Home",
            MainFunder = "MCCSS", AddlFunder = "— None —", Ministry = "Adult Case Management (ACM)",
            MccssTypes = new() { "Permanent Funding Allocation - Children's" },
            PlanAmounts = new() { ["Permanent Funding Allocation - Children's"] = 18000 },
            PlanType = "Restricted",
            Creator = "Jatin Kakrey", Date = "Apr 28, 2026", Time = "11:45 AM",
            PlanStart = "2026-04-01", PlanEnd = "2027-03-31",
            Services = new()
            {
                new ServiceLine { Name = "Therapy Services", Plan = "Permanent Funding Allocation - Children's",
                    Provider = "Service Provider Organization", Vendor = "Maple Care Inc.",
                    Unit = "Session", Rate = 145, Units = 100 },
                new ServiceLine { Name = "Skills Development Coaching", Plan = "Permanent Funding Allocation - Children's",
                    Provider = "Service Provider Organization", Vendor = "Maple Care Inc.",
                    Unit = "Hour", Rate = 90, Units = 35 }
            }
        });
    }

    private void WirePf(string commitId, string pfId)
    {
        var r = FindByCommitId(commitId);
        if (r != null) r.PermanentFundingId = pfId;
    }
}
