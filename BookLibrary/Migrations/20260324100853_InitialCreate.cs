using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookLibrary.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Biography = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ISBN = table.Column<string>(type: "TEXT", maxLength: 13, nullable: true),
                    PublishedYear = table.Column<int>(type: "INTEGER", nullable: false),
                    PageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Books_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Authors",
                columns: new[] { "Id", "Biography", "BirthDate", "FirstName", "LastName" },
                values: new object[,]
                {
                    { 1, "American science fiction author, best known for the Dune series.", new DateTime(1920, 10, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "Frank", "Herbert" },
                    { 2, "English author and philologist, creator of Middle-earth.", new DateTime(1892, 1, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "J.R.R.", "Tolkien" },
                    { 3, "English novelist and essayist known for 1984 and Animal Farm.", new DateTime(1903, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "George", "Orwell" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Novels, short stories, and other fictional works.", "Fiction" },
                    { 2, "Factual writing including biographies, history, and essays.", "Non-Fiction" },
                    { 3, "Natural sciences, technology, and mathematics.", "Science" },
                    { 4, "Speculative fiction with magical or supernatural elements.", "Fantasy" },
                    { 5, "Life stories of real people.", "Biography" }
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "AuthorId", "CategoryId", "Description", "ISBN", "IsAvailable", "PageCount", "Price", "PublishedYear", "Title" },
                values: new object[,]
                {
                    { 1, 1, 3, "A sweeping tale of politics, religion, and ecology set on the desert planet Arrakis.", "9780441172719", true, 412, 14.99m, 1965, "Dune" },
                    { 2, 2, 4, "The epic quest to destroy the One Ring and defeat the Dark Lord Sauron.", "9780618640157", true, 1178, 24.99m, 1954, "The Lord of the Rings" },
                    { 3, 3, 1, "A chilling dystopian vision of a totalitarian society under constant surveillance.", "9780451524935", true, 328, 12.99m, 1949, "1984" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AuthorId",
                table: "Books",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CategoryId",
                table: "Books",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                table: "Books",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
