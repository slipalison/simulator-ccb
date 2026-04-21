const fs = require('fs');

let di = fs.readFileSync('src/Onboarding.Infrastructure/DependencyInjection.cs', 'utf8');

const replacement = `
        var keycloakBaseUrl = configuration["Keycloak:AuthServerUrl"]
            ?? throw new InvalidOperationException("Keycloak:AuthServerUrl not configured.");
        var adminClientId = configuration["Keycloak:AdminClientId"]
            ?? "onboarding-api-admin";
        var adminClientSecret = configuration["Keycloak:AdminClientSecret"]
            ?? throw new InvalidOperationException("Keycloak:AdminClientSecret not configured.");

        services.AddClientCredentialsTokenManagement()
            .AddClient("keycloak-admin-client", client =>
            {
                client.ClientId = ClientId.Parse(adminClientId);
                client.ClientSecret = ClientSecret.Parse(adminClientSecret);
                client.TokenEndpoint = new Uri(
                    $"{keycloakBaseUrl.TrimEnd('/')}/realms/client/protocol/openid-connect/token");
            })
            .AddClient("keycloak-admin-backoffice", client =>
            {
                client.ClientId = ClientId.Parse(adminClientId);
                client.ClientSecret = ClientSecret.Parse(adminClientSecret);
                client.TokenEndpoint = new Uri(
                    $"{keycloakBaseUrl.TrimEnd('/')}/realms/backoffice/protocol/openid-connect/token");
            });

        services.AddHttpClient("keycloak-admin-client", client =>
            {
                client.BaseAddress = new Uri(keycloakBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddClientCredentialsTokenHandler("keycloak-admin-client");

        services.AddHttpClient("keycloak-admin-backoffice", client =>
            {
                client.BaseAddress = new Uri(keycloakBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddClientCredentialsTokenHandler("keycloak-admin-backoffice");
`;

// regex to replace from "var keycloakBaseUrl" to "services.AddScoped<IKeycloakUserService, KeycloakUserService>();"
const regex = /var keycloakBaseUrl.*?services\.AddHttpClient\("keycloak-admin-api"[\s\S]*?\.AddClientCredentialsTokenHandler\(.*?\);/s;
di = di.replace(regex, replacement.trim());

// Also remove AddKeycloakAdminHttpClient block if present
const sdkRegex = /services\.AddKeycloakAdminHttpClient[\s\S]*?;\s*/s;
di = di.replace(sdkRegex, '');

fs.writeFileSync('src/Onboarding.Infrastructure/DependencyInjection.cs', di);
console.log('Replaced DI configuration');
