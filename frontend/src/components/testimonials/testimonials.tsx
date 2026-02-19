import { SimpleGrid } from '@chakra-ui/react'
import {
  Section,
  SectionProps,
  SectionTitle,
  SectionTitleProps,
} from '#components/section'
import { Testimonial } from './testimonial'

export interface TestimonialsProps
  extends Omit<SectionProps, 'title'>,
    Pick<SectionTitleProps, 'title' | 'description'> {
  columns?: number | (number | null)[]
}

export const Testimonials: React.FC<TestimonialsProps> = (props) => {
  const { children, title, columns = [1, null, 2], ...rest } = props
  return (
    <Section {...rest}>
      <SectionTitle title={title} />
      <SimpleGrid columns={columns} gap="8">
        {children}
      </SimpleGrid>
    </Section>
  )
}
