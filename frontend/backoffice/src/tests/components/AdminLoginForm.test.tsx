// ---------------------------------------------------------------------------
// AdminLoginForm — render, validation, submit, server error
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AdminLoginForm } from "@/components/molecules/AdminLoginForm";

describe("AdminLoginForm", () => {
  it("renders email and password inputs with labels", () => {
    render(<AdminLoginForm onSubmit={vi.fn()} />);
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/senha/i)).toBeInTheDocument();
  });

  it("renders submit button", () => {
    render(<AdminLoginForm onSubmit={vi.fn()} />);
    expect(screen.getByTestId("admin-login-button")).toBeInTheDocument();
  });

  it("shows server error when provided", () => {
    render(<AdminLoginForm onSubmit={vi.fn()} serverError="Credenciais inválidas" />);
    expect(screen.getByRole("alert")).toHaveTextContent("Credenciais inválidas");
  });

  it("does not show alert when serverError is null", () => {
    render(<AdminLoginForm onSubmit={vi.fn()} serverError={null} />);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows validation error on empty submit", async () => {
    const user = userEvent.setup();
    render(<AdminLoginForm onSubmit={vi.fn()} />);
    await user.click(screen.getByTestId("admin-login-button"));
    await waitFor(() =>
      expect(screen.getByText(/email e obrigatorio/i)).toBeInTheDocument()
    );
  });

  it("shows email validation error on invalid email", async () => {
    const user = userEvent.setup();
    render(<AdminLoginForm onSubmit={vi.fn()} />);
    await user.type(screen.getByTestId("admin-email"), "not-an-email");
    await user.click(screen.getByTestId("admin-login-button"));
    await waitFor(() =>
      expect(screen.getByText(/email invalido/i)).toBeInTheDocument()
    );
  });

  it("calls onSubmit with email and password on valid submit", async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<AdminLoginForm onSubmit={onSubmit} />);
    await user.type(screen.getByTestId("admin-email"), "admin@onboarding.local");
    await user.type(screen.getByTestId("admin-password"), "password123");
    await user.click(screen.getByTestId("admin-login-button"));
    await waitFor(() =>
      expect(onSubmit).toHaveBeenCalledWith(
        { email: "admin@onboarding.local", password: "password123" },
        expect.anything()
      )
    );
  });

  it("shows loading text when isLoading=true", () => {
    render(<AdminLoginForm onSubmit={vi.fn()} isLoading={true} />);
    expect(screen.getByTestId("admin-login-button")).toHaveTextContent("Entrando...");
  });

  it("disables button when isLoading=true", () => {
    render(<AdminLoginForm onSubmit={vi.fn()} isLoading={true} />);
    expect(screen.getByTestId("admin-login-button")).toBeDisabled();
  });
});
