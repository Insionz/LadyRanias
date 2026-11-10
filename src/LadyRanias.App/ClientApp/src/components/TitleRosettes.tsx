import { Group, Stack, Text, Tooltip } from '@mantine/core'
import type { Title } from '../utils/titles'

/**
 * Farger på rosettene. NRR og FIFe sier ingenting om farger, så disse er våre egne:
 * champion-stigen blir «varmere» oppover, og hel og kastrert katt på samme nivå får samme farge.
 */
function colors(title: Title): { ring: string; center: string } {
  if (title.kind === 'champion') {
    switch (title.level) {
      case 1:
      case 2:
        return { ring: '#7cc3d8', center: '#2f8aa6' } // KCH, JCH
      case 3:
        return { ring: '#6f9be8', center: '#2d5fbf' } // CH, PR
      case 4:
        return { ring: '#e57373', center: '#b3261e' } // IC, IP
      case 5:
        return { ring: '#e9c46a', center: '#b8860b' } // GIC, GIP
      default:
        return { ring: '#b08ad6', center: '#6a3d9a' } // SC, SP, EC, EP
    }
  }
  if (title.kind === 'winner') return { ring: '#6cc49a', center: '#1e7a4f' }
  if (title.kind === 'offspring') return { ring: '#c9976b', center: '#8a5a2b' } // DM
  return { ring: '#c3c9d1', center: '#6b7280' } // JW, DSW, DSM, DVM
}

/** Spissene rundt kanten: en stjerne med 16 tagger, som en plissert sløyfe. */
const ringPoints = Array.from({ length: 32 }, (_, i) => {
  const angle = (Math.PI * i) / 16
  const r = i % 2 === 0 ? 19 : 15.5
  return `${(20 + r * Math.sin(angle)).toFixed(2)},${(20 - r * Math.cos(angle)).toFixed(2)}`
}).join(' ')

/** En rosett vi har tegnet selv: plissert ring, en sirkel med forkortelsen, og to bånd under. */
export function Rosette({ title, size = 40 }: { title: Title; size?: number }) {
  const { ring, center } = colors(title)
  const fontSize = title.code.length <= 2 ? 10 : title.code.length === 3 ? 8.5 : 7

  return (
    <Tooltip label={title.name} withArrow>
      <svg width={size} height={size * 1.3} viewBox="0 0 40 52" role="img" aria-label={title.name}>
        <polygon points="13,26 7,50 12.5,46.5 17,51 20,31" fill={center} />
        <polygon points="27,26 33,50 27.5,46.5 23,51 20,31" fill={center} />
        <polygon points={ringPoints} fill={ring} />
        <circle cx="20" cy="20" r="12" fill={center} />
        <circle cx="20" cy="20" r="12" fill="none" stroke="#ffffff" strokeOpacity="0.6" strokeWidth="1" />
        <text
          x="20"
          y={title.year ? 19.5 : 23}
          textAnchor="middle"
          fontSize={fontSize}
          fontWeight={700}
          fill="#ffffff"
          fontFamily="system-ui, sans-serif"
        >
          {title.code}
        </text>
        {title.year && (
          <text x="20" y="27.5" textAnchor="middle" fontSize="6.5" fill="#ffffff" fontFamily="system-ui, sans-serif">
            {title.year}
          </text>
        )}
      </svg>
    </Tooltip>
  )
}

/** Alle de kjente titlene som rosetter, i samme rekkefølge som i navnet. Ukjente titler står bare i navnet. */
export default function TitleRosettes({ titles, size = 40 }: { titles: Title[]; size?: number }) {
  const shown = titles.filter(t => t.kind !== 'unknown')
  if (shown.length === 0) return null

  return (
    <Group gap={6} wrap="wrap">
      {shown.map(title => (
        <Rosette key={title.text} title={title} size={size} />
      ))}
    </Group>
  )
}

/** Titlene som en liste: rosetten og det fulle navnet ved siden av, så de betyr noe også for den som ikke kan forkortelsene. */
export function TitleList({ titles }: { titles: Title[] }) {
  const shown = titles.filter(t => t.kind !== 'unknown')
  if (shown.length === 0) return null

  return (
    <Stack gap={6}>
      {shown.map(title => (
        <Group key={title.text} gap="sm" wrap="nowrap">
          <Rosette title={title} size={32} />
          <Text size="sm">{title.name}</Text>
        </Group>
      ))}
    </Stack>
  )
}
