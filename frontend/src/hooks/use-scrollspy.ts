import { useEffect, useRef, useState } from 'react'

export function useScrollSpy(
  selectors: string[],
  options?: IntersectionObserverInit,
): string | null {
  const [activeId, setActiveId] = useState<string | null>(null)
  const observer = useRef<IntersectionObserver | null>(null)

  useEffect(() => {
    const elements = selectors
      .map((s) => document.querySelector(s))
      .filter(Boolean) as Element[]

    observer.current?.disconnect()

    observer.current = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          setActiveId(entry.target.id)
        }
      }
    }, options)

    elements.forEach((el) => observer.current?.observe(el))

    return () => observer.current?.disconnect()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectors.join(',')])

  return activeId
}
