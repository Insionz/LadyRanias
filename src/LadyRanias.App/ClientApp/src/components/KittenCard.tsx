import { Anchor, Badge, Card, Group, Text } from '@mantine/core'
import { Link } from 'react-router'
import type { Kitten } from '../types/types'
import { kittenStatusBadge, sexLabel } from '../utils/format'
import { contactLink } from '../utils/contactLink'

export default function KittenCard({ kitten }: { kitten: Kitten }) {
  const badge = kittenStatusBadge(kitten.status)
  const unavailable = kitten.status === 'Sold' || kitten.status === 'Kept'

  return (
    // Solgte kattunger og de vi beholder tones litt ned, så de ledige skiller seg ut.
    <Card withBorder radius="md" padding="md" className="fade-up" style={{ opacity: unavailable ? 0.65 : 1 }}>
      <Group justify="space-between" wrap="nowrap" mb={4}>
        <Text fw={600} truncate>
          {kitten.name}
        </Text>
        <Badge color={badge.color}>{badge.label}</Badge>
      </Group>
      <Text size="sm" c="dimmed" mb={kitten.description ? 'xs' : 0}>
        {sexLabel(kitten.sex)}
      </Text>
      {kitten.description && <Text size="sm">{kitten.description}</Text>}
      {kitten.status === 'Available' && (
        <Anchor component={Link} to={contactLink(`Kattungen ${kitten.name}`)} size="sm" mt="xs" display="block">
          Spør om {kitten.name}
        </Anchor>
      )}
    </Card>
  )
}
