import { useState } from 'react'
import { Button, Group, Modal, Stack, Text } from '@mantine/core'
import { useApi } from '../../api/api'
import { ErrorMessage, Loading } from '../../components/Status'
import type { Cat, Litter } from '../../types/types'
import LitterAdminCard from './LitterAdminCard'
import LitterForm from './LitterForm'

export default function AdminLitters() {
  // Det offentlige endepunktet gir alle kull med kattunger, så vi trenger ikke et eget admin-endepunkt for lesing.
  const litters = useApi<Litter[]>('/api/public/litters')
  const cats = useApi<Cat[]>('/api/admin/cats')
  const [creating, setCreating] = useState(false)

  if (litters.loading || cats.loading) return <Loading />
  if (litters.error) return <ErrorMessage message={litters.error} />
  if (cats.error) return <ErrorMessage message={cats.error} />

  const allCats = cats.data ?? []
  const catsById = new Map(allCats.map(cat => [cat.id, cat]))

  return (
    <>
      <Group justify="space-between" mb="md">
        <Text c="dimmed">{litters.data?.length ?? 0} kull</Text>
        <Button onClick={() => setCreating(true)}>Nytt kull</Button>
      </Group>

      <Stack>
        {litters.data?.map(litter => (
          <LitterAdminCard
            key={litter.id}
            litter={litter}
            cats={catsById}
            // Å koble en katt til et kull endrer katten også, så begge listene hentes på nytt.
            onChanged={() => {
              litters.reload()
              cats.reload()
            }}
          />
        ))}
      </Stack>

      <Modal opened={creating} onClose={() => setCreating(false)} title="Nytt kull">
        {creating && (
          <LitterForm
            cats={allCats}
            onCancel={() => setCreating(false)}
            onSaved={() => {
              setCreating(false)
              litters.reload()
            }}
          />
        )}
      </Modal>
    </>
  )
}
