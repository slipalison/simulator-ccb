import { createRoot } from "react-dom/client";
import "@/globals.css";

function App() {
  return (
    <main className="min-h-screen bg-background flex items-center justify-center">
      <div className="text-center space-y-4">
        <h1 className="text-3xl font-bold text-foreground">Onboarding</h1>
        <p className="text-muted-foreground">Frontend Foundation — Wave 1 completa</p>
      </div>
    </main>
  );
}

const rootEl = document.getElementById("root");
if (!rootEl) throw new Error("Elemento #root não encontrado no DOM");
createRoot(rootEl).render(<App />);
