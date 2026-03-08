using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheLsmArchive.Database.Migrations;

/// <inheritdoc />
public partial class AddTopicNormalizedNameKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "normalized_name",
            table: "topics",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        // Backfill canonical keys so spacing/punctuation variants map to the same value.
        migrationBuilder.Sql(
            """
            UPDATE topics
            SET normalized_name = COALESCE(
                NULLIF(regexp_replace(lower(trim(name)), '[^[:alnum:]]+', '', 'g'), ''),
                lower(trim(name))
            );
            """);

        // Merge duplicate topic records that collapse to the same normalized key.
        migrationBuilder.Sql(
            """
            WITH canonical_topics AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM topics
                GROUP BY normalized_name
            ),
            duplicate_topics AS (
                SELECT t.id AS duplicate_id, ct.canonical_id
                FROM topics t
                INNER JOIN canonical_topics ct ON ct.normalized_name = t.normalized_name
                WHERE t.id <> ct.canonical_id
            )
            INSERT INTO topic_episodes (topic_id, episode_id)
            SELECT dt.canonical_id, te.episode_id
            FROM topic_episodes te
            INNER JOIN duplicate_topics dt ON dt.duplicate_id = te.topic_id
            ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_topics AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM topics
                GROUP BY normalized_name
            ),
            duplicate_topics AS (
                SELECT t.id AS duplicate_id, ct.canonical_id
                FROM topics t
                INNER JOIN canonical_topics ct ON ct.normalized_name = t.normalized_name
                WHERE t.id <> ct.canonical_id
            )
            INSERT INTO person_topics (person_id, topic_id)
            SELECT pt.person_id, dt.canonical_id
            FROM person_topics pt
            INNER JOIN duplicate_topics dt ON dt.duplicate_id = pt.topic_id
            ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_topics AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM topics
                GROUP BY normalized_name
            ),
            duplicate_topics AS (
                SELECT t.id AS duplicate_id, ct.canonical_id
                FROM topics t
                INNER JOIN canonical_topics ct ON ct.normalized_name = t.normalized_name
                WHERE t.id <> ct.canonical_id
            )
            DELETE FROM topic_episodes te
            USING duplicate_topics dt
            WHERE te.topic_id = dt.duplicate_id;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_topics AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM topics
                GROUP BY normalized_name
            ),
            duplicate_topics AS (
                SELECT t.id AS duplicate_id, ct.canonical_id
                FROM topics t
                INNER JOIN canonical_topics ct ON ct.normalized_name = t.normalized_name
                WHERE t.id <> ct.canonical_id
            )
            DELETE FROM person_topics pt
            USING duplicate_topics dt
            WHERE pt.topic_id = dt.duplicate_id;
            """);

        migrationBuilder.Sql(
            """
            WITH canonical_topics AS (
                SELECT normalized_name, MIN(id) AS canonical_id
                FROM topics
                GROUP BY normalized_name
            ),
            duplicate_topics AS (
                SELECT t.id AS duplicate_id, ct.canonical_id
                FROM topics t
                INNER JOIN canonical_topics ct ON ct.normalized_name = t.normalized_name
                WHERE t.id <> ct.canonical_id
            )
            DELETE FROM topics t
            USING duplicate_topics dt
            WHERE t.id = dt.duplicate_id;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "normalized_name",
            table: "topics",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_topics_normalized_name",
            table: "topics",
            column: "normalized_name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_topics_normalized_name",
            table: "topics");

        migrationBuilder.DropColumn(
            name: "normalized_name",
            table: "topics");
    }
}
