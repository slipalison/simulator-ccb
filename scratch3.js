const fs = require('fs');

let code = fs.readFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', 'utf8');

// 1. Remove hardcoded _backofficeRealm and _clientRealm
code = code.replace(/    private readonly string _backofficeRealm = "backoffice";\n    private readonly string _clientRealm = "client";\n/, '');

// 2. Add 'string targetRealm' to all interface implementation methods
const methods = [
    'CreateUserAsync', 'CreateAdminUserAsync', 'DeleteUserByEmailAsync', 
    'UserExistsByEmailAsync', 'GetUserByEmailAsync', 'GetUserByIdAsync',
    'UpdateUserPasswordAsync', 'SetTemporaryPasswordFlagAsync', 
    'RemoveUpdatePasswordRequiredActionAsync', 'AssignAdminRoleAsync',
    'BlockUserAsync', 'UnblockUserAsync', 'GetUsersByRoleAsync', 'ClearFirstLoginFlagAsync'
];

for (let m of methods) {
    let regex = new RegExp(`public async Task<.*?> ${m}\\(`, 'g');
    let match = regex.exec(code);
    if (!match) {
        regex = new RegExp(`public async Task ${m}\\(`, 'g');
        match = regex.exec(code);
    }
    if (match) {
        code = code.replace(match[0], match[0] + 'string targetRealm, ');
    }
}

// 3. Replace internal calls to use targetRealm instead of _clientRealm / _backofficeRealm
code = code.replace(/_clientRealm/g, 'targetRealm');
code = code.replace(/_backofficeRealm/g, 'targetRealm');

// 4. Update the specific calls inside KeycloakUserService where one method calls another method!
// e.g., AssignAdminRoleAsync(userId, ct) -> AssignAdminRoleAsync(targetRealm, userId, ct)
code = code.replace(/AssignAdminRoleAsync\(userId/g, 'AssignAdminRoleAsync(targetRealm, userId');

fs.writeFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', code);
console.log('Fixed KeycloakUserService.cs');
