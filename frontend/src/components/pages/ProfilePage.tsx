import { useEffect, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Separator } from "@/components/ui/separator"
import { Button } from "@/components/ui/button"
import { Header } from "../organisms/Header"
import { getProfileClient } from "../../lib/api"
import { useAuth } from "../../lib/auth-context"
import { useNavigate } from "@tanstack/react-router"
import { LogOut } from "lucide-react"

interface ClientProfile {
  personType: "pf" | "pj"
  nome?: string
  razaoSocial?: string
  cpf?: string
  cnpj?: string
  email: string
  telefone?: string
}

/**
 * ProfilePage: exibe dados cadastrais do cliente autenticado em modo leitura.
 * Usa shadcn Card, Badge, Skeleton. Protegida por auth guard interno.
 */
export function ProfilePage() {
  const { auth, logout } = useAuth()
  const navigate = useNavigate()
  const [profile, setProfile] = useState<ClientProfile | null>(null)
  const [loading, setLoading] = useState(true)

  // Auth guard
  useEffect(() => {
    if (!auth.isAuthenticated) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      navigate({ to: "/login" as any })
    }
  }, [auth.isAuthenticated, navigate])

  // Fetch profile data
  useEffect(() => {
    async function fetchProfile() {
      try {
        const data = await getProfileClient()
        // Map backend DTO to our UI shape
        const mapped: ClientProfile = {
          personType: data.type === "PessoaFisica" ? "pf" : "pj",
          nome: data.name || data.cpf ? data.name : undefined,
          razaoSocial: data.razaoSocial || undefined,
          cpf: data.cpf || undefined,
          cnpj: data.cnpj || undefined,
          email: data.email,
          telefone: data.phone || undefined,
        }
        setProfile(mapped)
      } catch (err) {
        console.error("Failed to fetch profile:", err)
      } finally {
        setLoading(false)
      }
    }
    if (auth.isAuthenticated) {
      fetchProfile()
    }
  }, [auth.isAuthenticated])

  async function handleLogout() {
    logout()
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    navigate({ to: "/login" as any })
  }

  if (!auth.isAuthenticated) return null

  if (loading) {
    return (
      <div className="min-h-screen bg-background">
        <Header />
        <div className="container mx-auto max-w-2xl py-8 px-4">
          <Card>
            <CardHeader>
              <Skeleton className="h-8 w-40" />
            </CardHeader>
            <CardContent className="space-y-4">
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  if (!profile) return null

  return (
    <div className="min-h-screen bg-background">
      <Header />
      <div className="container mx-auto max-w-2xl py-8 px-4">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle className="text-2xl">Meu Perfil</CardTitle>
              <Badge variant={profile.personType === "pf" ? "default" : "secondary"}>
                {profile.personType === "pf" ? "Pessoa Física" : "Pessoa Jurídica"}
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="space-y-6">
            {/* PF Fields */}
            {profile.personType === "pf" && profile.nome && (
              <div>
                <p className="text-sm text-muted-foreground">Nome completo</p>
                <p className="text-base font-medium">{profile.nome}</p>
              </div>
            )}

            {profile.personType === "pf" && profile.cpf && (
              <div>
                <p className="text-sm text-muted-foreground">CPF</p>
                <p className="text-base font-mono">{profile.cpf}</p>
              </div>
            )}

            {/* PJ Fields */}
            {profile.personType === "pj" && profile.razaoSocial && (
              <div>
                <p className="text-sm text-muted-foreground">Razao Social</p>
                <p className="text-base font-medium">{profile.razaoSocial}</p>
              </div>
            )}

            {profile.personType === "pj" && profile.cnpj && (
              <div>
                <p className="text-sm text-muted-foreground">CNPJ</p>
                <p className="text-base font-mono">{profile.cnpj}</p>
              </div>
            )}

            <Separator />

            {/* Common Fields */}
            <div>
              <p className="text-sm text-muted-foreground">Email</p>
              <p className="text-base">{profile.email}</p>
            </div>

            {profile.telefone && (
              <div>
                <p className="text-sm text-muted-foreground">Telefone</p>
                <p className="text-base">{profile.telefone}</p>
              </div>
            )}

            <Separator />

            {/* Logout */}
            <Button variant="outline" className="w-full" onClick={handleLogout}>
              <LogOut className="mr-2 h-4 w-4" />
              Sair
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
