import { VStack, Heading, Box, StackProps } from '@chakra-ui/react'

export interface SectionTitleProps extends Omit<StackProps, 'title'> {
  title: React.ReactNode
  description?: React.ReactNode
  align?: 'left' | 'center'
  variant?: string
}

export const SectionTitle: React.FC<SectionTitleProps> = (props) => {
  const { title, description, align, variant, ...rest } = props
  return (
    <VStack
      alignItems={align === 'left' ? 'flex-start' : 'center'}
      gap={4}
      {...rest}
    >
      <Heading as="h2">{title}</Heading>
      {description && (
        <Box textAlign={align}>{description}</Box>
      )}
    </VStack>
  )
}
