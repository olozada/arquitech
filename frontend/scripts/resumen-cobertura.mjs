import { appendFileSync, readFileSync } from 'node:fs'

const total = JSON.parse(readFileSync(new URL('../coverage/coverage-summary.json', import.meta.url), 'utf8')).total
const texto = [
  '### Cobertura frontend',
  '',
  '| | % |',
  '|---|---:|',
  `| Líneas | ${total.lines.pct} |`,
  `| Ramas | ${total.branches.pct} |`,
  `| Funciones | ${total.functions.pct} |`,
  '',
].join('\n')

console.log(texto)
if (process.env.GITHUB_STEP_SUMMARY) {
  appendFileSync(process.env.GITHUB_STEP_SUMMARY, texto)
}
