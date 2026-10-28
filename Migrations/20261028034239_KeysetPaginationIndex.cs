using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonationApi.Migrations
{
    /// <inheritdoc />
    public partial class KeysetPaginationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donations_CreatedAt",
                table: "Donations");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_CreatedAt_Id",
                table: "Donations",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0])
                .Annotation("SqlServer:Include", new[] { "Amount", "DonorName", "IsAnonymous" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Donations_CreatedAt_Id",
                table: "Donations");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_CreatedAt",
                table: "Donations",
                column: "CreatedAt");
        }
    }
}
