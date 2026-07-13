import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ConfigProvider } from "antd";
import zhCN from "antd/locale/zh_CN";
import "./index.css";
import AppShell from "./app-shell";

const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: 1, staleTime: 15_000 } },
});

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <ConfigProvider
            locale={zhCN}
            theme={{
                token: {
                    colorPrimary: "#176b5b",
                    colorInfo: "#176b5b",
                    borderRadius: 6,
                    fontFamily: "Avenir Next, Segoe UI, sans-serif",
                },
            }}
        >
            <QueryClientProvider client={queryClient}>
                <AppShell />
            </QueryClientProvider>
        </ConfigProvider>
    </StrictMode>,
);
