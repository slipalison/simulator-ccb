import type { ReactNode } from "react";

interface PageLayoutProps {
  children: ReactNode;
}

/**
 * Template: layout de página com header, main e footer como slots.
 * Stateless — recebe children e os posiciona. Sem lógica de negócio.
 */
export function PageLayout({ children }: PageLayoutProps) {
  return (
    <div className="min-h-screen flex flex-col bg-background">
      <header className="border-b px-6 py-4">
        <span className="font-semibold text-foreground">Onboarding</span>
      </header>
      <main className="flex-1 px-6 py-8">{children}</main>
      <footer className="border-t px-6 py-4 text-sm text-muted-foreground text-center">
        &copy; {new Date().getFullYear()} Onboarding
      </footer>
    </div>
  );
}
