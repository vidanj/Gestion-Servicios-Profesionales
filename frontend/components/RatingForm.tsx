"use client";

import { useState } from "react";
import { Box, Button, Textarea, VStack, HStack, Text, Heading } from "@chakra-ui/react";
import { FaStar } from "react-icons/fa";
import axios from "axios";

export default function RatingForm({ professionalId, requestId }: { professionalId: string, requestId: number }) {
    const [score, setScore] = useState(0);
    const [hover, setHover] = useState(0);
    const [comment, setComment] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isSuccess, setIsSuccess] = useState(false); 

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (score === 0) {
            alert("Por favor, selecciona una calificación del 1 al 5.");
            return;
        }

        setIsSubmitting(true);
        try {
            const token = localStorage.getItem("token");

            await axios.post("http://localhost:5000/api/Ratings", {
                professionalId,
                requestId,
                score,
                comment
            }, {
                headers: { Authorization: `Bearer ${token}` }
            });

            setIsSuccess(true);
        } catch (error: any) {
            console.error(error);
            alert(error.response?.data?.message || "Ocurrió un error al enviar la calificación. (¿Iniciaste sesión?)");
        } finally {
            setIsSubmitting(false);
        }
    };


    if (isSuccess) {
        return (
            <Box p={6} borderWidth="1px" borderRadius="lg" boxShadow="md" bg="green.50" maxW="md" mx="auto" mt={8} textAlign="center">
                <Heading as="h3" size="md" color="green.600" mb={2}>¡Calificación enviada!</Heading>
                <Text color="green.700">Gracias por compartir tu experiencia con este profesional.</Text>
            </Box>
        );
    }


    return (
        <Box p={6} borderWidth="1px" borderRadius="lg" boxShadow="md" bg="white" maxW="md" mx="auto" mt={8}>
            <Heading as="h3" size="md" mb={6} color="gray.800" textAlign="center">
                Califica el Servicio
            </Heading>

            <form onSubmit={handleSubmit}>
                <VStack gap={4} align="stretch">
                    {/* Criterio 1: Selector de Estrellas */}
                    <HStack justify="center" gap={2}>
                        {[1, 2, 3, 4, 5].map((star) => (
                            <button
                                type="button"
                                key={star}
                                onClick={() => setScore(star)}
                                onMouseEnter={() => setHover(star)}
                                onMouseLeave={() => setHover(0)}
                                style={{
                                    background: "none",
                                    border: "none",
                                    cursor: "pointer",
                                    color: star <= (hover || score) ? "#ecc94b" : "#e2e8f0",
                                    transition: "transform 0.2s",
                                    transform: hover === star ? "scale(1.15)" : "scale(1)",
                                }}
                            >
                                <FaStar size={36} />
                            </button>
                        ))}
                    </HStack>

                    {/* Criterio 2: Campo Comentario */}
                    <Box>
                        <Textarea
                            placeholder="¿Qué tal te pareció el servicio? (Opcional)"
                            value={comment}
                            onChange={(e) => setComment(e.target.value)}
                            maxLength={500}
                            rows={4}
                            resize="none"
                            color="black"
                        />
                        <Text fontSize="xs" color="gray.500" textAlign="right" mt={1}>
                            {comment.length}/500 caracteres
                        </Text>
                    </Box>

                    <Button
                        type="submit"
                        colorScheme="blue"
                        size="md"
                        width="full"
                        disabled={isSubmitting}
                        opacity={isSubmitting ? 0.7 : 1}
                    >
                        {isSubmitting ? "Enviando..." : "Enviar Calificación"}
                    </Button>
                </VStack>
            </form>
        </Box>
    );
}