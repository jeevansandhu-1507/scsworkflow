# SCSPortal — Funding Commitment Rollover (Blazor WebAssembly)

A .NET 8 Blazor WebAssembly port of the `funding-commitments.html` prototype.

## Run it

```bash
cd SCSPortal
dotnet run
```

Then open the URL printed in the console (usually `https://localhost:5001`).

Requires the .NET 8 SDK.

## Project structure

```
SCSPortal/
├── SCSPortal.csproj                       # net8.0, Blazor WASM SDK
├── Program.cs                             # bootstrap + DI registrations
├── App.razor                              # Router
├── _Imports.razor                         # global usings
│
├── Models/
│   └── FundingCommitment.cs               # FundingCommitment, ServiceLine,
│                                          # CommitmentStatus enum, StatusInfo,
│                                          # Catalog (funders, MCCSS types, etc.)
│
├── Services/
│   ├── FundingCommitmentService.cs        # in-memory state (rows, filter, search,
│   │                                      # page), seed data, approve/reject/select
│   └── ToastService.cs                    # pub/sub for toast notifications
│
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor               # shell: <Sidebar/> + <Topbar/> + @Body
│   │   ├── Sidebar.razor                  # nav (Funding section is active)
│   │   ├── Topbar.razor                   # breadcrumbs, search, lang, notif
│   │   └── Toast.razor                    # toast notification renderer
│   └── CommitmentCard.razor               # one row in the queue
│
├── Pages/
│   └── FundingCommitments.razor           # the @page "/" — stat strip, filters,
│                                          # list, pagination, bulk actions
│
├── Properties/
│   └── launchSettings.json
│
└── wwwroot/
    ├── index.html                         # WASM host page
    └── css/app.css                        # ported design tokens + component CSS
```

## What's included from the prototype

- Sidebar with the same nav structure, user chip, and SCS logo
- Topbar with breadcrumbs, global search, date/time, language toggle, notifications
- Page header with eyebrow, title, and 4-card stat strip (clickable filters)
- Filter chips (In Draft / All / Pending / Approved / Not Approved) with live counts
- Commitment cards in compact view: avatar, funding chips, ID, status pill, activity
  icons, committed amount, action buttons
- Search across client name, ID, commit ID, ministry, MCCSS type, funders, vendors,
  services — same fields as the prototype's `getFiltered()`
- Approve / Revise actions, single and bulk
- Row selection with selection-aware toolbar
- Pagination (8 per page, matches the prototype)
- Toast notifications
- All design tokens, status pill colors, and the brand teal palette

## What was deferred (next steps)

These are present in the prototype but not in this conversion — they're substantial
enough that they were better tackled as separate iterations:

1. **Bulk-edit drawer** (`#bulkDrawer`) — the right-side panel for setting status,
   plan period, MCCSS types, plan type, ministry, outcome on many rows at once.
2. **Full-record view** (`#fullView`) — the modal-style detail view with tabs for
   Details / Services / Funding / Attachments / Messages / History.
3. **Inline editing** of plan period, main funder, MCCSS types, plan amounts,
   funding/placement/ministry pickers, and the per-row services table.
4. **Compact / Detailed / Ultra view modes** — the `viewToggle` cycle. Currently
   only `compact` is rendered.
5. **Keyboard shortcuts** (J/K/Space/A/E/Cmd+K). Easy to add via a JS interop
   `@onkeydown` on the page root once the views above are in.
6. **Status dropdown menu** on the pill — currently the pill renders but isn't
   interactive; approve/revise buttons drive state changes instead.
7. **Persistence** — everything is in-memory. Refresh resets the seed.

## Notes on the architecture choice

- **State lives in `FundingCommitmentService`** (singleton). Components subscribe
  to `OnChange` and call `NotifyChanged()` after mutations. This matches how the
  prototype's `state` object worked — one source of truth, broadcast-on-change.
- **No JS interop is needed** for the current feature set. The prototype's
  client-side `state` + `renderRows()` model maps cleanly to Blazor's component
  re-render cycle.
- **CSS is ported wholesale** rather than rewritten as Razor scoped styles, so
  the visual fidelity to the prototype stays high and the tokens remain easy to
  tweak in one place.
# scsworkflow
