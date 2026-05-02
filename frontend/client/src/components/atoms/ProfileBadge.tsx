import type { AccessGroup } from "@/lib/auth-context";

// ---------------------------------------------------------------------------
// Atom: ProfileBadge
// ---------------------------------------------------------------------------
// Visual indicator showing user's access group in the company.
// D-04: Admin Empresa (green), Viewer (gray), Dashboard (blue).
// ---------------------------------------------------------------------------

export interface ProfileBadgeProps {
  group: AccessGroup;
}

const GROUP_CONFIG: Record<AccessGroup, { label: string; className: string }> = {
  "admin-empresa": {
    label: "Admin Empresa",
    className: "inline-flex items-center rounded-full bg-green-100 px-3 py-1 text-xs font-medium text-green-800",
  },
  viewer: {
    label: "Viewer",
    className: "inline-flex items-center rounded-full bg-gray-100 px-3 py-1 text-xs font-medium text-gray-800",
  },
  dashboard: {
    label: "Dashboard",
    className: "inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-xs font-medium text-blue-800",
  },
};

/**
 * Atom: exibe um badge colorido indicando o grupo de acesso do usuário.
 */
export function ProfileBadge({ group }: ProfileBadgeProps) {
  const config = GROUP_CONFIG[group];
  return <span className={config.className}>{config.label}</span>;
}