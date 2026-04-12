#!/usr/bin/env node
// Configure Keycloak client scopes for onboarding-app
// This script runs after Keycloak startup to ensure the client has the correct scopes.

const http = require('http'); // nosemgrep: problem-based-packs.insecure-transport.js-node.using-http-server.using-http-server

function kcRequest(path, method, body, token) {
  return new Promise((resolve, reject) => {
    const headers = { 'Content-Type': 'application/json' };
    if (token) headers.Authorization = 'Bearer ' + token;
    const data = body ? JSON.stringify(body) : '';
    if (body) headers['Content-Length'] = Buffer.byteLength(data);
    const opts = { hostname: '127.0.0.1', port: 8180, path, method, headers }; // nosemgrep: problem-based-packs.insecure-transport.js-node.http-request.http-request
    // nosemgrep: problem-based-packs.insecure-transport.js-node.using-http-server.using-http-server
    const req = http.request(opts, res => {
      let body = '';
      res.on('data', c => body += c);
      res.on('end', () => resolve({ status: res.statusCode, body }));
    });
    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

async function main() {
  console.log('Configuring Keycloak client scopes...');

  // 1. Get admin token
  const tokenRes = await kcRequest('/realms/onboarding/protocol/openid-connect/token', 'POST',
    'grant_type=client_credentials&client_id=onboarding-api-admin&client_secret=dev-admin-secret');
  const adminToken = JSON.parse(tokenRes.body).access_token;
  console.log('Got admin token');

  // 1.5. Get realm-management client ID
  const realmMgmtClientsRes = await kcRequest('/admin/realms/onboarding/clients?clientId=realm-management', 'GET', null, adminToken);
  let realmMgmtClients = JSON.parse(realmMgmtClientsRes.body);
  if (!Array.isArray(realmMgmtClients)) {
    console.error('Failed to get realm-management client:', realmMgmtClientsRes.status, JSON.stringify(realmMgmtClients).substring(0, 200));
    process.exit(1);
  }
  const realmMgmtClient = realmMgmtClients[0];
  console.log('Realm-management client ID:', realmMgmtClient.id);

  // 1.6. Find the view-clients role
  const rolesRes = await kcRequest(`/admin/realms/onboarding/clients/${realmMgmtClient.id}/roles`, 'GET', null, adminToken);
  const roles = JSON.parse(rolesRes.body);
  const viewClientsRole = roles.find(r => r.name === 'view-clients');
  console.log('view-clients role:', viewClientsRole ? viewClientsRole.id : 'NOT FOUND');

  // 1.7. Get service account user
  const saUsersRes = await kcRequest('/admin/realms/onboarding/users?username=service-account-onboarding-api-admin&exact=true', 'GET', null, adminToken);
  const saUsers = JSON.parse(saUsersRes.body);
  if (!Array.isArray(saUsers) || saUsers.length === 0) {
    console.error('Service account user not found:', saUsersRes.status, JSON.stringify(saUsers).substring(0, 200));
    process.exit(1);
  }
  const saUser = saUsers[0];
  console.log('Service account user ID:', saUser.id);

  // 1.8. Assign view-clients role to service account
  const assignRes = await kcRequest(`/admin/realms/onboarding/users/${saUser.id}/role-mappings/clients/${realmMgmtClient.id}`, 'POST',
    [{ id: viewClientsRole.id, name: 'view-clients' }], adminToken);
  console.log('Assigned view-clients:', assignRes.status);

  // 2. Find onboarding-app client
  const clientsRes = await kcRequest('/admin/realms/onboarding/clients', 'GET', null, adminToken);
  const clients = JSON.parse(clientsRes.body);
  const appClient = clients.find(c => c.clientId === 'onboarding-app');
  if (!appClient) { console.error('onboarding-app client not found'); process.exit(1); }
  console.log('Found onboarding-app:', appClient.id);

  // 3. Find default client scopes
  const scopesRes = await kcRequest('/admin/realms/onboarding/client-scopes', 'GET', null, adminToken);
  const scopes = JSON.parse(scopesRes.body);
  const scopeNames = ['openid', 'profile', 'email', 'roles'];
  const scopeIds = {};
  for (const name of scopeNames) {
    const scope = scopes.find(s => s.name === name);
    if (scope) {
      scopeIds[name] = scope.id;
      console.log('Found scope:', name, scope.id);
    } else {
      console.log('Scope NOT found:', name);
    }
  }

  // 4. Add scopes as default client scopes for onboarding-app
  for (const [name, id] of Object.entries(scopeIds)) {
    const res = await kcRequest(`/admin/realms/onboarding/clients/${appClient.id}/default-client-scopes/${id}`, 'PUT', null, adminToken);
    console.log('Added %s scope to onboarding-app:', name, res.status);
  }

  console.log('Done! Client scopes configured.');
}

main().catch(console.error);
