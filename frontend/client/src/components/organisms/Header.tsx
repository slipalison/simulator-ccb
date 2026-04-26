import { ThemeToggle } from "../atoms/ThemeToggle"
import { ProfileBadge } from "../atoms/ProfileBadge"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { User, LogOut } from "lucide-react"
import { useAuth } from "../../lib/auth-context"

export function Header() {
  const { auth, logout } = useAuth()

  function handleLogout() {
    logout() // synchronous redirect to /auth/logout via Vinxi server
  }

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto flex h-14 items-center px-4">
        {/* Logo */}
        <div className="flex items-center gap-2 mr-auto">
          <span className="text-xl font-bold">{"\uD83C\uDFE2"} Onboarding</span>
        </div>

        {/* Right side */}
        <div className="flex items-center gap-2">
          <ThemeToggle />

          {/* Access group badge (when authenticated) */}
          {auth.isAuthenticated && auth.accessGroup && (
            <ProfileBadge group={auth.accessGroup} />
          )}

          {/* User menu */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" aria-label="User menu">
                <User className="h-5 w-5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {auth.isAuthenticated && (
                <DropdownMenuItem onClick={() => { window.location.href = "/profile"; }}>
                  Meu Perfil
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleLogout}>
                <LogOut className="mr-2 h-4 w-4" />
                <span>Sair</span>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  )
}