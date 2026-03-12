using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheLsmArchive.Database.Migrations;

/// <inheritdoc />
public partial class AddUniqueIndexOnName : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_topics_name",
            table: "topics",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_persons_name",
            table: "persons",
            column: "name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_topics_name",
            table: "topics");

        migrationBuilder.DropIndex(
            name: "ix_persons_name",
            table: "persons");
    }
}
