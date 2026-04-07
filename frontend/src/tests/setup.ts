import '@testing-library/jest-dom'

// Suprimir aviso de window.scrollTo não implementado no jsdom
// TanStack Router usa scroll restoration que jsdom não suporta — não afeta os testes
Object.defineProperty(window, 'scrollTo', {
  value: () => {},
  writable: true,
})
