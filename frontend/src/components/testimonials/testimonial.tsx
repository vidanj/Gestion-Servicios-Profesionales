import {
  Avatar,
  BoxProps,
  Card,
  Heading,
  Stack,
  Text,
} from "@chakra-ui/react";
import { FaTwitter } from "react-icons/fa";

export interface TestimonialProps extends BoxProps {
  name: string;
  description: React.ReactNode;
  avatar: string;
  href?: string;
  children?: React.ReactNode;
}

export const Testimonial = ({
  name,
  description,
  avatar,
  href,
  children,
  ...rest
}: TestimonialProps) => {
  return (
    <Card.Root position="relative" {...rest}>
      <Card.Header display="flex" flexDirection="row" alignItems="center">
        <Avatar.Root size="sm">
          {avatar ? <Avatar.Image src={avatar} /> : null}
          <Avatar.Fallback name={name} />
        </Avatar.Root>
        <Stack gap="1" ms="4">
          <Heading size="sm">{name}</Heading>
          <Text color="muted" fontSize="xs">
            {description}
          </Text>
        </Stack>
      </Card.Header>
      <Card.Body>
        {children}

        {href && (
          <a href={href} style={{ position: 'absolute', top: '1rem', right: '1rem' }}>
            <FaTwitter />
          </a>
        )}
      </Card.Body>
    </Card.Root>
  );
};
