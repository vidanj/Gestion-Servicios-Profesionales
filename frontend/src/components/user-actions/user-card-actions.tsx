"use client";

import { HStack, IconButton } from "@chakra-ui/react";
import { FiEdit2, FiTrash2 } from "react-icons/fi";

type UserCardActionsProps = {
  onEdit: () => void;
  onDelete: () => void;
};

export const UserCardActions = ({ onEdit, onDelete }: UserCardActionsProps) => (
  <HStack spacing="1">
    <IconButton
      aria-label="Editar"
      icon={<FiEdit2 />}
      size="sm"
      variant="ghost"
      isRound
      onClick={onEdit}
    />
    <IconButton
      aria-label="Eliminar"
      icon={<FiTrash2 />}
      size="sm"
      variant="ghost"
      colorScheme="red"
      isRound
      onClick={onDelete}
    />
  </HStack>
);
