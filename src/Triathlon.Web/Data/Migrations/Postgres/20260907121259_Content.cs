using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Triathlon.Web.Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Content : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Athletes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    CityKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: true),
                    InterestEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LicenceNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ConsentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreferredCulture = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NameEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CityEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CityAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Committees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KindEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    KindAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NameAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DescriptionEn = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    DescriptionAr = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Committees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NavItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LabelEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LabelAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Href = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsPosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TitleAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SummaryEn = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SummaryAr = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    BodyEn = table.Column<string>(type: "text", nullable: false),
                    BodyAr = table.Column<string>(type: "text", nullable: false),
                    HeroImagePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TitleEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TitleAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MetaDescriptionEn = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    MetaDescriptionAr = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Variant = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Anchor = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EyebrowEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EyebrowAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TitleEn = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TitleAr = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BodyEn = table.Column<string>(type: "text", nullable: true),
                    BodyAr = table.Column<string>(type: "text", nullable: true),
                    ItemsJson = table.Column<string>(type: "text", nullable: false),
                    CtaLabelEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CtaLabelAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CtaHref = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SecondaryLabelEn = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SecondaryLabelAr = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SecondaryHref = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageBlocks_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_Email",
                table: "Athletes",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_Status",
                table: "Athletes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_IsActive_SortOrder",
                table: "Clubs",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Committees_IsPublished_SortOrder",
                table: "Committees",
                columns: new[] { "IsPublished", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NavItems_Location_SortOrder",
                table: "NavItems",
                columns: new[] { "Location", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_IsPublished_PublishedOn",
                table: "NewsPosts",
                columns: new[] { "IsPublished", "PublishedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPosts_Slug",
                table: "NewsPosts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageBlocks_PageId_SortOrder",
                table: "PageBlocks",
                columns: new[] { "PageId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Slug",
                table: "Pages",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Athletes");

            migrationBuilder.DropTable(
                name: "Clubs");

            migrationBuilder.DropTable(
                name: "Committees");

            migrationBuilder.DropTable(
                name: "NavItems");

            migrationBuilder.DropTable(
                name: "NewsPosts");

            migrationBuilder.DropTable(
                name: "PageBlocks");

            migrationBuilder.DropTable(
                name: "Pages");
        }
    }
}
