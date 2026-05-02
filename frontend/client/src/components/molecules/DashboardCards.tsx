import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Users,
  UserCheck,
  UserX,
  LogIn,
  Activity,
  Clock,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Dashboard Cards — 6 mock data cards per D-15, D-16, D-17, D-18
// ---------------------------------------------------------------------------
// All values are hardcoded mock data except Total Funcionários which can
// optionally call the API for a real count.
// ---------------------------------------------------------------------------

const MOCK_DASHBOARD_DATA = {
  totalEmployees: 24,
  activeEmployees: 22,
  blockedEmployees: 2,
  recentLogins7d: 45,
  recentActions7d: 128,
  lastLogin: "há 2h",
};

// 7 data points for sparklines (last 7 days)
const LOGIN_SPARKLINE_DATA = [3, 8, 6, 7, 5, 9, 7];
const ACTIONS_SPARKLINE_DATA = [15, 22, 18, 20, 17, 21, 15];

// ---------------------------------------------------------------------------
// SVG Sparkline component — lightweight CSS-based mini chart
// ---------------------------------------------------------------------------

function Sparkline({
  data,
  color = "currentColor",
  width = 80,
  height = 32,
}: {
  data: number[];
  color?: string;
  width?: number;
  height?: number;
}) {
  const max = Math.max(...data);
  const min = Math.min(...data);
  const range = max - min || 1;
  const padding = 2;

  const points = data.map((value, index) => {
    const x = padding + (index / (data.length - 1)) * (width - 2 * padding);
    const y = height - padding - ((value - min) / range) * (height - 2 * padding);
    return `${x},${y}`;
  });

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      width={width}
      height={height}
      className="inline-block"
    >
      <polyline
        points={points.join(" ")}
        fill="none"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

// ---------------------------------------------------------------------------
// Progress Bar component — for Ativos/Bloqueados cards
// ---------------------------------------------------------------------------

function ProgressBar({
  value,
  max,
  colorClass = "bg-green-500",
  bgColorClass = "bg-muted",
}: {
  value: number;
  max: number;
  colorClass?: string;
  bgColorClass?: string;
}) {
  const percentage = max > 0 ? Math.round((value / max) * 100) : 0;

  return (
    <div className={`h-2 w-full rounded-full ${bgColorClass}`}>
      <div
        className={`h-2 rounded-full ${colorClass} transition-all`}
        style={{ width: `${percentage}%` }}
      />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Individual card components
// ---------------------------------------------------------------------------

export function TotalEmployeesCard({
  count = MOCK_DASHBOARD_DATA.totalEmployees,
}: {
  count?: number;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Total Funcionários
        </CardTitle>
        <Users className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{count}</div>
        <p className="text-xs text-muted-foreground">Total de funcionários</p>
      </CardContent>
    </Card>
  );
}

export function ActiveEmployeesCard({
  active = MOCK_DASHBOARD_DATA.activeEmployees,
  total = MOCK_DASHBOARD_DATA.totalEmployees,
}: {
  active?: number;
  total?: number;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Ativos
        </CardTitle>
        <UserCheck className="h-4 w-4 text-green-500" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-green-600">{active}</div>
        <div className="mt-2">
          <ProgressBar value={active} max={total} colorClass="bg-green-500" />
          <p className="mt-1 text-xs text-muted-foreground">
            {total > 0 ? Math.round((active / total) * 100) : 0}% do total
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

export function BlockedEmployeesCard({
  blocked = MOCK_DASHBOARD_DATA.blockedEmployees,
  total = MOCK_DASHBOARD_DATA.totalEmployees,
}: {
  blocked?: number;
  total?: number;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Bloqueados
        </CardTitle>
        <UserX className="h-4 w-4 text-red-500" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-red-600">{blocked}</div>
        <div className="mt-2">
          <ProgressBar value={blocked} max={total} colorClass="bg-red-500" />
          <p className="mt-1 text-xs text-muted-foreground">
            {total > 0 ? Math.round((blocked / total) * 100) : 0}% do total
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

export function RecentLoginsCard({
  count = MOCK_DASHBOARD_DATA.recentLogins7d,
  sparklineData = LOGIN_SPARKLINE_DATA,
}: {
  count?: number;
  sparklineData?: number[];
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Logins Recentes
        </CardTitle>
        <LogIn className="h-4 w-4 text-blue-500" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{count}</div>
        <p className="text-xs text-muted-foreground">Últimos 7 dias</p>
        <div className="mt-2">
          <Sparkline data={sparklineData} color="#3b82f6" />
        </div>
      </CardContent>
    </Card>
  );
}

export function RecentActionsCard({
  count = MOCK_DASHBOARD_DATA.recentActions7d,
  sparklineData = ACTIONS_SPARKLINE_DATA,
}: {
  count?: number;
  sparklineData?: number[];
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Ações Recentes
        </CardTitle>
        <Activity className="h-4 w-4 text-purple-500" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{count}</div>
        <p className="text-xs text-muted-foreground">Últimos 7 dias</p>
        <div className="mt-2">
          <Sparkline data={sparklineData} color="#a855f7" />
        </div>
      </CardContent>
    </Card>
  );
}

export function LastLoginCard({
  lastLogin = MOCK_DASHBOARD_DATA.lastLogin,
}: {
  lastLogin?: string;
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          Último Login
        </CardTitle>
        <Clock className="h-4 w-4 text-orange-500" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{lastLogin}</div>
        <p className="text-xs text-muted-foreground">
          Último acesso registrado
        </p>
      </CardContent>
    </Card>
  );
}

// ---------------------------------------------------------------------------
// Exported mock data for DashboardPage to optionally override Total Funcionários
// ---------------------------------------------------------------------------

export { MOCK_DASHBOARD_DATA };