import {
  Box,
  BoxProps,
  Grid,
  GridItem,
  GridItemProps,
  Heading,
} from '@chakra-ui/react'

import { Section, SectionProps } from '#components/section'
import { Testimonial, TestimonialProps } from '#components/testimonials'

export interface HighlightBoxProps
  extends GridItemProps,
    Omit<BoxProps, 'title'> {}

export const HighlightsItem: React.FC<HighlightBoxProps> = (props) => {
  const { children, title, ...rest } = props
  return (
    <GridItem
      borderRadius="md"
      borderWidth="1px"
      p="8"
      flex="1 0"
      alignItems="flex-start"
      overflow="hidden"
      position="relative"
      bg="white"
      _dark={{ bg: 'gray.800' }}
      {...rest}
    >
      {title && (
        <Heading fontSize="3xl" mb="8">
          {title}
        </Heading>
      )}
      {children}
    </GridItem>
  )
}

export const HighlightsTestimonialItem: React.FC<
  HighlightBoxProps & TestimonialProps & { gradient: [string, string] }
> = (props) => {
  const {
    name,
    description,
    avatar,
    children,
    gradient = ['primary.500', 'secondary.500'],
    ...rest
  } = props
  return (
    <HighlightsItem
      justifyContent="center"
      _dark={{ borderColor: 'whiteAlpha.300' }}
      p="4"
      {...rest}
    >
      <Box
        bgGradient={`linear(to-br, ${gradient[0]}, ${gradient[1]})`}
        opacity="0.8"
        position="absolute"
        inset="0px"
        pointerEvents="none"
        zIndex="0"
        _dark={{ opacity: 0.5, filter: 'blur(50px)' }}
      />
      <Testimonial
        name={name}
        description={
          <Box as="span" color="whiteAlpha.700">
            {description}
          </Box>
        }
        avatar={avatar}
        position="relative"
      >
        {children}
      </Testimonial>
    </HighlightsItem>
  )
}

export const Highlights: React.FC<SectionProps> = (props) => {
  const { children, ...rest } = props

  return (
    <Section
      innerWidth="container.xl"
      position="relative"
      overflow="hidden"
      {...rest}
    >
      <Grid
        templateColumns={{ base: 'repeat(1, 1fr)', lg: 'repeat(3, 1fr)' }}
        gap={8}
        position="relative"
      >
        {children}
      </Grid>
    </Section>
  )
}
