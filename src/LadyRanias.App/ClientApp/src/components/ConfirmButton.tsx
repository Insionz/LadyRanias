import { useState } from 'react'
import { Button, Group, Popover, Text } from '@mantine/core'

type Props = {
  label: string
  question: string
  onConfirm: () => void | Promise<void>
}

/** Slett-knapp som spør «Er du sikker?» før noe skjer. Uten nettleserens confirm()-dialog. */
export default function ConfirmButton({ label, question, onConfirm }: Props) {
  const [opened, setOpened] = useState(false)

  return (
    <Popover opened={opened} onChange={setOpened} position="bottom-end" withArrow shadow="md">
      <Popover.Target>
        <Button color="red" variant="light" size="xs" onClick={() => setOpened(o => !o)}>
          {label}
        </Button>
      </Popover.Target>
      <Popover.Dropdown>
        <Text size="sm" mb="xs">
          {question}
        </Text>
        <Group gap="xs" justify="flex-end">
          <Button size="xs" variant="default" onClick={() => setOpened(false)}>
            Avbryt
          </Button>
          <Button
            size="xs"
            color="red"
            onClick={() => {
              setOpened(false)
              void onConfirm()
            }}
          >
            Ja, slett
          </Button>
        </Group>
      </Popover.Dropdown>
    </Popover>
  )
}
