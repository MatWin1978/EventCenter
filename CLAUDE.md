# EventCenter — Entwicklungsregeln für Claude

## Blazor Render Mode

**Pflicht:** Jede Seite mit `@onclick`, `@bind`, `EditForm.OnValidSubmit` oder anderen Event-Handlern **muss** `@rendermode InteractiveServer` direkt nach den `@attribute`/`@layout`-Direktiven haben.

```razor
@page "/admin/events/create"
@attribute [Authorize(Roles = "Admin")]
@layout MainLayout
@rendermode InteractiveServer   ← IMMER bei interaktiven Seiten
```

**Ohne `@rendermode InteractiveServer`:**
- `@onclick`-Handler feuern nicht (Klick tut nichts)
- `EditForm.OnValidSubmit` wirft Antiforgery-Fehler
- `@bind`-Werte werden nicht aktualisiert

**Betrifft alle Seiten mit Formularen oder dynamischen Aktionen.** Reine Anzeigeseiten (nur `<a href>`, keine Events) können Static SSR bleiben.

**Navigation in Buttons:** Statt `@onclick="() => NavigationManager.NavigateTo(...)"` immer `<a href="...">` verwenden — funktioniert auch in Static SSR.

---

## Keycloak Rollenkonfiguration

### Pflichteinstellung im Keycloak-Client

Der `realm roles`-Mapper im `roles`-Client-Scope muss **`id.token.claim = true`** haben (Standard ist `false`).

Ohne diese Einstellung kommen Realm-Rollen **nur im Access Token** an, nicht im ID Token. Die OIDC-Middleware verarbeitet aber den ID Token → `realm_access`-Claim fehlt → `IsInRole()` gibt immer `false` zurück.

**Keycloak Admin-Konsole:**
Realm Settings → Client Scopes → `roles` → Mappers → `realm roles` → `Add to ID token: ON`

**Oder per API:**
```bash
# realm roles mapper: id.token.claim auf true setzen
curl -X PUT http://localhost:8080/admin/realms/eventcenter/client-scopes/{SCOPE_ID}/protocol-mappers/models/{MAPPER_ID} \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"config": {"id.token.claim": "true", ...}}'
```

### Claim-Typ in Program.cs

`TokenValidationParameters.RoleClaimType = "role"` und die manuell hinzugefügten Claims **müssen denselben Typ** verwenden:

```csharp
// RICHTIG: Typ "role" passend zu RoleClaimType = "role"
claimsIdentity.AddClaim(new Claim("role", roleValue));

// FALSCH: ClaimTypes.Role = "http://schemas.microsoft.com/.../role" → IsInRole() findet nichts
claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
```

---

## OIDC Logout mit id_token_hint

Der `id_token` muss **vor** dem Cookie-Clear gelesen werden, da der OIDC-Handler ihn aus dem aktiven Ticket liest:

```csharp
app.MapGet("/auth/signout", async (HttpContext context) =>
{
    // id_token ZUERST lesen, solange Cookie noch gültig ist
    var idToken = await context.GetTokenAsync("id_token");

    var properties = new AuthenticationProperties { RedirectUri = "/" };
    if (!string.IsNullOrEmpty(idToken))
        properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = idToken }]);

    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
});
```

Falsche Reihenfolge (Cookie zuerst) → `id_token_hint` fehlt → Keycloak-Session bleibt aktiv.

---

## Home.razor Redirect-Logik

`Home.razor` (`/`) nutzt `[Authorize]` für unauthentifizierte Nutzer (delegiert an `AuthorizeRouteView` → `RedirectToLogin`). Für authentifizierte Nutzer erfolgt die rollenbasierte Weiterleitung in `OnInitializedAsync`. `forceLoad: true` ist Pflicht (Static SSR, kein aktiver Blazor-Circuit beim ersten Seitenaufruf).

---

## DateTime und EF Core / SQL Server

SQL Server speichert keine Zeitzoneninformation. EF Core materialisiert `DateTime`-Werte daher immer mit `Kind = DateTimeKind.Unspecified`. Niemals auf `Kind == Utc` prüfen und dann werfen — stattdessen `Unspecified` als UTC behandeln:

```csharp
// TimeZoneHelper.ConvertUtcToCet — korrekte Behandlung:
if (utcDateTime.Kind == DateTimeKind.Unspecified)
    utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
else if (utcDateTime.Kind != DateTimeKind.Utc)
    throw new ArgumentException("DateTime must be UTC", nameof(utcDateTime));
```

Überall wo DateTime aus dem DB-Kontext kommt und an `ConvertUtcToCet` / `FormatDateTimeCet` übergeben wird, entweder `DateTime.SpecifyKind(dt, DateTimeKind.Utc)` aufrufen oder den obigen Fallback in der Helper-Methode verlassen.

---

## Testvorgaben

| Rolle | Nutzer | Passwort | Ziel nach Login |
|-------|--------|----------|-----------------|
| Admin | `admin` | `Test1234!` | `/admin/events` |
| Makler | `makler` | `Test1234!` | `/portal/events` |

Für Tests mit unauthentifizierten Nutzern: **Incognito-Fenster** verwenden (aktive Keycloak-Session im Normalbrowser führt zu Silent-Login).
