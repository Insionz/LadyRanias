/** Lenke til kontaktskjemaet med emnet ferdig utfylt, for eksempel fra en kattunge eller et kull. */
export function contactLink(subject: string) {
  return `/kontakt?emne=${encodeURIComponent(subject)}`
}
