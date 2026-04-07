#!/usr/bin/env node
/**
 * UAT Test Runner — Onboarding API
 *
 * Uso:
 *   node tests/run-uat.mjs                  # todos os testes (sem brute force)
 *   node tests/run-uat.mjs --setup          # desbloqueia contas no Keycloak antes de rodar
 *   node tests/run-uat.mjs --brute-force    # inclui testes de brute force
 *   node tests/run-uat.mjs --verbose        # mostra corpo das respostas
 *   node tests/run-uat.mjs --fail-fast      # para no primeiro erro real
 */

import fs   from 'fs';
import path from 'path';
import { randomUUID }    from 'crypto';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// ─── CLI ──────────────────────────────────────────────────────────────────────
const args              = process.argv.slice(2);
const INCLUDE_BRUTE     = args.includes('--brute-force');
const VERBOSE           = args.includes('--verbose') || args.includes('-v');
const FAIL_FAST         = args.includes('--fail-fast');
const SETUP_MODE        = args.includes('--setup');
const HTTP_FILE         = args.find(a => a.endsWith('.http'))
                        ?? path.join(__dirname, 'uat-tests.http');

// Keycloak admin — mesmos valores do compose.yaml / .env
const KC_URL            = 'http://localhost:8180';
const KC_REALM          = 'onboarding';
const KC_ADMIN_USER     = 'admin';
const KC_ADMIN_PASS     = process.env.KC_ADMIN_PASSWORD ?? 'Admin@Keycloak2026!';

// Contas que precisam existir e estar desbloqueadas para os testes rodarem
const REQUIRED_USERS    = ['joao.uat@teste.com'];

// ─── ANSI ─────────────────────────────────────────────────────────────────────
const c = {
  reset:  '\x1b[0m',  bold:   '\x1b[1m',
  green:  '\x1b[32m', red:    '\x1b[31m',
  yellow: '\x1b[33m', cyan:   '\x1b[36m',
  gray:   '\x1b[90m',
};
const ok   = s => `${c.green}${s}${c.reset}`;
const fail = s => `${c.red}${s}${c.reset}`;
const warn = s => `${c.yellow}${s}${c.reset}`;
const dim  = s => `${c.gray}${s}${c.reset}`;
const bold = s => `${c.bold}${s}${c.reset}`;

// ─── Globals (simula client.global do Rider) ──────────────────────────────────
const globals = {};

// ─── Keycloak Admin helpers ───────────────────────────────────────────────────
async function kcAdminToken() {
  const res = await fetch(
    `${KC_URL}/realms/master/protocol/openid-connect/token`,
    {
      method:  'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body:    `grant_type=password&client_id=admin-cli&username=${KC_ADMIN_USER}&password=${encodeURIComponent(KC_ADMIN_PASS)}`,
    }
  );
  if (!res.ok) throw new Error(`Admin token falhou: ${res.status}`);
  return (await res.json()).access_token;
}

