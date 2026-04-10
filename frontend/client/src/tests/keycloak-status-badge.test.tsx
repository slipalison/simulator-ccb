import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { KeycloakStatusBadge } from "@/components/molecules/KeycloakStatusBadge";

describe("Keycloak Status Badge", () => {
  it("shows enabled and email verified badges", () => {
    render(<KeycloakStatusBadge enabled emailVerified />);

    expect(screen.getByTestId("status-enabled")).toBeInTheDocument();
    expect(screen.getByTestId("status-email-verified")).toBeInTheDocument();
    expect(screen.getByText("Ativo")).toBeInTheDocument();
    expect(screen.getByText("Email verificado")).toBeInTheDocument();
  });

  it("shows disabled and email not verified badges", () => {
    render(<KeycloakStatusBadge enabled={false} emailVerified={false} />);

    expect(screen.getByTestId("status-disabled")).toBeInTheDocument();
    expect(screen.getByTestId("status-email-not-verified")).toBeInTheDocument();
    expect(screen.getByText("Inativo")).toBeInTheDocument();
    expect(screen.getByText("Email nao verificado")).toBeInTheDocument();
  });

  it("shows mixed state: enabled but email not verified", () => {
    render(<KeycloakStatusBadge enabled emailVerified={false} />);

    expect(screen.getByTestId("status-enabled")).toBeInTheDocument();
    expect(screen.getByTestId("status-email-not-verified")).toBeInTheDocument();
    expect(screen.getByText("Ativo")).toBeInTheDocument();
    expect(screen.getByText("Email nao verificado")).toBeInTheDocument();
  });

  it("shows mixed state: disabled but email verified", () => {
    render(<KeycloakStatusBadge enabled={false} emailVerified />);

    expect(screen.getByTestId("status-disabled")).toBeInTheDocument();
    expect(screen.getByTestId("status-email-verified")).toBeInTheDocument();
    expect(screen.getByText("Inativo")).toBeInTheDocument();
    expect(screen.getByText("Email verificado")).toBeInTheDocument();
  });

  it("renders both badges in the container", () => {
    render(<KeycloakStatusBadge enabled emailVerified />);

    const container = screen.getByTestId("keycloak-status");
    expect(container).toBeInTheDocument();
    expect(container.children.length).toBe(2);
  });
});
