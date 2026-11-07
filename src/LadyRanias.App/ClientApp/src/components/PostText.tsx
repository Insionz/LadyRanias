import { Anchor, Stack, Text } from '@mantine/core'
import { Fragment } from 'react'
import { linkify, paragraphs } from '../utils/linkify'

/**
 * Teksten i et innlegg: avsnitt, linjeskift og klikkbare lenker. Vanlig tekst, ingen HTML:
 * React gjør alt om til tekst, og lenkene lager vi selv. Da kan ingenting i teksten bli til kode på siden.
 */
export default function PostText({ body }: { body: string }) {
  return (
    <Stack gap="sm">
      {paragraphs(body).map((lines, p) => (
        <Text key={p}>
          {lines.map((line, i) => (
            <Fragment key={i}>
              {i > 0 && <br />}
              {linkify(line).map((part, j) =>
                part.kind === 'link' ? (
                  <Anchor key={j} href={part.href} target="_blank" rel="noopener noreferrer">
                    {part.text}
                  </Anchor>
                ) : (
                  <Fragment key={j}>{part.text}</Fragment>
                ),
              )}
            </Fragment>
          ))}
        </Text>
      ))}
    </Stack>
  )
}
