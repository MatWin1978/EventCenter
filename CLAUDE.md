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

### Problem

Keycloak meldet `"Missing parameters: id_token_hint"` beim Logout.

**Ursache:** `GetTokenAsync("id_token")` kann `null` zurückgeben (z. B. wenn der Cookie-Ticket in einem Edge-Case keine Tokens enthält). Dann wird kein Token in die `AuthenticationProperties` geschrieben, und der OIDC-Handler schickt kein `id_token_hint` an Keycloak.

**Funktioniert nicht (zu fragil):**
```csharp
var idToken = await context.GetTokenAsync("id_token"); // kann null sein!
properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = idToken }]);
await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
```

### Lösung: `OnRedirectToIdentityProviderForSignOut` + OIDC zuerst

**1.** Im OIDC-Event das `id_token` explizit aus dem noch-aktiven Cookie lesen und direkt auf die Protocol Message setzen:

```csharp
// In AddOpenIdConnect → options.Events:
OnRedirectToIdentityProviderForSignOut = async context =>
{
    // Cookie ist hier noch aktiv (OIDC SignOut feuert zuerst)
    var result = await context.HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    var idToken = result?.Properties?.GetTokenValue("id_token");
    if (!string.IsNullOrEmpty(idToken))
    {
        context.ProtocolMessage.IdTokenHint = idToken;
    }
}
```

**2.** Im Endpoint OIDC zuerst abmelden (Cookie noch aktiv), dann Cookie löschen:

```csharp
app.MapGet("/auth/signout", async (HttpContext context) =>
{
    // OIDC zuerst → Event liest id_token aus aktivem Cookie
    var properties = new AuthenticationProperties { RedirectUri = "/" };
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
    // Cookie danach → Set-Cookie-Header wird zur 302-Response hinzugefügt
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
});
```

**Warum die Reihenfolge funktioniert:** `Response.Redirect()` committed die Response nicht sofort — `Set-Cookie`-Header können noch hinzugefügt werden, bevor die Response tatsächlich gesendet wird.

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
