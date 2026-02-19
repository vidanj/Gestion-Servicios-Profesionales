import { Box, BoxProps, Container } from '@chakra-ui/react'

export interface SectionProps extends BoxProps {
  children: React.ReactNode
  innerWidth?: string
}

export const Section: React.FC<SectionProps> = (props) => {
  const { children, innerWidth = 'container.lg', ...rest } = props
  return (
    <Box py="20" {...rest}>
      <Container height="full" maxW={innerWidth}>
        {children}
      </Container>
    </Box>
  )
}
