import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/tests/setup.ts'],
    exclude: [
      'node_modules/**',
      'dist/**',
      '.git/**',
      'playwright/**',
      'e2e/**',
    ],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      reportOnFailure: true,
      // D-2 gate: thresholds apply only to files created AFTER boundary commit
      // 968eefb (pre-D-2 files like profile-page, registration-form are excluded
      // to prevent legacy failures from blocking the gate).
      thresholds: {
        perFile: true,
        branches: 80,
        functions: 80,
        lines: 80,
        statements: 80,
      },
      include: [
        // D-2 new files (post-968eefb boundary) — explicit list
        'src/lib/api-errors.ts',
        'src/lib/fundos-api.ts',
        'src/lib/fundos-schemas.ts',
        'src/lib/query-client.ts',
        'src/lib/use-allowed-transitions.ts',
        'src/lib/telemetry.ts',
        'src/locales/pt-BR/fundos.ts',
        'src/components/atoms/CedenteTipoToggle.tsx',
        'src/components/guards/AuthGuard.tsx',
        'src/components/molecules/DateRangeInput.tsx',
        'src/components/molecules/LimiteExposicaoInput.tsx',
        'src/components/molecules/Paginator.tsx',
        'src/components/molecules/SearchInput.tsx',
        'src/components/organisms/AssociationForm.tsx',
        'src/components/organisms/AssociationTable.tsx',
        'src/components/organisms/CedentePfForm.tsx',
        'src/components/organisms/CedentePjForm.tsx',
        'src/components/organisms/CedenteTable.tsx',
        'src/components/organisms/CedenteTipoToggle.tsx',
        'src/components/organisms/ConsultoriaFundoForm.tsx',
        'src/components/organisms/ConsultoriaFundoTable.tsx',
        'src/components/organisms/CustodianteForm.tsx',
        'src/components/organisms/CustodianteTable.tsx',
        'src/components/organisms/FundoForm.tsx',
        'src/components/organisms/FundoStatusBadge.tsx',
        'src/components/organisms/FundoTable.tsx',
        'src/components/organisms/StatusTransitionDropdown.tsx',
        'src/components/organisms/TipoAtivoForm.tsx',
        'src/components/organisms/TipoAtivoTable.tsx',
        'src/components/pages/CedenteDetailPage.tsx',
        'src/components/pages/CedenteTiposAtivosTabPage.tsx',
        'src/components/pages/CedentesListPage.tsx',
        'src/components/pages/ConsultoriasFundoListPage.tsx',
        'src/components/pages/CustodiantesListPage.tsx',
        'src/components/pages/FundoCedentesTabPage.tsx',
        'src/components/pages/FundoDetailPage.tsx',
        'src/components/pages/FundoTiposAtivosTabPage.tsx',
        'src/components/pages/FundosListPage.tsx',
        'src/components/pages/TiposAtivoListPage.tsx',
      ],
      exclude: [
        'src/**/*.test.{ts,tsx}',
        'src/main.tsx',
        'src/router.tsx',
        'src/vinxi.d.ts',
      ],
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
