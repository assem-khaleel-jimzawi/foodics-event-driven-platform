using FoodFlow.Payments.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodFlow.Payments.Infrastructure.Migrations
{
    [DbContext(typeof(PaymentsDbContext))]
    [Migration("20260905220000_OutboxTraceParent")]
    public partial class OutboxTraceParent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                schema: "payments",
                table: "outbox_messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceParent",
                schema: "payments",
                table: "outbox_messages");
        }
    }
}
