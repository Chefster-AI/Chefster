using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chefster.Migrations
{
    /// <inheritdoc />
    public partial class RestructureSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscribers",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "PaymentCreatedDate",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "ReceiptUrl",
                table: "Subscribers");

            migrationBuilder.RenameColumn(
                name: "FamilyId",
                table: "Subscribers",
                newName: "StartDate");

            migrationBuilder.AlterColumn<int>(
                name: "UserStatus",
                table: "Subscribers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionId",
                table: "Subscribers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "Subscribers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Subscribers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Subscribers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InvoiceUrl",
                table: "Subscribers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscribers",
                table: "Subscribers",
                column: "SubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Subscribers",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Subscribers");

            migrationBuilder.DropColumn(
                name: "InvoiceUrl",
                table: "Subscribers");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Subscribers",
                newName: "FamilyId");

            migrationBuilder.AlterColumn<int>(
                name: "UserStatus",
                table: "Subscribers",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerId",
                table: "Subscribers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionId",
                table: "Subscribers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "PaymentCreatedDate",
                table: "Subscribers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptUrl",
                table: "Subscribers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subscribers",
                table: "Subscribers",
                column: "FamilyId");
        }
    }
}
