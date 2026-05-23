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
    ],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      thresholds: {
        perFile: true,
        lines: 80,
        branches: 80,
        functions: 80,
        statements: 80,
      },
      include: [
        // D-2 NEW files only — Phase 52 additions (post-boundary 968eefb)
        // Lib layer
        'src/lib/admin-fundos-schemas.ts',
        'src/lib/admin-fundos-api.ts',
        'src/lib/admin-companies-api.ts',
        'src/lib/query-client.ts',
        // Atoms
        'src/components/atoms/AuditEventRow.tsx',
        // Molecules — Phase 52 new
        'src/components/molecules/AdminAssociationTable.tsx',
        'src/components/molecules/AdminEntityHeader.tsx',
        'src/components/molecules/AdminFieldsGrid.tsx',
        'src/components/molecules/AdminPaginator.tsx',
        'src/components/molecules/AdminSearchInput.tsx',
        'src/components/molecules/AuditHistorySection.tsx',
        'src/components/molecules/EmpresaFilterDropdown.tsx',
        // Pages — Phase 52 new (T-4, T-5, T-6, T-7)
        'src/components/pages/AdminFundosListPage.tsx',
        'src/components/pages/AdminCedentesListPage.tsx',
        'src/components/pages/AdminConsultoriasFundoListPage.tsx',
        'src/components/pages/AdminCustodiantesListPage.tsx',
        'src/components/pages/AdminFundoDetailPage.tsx',
        'src/components/pages/AdminCedenteDetailPage.tsx',
        'src/components/pages/AdminConsultoriaFundoDetailPage.tsx',
        'src/components/pages/AdminCustodianteDetailPage.tsx',
        'src/components/pages/AdminFundoCedentesListPage.tsx',
        'src/components/pages/AdminFundoTiposAtivosListPage.tsx',
        'src/components/pages/AdminCedenteTiposAtivosListPage.tsx',
      ],
      exclude: ['src/**/*.test.tsx', 'src/**/*.test.ts'],
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
