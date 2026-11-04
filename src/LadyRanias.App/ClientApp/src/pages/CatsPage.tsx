import { useState } from 'react'
import { Group, SegmentedControl, Text, Title } from '@mantine/core'
import { useApi } from '../api/api'
import CatGrid from '../components/CatGrid'
import { ErrorMessage, Loading } from '../components/Status'
import type { Cat } from '../types/types'

type Filter = 'all' | 'Female' | 'Male'

export default function CatsPage() {
  const { data: cats, loading, error } = useApi<Cat[]>('/api/public/cats')
  const [filter, setFilter] = useState<Filter>('all')

  const shown = cats?.filter(cat => filter === 'all' || cat.sex === filter) ?? []

  return (
    <>
      <title>Katter – LadyRanias</title>

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
      {shown.length > 0 && <CatGrid cats={shown} />}
    </>
  )
}
