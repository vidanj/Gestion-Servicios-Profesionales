import * as React from 'react'
import {
  Box,
  BoxProps,
  Stack,
  VStack,
  SimpleGrid,
  Heading,
  Text,
} from '@chakra-ui/react'

import { Section, SectionTitle, SectionTitleProps } from '#components/section'

const Revealer = ({ children }: any) => children

export interface FeaturesProps
  extends Omit<SectionTitleProps, 'title' | 'variant'> {
  title?: React.ReactNode
  description?: React.ReactNode
  features: Array<FeatureProps>
  columns?: number | number[]
  spacing?: string | number
  aside?: React.ReactNode
  reveal?: React.FC<any>
  iconSize?: number
  innerWidth?: string
}

export interface FeatureProps {
  title?: React.ReactNode
  description?: React.ReactNode
  icon?: React.ElementType
  iconPosition?: 'left' | 'top'
  iconSize?: number
  ip?: 'left' | 'top'
  variant?: string
  delay?: number
}

export const Feature: React.FC<FeatureProps> = ({
  title,
  description,
  icon: IconComponent,
  iconPosition,
  iconSize = 20,
  ip,
}) => {
  const pos = iconPosition || ip
  const direction = pos === 'left' ? 'row' : 'column'

  return (
    <Stack direction={direction}>
      {IconComponent && (
        <Box
          display="flex"
          alignItems="center"
          justifyContent="center"
          borderRadius="full"
          boxSize="10"
          bg="primary.500"
          color="white"
          flexShrink={0}
        >
          <IconComponent size={iconSize} />
        </Box>
      )}

      <Box>
        <Heading fontSize="md" mb="1">
          {title}
        </Heading>
        <Text color="muted">
          {description}
        </Text>
      </Box>
    </Stack>
  )
}

export const Features: React.FC<FeaturesProps> = ({
  title,
  description,
  features,
  columns = [1, 2, 3],
  spacing = 8,
  align: alignProp = 'center',
  iconSize = 20,
  aside,
  reveal: Wrap = Revealer,
  ...rest
}) => {
  const align = aside ? 'left' : alignProp
  const ip = align === 'left' ? 'left' : 'top'

  return (
    <Section {...rest}>
      <Stack direction="row" align="flex-start">
        <VStack flex="1" gap={[4, null, 8]} alignItems="stretch">
          {(title || description) && (
            <Wrap>
              <SectionTitle
                title={title}
                description={description}
                align={align}
              />
            </Wrap>
          )}

          <SimpleGrid columns={columns} gap={spacing}>
            {features.map((feature, i) => (
              <Wrap key={i} delay={feature.delay}>
                <Feature
                  {...feature}
                  iconSize={iconSize}
                  ip={ip}
                />
              </Wrap>
            ))}
          </SimpleGrid>
        </VStack>

        {aside && (
          <Box flex="1" p="8">
            {aside}
          </Box>
        )}
      </Stack>
    </Section>
  )
}