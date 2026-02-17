"use client";

import {
  Box,
  Button,
  Container,
  Divider,
  Heading,
  HStack,
  SimpleGrid,
  Stack,
  Text,
} from "@chakra-ui/react";

import { MarketingLayout } from "#components/layout";
import { Hero } from "#components/hero";
import { ButtonLink } from "#components/button-link/button-link";
import {
  Highlights,
  HighlightsItem,
  HighlightsTestimonialItem,
} from "#components/highlights";

import { Features } from "#components/features";
import testimonials from "#data/testimonials";

export default function About() {
  return (
    <MarketingLayout
      announcementProps={{
        title: "Sobre nosotros",
        description: "Conoce más sobre Gestión de Servicios Profesionales",
        href: "#about",
        action: "Ver",
      }}
    >
      <Box pt={{ base: 24, md: 32 }} id="about">
        <Hero
          title="Conectamos talento con oportunidades reales"
          description="Gestión de Servicios Profesionales es una plataforma inspirada en Fiverr, creada para facilitar la contratación y publicación de servicios profesionales de manera rápida, segura y organizada."
        >
          <HStack spacing="4" mt="8" flexWrap="wrap">
            <ButtonLink href="#mission" colorScheme="primary">
              Nuestra misión
            </ButtonLink>
            <Button variant="outline" as="a" href="#values">
              Nuestros valores
            </Button>
          </HStack>
        </Hero>

        <Container maxW="6xl" mt="20">
          <SimpleGrid columns={{ base: 1, md: 2 }} spacing="12">
            <Stack spacing="4">
              <Heading fontSize="2xl">¿Qué es este sistema?</Heading>
              <Text color="muted">
                Gestión de Servicios Profesionales es un sistema diseñado para
                conectar clientes con expertos en distintas áreas como diseño,
                programación, marketing, consultoría y más.
              </Text>
              <Text color="muted">
                Nuestro objetivo es que cualquier persona pueda ofrecer su
                conocimiento como un servicio, y que los clientes puedan
                contratar fácilmente con confianza y transparencia.
              </Text>
            </Stack>

            <Stack spacing="4">
              <Heading fontSize="2xl">¿Por qué lo creamos?</Heading>
              <Text color="muted">
                Porque contratar servicios no debería ser complicado. Queremos
                una plataforma clara, moderna y accesible, donde se puedan
                gestionar solicitudes, entregas y pagos de forma eficiente.
              </Text>
              <Text color="muted">
                Buscamos impulsar el trabajo independiente y dar herramientas
                reales para crecer profesionalmente.
              </Text>
            </Stack>
          </SimpleGrid>

          <Divider my="16" />

          <Stack spacing="10" id="mission">
            <Heading fontSize="3xl">Misión y visión</Heading>

            <SimpleGrid columns={{ base: 1, md: 2 }} spacing="10">
              <Box p="8" borderRadius="2xl" borderWidth="1px">
                <Heading fontSize="xl" mb="3">
                  🎯 Misión
                </Heading>
                <Text color="muted">
                  Facilitar la conexión entre profesionales y clientes mediante
                  una plataforma intuitiva que permita contratar servicios con
                  rapidez, confianza y calidad.
                </Text>
              </Box>

              <Box p="8" borderRadius="2xl" borderWidth="1px">
                <Heading fontSize="xl" mb="3">
                  🚀 Visión
                </Heading>
                <Text color="muted">
                  Convertirnos en una plataforma líder para la gestión de
                  servicios profesionales, impulsando el talento independiente y
                  fortaleciendo la economía digital.
                </Text>
              </Box>
            </SimpleGrid>
          </Stack>

          <Divider my="16" />

          <Highlights id="values">
            <HighlightsItem title="Transparencia">
              <Text color="muted">
                Queremos que cada trato sea claro: precios, entregas y
                expectativas desde el inicio.
              </Text>
            </HighlightsItem>

            <HighlightsTestimonialItem
              gradient={["primary.500", "secondary.500"]}
              name={testimonials.items[1].name}
              description={testimonials.items[1].description}
              avatar={testimonials.items[1].avatar}
            >
              {testimonials.items[1].children}
            </HighlightsTestimonialItem>

            <HighlightsItem title="Calidad profesional">
              <Text color="muted">
                La plataforma está pensada para elevar el nivel de servicio y
                garantizar una experiencia profesional.
              </Text>
            </HighlightsItem>
          </Highlights>

          <Divider my="16" />

          <Features
            id="platform"
            title="¿Qué ofrece la plataforma?"
            description="Herramientas pensadas para profesionales y clientes."
            features={[
              {
                title: "Publicación de servicios",
                description:
                  "Crea y administra tus servicios con precios, descripciones y categorías.",
                icon: "FiBriefcase",
              },
              {
                title: "Gestión de pedidos",
                description:
                  "Controla solicitudes, avances y entregas desde un solo lugar.",
                icon: "FiClipboard",
              },
              {
                title: "Comunicación directa",
                description:
                  "Chat y seguimiento para que cliente y profesional estén alineados.",
                icon: "FiMessageCircle",
              },
              {
                title: "Sistema moderno y escalable",
                description:
                  "Diseñado para crecer y adaptarse a nuevas funciones en el futuro.",
                icon: "FiTrendingUp",
              },
            ]}
          />

          <Divider my="16" />

          <Box
            p={{ base: 8, md: 14 }}
            borderRadius="2xl"
            borderWidth="1px"
            textAlign="center"
          >
            <Heading fontSize={{ base: "2xl", md: "3xl" }}>
              ¿Listo para comenzar?
            </Heading>
            <Text mt="4" color="muted" maxW="2xl" mx="auto">
              Únete a Gestión de Servicios Profesionales y comienza a ofrecer o
              contratar servicios con una experiencia moderna y profesional.
            </Text>

            <HStack justify="center" mt="8" spacing="4" flexWrap="wrap">
              <ButtonLink href="/signup" colorScheme="primary">
                Crear cuenta
              </ButtonLink>
              <Button variant="outline" as="a" href="/#pricing">
                Ver planes
              </Button>
            </HStack>
          </Box>
        </Container>
      </Box>
    </MarketingLayout>
  );
}
