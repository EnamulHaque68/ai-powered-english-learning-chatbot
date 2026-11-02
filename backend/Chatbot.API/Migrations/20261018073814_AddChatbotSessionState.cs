using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chatbot.API.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotSessionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveMode",
                table: "ConversationSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Learning");

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveCorrectCount",
                table: "ConversationSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentTopic",
                table: "ConversationSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "Colors");

            migrationBuilder.AddColumn<string>(
                name: "RecentContextJson",
                table: "ConversationSessions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StarsEarned",
                table: "ConversationSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveMode",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "ConsecutiveCorrectCount",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "CurrentTopic",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "RecentContextJson",
                table: "ConversationSessions");

            migrationBuilder.DropColumn(
                name: "StarsEarned",
                table: "ConversationSessions");
        }
    }
}
