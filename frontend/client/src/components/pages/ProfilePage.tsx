import { useEffect, useState } from "react"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { Separator } from "@/components/ui/separator"
import { Header } from "../organisms/Header"
import { getProfileClient } from "../../lib/api"
import { useAuth } from "../../lib/auth-context"
import { useNavigate } from "@tanstack/react-router"
import type { CompanyProfileDto } from "@/lib/types"

/**
 * ProfilePage: exibe dados cadastrais da empresa em modo leitura.
 * PJ-only layout. Protegida por auth guard interno.
 */
export function ProfilePage() {
  const { auth } = useAuth()
  const navigate = useNavigate()
  const [profile, setProfile] = useState<CompanyProfileDto | null>(null)
  const [loading, setLoading] = useState(true)

  // Auth guard
  useEffect(() => {
    if (!auth.isLoading && !auth.isAuthenticated) {
      navigate({ to: "/auth/login" as any })
    }
  }, [auth.isLoading, auth.isAuthenticated, navigate])

  // Fetch profile data
  useEffect(() => {
    async function fetchProfile() {
      try {
        const data = await getProfileClient()
        setProfile(data)
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
            <CardTitle className="text-2xl">Perfil da Empresa</CardTitle>
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
    </div>
  )
}