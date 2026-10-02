using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusServiceMarketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class SpecGapFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LandlinePlanId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BilledOnBillId",
                table: "ConnectionProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReplacementChargeAmount",
                table: "ConnectionProducts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BilledPlanId",
                table: "Bills",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CallCharge",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LocalMinutes",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MobileMinutes",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlanValidUntil",
                table: "Bills",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousBalance",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StdMinutes",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LandlinePlanId",
                table: "Orders",
                column: "LandlinePlanId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_LandlinePlan_DialUp",
                table: "Orders",
                sql: "[LandlinePlanId] IS NULL OR [ConnectionType] = 'D'");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionProducts_BilledOnBillId",
                table: "ConnectionProducts",
                column: "BilledOnBillId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConnectionProducts_ReplacementCharge",
                table: "ConnectionProducts",
                sql: "[ReplacementChargeAmount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_BilledPlanId",
                table: "Bills",
                column: "BilledPlanId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bills_Usage_NonNegative",
                table: "Bills",
                sql: "[LocalMinutes] >= 0 AND [StdMinutes] >= 0 AND [MobileMinutes] >= 0 AND [CallCharge] >= 0 AND [PreviousBalance] >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Plans_BilledPlanId",
                table: "Bills",
                column: "BilledPlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectionProducts_Bills_BilledOnBillId",
                table: "ConnectionProducts",
                column: "BilledOnBillId",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Plans_LandlinePlanId",
                table: "Orders",
                column: "LandlinePlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // ---- Backfill rows created before this migration ----
            // Bills that charged the connection's current plan fee: record which plan and until when it is paid,
            // so the next bill does not charge the fee again inside the plan's validity period.
            migrationBuilder.Sql(@"
UPDATE b SET b.BilledPlanId = c.PlanId,
             b.PlanValidUntil = DATEADD(day, -1, DATEADD(month, CASE WHEN p.ValidityMonths < 1 THEN 1 ELSE p.ValidityMonths END, b.PeriodStart))
FROM Bills b
JOIN Connections c ON c.Id = b.ConnectionId
JOIN Plans p ON p.Id = c.PlanId
WHERE b.PlanCharge > 0 AND b.PlanCharge = p.Price;");

            // Replacement units issued before this migration: take the product's replacement charge. A unit is treated
            // as already billed when a later bill of the same connection carried a replacement charge.
            migrationBuilder.Sql(@"
UPDATE cp SET cp.ReplacementChargeAmount = pr.ReplacementCharge
FROM ConnectionProducts cp
JOIN Products pr ON pr.Id = cp.ProductId
WHERE cp.IsReplacement = 1;

UPDATE cp SET cp.BilledOnBillId = (
    SELECT TOP 1 b.Id FROM Bills b
    WHERE b.ConnectionId = cp.ConnectionId AND b.ReplacementCharge > 0 AND b.GeneratedAtUtc >= cp.IssuedAtUtc
    ORDER BY b.GeneratedAtUtc)
FROM ConnectionProducts cp
WHERE cp.IsReplacement = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Plans_BilledPlanId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectionProducts_Bills_BilledOnBillId",
                table: "ConnectionProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Plans_LandlinePlanId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_LandlinePlanId",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_LandlinePlan_DialUp",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_ConnectionProducts_BilledOnBillId",
                table: "ConnectionProducts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConnectionProducts_ReplacementCharge",
                table: "ConnectionProducts");

            migrationBuilder.DropIndex(
                name: "IX_Bills_BilledPlanId",
                table: "Bills");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bills_Usage_NonNegative",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "LandlinePlanId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BilledOnBillId",
                table: "ConnectionProducts");

            migrationBuilder.DropColumn(
                name: "ReplacementChargeAmount",
                table: "ConnectionProducts");

            migrationBuilder.DropColumn(
                name: "BilledPlanId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "CallCharge",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "LocalMinutes",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "MobileMinutes",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "PlanValidUntil",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "PreviousBalance",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "StdMinutes",
                table: "Bills");
        }
    }
}
