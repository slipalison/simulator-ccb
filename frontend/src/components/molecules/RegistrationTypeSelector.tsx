import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";

export type RegistrationType = 'PF' | 'PJ';

interface RegistrationTypeSelectorProps {
  onSelect: (type: RegistrationType) => void;
}

export function RegistrationTypeSelector({ onSelect }: RegistrationTypeSelectorProps) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <Card
        className="cursor-pointer transition-all hover:ring-2 hover:ring-primary"
        onClick={() => onSelect('PF')}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onSelect('PF');
          }
        }}
      >
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <span className="text-2xl">&#128100;</span>
            Pessoa F&#237;sica
          </CardTitle>
          <CardDescription>CPF</CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Cadastro para pessoas f&#237;sicas com CPF.
          </p>
        </CardContent>
      </Card>

      <Card
        className="cursor-pointer transition-all hover:ring-2 hover:ring-primary"
        onClick={() => onSelect('PJ')}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            onSelect('PJ');
          }
        }}
      >
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <span className="text-2xl">&#127970;</span>
            Pessoa Jur&#237;dica
          </CardTitle>
          <CardDescription>CNPJ</CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            Cadastro para pessoas jur&#237;dicas com CNPJ.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
