"use client";

import { HStack, IconButton } from "@chakra-ui/react";
import { FiEdit2, FiTrash2 } from "react-icons/fi";

export interface UserCardActionsProps {
  onEdit: () => void;
  onDelete: () => void;
}

export const UserCardActions = ({ onEdit, onDelete }: UserCardActionsProps) => {
  return (
    <HStack spacing="1">
      <IconButton
        aria-label="Editar usuario"
        icon={<FiEdit2 />}
        size="sm"
        variant="ghost"
        onClick={onEdit}
      />
      <IconButton
        aria-label="Eliminar usuario"
        icon={<FiTrash2 />}
        size="sm"
        variant="ghost"
        colorScheme="red"
        onClick={onDelete}
      />
    </HStack>
  );
};
