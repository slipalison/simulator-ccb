import type { ReactNode } from "react";

interface AuthLayoutProps {
  children: ReactNode;
  title: string;
  subtitle: string;
  footer?: ReactNode;
}

/**
 * Template: layout padrao para telas de autenticacao (login, registro, etc).
 * Card centralizado com header, body e footer visualmente distintos.
 * Background suave da pagina, card com shadow e bordas arredondadas.
 */
export function AuthLayout({ children, title, subtitle, footer }: AuthLayoutProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 to-slate-100 px-4 py-12">
      <div className="w-full max-w-md">
        {/* Card */}
        <div className="rounded-xl border bg-white shadow-lg overflow-hidden">
          {/* Header: branding area */}
          <div className="border-b border-slate-200 px-8 pt-8 pb-6">
            <div className="flex items-center gap-3 mb-4">
              {/* Brand icon */}
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  className="h-5 w-5 text-primary"
                >
                  <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
                  <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
              </div>
            </div>
            <h1 className="text-2xl font-bold text-slate-900">{title}</h1>
            <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
          </div>

          {/* Body: form content area */}
          <div className="px-8 py-6">{children}</div>

          {/* Footer: secondary actions/links */}
          {footer && (
            <div className="border-t border-slate-200 bg-slate-50 px-8 py-4">
              {footer}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
