// ---------------------------------------------------------------------------
// StatusTransitionDropdown.test.tsx — render + interaction + a11y tests (T-6)
// ---------------------------------------------------------------------------

import { describe, it, expect, vi } from "vitest";
import React from "react";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { StatusTransitionDropdown } from "@/components/organisms/StatusTransitionDropdown";
import { FUNDO_STATUS_LABELS } from "@/lib/fundos-schemas";

// Mock Radix DropdownMenu with plain HTML elements.
// jsdom cannot simulate Radix pointer events / portals.
vi.mock("@/components/ui/dropdown-menu", () => ({
  DropdownMenu: ({ children }: { children: React.ReactNode }) =>
    React.createElement("div", null, children),

  DropdownMenuTrigger: ({
    children,
    asChild,
  }: {
    children: React.ReactNode;
    asChild?: boolean;
  }) => {
    if (asChild) {
      // Render the child element directly without cloning (avoids Rules-of-Hooks issue)
      return React.createElement("div", { "data-testid": "dropdown-trigger" }, children);
    }
    return React.createElement("div", null, children);
  },

  DropdownMenuContent: ({ children }: { children: React.ReactNode }) =>
    React.createElement("div", { role: "listbox" }, children),

  DropdownMenuItem: ({
    children,
    onSelect,
    disabled,
  }: {
    children: React.ReactNode;
    onSelect?: () => void;
    disabled?: boolean;
  }) =>
    React.createElement(
      "button",
      {
        role: "option",
        onClick: onSelect,
        disabled,
        "aria-selected": false,
      },
      children
    ),
}));

describe("StatusTransitionDropdown", () => {
  it("renders trigger button with current status label", () => {
    render(
      <StatusTransitionDropdown
        currentStatus="RASCUNHO"
        allowedTransitions={["ATIVO"]}
        isLoadingTransitions={false}
        onTransition={vi.fn()}
        statusLabels={FUNDO_STATUS_LABELS}
      />
    );
    expect(screen.getByText("Rascunho")).toBeInTheDocument();
  });

  it("disables button when no transitions available", () => {
    render(
      <StatusTransitionDropdown
        currentStatus="ENCERRADO"
        allowedTransitions={[]}
        isLoadingTransitions={false}
        onTransition={vi.fn()}
        statusLabels={FUNDO_STATUS_LABELS}
      />
    );
    expect(screen.getByRole("button", { name: /alterar status/i })).toBeDisabled();
  });

  it("disables button while loading transitions", () => {
    render(
      <StatusTransitionDropdown
        currentStatus="ATIVO"
        allowedTransitions={undefined}
        isLoadingTransitions={true}
        onTransition={vi.fn()}
        statusLabels={FUNDO_STATUS_LABELS}
      />
    );
    expect(screen.getByRole("button", { name: /alterar status/i })).toBeDisabled();
  });

  it("shows allowed transitions in dropdown menu", () => {
    render(
      <StatusTransitionDropdown
        currentStatus="RASCUNHO"
        allowedTransitions={["ATIVO"]}
        isLoadingTransitions={false}
        onTransition={vi.fn()}
        statusLabels={FUNDO_STATUS_LABELS}
      />
    );
    // With mocked Radix, DropdownMenuContent always renders (no portal/open state)
    expect(screen.getByText("Ativo")).toBeInTheDocument();
  });

  it("calls onTransition with selected status", async () => {
    const onTransition = vi.fn().mockResolvedValue(undefined);
    render(
      <StatusTransitionDropdown
        currentStatus="RASCUNHO"
        allowedTransitions={["ATIVO"]}
        isLoadingTransitions={false}
        onTransition={onTransition}
        statusLabels={FUNDO_STATUS_LABELS}
      />
    );
    fireEvent.click(screen.getByRole("option", { name: "Ativo" }));
    await waitFor(() => {
      expect(onTransition).toHaveBeenCalledWith("ATIVO");
    });
  });

  it("has accessible aria-haspopup attribute on trigger button", () => {
    render(
      <StatusTransitionDropdown
        currentStatus="ATIVO"
        allowedTransitions={["SUSPENSO"]}
        isLoadingTransitions={false}
        onTransition={vi.fn()}
        statusLabels={FUNDO_STATUS_LABELS}
      />
    );
    expect(
      screen.getByRole("button", { name: /alterar status/i })
    ).toHaveAttribute("aria-haspopup", "listbox");
  });
});
