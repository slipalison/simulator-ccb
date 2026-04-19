const fs = require('fs');
let code = fs.readFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', 'utf8');

code = code.replace(/exact=true", ct\) \?\? new List<UserRepresentation>\(\)/g, 'exact=true", ct) ?? new List<UserRepresentation>();');
code = code.replace(/await GetClient\(targetRealm\)\.GetFromJsonAsync<UserRepresentation>\(\$"admin\/realms\/\{targetRealm\}\/users\/\{\( keycloakUserId \)\}", ct\)/g, 'await GetClient(targetRealm).GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{keycloakUserId}", ct);');
code = code.replace(/await GetClient\(targetRealm\)\.GetFromJsonAsync<UserRepresentation>\(\$"admin\/realms\/\{targetRealm\}\/users\/\{\( userId \)\}", ct\)/g, 'await GetClient(targetRealm).GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{userId}", ct);');
code = code.replace(/await GetClient\(targetRealm\)\.GetFromJsonAsync<UserRepresentation>\(\$"admin\/realms\/\{targetRealm\}\/users\/\{\( keycloakUserId \)\}", ct\);;/g, 'await GetClient(targetRealm).GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{keycloakUserId}", ct);');
code = code.replace(/await GetClient\(targetRealm\)\.GetFromJsonAsync<UserRepresentation>\(\$"admin\/realms\/\{targetRealm\}\/users\/\{\( userId \)\}", ct\);;/g, 'await GetClient(targetRealm).GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{userId}", ct);');

fs.writeFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', code);
