import { Anchor, Text } from '@mantine/core'
import { Link } from 'react-router'
import type { Litter, Parent } from '../types/types'
import { nameWithTitles } from '../utils/format'

/** Én forelder. Våre egne katter lenkes til. En ekstern hannkatt har ingen side her, så han står som tekst. */
function ParentName({ parent }: { parent?: Parent | null }) {
  if (!parent) {
    return (
      <Text span c="dimmed">
        ukjent
      </Text>
    )
  }

  const name = nameWithTitles(parent.name, parent.titles)
  return (
    <>
      {parent.isExternal ? (
        <Text span>{name}</Text>
      ) : (
        <Anchor component={Link} to={`/katter/${parent.id}`}>
          {name}
        </Anchor>
      )}
      {parent.emsCode && (
        <Text span c="dimmed">
          {' '}
          ({parent.emsCode})
        </Text>
      )}
    </>
  )
}

/** «Mor: IC Kiara (MCO n 22) · Far: Børre (MCO a)» */
export default function Parents({ litter }: { litter: Litter }) {
  return (
    <Text size="sm">
      Mor: <ParentName parent={litter.mother} /> · Far: <ParentName parent={litter.father} />
    </Text>
  )
}
