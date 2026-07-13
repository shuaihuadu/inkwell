import type { InkwellDesktopApi } from './shared/network/contracts'

declare global {
  interface Window {
    inkwell: InkwellDesktopApi
  }
}

export {}