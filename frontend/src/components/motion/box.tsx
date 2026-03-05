import { HTMLChakraProps, chakra } from '@chakra-ui/react'
import { HTMLMotionProps, motion } from 'framer-motion'

type Merge<P, T> = Omit<P, keyof T> & T

export type MotionBoxProps = Merge<HTMLChakraProps<'div'>, HTMLMotionProps<'div'>> & {
  children?: React.ReactNode
}

export const MotionBox = motion.create(chakra.div)
