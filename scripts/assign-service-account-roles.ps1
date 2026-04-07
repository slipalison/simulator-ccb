# Assign manage-users and view-users roles to onboarding-api-admin service account

$adminPass = "Admin@Keycloak2026!"
$kcUrl = "http://localhost:8180"
$realm = "onboarding"
$apiAdminClientId = "onboarding-api-admin"

Write-Host "Step 1: Getting admin token..."
$tokenResp = Invoke-RestMethod -Uri "$kcUrl/realms/master/protocol/openid-connect/token" -Method Post -ContentType "application/x-www-form-urlencoded" -Body "username=admin&password=$([System.Net.WebUtility]::UrlEncode($adminPass))&grant_type=password&client_id=admin-cli"
$token = $tokenResp.access_token
$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }

Write-Host "Step 2: Getting API admin client ID..."
$clients = Invoke-RestMethod -Uri "$kcUrl/admin/realms/$realm/clients?clientId=$apiAdminClientId" -Headers $headers
$apiAdminId = $clients[0].id
Write-Host "  Client ID: $apiAdminId"

Write-Host "Step 3: Getting service account user..."
$saUser = Invoke-RestMethod -Uri "$kcUrl/admin/realms/$realm/clients/$apiAdminId/service-account-user" -Headers $headers
$saUserId = $saUser.id
Write-Host "  Service Account User ID: $saUserId"

Write-Host "Step 4: Getting realm-management client ID..."
$rmClients = Invoke-RestMethod -Uri "$kcUrl/admin/realms/$realm/clients?clientId=realm-management" -Headers $headers
$rmClientId = $rmClients[0].id
Write-Host "  Realm Management Client ID: $rmClientId"

Write-Host "Step 5: Getting role IDs..."
$roles = Invoke-RestMethod -Uri "$kcUrl/admin/realms/$realm/clients/$rmClientId/roles" -Headers $headers
$manageUsers = $roles | Where-Object { $_.name -eq "manage-users" }
$viewUsers = $roles | Where-Object { $_.name -eq "view-users" }
Write-Host "  manage-users: $($manageUsers.id)"
Write-Host "  view-users: $($viewUsers.id)"

Write-Host "Step 6: Assigning roles to service account..."
$rolesToAdd = @(
    @{ id = $manageUsers.id; name = "manage-users"; clientRole = $true; containerId = $rmClientId },
    @{ id = $viewUsers.id; name = "view-users"; clientRole = $true; containerId = $rmClientId }
)

Invoke-RestMethod -Uri "$kcUrl/admin/realms/$realm/users/$saUserId/role-mappings/clients/$rmClientId" -Method Post -Headers $headers -Body ($rolesToAdd | ConvertTo-Json -Depth 3)

Write-Host ""
Write-Host "SUCCESS: Roles assigned to service account!" -ForegroundColor Green
Write-Host ""

# Verify
Write-Host "Verifying roles..."
$assignedRoles = Invoke-RestMethod -Uri "$kcUrl/admin/realms/$realm/users/$saUserId/role-mappings/clients/$rmClientId" -Headers $headers
$assignedRoles | ForEach-Object { Write-Host "  ✅ $($_.name)" -ForegroundColor Green }
