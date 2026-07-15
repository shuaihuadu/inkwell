import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import RootLayout from './layouts/RootLayout'
import DesignLab from './pages/DesignLab'
import ThemeExplorer from './pages/ThemeExplorer'
import LogoExplorer from './pages/LogoExplorer'
import LoginExplorer from './pages/LoginExplorer'
import AppShellExplorer from './pages/AppShellExplorer'

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <DesignLab /> },
      { path: 'themes', element: <ThemeExplorer /> },
      { path: 'logos', element: <LogoExplorer /> },
      { path: 'login', element: <LoginExplorer /> },
      { path: 'shell', element: <AppShellExplorer /> },
    ],
  },
])

export default function App() {
  return <RouterProvider router={router} />
}
