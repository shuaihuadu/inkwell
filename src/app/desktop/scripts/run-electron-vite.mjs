import { execFileSync, spawn } from 'node:child_process'
import { existsSync, mkdirSync, readFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const environment = { ...process.env }
delete environment.ELECTRON_RUN_AS_NODE

const prepareMacDevelopmentRuntime = () => {
  if (process.platform !== 'darwin') return undefined

  const electronPackagePath = fileURLToPath(import.meta.resolve('electron/package.json'))
  const electronPackageDirectory = dirname(electronPackagePath)
  const electronPackage = JSON.parse(readFileSync(electronPackagePath, 'utf8'))
  const runtimeDirectory = join(
    electronPackageDirectory,
    '..',
    '.cache',
    'inkwell-electron',
    electronPackage.version,
    'v2',
  )
  const sourceApplication = join(electronPackageDirectory, 'dist', 'Electron.app')
  const targetApplication = join(runtimeDirectory, 'Inkwell.app')
  const infoPlist = join(targetApplication, 'Contents', 'Info.plist')
  let requiresSigning = false

  if (!existsSync(targetApplication)) {
    mkdirSync(runtimeDirectory, { recursive: true })
    execFileSync('ditto', [sourceApplication, targetApplication])
    requiresSigning = true
  }

  const metadata = {
    CFBundleDisplayName: 'Inkwell',
    CFBundleName: 'Inkwell',
    CFBundleIdentifier: 'com.shuaihuadu.inkwell.dev',
  }

  for (const [key, value] of Object.entries(metadata)) {
    const currentValue = execFileSync(
      'plutil',
      ['-extract', key, 'raw', '-o', '-', infoPlist],
      { encoding: 'utf8' },
    ).trim()
    if (currentValue === value) continue

    execFileSync('plutil', ['-replace', key, '-string', value, infoPlist])
    requiresSigning = true
  }

  if (requiresSigning) {
    execFileSync(
      'codesign',
      ['--force', '--deep', '--sign', '-', targetApplication],
      { stdio: 'inherit' },
    )
  }

  return runtimeDirectory
}

const developmentRuntime = prepareMacDevelopmentRuntime()
if (developmentRuntime) {
  environment.ELECTRON_EXEC_PATH = join(
    developmentRuntime,
    'Inkwell.app',
    'Contents',
    'MacOS',
    'Electron',
  )
}

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