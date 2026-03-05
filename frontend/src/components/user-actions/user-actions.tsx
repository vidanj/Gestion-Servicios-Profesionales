"use client";

import { HStack, IconButton } from "@chakra-ui/react";
import { FiEdit2, FiTrash2 } from "react-icons/fi";

export interface UserCardActionsProps {
  onEdit: () => void;
  onDelete: () => void;
}

export const UserCardActions = ({ onEdit, onDelete }: UserCardActionsProps) => {
  return (
    <HStack gap="1">
      <IconButton
        aria-label="Editar usuario"
        size="sm"
        variant="ghost"
        onClick={onEdit}
      >
        <FiEdit2 />
      </IconButton>
      <IconButton
        aria-label="Eliminar usuario"
        size="sm"
        variant="ghost"
        colorPalette="red"
        onClick={onDelete}
      >
        <FiTrash2 />
      </IconButton>
    </HStack>
  );
};