async function kcGetUser(token, email) {
  const res = await fetch(
    `${KC_URL}/admin/realms/${KC_REALM}/users?email=${encodeURIComponent(email)}&exact=true`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  if (!res.ok) throw new Error(`Busca de usuário falhou: ${res.status}`);
  const users = await res.json();
  return users[0] ?? null;
}

async function kcClearBruteForce(token, userId) {
  await fetch(
    `${KC_URL}/admin/realms/${KC_REALM}/attack-detection/brute-force/users/${userId}`,
    { method: 'DELETE', headers: { Authorization: `Bearer ${token}` } }
  );
}

async function kcPatchLastName(token, userId, user) {
  if (user.lastName && user.lastName !== '') return false; // já tem lastName
  await fetch(
    `${KC_URL}/admin/realms/${KC_REALM}/users/${userId}`,
    {
      method:  'PUT',
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      body:    JSON.stringify({ lastName: '-' }),
    }
  );
  return true;
}

async function kcEnableUser(token, userId) {
  await fetch(
    `${KC_URL}/admin/realms/${KC_REALM}/users/${userId}`,
    {
      method:  'PUT',
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      body:    JSON.stringify({ enabled: true }),
    }
  );
}

// ─── Setup: desbloqueia contas ────────────────────────────────────────────────
async function runSetup() {
  console.log(`\n${bold(c.cyan + '  Setup: desbloqueando contas no Keycloak...' + c.reset)}`);
  let token;
  try {
    token = await kcAdminToken();
  } catch (e) {
    console.log(`  ${fail('✗')} Admin token: ${e.message}`);
    console.log(`  ${warn('→ Verifique se o Keycloak está rodando e KC_ADMIN_PASSWORD está correto.')}`);
    return false;
  }
  console.log(`  ${ok('✓')} Token admin obtido`);

  let allOk = true;
  for (const email of REQUIRED_USERS) {
    const user = await kcGetUser(token, email);
    if (!user) {
      console.log(`  ${warn('⚠')} ${email} não encontrado no Keycloak ${dim('(será criado pelo Test 10)')}`);
      continue;
    }
    await kcClearBruteForce(token, user.id);
    await kcEnableUser(token, user.id);
    const patched = await kcPatchLastName(token, user.id, user);
    const patchTag = patched ? warn(' +lastName corrigido') : '';
    console.log(`  ${ok('✓')} ${email} desbloqueado${patchTag} ${dim(`(id: ${user.id.slice(0, 8)}…)`)}`);
  }
  console.log();
  return allOk;
}

// ─── Parser ───────────────────────────────────────────────────────────────────
function parseHttpFile(filePath) {
  const content  = fs.readFileSync(filePath, 'utf-8');
  const lines    = content.split(/\r?\n/);
  const fileVars = {};

  for (const line of lines) {
    const m = line.match(/^@(\w+)\s*=\s*(.+)$/);
    if (m) fileVars[m[1]] = m[2].trim();
  }

  const rawBlocks = [];
  let cur = [];
  for (const line of lines) {
    if (line.startsWith('###')) { if (cur.length) rawBlocks.push(cur); cur = [line]; }
    else cur.push(line);
  }
  if (cur.length) rawBlocks.push(cur);

  return { fileVars, tests: rawBlocks.map(parseBlock).filter(Boolean) };
}

function parseBlock(lines) {
  const title = lines[0].replace(/^###\s*/, '').trim();

  // Lê comentários
  let expected = null, atName = null, acceptAlso = null;
  const isBruteForce = /brute force/i.test(title) ||
                       /registrar usuário dedicado/i.test(title);

  for (const line of lines.slice(1)) {
    if (!/^\s*#/.test(line)) break;
    const mExp  = line.match(/Expected:\s*(\d{3})/i);
    const mName = line.match(/@name\s+(\w+)/);
    const mAlso = line.match(/Accept-Also:\s*(\d{3})/i);   // extensão custom
    if (mExp)  expected   = parseInt(mExp[1],  10);
    if (mName) atName     = mName[1];
    if (mAlso) acceptAlso = parseInt(mAlso[1], 10);
  }

  // Linha do método HTTP
  let mIdx = -1;
  for (let i = 1; i < lines.length; i++) {
    if (/^(GET|POST|PUT|PATCH|DELETE)\s+\S/.test(lines[i].trim())) { mIdx = i; break; }
  }
  if (mIdx === -1) return null;

  const [method, rawUrl] = lines[mIdx].trim().split(/\s+/, 2);

  // Headers
  const headers = {};
  let bodyStart = -1;
  for (let i = mIdx + 1; i < lines.length; i++) {
    const line = lines[i];
    if (line.trim() === '') { bodyStart = i + 1; break; }
    const m = line.match(/^([^:]+):\s*(.+)$/);
    if (m) headers[m[1].trim()] = m[2].trim();
  }

  // Body + post-processor
  let body = null, postProcessor = null;
  if (bodyStart !== -1) {
    const bodyLines = [], ppLines = [];
    let inPP = false;
    for (let i = bodyStart; i < lines.length; i++) {
      const line = lines[i];
      if (!inPP && line.trimStart().startsWith('> {%')) { inPP = true; continue; }
      if (inPP) {
        if (line.trim() === '%}') { inPP = false; postProcessor = ppLines.join('\n'); }
        else ppLines.push(line);
        continue;
      }
      bodyLines.push(line);
    }
    const raw = bodyLines.join('\n').trim();
    if (raw) body = raw;
  }

  return { title, atName, expected, acceptAlso, method, url: rawUrl,
           headers, body, postProcessor, isBruteForce };
}

// ─── Variable substitution ────────────────────────────────────────────────────
function sub(str, vars) {
  return str.replace(/\{\{([^}]+)\}\}/g, (m, name) => {
    name = name.trim();
    if (name === '$guid') return randomUUID();
    if (name in globals)  return globals[name];
    if (name in vars)     return vars[name];
    return m;
  });
}

const unresolved = str => str.match(/\{\{[^}]+\}\}/g) ?? [];

// ─── Post-processor ───────────────────────────────────────────────────────────
function applyPP(script, resText) {
  let body = {};
  try { body = JSON.parse(resText); } catch {}
  for (const m of script.matchAll(/client\.global\.set\(\s*"([^"]+)"\s*,\s*([^)]+)\)/g)) {
    const propM = m[2].match(/response\.body\.(\w+)/);
    if (propM && body[propM[1]] != null) globals[m[1]] = body[propM[1]];
  }
}

// ─── HTTP request ──────────────────────────────────────────────────────────────
async function runRequest(test, fileVars) {
  const url     = sub(test.url, fileVars);
  const headers = Object.fromEntries(
    Object.entries(test.headers).map(([k, v]) => [k, sub(v, fileVars)])
  );
  let body = test.body ? sub(test.body, fileVars) : null;

  const bad = [
    ...unresolved(url),
    ...Object.values(headers).flatMap(unresolved),
    ...(body ? unresolved(body) : []),
  ];
  if (bad.length) return { status: null, body: null, ms: 0, unresolved: bad };

  const t0 = Date.now();
  try {
    const res     = await fetch(url, {
      method:  test.method,
      headers,
      body:    test.method !== 'GET' && body ? body : undefined,
    });
    const ms      = Date.now() - t0;
    const resText = await res.text();
    if (test.postProcessor) applyPP(test.postProcessor, resText);
    return { status: res.status, body: resText, ms, unresolved: [] };
  } catch (e) {
    return { status: null, body: null, ms: Date.now() - t0,
             unresolved: [], networkError: e.message };
  }
}

// ─── Formatters ───────────────────────────────────────────────────────────────
function fmtStatus(code) {
  if (!code)    return fail('???');
  if (code < 300) return ok(code);
  if (code < 500) return warn(code);
  return fail(code);
}

function prettyBody(text, indent = '    ') {
  try {
    return JSON.stringify(JSON.parse(text), null, 2)
      .split('\n').map(l => dim(indent + l)).join('\n');
  } catch {
    return dim(indent + (text.length > 300 ? text.slice(0, 300) + '…' : text));
  }
}

// ─── Main ─────────────────────────────────────────────────────────────────────
async function main() {
  console.log(`\n${c.bold}${c.cyan}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${c.reset}`);
  console.log(`${c.bold}${c.cyan}  UAT Test Runner — Onboarding API${c.reset}`);
  console.log(`${c.bold}${c.cyan}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${c.reset}`);
  const flags = [
    SETUP_MODE    ? warn('--setup')       : null,
    INCLUDE_BRUTE ? warn('--brute-force') : null,
    VERBOSE       ? warn('--verbose')     : null,
    FAIL_FAST     ? warn('--fail-fast')   : null,
  ].filter(Boolean).join(' ') || dim('padrão');
  console.log(`  Flags   : ${flags}`);
  console.log(`  Arquivo : ${dim(HTTP_FILE)}`);

  if (SETUP_MODE) await runSetup();

  const { fileVars, tests } = parseHttpFile(HTTP_FILE);
  console.log();

  const results   = [];
  const missingGlobals = new Set();
  let nPass = 0, nFail = 0, nWarn = 0, nSkip = 0, nCascade = 0;

  for (const test of tests) {
    // ── Skip brute force ──────────────────────────────────────────────────
    if (test.isBruteForce && !INCLUDE_BRUTE) {
      nSkip++;
      console.log(`  ${dim('⏭')}  ${dim(test.title)}`);
      results.push({ test, outcome: 'skip' });
      continue;
    }

    // ── Detecta cascata ───────────────────────────────────────────────────
    const authHeader  = test.headers['Authorization'] ?? '';
    const bodyStr     = test.body ?? '';
    const isCascade   =
      (authHeader.includes('{{accessToken}}')  && missingGlobals.has('accessToken')) ||
      (bodyStr.includes('{{refreshToken}}')    && missingGlobals.has('refreshToken'));

    if (isCascade) {
      nCascade++; nFail++;
      console.log(`  ${warn('↯')}  ${bold(test.title)}  ${warn('CASCATA')} ${dim('(token não disponível)')}`);
      results.push({ test, outcome: 'cascade' });
      if (FAIL_FAST) break;
      continue;
    }

    // ── Executa ───────────────────────────────────────────────────────────
    const r = await runRequest(test, fileVars);

    // ── Avalia ────────────────────────────────────────────────────────────
    let outcome = 'info';

    if (r.networkError) {
      outcome = 'error'; nFail++;
    } else if (r.unresolved.length) {
      outcome = 'unresolved'; nFail++;
      r.unresolved.forEach(u => {
        const m = u.match(/\{\{(\w+)\}\}/);
        if (m) missingGlobals.add(m[1]);
      });
    } else if (test.expected != null) {
      const isExact  = r.status === test.expected;
      const isAlso   = test.acceptAlso != null && r.status === test.acceptAlso;
      // Re-run tolerância: registro retorna 409 quando expected=201 com mensagem de conflito
      const isRerun  = test.expected === 201 && r.status === 409;

      if (isExact) {
        outcome = 'pass'; nPass++;
      } else if (isAlso) {
        outcome = 'pass'; nPass++;
      } else if (isRerun) {
        outcome = 'rerun'; nWarn++;        // não conta como falha
      } else {
        outcome = 'fail'; nFail++;
        // Se loginValid falhou, tokens não estarão disponíveis
        if (test.atName === 'loginValid') {
          missingGlobals.add('accessToken');
          missingGlobals.add('refreshToken');
        }
      }
    } else {
      outcome = 'info'; nPass++;
    }

    results.push({ test, r, outcome });

    // ── Linha de saída ────────────────────────────────────────────────────
    const icon = {
      pass:       ok('✓'),
      fail:       fail('✗'),
      error:      fail('⚠'),
      unresolved: fail('⚠'),
      rerun:      warn('↺'),
      info:       `${c.cyan}·${c.reset}`,
    }[outcome] ?? dim('?');

    const expStr   = test.expected != null ? dim(`esperado: ${test.expected}`) : '';
    const mismatch = outcome === 'fail'  ? fail(' ← DIVERGÊNCIA') : '';
    const rerunTag = outcome === 'rerun' ? warn(' re-run (409 ok)') : '';

    if (r.networkError) {
      console.log(`  ${icon}  ${bold(test.title)}`);
      console.log(`       ${fail('Erro de rede:')} ${r.networkError}`);
    } else if (r.unresolved?.length) {
      console.log(`  ${icon}  ${bold(test.title)}`);
      console.log(`       ${fail('Var não resolvida:')} ${r.unresolved.join(', ')}`);
    } else {
      console.log(`  ${icon}  ${bold(test.title)}  ${fmtStatus(r.status)} ${expStr}${mismatch}${rerunTag}  ${dim(`(${r.ms}ms)`)}`);
      if (VERBOSE && r.body) console.log(prettyBody(r.body));
    }

    if (FAIL_FAST && outcome === 'fail') break;
  }

  // ─── Resumo ───────────────────────────────────────────────────────────────
  console.log(`\n${c.bold}${c.cyan}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${c.reset}`);
  const cascadeTxt = nCascade ? ` | ${warn(`${nCascade} cascata`)}` : '';
  const warnTxt    = nWarn    ? ` | ${warn(`${nWarn} re-run`)}` : '';
  console.log(`  ${ok(`${nPass} passou`)} | ${fail(`${nFail} falhou`)}${cascadeTxt}${warnTxt} | ${dim(`${nSkip} ignorado`)}`);
  console.log(`${c.bold}${c.cyan}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${c.reset}`);

  // ─── Detalhes de falhas ───────────────────────────────────────────────────
  const failures = results.filter(r => ['fail', 'error', 'unresolved'].includes(r.outcome));
  if (failures.length) {
    console.log(`\n${c.bold}${c.red}  Falhas:${c.reset}`);
    for (const { test, r } of failures) {
      console.log(`\n  ${fail('✗')} ${bold(test.title)}`);
      if (r.networkError) {
        console.log(`    ${fail('Erro de rede:')} ${r.networkError}`);
        console.log(`    ${warn('→ Verifique: docker compose ps')}`);
      } else if (r.unresolved?.length) {
        console.log(`    ${fail('Var não resolvida:')} ${r.unresolved.join(', ')}`);
      } else {
        console.log(`    Obtido: ${fmtStatus(r.status)}   Esperado: ${test.expected}`);
        if (r.body) console.log(prettyBody(r.body));

        // Diagnóstico contextual
        if (r.status === 503)
          console.log(`\n    ${warn('→ 503: Keycloak indisponível. Rode: docker compose ps')}`);
        if (r.status === 401 && test.atName === 'loginValid') {
          console.log(`\n    ${warn('→ Login falhou. Diagnóstico:')}`);
          console.log(`      ${dim('• Conta bloqueada por brute force → rode: node tests/run-uat.mjs --setup')}`);
          console.log(`      ${dim('• Usuário não existe no Keycloak  → delete dados e rode Test 10 primeiro')}`);
          console.log(`      ${dim('• Senha incorreta no .http         → verifique "Str0ng!Pass"')}`);
        }
        if (r.status === 422 && test.expected === 409)
          console.log(`\n    ${warn('→ 422 em vez de 409: provavelmente o CPF/CNPJ do payload é inválido.')}`);
      }
    }
  }

  // ─── Cascata ──────────────────────────────────────────────────────────────
  const cascades = results.filter(r => r.outcome === 'cascade');
  if (cascades.length) {
    console.log(`\n${c.bold}${c.yellow}  Cascata (${cascades.length}):${c.reset}`);
    console.log(`  ${warn('loginValid não retornou 200 → tokens ausentes.')}`);
    console.log(`  ${warn('→ Corrija o login e estes testes passarão automaticamente:')}`);
    cascades.forEach(({ test }) => console.log(`    ${dim('↳')} ${test.title}`));
  }

  // ─── Re-runs ──────────────────────────────────────────────────────────────
  const reruns = results.filter(r => r.outcome === 'rerun');
  if (reruns.length && !VERBOSE) {
    console.log(`\n${dim(`  Re-runs (${reruns.length}): usuários já existem de execução anterior — normal.`)}`);
    reruns.forEach(({ test }) => console.log(`    ${dim('↺')} ${dim(test.title)}`));
  }

  console.log();
  process.exit(nFail > 0 ? 1 : 0);
}

main().catch(e => { console.error(e); process.exit(1); });
