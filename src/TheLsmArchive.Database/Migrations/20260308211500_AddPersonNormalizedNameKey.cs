using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using TheLsmArchive.Database.DbContext;

#nullable disable

namespace TheLsmArchive.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(ReadWriteDbContext))]
[Migration("20260308211500_AddPersonNormalizedNameKey")]
public partial class AddPersonNormalizedNameKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "normalized_name",
            table: "persons",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        // Backfill canonical keys so spacing/punctuation variants map to the same value.
        migrationBuilder.Sql(
            """
            UPDATE persons
            SET normalized_name = COALESCE(
                NULLIF(regexp_replace(lower(trim(name)), '[^[:alnum:]]+', '', 'g'), ''),
                lower(trim(name))
            );
            """);

        // Merge duplicate person records that collapse to the same normalized key.
        migrationBuilder.Sql(
            """
            WITH canonical_persons AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM persons
                GROUP BY normalized_name
            ),
            duplicate_persons AS (
                SELECT p.id AS duplicate_id, cp.canonical_id
                FROM persons p
                INNER JOIN canonical_persons cp ON cp.normalized_name = p.normalized_name
                WHERE p.id <> cp.canonical_id
            )
            INSERT INTO person_episodes (person_id, episode_id)
            SELECT dp.canonical_id, pe.episode_id
            FROM person_episodes pe
            INNER JOIN duplicate_persons dp ON dp.duplicate_id = pe.person_id
            ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_persons AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM persons
                GROUP BY normalized_name
            ),
            duplicate_persons AS (
                SELECT p.id AS duplicate_id, cp.canonical_id
                FROM persons p
                INNER JOIN canonical_persons cp ON cp.normalized_name = p.normalized_name
                WHERE p.id <> cp.canonical_id
            )
            INSERT INTO person_topics (person_id, topic_id)
            SELECT dp.canonical_id, pt.topic_id
            FROM person_topics pt
            INNER JOIN duplicate_persons dp ON dp.duplicate_id = pt.person_id
            ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_persons AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM persons
                GROUP BY normalized_name
            ),
            duplicate_persons AS (
                SELECT p.id AS duplicate_id, cp.canonical_id
                FROM persons p
                INNER JOIN canonical_persons cp ON cp.normalized_name = p.normalized_name
                WHERE p.id <> cp.canonical_id
            )
            DELETE FROM person_episodes pe
            USING duplicate_persons dp
            WHERE pe.person_id = dp.duplicate_id;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_persons AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM persons
                GROUP BY normalized_name
            ),
            duplicate_persons AS (
                SELECT p.id AS duplicate_id, cp.canonical_id
                FROM persons p
                INNER JOIN canonical_persons cp ON cp.normalized_name = p.normalized_name
                WHERE p.id <> cp.canonical_id
            )
            DELETE FROM person_topics pt
            USING duplicate_persons dp
            WHERE pt.person_id = dp.duplicate_id;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_persons AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM persons
                GROUP BY normalized_name
            ),
            duplicate_persons AS (
                SELECT p.id AS duplicate_id, cp.canonical_id
                FROM persons p
                INNER JOIN canonical_persons cp ON cp.normalized_name = p.normalized_name
                WHERE p.id <> cp.canonical_id
            )
            DELETE FROM persons p
            USING duplicate_persons dp
            WHERE p.id = dp.duplicate_id;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "normalized_name",
            table: "persons",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_persons_normalized_name",
            table: "persons",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_persons_normalized_name",
            table: "persons");

        migrationBuilder.DropColumn(
            name: "normalized_name",
            table: "persons");
    }
}
