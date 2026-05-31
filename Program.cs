using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SCSPortal;
using SCSPortal.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<UserContextService>();
builder.Services.AddSingleton<LocaleService>();
builder.Services.AddSingleton<ClientDirectoryService>();
builder.Services.AddSingleton<ProjectService>();
builder.Services.AddSingleton<VendorService>();
builder.Services.AddSingleton<PermanentFundingService>();
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<ActiveContextService>();
builder.Services.AddSingleton<PermissionsService>();
builder.Services.AddSingleton<FundingCommitmentService>();
builder.Services.AddSingleton<SchedulingService>();
builder.Services.AddSingleton<InvoicingService>();
builder.Services.AddSingleton<FinancialPressureService>();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
