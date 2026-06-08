using SCSPortal.Models;

namespace SCSPortal.Services;

public class UserContextService
{
    public List<AppUser> Users { get; } = new()
    {
        new() { Id = "u-admin",    FullName = "Alex Admin",       Role = AppRole.Admin,           Email = "alex.admin@scs.test" },
        new() { Id = "u-finance",  FullName = "Fatima Finance",   Role = AppRole.Finance,         Email = "fatima.f@scs.test" },
        new() { Id = "u-cm",       FullName = "Jatin Kakrey",     Role = AppRole.CaseManager,     Email = "jatin.k@scs.test" },
        new() { Id = "u-special",  FullName = "Sam Special",      Role = AppRole.SpecialApprover, Email = "sam.s@scs.test" },
        new() { Id = "u-client",   FullName = "Aaron Mackenzie",  Role = AppRole.Client,          Email = "aaron@example.com",  LinkedClientId = "10342" },
        new() { Id = "u-isw",      FullName = "Ima Worker",       Role = AppRole.VendorIsw,       Email = "ima@worker.test",     LinkedVendorId = "v-isw-01" },
        new() { Id = "u-sp",       FullName = "Maple Care Admin", Role = AppRole.VendorSp,        Email = "ops@maplecare.test",  LinkedVendorId = "v-sp-01" }
    };

    public AppUser Current { get; private set; }
    public AppUser? Impersonating { get; private set; }   // when admin logs-in-as
    public AppUser Effective => Impersonating ?? Current;

    public event Action? OnChange;
    public void NotifyChanged() => OnChange?.Invoke();

    public UserContextService()
    {
        Current = Users.First(u => u.Role == AppRole.CaseManager);
    }

    public void Switch(string userId)
    {
        var u = Users.FirstOrDefault(x => x.Id == userId);
        if (u == null) return;
        Current = u;
        Impersonating = null;
        OnChange?.Invoke();
    }

    public void Impersonate(string userId)
    {
        if (Current.Role != AppRole.Admin) return;
        var u = Users.FirstOrDefault(x => x.Id == userId);
        if (u == null || u.Id == Current.Id) return;
        Impersonating = u;
        OnChange?.Invoke();
    }

    public void StopImpersonating()
    {
        Impersonating = null;
        OnChange?.Invoke();
    }

    public bool IsInternal => RoleInfo.IsInternal(Effective.Role);
    public bool IsAdmin => Effective.Role == AppRole.Admin;
    public bool IsFinance => Effective.Role == AppRole.Finance;
    public bool IsCaseManager => Effective.Role == AppRole.CaseManager;
    public bool IsSpecialApprover => Effective.Role == AppRole.SpecialApprover;
    public bool IsVendor => Effective.Role is AppRole.VendorIsw or AppRole.VendorSp;
    public bool IsClient => Effective.Role == AppRole.Client;
}

public class LocaleService
{
    public static AppLocale CurrentLocale { get; set; } = AppLocale.En;

    private AppLocale _locale = AppLocale.En;
    public AppLocale Locale
    {
        get => _locale;
        private set
        {
            _locale = value;
            CurrentLocale = value;
        }
    }
    public event Action? OnChange;

    public LocaleService()
    {
        Locale = AppLocale.En;
    }

    public void Toggle()
    {
        Locale = Locale == AppLocale.En ? AppLocale.Fr : AppLocale.En;
        OnChange?.Invoke();
    }

    public string T(string en, string fr) => Locale == AppLocale.En ? en : fr;

    // Common UI labels with FR translations (just enough to demo bilingual)
    public string FundingCommitments => T("Funding Commitments", "Engagements de financement");
    public string PermanentFunding => T("Permanent Funding", "Financement permanent");
    public string Projects => T("Projects", "Projets");
    public string Clients => T("Clients", "Clients");
    public string Vendors => T("Vendors", "Fournisseurs");
    public string Rollover => T("Funding Commitment Rollover", "Report d'engagement de financement");
    public string Scheduling => T("Scheduling", "Planification");
    public string Invoices => T("Invoices", "Factures");
    public string FinancialPressure => T("Financial Pressure", "Pression financière");
    public string Notifications => T("Notifications", "Notifications");
    public string AuditLog => T("Audit Log", "Journal d'audit");
    public string Admin => T("Admin", "Administration");
    public string Dashboard => T("Dashboard", "Tableau de bord");
    public string Approve => T("Approve", "Approuver");
    public string Reject => T("Reject", "Rejeter");
    public string Submit => T("Submit", "Soumettre");
}
