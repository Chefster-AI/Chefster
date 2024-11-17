using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chefster.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriberModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscribers",
                columns: table => new
                {
                    FamilyId = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: true),
                    SubscriptionId = table.Column<string>(type: "TEXT", nullable: true),
                    UserStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    PaymentCreatedDate = table.Column<string>(type: "TEXT", nullable: true),
                    ReceiptUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscribers", x => x.FamilyId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscribers");
        }
    }
}
