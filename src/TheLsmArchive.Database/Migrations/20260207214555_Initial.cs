using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TheLsmArchive.Database.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "persons",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_persons", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "shows",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_shows", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "topics",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_topics", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "patreon_posts",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                show_id = table.Column<int>(type: "integer", nullable: false),
                patreon_id = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                published = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                summary = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                link = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                audio_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                processing_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                episode_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_patreon_posts", x => x.id);
                table.ForeignKey(
                    name: "fk_patreon_posts_shows_show_id",
                    column: x => x.show_id,
                    principalTable: "shows",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "person_topics",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                topic_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_person_topics", x => x.id);
                table.ForeignKey(
                    name: "fk_person_topics_persons_person_id",
                    column: x => x.person_id,
                    principalTable: "persons",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_person_topics_topics_topic_id",
                    column: x => x.topic_id,
                    principalTable: "topics",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "episodes",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                show_id = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                release_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                patreon_post_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_episodes", x => x.id);
                table.ForeignKey(
                    name: "fk_episodes_patreon_posts_patreon_post_id",
                    column: x => x.patreon_post_id,
                    principalTable: "patreon_posts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_episodes_shows_show_id",
                    column: x => x.show_id,
                    principalTable: "shows",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "person_episodes",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                person_id = table.Column<int>(type: "integer", nullable: false),
                episode_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_person_episodes", x => x.id);
                table.ForeignKey(
                    name: "fk_person_episodes_episodes_episode_id",
                    column: x => x.episode_id,
                    principalTable: "episodes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_person_episodes_persons_person_id",
                    column: x => x.person_id,
                    principalTable: "persons",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "topic_episodes",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                topic_id = table.Column<int>(type: "integer", nullable: false),
                episode_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_topic_episodes", x => x.id);
                table.ForeignKey(
                    name: "fk_topic_episodes_episodes_episode_id",
                    column: x => x.episode_id,
                    principalTable: "episodes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_topic_episodes_topics_topic_id",
                    column: x => x.topic_id,
                    principalTable: "topics",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_episodes_patreon_post_id",
            table: "episodes",
            column: "patreon_post_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_episodes_show_id",
            table: "episodes",
            column: "show_id");

        migrationBuilder.CreateIndex(
            name: "ix_patreon_posts_show_id_patreon_id",
            table: "patreon_posts",
            columns: ["show_id", "patreon_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_person_episodes_episode_id",
            table: "person_episodes",
            column: "episode_id");

        migrationBuilder.CreateIndex(
            name: "ix_person_episodes_person_id_episode_id",
            table: "person_episodes",
            columns: ["person_id", "episode_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_person_topics_person_id_topic_id",
            table: "person_topics",
            columns: ["person_id", "topic_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_person_topics_topic_id",
            table: "person_topics",
            column: "topic_id");

        migrationBuilder.CreateIndex(
            name: "ix_shows_name",
            table: "shows",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_topic_episodes_episode_id",
            table: "topic_episodes",
            column: "episode_id");

        migrationBuilder.CreateIndex(
            name: "ix_topic_episodes_topic_id_episode_id",
            table: "topic_episodes",
            columns: ["topic_id", "episode_id"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "person_episodes");

        migrationBuilder.DropTable(
            name: "person_topics");

        migrationBuilder.DropTable(
            name: "topic_episodes");

        migrationBuilder.DropTable(
            name: "persons");

        migrationBuilder.DropTable(
            name: "episodes");

        migrationBuilder.DropTable(
            name: "topics");

        migrationBuilder.DropTable(
            name: "patreon_posts");

        migrationBuilder.DropTable(
            name: "shows");
    }
}
