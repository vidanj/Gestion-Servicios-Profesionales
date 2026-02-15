"use client";

import { Box, Button, HStack, Stack, Text } from "@chakra-ui/react";

import { ButtonLink } from "#components/button-link/button-link";
import { Features } from "#components/features";
import {
  Highlights,
  HighlightsItem,
  HighlightsTestimonialItem,
} from "#components/highlights";
import { Hero } from "#components/hero";
import { MarketingLayout } from "#components/layout";
import { ChakraLogo, NextjsLogo, ReactLogo } from "#components/logos";
import { Pricing } from "#components/pricing/pricing";
import { Faq } from "#components/faq";
import { Testimonial, Testimonials } from "#components/testimonials";

import faq from "#data/faq";
import pricing from "#data/pricing";
import siteConfig from "#data/config";
import testimonials from "#data/testimonials";

export default function LandingPage() {
  return (
    <MarketingLayout
      announcementProps={{
        title: "Nuevo",
        description: "Template listo para usar",
        href: "#features",
        action: "Ver",
      }}
    >
      <Box pt={{ base: 24, md: 32 }}>
        <Hero
          title="Gestiona tus servicios profesionales"
          description="Una base moderna para tu sitio, con secciones listas para personalizar."
        >
          <HStack spacing="4" mt="8" flexWrap="wrap">
            <ButtonLink href="#pricing" colorScheme="primary">
              Ver planes
            </ButtonLink>
            <Button variant="outline" as="a" href="#features">
              Ver funciones
            </Button>
          </HStack>

          <Stack
            mt="10"
            spacing="4"
            direction={{ base: "column", md: "row" }}
            align={{ base: "flex-start", md: "center" }}
          >
            <Text fontSize="sm" color="muted">
              Construido con
            </Text>
            <HStack spacing="6">
              <ChakraLogo height="28px" />
              <NextjsLogo height="28px" />
              <ReactLogo height="28px" />
            </HStack>
          </Stack>
        </Hero>

        <Features
          id="features"
          title="Funciones clave"
          description="Todo lo que necesitas para publicar un sitio profesional en minutos."
          features={siteConfig.signup.features}
        />

        <Highlights>
          <HighlightsItem title="Secciones listas">
            <Text color="muted">
              Hero, precios, testimonios y FAQ incluidos para empezar rapido.
            </Text>
          </HighlightsItem>
          <HighlightsTestimonialItem
            gradient={["primary.500", "secondary.500"]}
            name={testimonials.items[0].name}
            description={testimonials.items[0].description}
            avatar={testimonials.items[0].avatar}
          >
            {testimonials.items[0].children}
          </HighlightsTestimonialItem>
          <HighlightsItem title="Personalizable">
            <Text color="muted">
              Cambia textos, colores e imagenes sin tocar la estructura base.
            </Text>
          </HighlightsItem>
        </Highlights>

        <Testimonials title={testimonials.title}>
          {testimonials.items.map((item) => (
            <Testimonial
              key={item.name}
              name={item.name}
              description={item.description}
              avatar={item.avatar}
            >
              {item.children}
            </Testimonial>
          ))}
        </Testimonials>

        <Pricing
          title={pricing.title}
          description={pricing.description}
          plans={pricing.plans}
        />

        <Faq title={faq.title} items={faq.items} />
      </Box>
    </MarketingLayout>
  );
}
