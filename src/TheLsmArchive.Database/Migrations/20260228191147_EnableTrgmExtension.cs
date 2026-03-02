using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheLsmArchive.Database.Migrations;

/// <inheritdoc />
public partial class EnableTrgmExtension : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
}
