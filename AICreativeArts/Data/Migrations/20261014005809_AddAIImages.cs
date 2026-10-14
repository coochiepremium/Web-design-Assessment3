using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AICreativeArts.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prompt = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ImageGenerator = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UploadDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    Like = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIImages", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AIImages",
                columns: new[] { "Id", "Filename", "ImageGenerator", "Like", "Prompt", "UploadDate" },
                values: new object[,]
                {
                    { 1, "sample-golden-currents.jpg", "Flow Field Generator", 7, "Thousands of golden light particles drifting along an invisible current, tracing silky strands across a deep midnight-blue canvas, long-exposure style.", new DateTime(2026, 9, 1, 9, 30, 0, 0, DateTimeKind.Unspecified) },
                    { 2, "sample-stained-glass.jpg", "Voronoi Mosaic Generator", 5, "A jewel-toned stained-glass window built from seventy irregular cells with dark lead lines, warm light glowing from the upper left.", new DateTime(2026, 9, 3, 14, 5, 0, 0, DateTimeKind.Unspecified) },
                    { 3, "sample-harmonograph.jpg", "Harmonograph Simulator", 4, "A decaying pendulum drawing in neon cyan fading to magenta, looping ribbons spiralling towards the centre on a black background.", new DateTime(2026, 9, 6, 19, 45, 0, 0, DateTimeKind.Unspecified) },
                    { 4, "sample-interference.jpg", "Wave Interference Generator", 3, "Three ripple sources overlapping on a still pond, rendered as a glowing heat map of indigo, magenta, orange and gold.", new DateTime(2026, 9, 10, 11, 20, 0, 0, DateTimeKind.Unspecified) },
                    { 5, "sample-circle-garden.jpg", "Circle Packing Generator", 2, "A playful circle-packing pattern of concentric dots in orange, lavender, teal and gold on warm cream paper, mid-century print style.", new DateTime(2026, 9, 14, 16, 0, 0, 0, DateTimeKind.Unspecified) },
                    { 6, "sample-dusk-ridges.jpg", "Procedural Landscape Generator", 1, "Layered violet mountain ridges fading into the haze at dusk under a pale full moon, minimalist flat illustration.", new DateTime(2026, 9, 18, 8, 15, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIImages_Like",
                table: "AIImages",
                column: "Like");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIImages");
        }
    }
}
