import '@testing-library/jest-dom'
import { vi } from 'vitest'

// Suprimir aviso de window.scrollTo não implementado no jsdom
// TanStack Router usa scroll restoration que jsdom não suporta — não afeta os testes
Object.defineProperty(window, 'scrollTo', {
  value: () => {},
  writable: true,
})

// Mock global de matchMedia para next-themes
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false, // default: light mode
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})
