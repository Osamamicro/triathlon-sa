using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triathlon.Web.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SvgX = table.Column<int>(type: "integer", nullable: false),
                    SvgY = table.Column<int>(type: "integer", nullable: false),
                    LabelAtEnd = table.Column<bool>(type: "boolean", nullable: false),
                    LabelDy = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Season = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DateStart = table.Column<DateOnly>(type: "date", nullable: false),
                    DateEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TitleAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    VenueEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    VenueAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false),
                    DescriptionAr = table.Column<string>(type: "text", nullable: false),
                    SwimDistance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BikeDistance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    RunDistance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Categories = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RegistrationMode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RegistrationOpen = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalRegistrationUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    HeroImagePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ResultsFilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventGalleryImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AltEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AltAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventGalleryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventGalleryImages_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Club = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventRegistrations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    AthleteEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AthleteAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ClubEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ClubAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Time = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventResults_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Key",
                table: "Cities",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventGalleryImages_EventId_SortOrder",
                table: "EventGalleryImages",
                columns: new[] { "EventId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_EventId_Status",
                table: "EventRegistrations",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventResults_EventId_Position",
                table: "EventResults",
                columns: new[] { "EventId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_CityId",
                table: "Events",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished_DateStart",
                table: "Events",
                columns: new[] { "IsPublished", "DateStart" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_Season",
                table: "Events",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Slug",
                table: "Events",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventGalleryImages");

            migrationBuilder.DropTable(
                name: "EventRegistrations");

            migrationBuilder.DropTable(
                name: "EventResults");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
