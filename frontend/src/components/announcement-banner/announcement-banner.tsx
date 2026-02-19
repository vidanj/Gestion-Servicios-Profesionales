"use client";

import NextLink from "next/link";
import {
  Box,
  Container,
  Flex,
  HStack,
  Icon,
  Text,
} from "@chakra-ui/react";
import { FiArrowRight } from "react-icons/fi";
import { FallInPlace } from "../motion/fall-in-place";

export interface AnnouncementBannerProps {
  title: string;
  description: string;
  href: string;
  action?: string;
}

export const AnnouncementBanner: React.FC<AnnouncementBannerProps> = (
  props
) => {
  const { title, description, href, action } = props;
  if (!title) {
    return null;
  }

  return (
    <Flex position="absolute" zIndex="10" top="100px" width="100%">
      <Container maxW="container.2xl" px="8">
        <FallInPlace delay={1.4} translateY="-100px">
          <NextLink href={href} style={{ textDecoration: "none" }}>
            <Box
              display="flex"
              bg="white"
              fontSize="sm"
              justifyContent="center"
              backgroundClip="padding-box"
              borderRadius="full"
              maxW="400px"
              margin="0 auto"
              borderColor="transparent"
              position="relative"
              py="4px"
              px="3"
              overflow="visible"
              cursor="pointer"
              transition="all .2s ease-out"
              _dark={{ bg: "gray.900" }}
              _before={{
                content: `""`,
                position: "absolute",
                zIndex: -1,
                top: 0,
                right: 0,
                bottom: 0,
                left: 0,
                borderRadius: "inherit",
                margin: "-2px",
                backgroundImage:
                  "linear-gradient(to right, var(--chakra-colors-purple-500), var(--chakra-colors-cyan-500))",
                transition: "background .2s ease-out",
              }}
              _hover={{ boxShadow: "md" }}
            >
              <HStack zIndex="2">
                <Text fontWeight="semibold" lineClamp={1} whiteSpace="nowrap">
                  {title}
                </Text>
                <Text
                  display={{ base: "none", md: "block" }}
                  dangerouslySetInnerHTML={{ __html: description }}
                />
                {action && (
                  <HStack gap="1" color="gray.500">
                    <Text fontSize="xs">{action}</Text>
                    <Icon
                      as={FiArrowRight}
                      transitionProperty="common"
                      transitionDuration="normal"
                    />
                  </HStack>
                )}
              </HStack>
            </Box>
          </NextLink>
        </FallInPlace>
      </Container>
    </Flex>
  );
};
