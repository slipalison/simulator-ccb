const fs = require('fs');

let code = fs.readFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', 'utf8');

// 1. Remove IKeycloakUserClient injection
code = code.replace(/private readonly IKeycloakUserClient _keycloakUserClient;/, '');
code = code.replace(/IKeycloakUserClient keycloakUserClient,/, '');
code = code.replace(/_keycloakUserClient = keycloakUserClient;/, '');
code = code.replace(/private readonly HttpClient _adminHttpClient;/, 'private readonly IHttpClientFactory _httpClientFactory;');
code = code.replace(/_adminHttpClient = httpClientFactory.CreateClient\("keycloak-admin-api"\);/, '_httpClientFactory = httpClientFactory;');

// 2. Add GetClient helper
code = code.replace(/public KeycloakUserService(.*?){\s*([\s\S]*?)\s*}/, `public KeycloakUserService$1{
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient(string targetRealm)
    {
        return targetRealm == "backoffice" 
            ? _httpClientFactory.CreateClient("keycloak-admin-backoffice")
            : _httpClientFactory.CreateClient("keycloak-admin-client");
    }`);

// 3. Replace _keycloakUserClient.CreateUserAsync
code = code.replace(/await _keycloakUserClient\.CreateUserAsync\(targetRealm, user, ct\);/g, `var response = await GetClient(targetRealm).PostAsJsonAsync($"admin/realms/{targetRealm}/users", user, ct);
            response.EnsureSuccessStatusCode();`);

// 4. Replace _keycloakUserClient.GetUsersAsync
code = code.replace(/await _keycloakUserClient\.GetUsersAsync\(\s*targetRealm,\s*new GetUsersRequestParameters { Email = (.*?), Exact = true },\s*ct\);/g, `await GetClient(targetRealm).GetFromJsonAsync<List<UserRepresentation>>($"admin/realms/{targetRealm}/users?email={Uri.EscapeDataString($1)}&exact=true", ct) ?? new List<UserRepresentation>()`);

// 5. Replace _keycloakUserClient.GetUserAsync
code = code.replace(/await _keycloakUserClient\.GetUserAsync\(\s*targetRealm, (.*?), cancellationToken: ct\)/g, `await GetClient(targetRealm).GetFromJsonAsync<UserRepresentation>($"admin/realms/{targetRealm}/users/{( $1 )}", ct)`);

// 6. Replace _keycloakUserClient.UpdateUserAsync
code = code.replace(/await _keycloakUserClient\.UpdateUserAsync\(targetRealm, (.*?), user, ct\);/g, `var updateResp = await GetClient(targetRealm).PutAsJsonAsync($"admin/realms/{targetRealm}/users/{( $1 )}", user, ct);
        updateResp.EnsureSuccessStatusCode();`);

// 7. Replace _keycloakUserClient.DeleteUserAsync
code = code.replace(/await _keycloakUserClient\.DeleteUserAsync\(targetRealm, (.*?), ct\);/g, `var delResp = await GetClient(targetRealm).DeleteAsync($"admin/realms/{targetRealm}/users/{( $1 )}", ct);
            delResp.EnsureSuccessStatusCode();`);

// 8. Replace _adminHttpClient usages with GetClient(targetRealm)
code = code.replace(/_adminHttpClient/g, 'GetClient(targetRealm)');

fs.writeFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', code);
console.log('Rewritten KeycloakUserService to strictly use IHttpClientFactory with explicit realms.');
