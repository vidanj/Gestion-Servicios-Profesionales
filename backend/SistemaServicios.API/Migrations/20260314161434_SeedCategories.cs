using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaServicios.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "Description",
                    "IconUrl",
                    "IsActive",
                    "CreatedAt",
                    "UpdatedAt",
                },
                values: new object[,]
                {
                    {
                        1,
                        "Plomería",
                        "Instalación y reparación de tuberías, llaves, calentadores y drenajes.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        2,
                        "Electricidad",
                        "Instalaciones eléctricas, cableado, tableros y reparación de fallas.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        3,
                        "Carpintería",
                        "Fabricación y reparación de muebles, puertas, ventanas y estructuras de madera.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        4,
                        "Pintura",
                        "Pintura de interiores, exteriores, texturas y acabados decorativos.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        5,
                        "Limpieza del Hogar",
                        "Limpieza general, profunda y mantenimiento de espacios residenciales.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        6,
                        "Jardinería",
                        "Diseño, poda y mantenimiento de jardines y áreas verdes.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        7,
                        "Albañilería",
                        "Construcción, remodelación, repellado y acabados en mampostería.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        8,
                        "Herrería",
                        "Fabricación y reparación de rejas, portones, escaleras y estructuras metálicas.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        9,
                        "Climatización",
                        "Instalación y mantenimiento de aires acondicionados, ventilación y calefacción.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        10,
                        "Mudanzas",
                        "Traslado de mobiliario y enseres con personal y vehículo especializado.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        11,
                        "Tecnología e IT",
                        "Soporte técnico, redes, instalación de equipos y reparación de dispositivos.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        12,
                        "Cerrajería",
                        "Apertura de cerraduras, duplicado de llaves e instalación de chapas.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        13,
                        "Fumigación",
                        "Control de plagas, desinfección y saneamiento de espacios.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        14,
                        "Diseño de Interiores",
                        "Asesoría y decoración de espacios residenciales y comerciales.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        15,
                        "Seguridad",
                        "Instalación de cámaras, alarmas y sistemas de control de acceso.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        16,
                        "Cuidado Personal",
                        "Peluquería, estética, masajes y servicios de bienestar a domicilio.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        17,
                        "Clases y Tutorías",
                        "Enseñanza personalizada de materias académicas, idiomas e instrumentos.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        18,
                        "Contabilidad y Legal",
                        "Asesoría contable, fiscal, trámites legales y elaboración de contratos.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        19,
                        "Fotografía y Video",
                        "Sesiones fotográficas, cobertura de eventos y producción audiovisual.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                    {
                        20,
                        "Mecánica Automotriz",
                        "Diagnóstico, mantenimiento y reparación de vehículos a domicilio o en taller.",
                        null,
                        true,
                        new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc),
                        null,
                    },
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9,
                    10,
                    11,
                    12,
                    13,
                    14,
                    15,
                    16,
                    17,
                    18,
                    19,
                    20,
                }
            );
        }
    }
}
