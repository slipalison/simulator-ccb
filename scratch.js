const fs = require('fs');

let code = fs.readFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', 'utf8');

code = code.replace(/private readonly string _realm;/, 'private readonly string _backofficeRealm = "backoffice";\n    private readonly string _clientRealm = "client";');
code = code.replace(/_realm = configuration\["Keycloak:Realm"\] \?\? "onboarding";/, '');

const backofficeMethods = ['CreateAdminUserAsync', 'AssignAdminRoleAsync', 'GetUsersByRoleAsync'];

for (const m of backofficeMethods) {
    const regex = new RegExp(`(public async Task.*${m}[\\s\\S]*?\\})\\s*public`, 'm');
    const match = code.match(regex);
    if (!match) continue;
    let methodBody = match[1];
    methodBody = methodBody.replace(/_realm/g, '_backofficeRealm');
    code = code.replace(match[1], methodBody);
}

// Just in case any are left/at the bottom
let lastRegex = /(public async Task.*GetUsersByRoleAsync[\s\S]*?\\})/;
if (code.match(lastRegex)) {
    // Actually the easiest way to ensure all methods got it is to just manually replace the last few.
    // Instead of regex for method bounds, I will just manually replace in the whole file and then fix the ones I want.
}

code = code.replace(/_realm/g, '_clientRealm');

fs.writeFileSync('src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs', code);
console.log('Done');
