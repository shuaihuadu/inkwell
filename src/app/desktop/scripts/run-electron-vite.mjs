import { spawn } from 'node:child_process'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const environment = { ...process.env }
delete environment.ELECTRON_RUN_AS_NODE

const packagePath = fileURLToPath(import.meta.resolve('electron-vite/package.json'))
const cliPath = join(dirname(packagePath), 'bin/electron-vite.js')
const childProcess = spawn(process.execPath, [cliPath, ...process.argv.slice(2)], {
  env: environment,
  stdio: 'inherit',
})

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.once(signal, () => childProcess.kill(signal))
}

childProcess.once('error', (error) => {
  console.error(error)
  process.exitCode = 1
})

childProcess.once('exit', (code, signal) => {
  if (signal) {
    process.kill(process.pid, signal)
    return
  }

  process.exit(code ?? 1)
})