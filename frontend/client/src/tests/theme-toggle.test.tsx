import { describe, it, expect, vi, beforeEach } from "vitest"
import { render, screen, fireEvent } from "@testing-library/react"
import { ThemeProvider } from "next-themes"
import { ThemeToggle } from "@/components/atoms/ThemeToggle"

const renderWithTheme = (props = {}) =>
  render(
    <ThemeProvider attribute="class" defaultTheme="light" enableSystem={false} {...props}>
      <ThemeToggle />
    </ThemeProvider>
  )

describe("ThemeToggle", () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  it("deve renderizar o botao de toggle de tema", () => {
    renderWithTheme()

    const button = screen.getByRole("button", { name: /alternar tema/i })
    expect(button).toBeInTheDocument()
  })

  it("deve alternar tema ao clicar no botao", () => {
    renderWithTheme()

    // Inicia com light (sem classe .dark)
    expect(document.documentElement.classList.contains("dark")).toBe(false)

    // Clica para mudar para dark
    fireEvent.click(screen.getByRole("button", { name: /alternar tema/i }))
    expect(document.documentElement.classList.contains("dark")).toBe(true)

    // Clica novamente para voltar para light
    fireEvent.click(screen.getByRole("button", { name: /alternar tema/i }))
    expect(document.documentElement.classList.contains("dark")).toBe(false)
  })
})
