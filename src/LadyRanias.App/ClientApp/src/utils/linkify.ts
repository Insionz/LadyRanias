/** En bit av en tekst: vanlig tekst, eller en lenke. */
export type TextPart = { kind: 'text'; text: string } | { kind: 'link'; text: string; href: string }

// http(s)://… eller www.…, fram til mellomrom. Punktum og komma rett etter lenken hører til setningen.
const urlPattern = /\b(https?:\/\/[^\s<]+|www\.[^\s<]+)/g

/** Deler opp en linje i tekst og lenker. Vi lager lenkene selv, så det aldri blir HTML fra teksten. */
export function linkify(line: string): TextPart[] {
  const parts: TextPart[] = []
  let last = 0
  for (const match of line.matchAll(urlPattern)) {
    let url = match[0]
    const trailing = /[.,!?:;)]+$/.exec(url)?.[0] ?? ''
    url = url.slice(0, url.length - trailing.length)
    const start = match.index ?? 0
    if (start > last) parts.push({ kind: 'text', text: line.slice(last, start) })
    parts.push({ kind: 'link', text: url, href: url.startsWith('www.') ? `https://${url}` : url })
    last = start + url.length
  }
  if (last < line.length) parts.push({ kind: 'text', text: line.slice(last) })
  return parts
}

/** Avsnitt (skilt med tom linje), og linjene i hvert avsnitt. */
export function paragraphs(body: string): string[][] {
  return body
    .split(/\n\s*\n/)
    .map(p => p.split('\n'))
    .filter(lines => lines.some(l => l.trim() !== ''))
}
