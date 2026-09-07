using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triathlon.Web.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Statistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GrowthPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Athletes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrowthPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kpis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LabelEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LabelAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    Suffix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ShowPlus = table.Column<bool>(type: "boolean", nullable: false),
                    NoteEn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NoteAr = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Color = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ShowOnHome = table.Column<bool>(type: "boolean", nullable: false),
                    HomeOrder = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kpis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegionStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Athletes = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrowthPoints_Year",
                table: "GrowthPoints",
                column: "Year",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_Key",
                table: "Kpis",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegionStats_Key",
                table: "RegionStats",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrowthPoints");

            migrationBuilder.DropTable(
                name: "Kpis");

            migrationBuilder.DropTable(
                name: "RegionStats");
        }
    }
}
