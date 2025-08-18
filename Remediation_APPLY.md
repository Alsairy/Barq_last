# BARQ Remediation Patch — Apply & Verify

This patch adds:
- Default authorization fallback (requires auth by default)
- Swagger protection in non-dev (401 if unauthenticated)
- Recycle Bin controller + service stubs
- Frontend components (DataTable, NotificationBell, RecycleBinPage, TenantSwitcher)
- Playwright E2E scaffolding

## Apply

```bash
git checkout -b feat/remediation-stubs
git am BARQ_remediation_patch.patch
```

> If `git am` fails, use `git apply BARQ_remediation_patch.patch` then commit.

## Backend integration steps

1. **Soft delete global filter (optional but recommended):**
   - Call the helper in `BarqDbContext.OnModelCreating` after `ApplyConfigurationsFromAssembly`:
   ```csharp
   modelBuilder.AddSoftDeleteQueryFilter();
   ```

2. **DI (already wired):**
   - `RecycleBinService` is registered in `Program.cs`:
   ```csharp
   builder.Services.AddScoped<BARQ.Application.Services.RecycleBin.IRecycleBinService, BARQ.Application.Services.RecycleBin.RecycleBinService>();
   ```

3. **Auth fallback (already wired):**
   - `builder.Services.AddDefaultAuthorization();` will require auth unless `[AllowAnonymous]` is present.

4. **Swagger guard (already wired):**
   - In non-dev, `/swagger` and `/api-docs` require authentication.

## Frontend usage

- Use `TenantSwitcher` in your header:
  ```tsx
  import TenantSwitcher from './src/features/tenants/TenantSwitcher';
  // ...
  <TenantSwitcher />
  ```

- Place `NotificationBell` in your header:
  ```tsx
  import NotificationBell from './src/features/notifications/NotificationBell';
  <NotificationBell />
  ```

- Mount `RecycleBinPage` on a route (e.g., `/recycle-bin`).

- Use `DataTable` in list views to unify search/filter/sort/paginate patterns.

## Run

```bash
# Backend
dotnet build Backend/BARQ.sln -c Release

# Frontend
cd Frontend/barq-frontend
npm i
npm run dev    # or npm run build && npm run preview
npm run e2e    # runs Playwright smoke test
```

## Verify

- Auth is required on API endpoints and on Swagger in non-dev.
- `/api/recycle-bin?entity=Project` lists soft-deleted records (stub reflection implementation).
- `/api/recycle-bin/{entity}/{id}/restore` restores a record by id.
- Frontend renders NotificationBell, TenantSwitcher, RecycleBinPage; `DataTable` can be used in list screens.

### Notes

- The generic recycle bin uses reflection and BaseEntity’s `IsDeleted/DeletedAt/DeletedById` fields. Consider replacing with strongly-typed services per entity for stricter policies.
- If your existing tests expect unauthenticated Swagger access in staging/prod, update them to authenticate first.
