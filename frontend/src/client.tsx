import { createRoot } from "react-dom/client";

function App() {
  return (
    <div>
      <h1>Onboarding</h1>
      <p>Infrastructure phase — placeholder</p>
    </div>
  );
}

const root = createRoot(document.getElementById("root")!);
root.render(<App />);
