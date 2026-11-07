import { useState } from 'react'
import { Group, SegmentedControl, Stack, Text, Title } from '@mantine/core'
import { useApi } from '../api/api'
import CatGrid from '../components/CatGrid'
import { ErrorMessage, Loading } from '../components/Status'
import type { Breed, Cat } from '../types/types'
import { breedLabel } from '../utils/format'

type Filter = 'all' | 'Female' | 'Male'

/** Rekkefølgen på seksjonene. Katter uten rase (lagt inn før rasen fantes) kommer til slutt. */
const breedOrder: Breed[] = ['MaineCoon', 'SacredBirman', 'Unknown']

export default function CatsPage() {
  // API-et gir bare våre egne katter: i avl og pensjonerte. Kattungene står under Kull.
  const { data: cats, loading, error } = useApi<Cat[]>('/api/public/cats')
  const [filter, setFilter] = useState<Filter>('all')

  const shown = cats?.filter(cat => filter === 'all' || cat.sex === filter) ?? []

  // Én seksjon per rase. I avl først, så de pensjonerte, innenfor hver rase.
  const sections = breedOrder
    .map(breed => ({
      breed,
      cats: shown
        .filter(cat => cat.breed === breed)
        .sort((a, b) => Number(a.role === 'Retired') - Number(b.role === 'Retired')),
    }))
    .filter(section => section.cats.length > 0)

  return (
    <>
      <title>Katter – Lady Rania’s</title>

      <Group justify="space-between" align="center" mb="lg">
        <Title order={2} fz={52}>
          Våre katter
        </Title>
        <SegmentedControl
          value={filter}
          onChange={value => setFilter(value as Filter)}
          data={[
            { value: 'all', label: 'Alle' },
            { value: 'Female', label: 'Hunnkatter' },
            { value: 'Male', label: 'Hannkatter' },
          ]}
        />
      </Group>

      {loading && <Loading />}
      {error && <ErrorMessage message={error} />}
      {cats && shown.length === 0 && (
        <Text c="dimmed">{cats.length === 0 ? 'Kattene kommer snart.' : 'Ingen katter å vise her.'}</Text>
      )}

      <Stack gap={48}>
        {sections.map(section => (
          <Stack key={section.breed} gap="md">
            {/* Overskrift bare når det er mer enn én rase å skille mellom. */}
            {sections.length > 1 && (
              <Title order={3} fz={40}>
                {section.breed === 'Unknown' ? 'Flere katter' : breedLabel(section.breed)}
              </Title>
            )}
            <CatGrid cats={section.cats} />
          </Stack>
        ))}
      </Stack>
    </>
  )
}
