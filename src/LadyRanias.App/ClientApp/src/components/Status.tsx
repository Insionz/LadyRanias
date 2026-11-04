import { Alert, Center, Loader } from '@mantine/core'

export function Loading() {
  return (
    <Center py="xl">
      <Loader aria-label="Laster" />
    </Center>
  )
}

export function ErrorMessage({ message }: { message: string }) {
  return (
    <Alert color="red" title="Noe gikk galt" my="md">
      {message} Prøv å laste siden på nytt.
    </Alert>
  )
}
