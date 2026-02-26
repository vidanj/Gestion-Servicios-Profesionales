import { Box, useToken } from "@chakra-ui/react"
import { useTheme } from "next-themes"

export const BackgroundGradient = ({ hideOverlay, ...props }: any) => {
  const { theme } = useTheme()
  const colorMode = theme === "dark" ? "dark" : "light"

  const [primary800, secondary500, cyan500, teal500] = useToken("colors", [
    "primary.800",
    "secondary.500",
    "cyan.500",
    "teal.500",
  ])

  const fallbackBackground = `
    radial-gradient(at top left, ${primary800} 30%, transparent 80%),
    radial-gradient(at bottom, ${secondary500} 0%, transparent 60%),
    radial-gradient(at bottom left, ${cyan500} 0%, transparent 50%),
    radial-gradient(at top right, ${teal500}, transparent),
    radial-gradient(at bottom right, ${primary800} 0%, transparent 50%);
  `

  const overlayColor =
    colorMode === "light" ? "white" : "gray-900"

  const gradientOverlay = `
    linear-gradient(
      0deg,
      var(--chakra-colors-${overlayColor}) 60%,
      rgba(0, 0, 0, 0) 100%
    );
  `

  const opacityValue = colorMode === "light" ? "0.3" : "0.5"

  return (
    <Box
      bgImage={fallbackBackground}
      bgBlendMode="saturation"
      position="absolute"
      top="0"
      left="0"
      zIndex="0"
      opacity={opacityValue}
      h="100vh"
      w="100%"
      overflow="hidden"
      pointerEvents="none"
      {...props}
    >
      <Box
        bgImage={!hideOverlay ? gradientOverlay : undefined}
        position="absolute"
        inset="0"
        zIndex="1"
      />
    </Box>
  )
}