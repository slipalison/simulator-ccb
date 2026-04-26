import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

interface TermsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * TermsDialog: modal with mock Terms of Use text (LGPD compliance notice).
 * Shows "Termos de Uso — Versão 1.0" and a "Li e concordo" button.
 */
export function TermsDialog({ open, onOpenChange }: TermsDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Termos de Uso — Versão 1.0</DialogTitle>
          <DialogDescription>
            Leia os termos antes de aceitar.
          </DialogDescription>
        </DialogHeader>

        <div className="overflow-y-auto flex-1 pr-2 text-sm text-muted-foreground space-y-4">
          <p>
            Bem-vindo à nossa plataforma de onboarding. Ao se cadastrar e utilizar
            nossos serviços, você concorda com os seguintes termos e condições.
          </p>

          <h3 className="font-semibold text-foreground">1. Dados Pessoais e LGPD</h3>
          <p>
            Em conformidade com a Lei Geral de Proteção de Dados (Lei nº 13.709/2018),
            informamos que os dados pessoais coletados durante o cadastro — incluindo
            razão social, CNPJ, e-mail, telefone e credenciais de acesso — serão
            utilizados exclusivamente para a prestação dos serviços contratados, bem
            como para comunicações relacionadas ao uso da plataforma.
          </p>

          <h3 className="font-semibold text-foreground">2. Responsabilidades do Usuário</h3>
          <p>
            O usuário é responsável pela veracidade dos dados informados no cadastro
            e pela segurança de suas credenciais de acesso. É vetada a compartilhação
            de senhas com terceiros. O usuário deve notificar imediatamente a plataforma
            em caso de uso não autorizado de sua conta.
          </p>

          <h3 className="font-semibold text-foreground">3. Uso da Plataforma</h3>
          <p>
            A plataforma destina-se exclusivamente ao cadastro e gestão de empresas
            (Pessoa Jurídica) e seus funcionários. O uso indevido, incluindo mas não
            limitado a atividades ilícitas, fraude ou violação de direitos de terceiros,
            resultará na suspensão imediata da conta.
          </p>

          <h3 className="font-semibold text-foreground">4. Modificações dos Termos</h3>
          <p>
            Reservamo-nos o direito de modificar estes termos a qualquer momento.
            As alterações entrarão em vigor na data de sua publicação na plataforma.
            O uso continuado dos serviços após a modificação constitui aceitação dos
            novos termos.
          </p>

          <h3 className="font-semibold text-foreground">5. Contato</h3>
          <p>
            Para dúvidas ou solicitações relacionadas a estes termos ou à proteção de
            seus dados pessoais, entre em contato através do canal de suporte da
            plataforma.
          </p>
        </div>

        <DialogFooter className="mt-4">
          <Button
            type="button"
            onClick={() => onOpenChange(false)}
            className="w-full sm:w-auto"
          >
            Li e concordo com os Termos de Uso
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}