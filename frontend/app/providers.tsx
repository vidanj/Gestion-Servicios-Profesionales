"use client";

import { ColorModeScript } from "@chakra-ui/react";
import { SaasProvider } from "@saas-ui/react";

import { theme } from "../src/theme";

type ProvidersProps = {
  children: React.ReactNode;
};

export default function Providers({ children }: ProvidersProps) {
  return (
    <SaasProvider theme={theme}>
      <ColorModeScript initialColorMode={theme.config.initialColorMode} />
      {children}
    </SaasProvider>
  );
}
