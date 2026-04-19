const fs = require('fs');

function replaceInFile(filePath, isBackoffice) {
    let content = fs.readFileSync(filePath, 'utf8');
    const methods = [
        'CreateUserAsync', 'CreateAdminUserAsync', 'DeleteUserByEmailAsync', 
        'UserExistsByEmailAsync', 'GetUserByEmailAsync', 'GetUserByIdAsync',
        'UpdateUserPasswordAsync', 'SetTemporaryPasswordFlagAsync', 
        'RemoveUpdatePasswordRequiredActionAsync', 'AssignAdminRoleAsync',
        'BlockUserAsync', 'UnblockUserAsync', 'GetUsersByRoleAsync', 'ClearFirstLoginFlagAsync'
    ];
    
    const realmStr = isBackoffice ? '"backoffice", ' : '"client", ';
    
    for (let m of methods) {
        let regex = new RegExp('_keycloakUserService\\.' + m + '\\(', 'g');
        content = content.replace(regex, '_keycloakUserService.' + m + '(' + realmStr);
    }
    fs.writeFileSync(filePath, content);
}

const clientFiles = [
    'src/Onboarding.Application/Clients/Commands/RegisterClientCommandHandler.cs',
    'src/Onboarding.Application/Admin/Queries/GetUserDetailsQuery.cs',
    'src/Onboarding.Application/Admin/Queries/GetPaginatedUsersQuery.cs',
    'src/Onboarding.Application/Admin/Commands/BlockUserCommand.cs',
    'src/Onboarding.Application/Admin/Commands/UnblockUserCommand.cs',
    'src/Onboarding.Application/Admin/Commands/ForcePasswordChangeCommand.cs',
    'src/Onboarding.Application/Admin/Commands/DeleteUserCommand.cs',
    'src/Onboarding.Application/Auth/Commands/ResetPasswordCommand.cs',
    'src/Onboarding.Application/Auth/Commands/ForgotPasswordCommand.cs'
];

const backofficeFiles = [
    'src/Onboarding.Application/Admin/Queries/GetAdministratorsQuery.cs',
    'src/Onboarding.Application/Admin/Commands/CreateAdminCommand.cs'
];

clientFiles.forEach(f => replaceInFile(f, false));
backofficeFiles.forEach(f => replaceInFile(f, true));
console.log('Parameters updated');
