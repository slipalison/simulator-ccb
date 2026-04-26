import { useEffect, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Separator } from "@/components/ui/separator"
import { getProfileClient } from "@/lib/api"
import { useAuth } from "@/lib/auth-context"
import type { CompanyProfileDto } from "@/lib/types"

/**
 * ProfilePage: exibe dados cadastrais da empresa em modo leitura.
 * PJ-only layout — Razão Social, CNPJ, Email, Telefone.
 * Rendered inside AppLayout (sidebar + header), so no standalone Header.
 */
export function ProfilePage() {
  const { auth } = useAuth()
  const [profile, setProfile] = useState<CompanyProfileDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Fetch profile data
  useEffect(() => {
    async function fetchProfile() {
      try {
        const data = await getProfileClient()
        setProfile(data)
      } catch (err) {
        console.error("Failed to fetch profile:", err)
        setError("Não foi possível carregar os dados da empresa.")
      } finally {
        setLoading(false)
      }
    }
    if (auth.isAuthenticated) {
      fetchProfile()
    }
  }, [auth.isAuthenticated])

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Perfil da Empresa</h1>
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
    )
  }

  if (error) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Perfil da Empresa</h1>
        <Card>
          <CardContent className="py-8">
            <p className="text-muted-foreground">{error}</p>
          </CardContent>
        </Card>
      </div>
    )
  }

  if (!profile) return null

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Perfil da Empresa</h1>
      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">{profile.razaoSocial}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <p className="text-sm text-muted-foreground">Razão Social</p>
            <p className="text-base font-medium">{profile.razaoSocial}</p>
          </div>

          <div>
            <p className="text-sm text-muted-foreground">CNPJ</p>
            <p className="text-base font-mono">{profile.cnpj}</p>
          </div>

          <Separator />

          <div>
            <p className="text-sm text-muted-foreground">Email</p>
            <p className="text-base">{profile.email}</p>
          </div>

          <div>
            <p className="text-sm text-muted-foreground">Telefone</p>
            <p className="text-base">{profile.phone}</p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}