import { describe, it, expect, vi, beforeEach } from "vitest"
import { render, screen, fireEvent } from "@testing-library/react"
import { ThemeProvider, useTheme } from "next-themes"

// Componente consumidor para expor os valores do hook
const ThemeConsumer = () => {
  const { theme, resolvedTheme, setTheme } = useTheme()
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <span data-testid="resolved-theme">{resolvedTheme}</span>
      <button onClick={() => setTheme("dark")}>Set Dark</button>
      <button onClick={() => setTheme("light")}>Set Light</button>
    </div>
  )
}

const renderWithTheme = (props = {}) =>
  render(
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem {...props}>
      <ThemeConsumer />
    </ThemeProvider>
  )

describe("ThemeProvider", () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it("deve renderizar com tema light por padrao quando system prefers light", () => {
    renderWithTheme()

    const theme = screen.getByTestId("theme")
    expect(theme.textContent).toBe("system")
    const resolved = screen.getByTestId("resolved-theme")
    expect(resolved.textContent).toBe("light")
  })

  it("deve aplicar classe .dark no html quando tema e dark", () => {
    renderWithTheme({ defaultTheme: "dark", enableSystem: false })

    expect(document.documentElement.classList.contains("dark")).toBe(true)
  })

  it("deve persistir o tema no localStorage ao alterar", () => {
    renderWithTheme({ defaultTheme: "light", enableSystem: false })

    fireEvent.click(screen.getByText("Set Dark"))

    expect(localStorage.getItem("theme")).toBe("dark")
  })

  it("deve ler tema persistido do localStorage ao recarregar", () => {
    localStorage.setItem("theme", "dark")

    renderWithTheme({ enableSystem: false })

    const theme = screen.getByTestId("theme")
    expect(theme.textContent).toBe("dark")
    const resolved = screen.getByTestId("resolved-theme")
    expect(resolved.textContent).toBe("dark")
  })

  it("deve respeitar prefers-color-scheme na primeira visita", () => {
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      value: vi.fn().mockImplementation((query: string) => ({
        matches: true, // dark mode
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    })

    renderWithTheme()

    const resolved = screen.getByTestId("resolved-theme")
    expect(resolved.textContent).toBe("dark")
  })

  it("deve alternar entre light e dark corretamente", () => {
    renderWithTheme({ defaultTheme: "light", enableSystem: false })

    expect(document.documentElement.classList.contains("dark")).toBe(false)

    fireEvent.click(screen.getByText("Set Dark"))
    expect(document.documentElement.classList.contains("dark")).toBe(true)

    fireEvent.click(screen.getByText("Set Light"))
    expect(document.documentElement.classList.contains("dark")).toBe(false)
  })
})
