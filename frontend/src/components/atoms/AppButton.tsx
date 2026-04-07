import { Button } from "@/components/ui/button";
import type { ComponentProps } from "react";

export type AppButtonProps = ComponentProps<typeof Button>;

/**
 * Atom: wrapper minimalista sobre shadcn Button.
 * Usa a variante "default" do projeto. Extensível via props.
 */
export function AppButton({ children, ...props }: AppButtonProps) {
  return <Button {...props}>{children}</Button>;
}
