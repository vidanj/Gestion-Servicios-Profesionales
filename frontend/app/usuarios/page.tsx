"use client";
import { useRouter } from "next/navigation";
import { useEffect, useState, useCallback } from "react";
import NextLink from "next/link";
import {
  Badge, Box, Button, ButtonGroup, Card, Container,
  Field, HStack, Heading, IconButton, Input,
  Separator, SimpleGrid, Stack, Table, Text,
} from "@chakra-ui/react";
import { FiEdit2, FiPlus, FiTrash2 } from "react-icons/fi";

const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const roles = [
  { value: "1", label: "Cliente" },
  { value: "2", label: "Profesionista" },
];

const roleColor: Record<string, string> = { "1": "blue", "2": "green", "0": "purple" };
const roleLabel: Record<string, string> = { "0": "Admin", "1": "Cliente", "2": "Profesionista" };

type UserRow = {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  role: number;
  status: boolean;
};

export default function UsersCrudPage() {
  const router = useRouter();
  const [mounted, setMounted]     = useState(false);
  const [users, setUsers]         = useState<UserRow[]>([]);
  const [total, setTotal]         = useState(0);
  const [loading, setLoading]     = useState(false);
  const [apiError, setApiError]   = useState("");

  // Campos del formulario
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName]   = useState("");
  const [email, setEmail]         = useState("");
  const [phoneNumber, setPhone]   = useState("");
  const [password, setPassword]   = useState("");
  const [role, setRole]           = useState("");
  const [roleOpen, setRoleOpen]   = useState(false);
  const [formError, setFormError] = useState("");

  const [editingUser, setEditingUser] = useState<UserRow | null>(null);
  const [editFirstName, setEditFirst] = useState("");
  const [editLastName, setEditLast] = useState("");
  const [editPhone, setEditPhone] = useState("");
  const [editRole, setEditRole] = useState("");
  const [editError, setEditError] = useState("");
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  const selectedRole = roles.find(r => r.value === role);

  // ── Helpers ────────────────────────────────────────────────
  const getToken = () =>
    typeof window !== "undefined" ? localStorage.getItem("token") : null;

  const authHeaders = () => ({
    "Content-Type": "application/json",
    Authorization: `Bearer ${getToken()}`,
  });

  // ── GET usuarios ───────────────────────────────────────────
  const fetchUsers = useCallback(async () => {
    const token = getToken();
    if (!token) return;
    setLoading(true);
    setApiError("");
    try {
      const res = await fetch(`${apiUrl}/api/Users?page=1&size=50`, {
        headers: authHeaders(),
      });
      if (!res.ok) {
        console.error("Status:", res.status, await res.text());
        throw new Error("Error al cargar usuarios");
    }
      const json = await res.json();
      setUsers(json.data ?? json.Data ?? []);
      setTotal(json.total ?? json.Total ?? 0);
    } catch (e) {
      setApiError("No se pudieron cargar los usuarios.");
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { setMounted(true); fetchUsers(); }, [fetchUsers]);
  if (!mounted) return null;

  // ── Validación del formulario ──────────────────────────────
  const validate = () => {
    if (!firstName.trim()) { setFormError("El nombre es obligatorio"); return false; }
    if (!lastName.trim())  { setFormError("El apellido es obligatorio"); return false; }
    if (!role.trim())      { setFormError("El rol es obligatorio"); return false; }
    if (!email.trim() || !email.includes("@")) { setFormError("El email no es válido"); return false; }
    if (!phoneNumber.trim()) { setFormError("El teléfono es obligatorio"); return false; }
    if (password.length < 6) { setFormError("La contraseña debe tener al menos 6 caracteres"); return false; }
    setFormError("");
    return true;
  };

  const clearForm = () => {
    setFirstName(""); setLastName(""); setEmail("");
    setPhone(""); setPassword(""); setRole(""); setFormError("");
  };

  // ── POST crear usuario ─────────────────────────────────────
  const handleCreate = async () => {
    if (!validate()) return;
    try {
      const res = await fetch(`${apiUrl}/api/Users`, {
        method: "POST",
        headers: authHeaders(),
        body: JSON.stringify({
          email,
          firstName,
          lastName,
          phoneNumber,
          password,
          role: parseInt(role),
        }),
      });
      if (!res.ok) {
        const err = await res.json();
        setFormError(err.message ?? "Error al crear usuario");
        return;
      }
      clearForm();
      fetchUsers();
    } catch {
      setFormError("Error de conexión");
    }
  };

  // ── DELETE (soft) usuario ──────────────────────────────────
  const handleDelete = async (id: string) => {
    try {
      const res = await fetch(`${apiUrl}/api/Users/${id}`, {
        method: "DELETE",
        headers: authHeaders(),
      });
      if (!res.ok) throw new Error("Error al eliminar");
      fetchUsers();
    } catch {
      setApiError("No se pudo eliminar el usuario.");
    }
  };

  const openEdit = (user: UserRow) => {
    setEditingUser(user);
    setEditFirst(user.firstName);
    setEditLast(user.lastName);
    setEditPhone(user.phoneNumber ?? "");
    setEditRole(String(user.role));
    setEditError("");
  };

  const handleUpdate = async () => {
      if (!editingUser) return;
      if (!editFirstName.trim()) { setEditError("El nombre es obligatorio"); return; }
      if (!editLastName.trim())  { setEditError("El apellido es obligatorio"); return; }
      if (!editRole)             { setEditError("El rol es obligatorio"); return; }

      try {
          const res = await fetch(`${apiUrl}/api/Users/${editingUser.id}`, {
              method: "PUT",
              headers: authHeaders(),
              body: JSON.stringify({
                  firstName: editFirstName,
                  lastName: editLastName,
                  phoneNumber: editPhone,
                  role: parseInt(editRole),
                  status: editingUser.status,
              }),
          });
          if (!res.ok) { setEditError("Error al actualizar"); return; }
          setEditingUser(null);
          fetchUsers();
      } catch {
          setEditError("Error de conexión");
      }
  };

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">

          <Stack gap="2">
            <Heading size="lg">CRUD de usuarios</Heading>
            <Text color="muted">Crea, edita y administra usuarios.</Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild colorPalette="purple"><NextLink href="/usuarios">CRUD</NextLink></Button>
              <Button asChild variant="ghost"><NextLink href="/usuarios/registrados">Usuarios registrados</NextLink></Button>
              <Button asChild variant="ghost"><NextLink href="/usuarios/logs">Logs de usuarios</NextLink></Button>
            </HStack>
          </Stack>

          {apiError && (
            <Text data-testid="api-error" color="red.500">{apiError}</Text>
          )}

          <SimpleGrid columns={{ base: 1, lg: 3 }} gap="6">

            {/* ── Formulario ── */}
            <Card.Root>
              <Card.Header><Heading size="md">Nuevo usuario</Heading></Card.Header>
              <Card.Body>
                <Stack gap="4">

                  <Field.Root>
                    <Field.Label>Nombre</Field.Label>
                    <Input data-testid="crud-firstname" placeholder="Juan"
                      value={firstName} onChange={e => setFirstName(e.target.value)} />
                  </Field.Root>

                  <Field.Root>
                    <Field.Label>Apellido</Field.Label>
                    <Input data-testid="crud-lastname" placeholder="Pérez"
                      value={lastName} onChange={e => setLastName(e.target.value)} />
                  </Field.Root>

                  <Field.Root>
                    <Field.Label>Correo</Field.Label>
                    <Input data-testid="crud-email" type="email" placeholder="correo@ejemplo.com"
                      value={email} onChange={e => setEmail(e.target.value)} />
                  </Field.Root>

                  <Field.Root>
                    <Field.Label>Teléfono</Field.Label>
                    <Input data-testid="crud-phone" type="tel" placeholder="+504 9999-9999"
                      value={phoneNumber} onChange={e => setPhone(e.target.value)} />
                  </Field.Root>

                  <Field.Root>
                    <Field.Label>Contraseña</Field.Label>
                    <Input data-testid="crud-password" type="password" placeholder="mínimo 6 caracteres"
                      value={password} onChange={e => setPassword(e.target.value)} />
                  </Field.Root>

                  {/* Dropdown rol */}
                  <Field.Root>
                    <Field.Label>Rol</Field.Label>
                    <div style={{ position: "relative" }}>
                      <button
                        data-testid="crud-role-button"
                        type="button"
                        onClick={() => setRoleOpen(!roleOpen)}
                        style={{
                          display: "flex", alignItems: "center", justifyContent: "space-between",
                          width: "100%", padding: "0.5rem 1rem", borderRadius: "9999px",
                          background: "transparent", border: "1px solid var(--chakra-colors-border)",
                          color: selectedRole ? "inherit" : "gray",
                          cursor: "pointer", fontFamily: "inherit", fontSize: "0.95rem",
                        }}
                      >
                        <span>{selectedRole ? selectedRole.label : "Selecciona un rol"}</span>
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24"
                          fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
                          style={{ transform: roleOpen ? "rotate(180deg)" : "rotate(0deg)", transition: "transform 0.2s" }}>
                          <polyline points="6 9 12 15 18 9" />
                        </svg>
                      </button>
                      {roleOpen && (
                        <div style={{
                          position: "absolute", top: "calc(100% + 4px)", left: 0, right: 0,
                          background: "var(--chakra-colors-bg)", border: "1px solid var(--chakra-colors-border)",
                          borderRadius: "0.75rem", overflow: "hidden",
                          boxShadow: "0 10px 25px rgba(0,0,0,0.3)", zIndex: 50,
                        }}>
                          {roles.map(r => (
                            <button
                              data-testid={`crud-role-option-${r.label.toLowerCase()}`}
                              key={r.value} type="button"
                              onClick={() => { setRole(r.value); setRoleOpen(false); }}
                              style={{
                                display: "block", width: "100%", textAlign: "left",
                                padding: "0.75rem 1rem", background: "transparent",
                                border: "none", color: role === r.value ? "#a78bfa" : "inherit",
                                fontSize: "0.95rem", cursor: "pointer", fontFamily: "inherit",
                                borderBottom: "1px solid var(--chakra-colors-border)",
                              }}
                            >
                              {role === r.value && "✓ "}{r.label}
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                  </Field.Root>

                  {formError && (
                    <Text data-testid="crud-error" color="red.500" fontSize="sm">{formError}</Text>
                  )}

                  <ButtonGroup>
                    <Button data-testid="crud-create-button" colorPalette="purple" onClick={handleCreate}>
                      <FiPlus /> Crear usuario
                    </Button>
                    <Button variant="outline" onClick={clearForm}>Limpiar</Button>
                  </ButtonGroup>
                </Stack>
              </Card.Body>
            </Card.Root>

            {/* ── Tabla ── */}
            <Card.Root gridColumn={{ lg: "span 2" }}>
              <Card.Header>
                <HStack justify="space-between">
                  <Heading size="md">Usuarios existentes</Heading>
                  <Button variant="outline" size="sm" onClick={fetchUsers}>Recargar</Button>
                </HStack>
              </Card.Header>
              <Card.Body>
                {loading ? (
                  <Text textAlign="center" py="4">Cargando...</Text>
                ) : (
                  <Table.Root variant="outline" size="sm">
                    <Table.Header>
                      <Table.Row>
                        <Table.ColumnHeader>Nombre</Table.ColumnHeader>
                        <Table.ColumnHeader>Correo</Table.ColumnHeader>
                        <Table.ColumnHeader>Teléfono</Table.ColumnHeader>
                        <Table.ColumnHeader>Rol</Table.ColumnHeader>
                        <Table.ColumnHeader textAlign="right">Acciones</Table.ColumnHeader>
                      </Table.Row>
                    </Table.Header>
                    <Table.Body>
                      {users.length === 0 ? (
                        <Table.Row>
                          <Table.Cell colSpan={5}>
                            <Text color="muted" textAlign="center" py="4">Sin usuarios.</Text>
                          </Table.Cell>
                        </Table.Row>
                      ) : (
                        users.map(user => (
                          <Table.Row key={user.id} data-testid="crud-user-row">
                            <Table.Cell>
                              <Text fontWeight="semibold">{user.firstName} {user.lastName}</Text>
                            </Table.Cell>
                            <Table.Cell>{user.email}</Table.Cell>
                            <Table.Cell>{user.phoneNumber ?? "—"}</Table.Cell>
                            <Table.Cell>
                              <Badge colorPalette={roleColor[String(user.role)]}>
                                {roleLabel[String(user.role)] ?? user.role}
                              </Badge>
                            </Table.Cell>
                            <Table.Cell textAlign="right">
                              <HStack justify="flex-end">
                                <IconButton
                                    aria-label="Editar" size="sm" variant="ghost"
                                    onClick={() => openEdit(user)}
                                >
                                    <FiEdit2 />
                                </IconButton>
                                <IconButton
                                    data-testid="crud-delete-button"
                                    aria-label="Eliminar" size="sm" colorPalette="red" variant="ghost"
                                    onClick={() => setDeleteConfirmId(user.id)}
                                >
                                    <FiTrash2 />
                                </IconButton>
                            </HStack>
                            </Table.Cell>
                          </Table.Row>
                        ))
                      )}
                    </Table.Body>
                  </Table.Root>
                )}

                <Separator my="6" />

                <SimpleGrid columns={{ base: 1, md: 3 }} gap="4">
                  <Card.Root variant="outline">
                    <Card.Body>
                      <Text fontSize="sm" color="muted">Total usuarios</Text>
                      <Heading size="md">{total}</Heading>
                    </Card.Body>
                  </Card.Root>
                </SimpleGrid>
              </Card.Body>
            </Card.Root>

          </SimpleGrid>
        </Stack>
        {/* Modal de edición */}
        {editingUser && (
            <div style={{
                position: "fixed", inset: 0, background: "rgba(0,0,0,0.6)",
                display: "flex", alignItems: "center", justifyContent: "center", zIndex: 100,
            }}>
                <div style={{
                    background: "var(--chakra-colors-bg)", borderRadius: "1.5rem",
                    padding: "2rem", width: "100%", maxWidth: "480px",
                    border: "1px solid var(--chakra-colors-border)",
                }}>
                    <Heading size="md" mb="4" style={{ color: "black" }}>Editar usuario</Heading>
                    <Stack gap="3">
                        <Field.Root>
                            <Field.Label style={{ color: "black" }} >Nombre</Field.Label>
                            <Input data-testid="edit-firstname" value={editFirstName}
                                onChange={e => setEditFirst(e.target.value)} style={{ color: "black" }} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label style={{ color: "black" }} >Apellido</Field.Label>
                            <Input data-testid="edit-lastname" value={editLastName}
                                onChange={e => setEditLast(e.target.value)} style={{ color: "black" }} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label style={{ color: "black" }} >Teléfono</Field.Label>
                            <Input data-testid="edit-phone" value={editPhone}
                                onChange={e => setEditPhone(e.target.value)} style={{ color: "black" }} />
                        </Field.Root>
                        <Field.Root>
                            <Field.Label style={{ color: "black" }} >Rol</Field.Label>
                            <select
                                data-testid="edit-role"
                                value={editRole}
                                onChange={e => setEditRole(e.target.value)}
                                style={{
                                    width: "100%", padding: "0.5rem 1rem", borderRadius: "9999px",
                                    background: "transparent", border: "1px solid var(--chakra-colors-border)",
                                    color: "black", fontSize: "0.95rem", cursor: "pointer",
                                }}
                            >
                                <option value="1">Cliente</option>
                                <option value="2">Profesionista</option>
                            </select>
                        </Field.Root>
                        {editError && <Text color="red.500" fontSize="sm">{editError}</Text>}
                        <HStack justify="flex-end" gap="3" mt="2">
                            <Button variant="outline" onClick={() => setEditingUser(null)}>Cancelar</Button>
                            <Button data-testid="edit-save-button" colorPalette="purple" onClick={handleUpdate}>
                                Guardar
                            </Button>
                        </HStack>
                    </Stack>
                </div>
            </div>
        )}
        {/* Modal de confirmación de eliminación */}
        {deleteConfirmId && (
            <div style={{
                position: "fixed", inset: 0, background: "rgba(0,0,0,0.6)",
                display: "flex", alignItems: "center", justifyContent: "center", zIndex: 100,
            }}>
                <div style={{
                    background: "var(--chakra-colors-bg)", borderRadius: "1.5rem",
                    padding: "2rem", width: "100%", maxWidth: "400px",
                    border: "1px solid var(--chakra-colors-border)",
                }}>
                    <Heading size="md" mb="2">Confirmar eliminación</Heading>
                    <Text mb="4" color="muted">
                        Esta acción desactivará al usuario. ¿Deseas continuar?
                    </Text>
                    <HStack justify="flex-end" gap="3">
                        <Button variant="outline" onClick={() => setDeleteConfirmId(null)}>Cancelar</Button>
                        <Button
                            data-testid="confirm-delete-button"
                            colorPalette="red"
                            onClick={() => handleDelete(deleteConfirmId)}
                        >
                            Eliminar
                        </Button>
                    </HStack>
                </div>
            </div>
        )}
      </Container>
    </Box>
  );
}