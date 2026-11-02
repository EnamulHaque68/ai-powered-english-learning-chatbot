using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chatbot.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialChildLearningDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: false),
                    MaxAge = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChildProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "en"),
                    Interests = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrentLevelId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildProfiles_LearningLevels_CurrentLevelId",
                        column: x => x.CurrentLevelId,
                        principalTable: "LearningLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    ChildMood = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationSessions_ChildProfiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "ChildProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProficiencyScore = table.Column<double>(type: "float", nullable: false),
                    CompletedActivitiesCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalTimeSpentMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MasteryStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningProgress_ChildProfiles_ChildProfileId",
                        column: x => x.ChildProfileId,
                        principalTable: "ChildProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TokensCount = table.Column<int>(type: "int", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    SentimentScore = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMessages_ConversationSessions_ConversationSessionId",
                        column: x => x.ConversationSessionId,
                        principalTable: "ConversationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "LearningLevels",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "MaxAge", "MinAge", "SortOrder", "Title" },
                values: new object[,]
                {
                    { 1, "LEVEL_1_TODDLER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sensory play, simple phonics, basic shapes, and color recognition.", true, 5, 3, 1, "Toddler Explorer" },
                    { 2, "LEVEL_2_EARLY_READER", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Sight words, phonics blending, basic addition, and curiosity-driven Q&A.", true, 7, 6, 2, "Early Reader & Counter" },
                    { 3, "LEVEL_3_ELEMENTARY", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Story comprehension, multiplication, introductory science, and logic puzzles.", true, 10, 8, 3, "Elementary Discoverer" },
                    { 4, "LEVEL_4_JUNIOR_SCHOLAR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Critical thinking, pre-algebra, scientific inquiry, and creative writing.", true, 13, 11, 4, "Junior Scholar" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_CurrentLevelId",
                table: "ChildProfiles",
                column: "CurrentLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildProfiles_ParentEmail",
                table: "ChildProfiles",
                column: "ParentEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_ConversationSessionId",
                table: "ConversationMessages",
                column: "ConversationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_Timestamp",
                table: "ConversationMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSessions_ChildProfileId",
                table: "ConversationSessions",
                column: "ChildProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationSessions_StartedAt",
                table: "ConversationSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LearningLevels_Code",
                table: "LearningLevels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningProgress_ChildProfileId_Topic",
                table: "LearningProgress",
                columns: new[] { "ChildProfileId", "Topic" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationMessages");

            migrationBuilder.DropTable(
                name: "LearningProgress");

            migrationBuilder.DropTable(
                name: "ConversationSessions");

            migrationBuilder.DropTable(
                name: "ChildProfiles");

            migrationBuilder.DropTable(
                name: "LearningLevels");
        }
    }
}
