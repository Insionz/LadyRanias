import { Anchor, Text } from '@mantine/core'
import { Link } from 'react-router'
import type { Litter } from '../types/types'
import { nameWithTitles } from '../utils/format'
import type { ParentInfo } from '../utils/parents'

/** Én forelder. Våre egne katter lenkes til. En ekstern katt, eller bare et navn, står som tekst. */
function ParentName({ parent }: { parent?: ParentInfo | null }) {
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
      {parent.id && !parent.isExternal ? (
        <Anchor component={Link} to={`/katter/${parent.id}`}>
          {name}
        </Anchor>
      ) : (
        <Text span>{name}</Text>
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
export function ParentsLine({ mother, father }: { mother?: ParentInfo | null; father?: ParentInfo | null }) {
  return (
    <Text size="sm">
      Mor: <ParentName parent={mother} /> · Far: <ParentName parent={father} />
    </Text>
  )
}

/** Mor og far i et kull. */
export default function Parents({ litter }: { litter: Litter }) {
  return <ParentsLine mother={litter.mother} father={litter.father} />
}
