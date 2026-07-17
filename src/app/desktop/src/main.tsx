import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "./index.css";
import AppShell from "./app-shell";
import { DesktopThemeProvider } from "./features/shell/desktop-theme-provider";

const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: 1, staleTime: 15_000 } },
});

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <DesktopThemeProvider>
            <QueryClientProvider client={queryClient}>
                <AppShell />
            </QueryClientProvider>
        </DesktopThemeProvider>
    </StrictMode>,
);
