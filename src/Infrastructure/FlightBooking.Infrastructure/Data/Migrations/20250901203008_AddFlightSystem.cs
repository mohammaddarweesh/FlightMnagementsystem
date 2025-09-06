using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IataCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IcaoCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(10,7)", precision: 10, scale: 7, nullable: false),
                    Elevation = table.Column<int>(type: "integer", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Amenities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    IconName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartureAirportId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArrivalAirportId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Distance = table.Column<int>(type: "integer", nullable: false),
                    EstimatedFlightTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsInternational = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_Airports_ArrivalAirportId",
                        column: x => x.ArrivalAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Routes_Airports_DepartureAirportId",
                        column: x => x.DepartureAirportId,
                        principalTable: "Airports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlightNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    AirlineCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AirlineName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AircraftType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DepartureTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ArrivalTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Gate = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Terminal = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flights_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FareClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlightId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ClassCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FareClasses_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FareClassAmenities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FareClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    AdditionalCost = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FareClassAmenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FareClassAmenities_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FareClassAmenities_FareClasses_FareClassId",
                        column: x => x.FareClassId,
                        principalTable: "FareClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlightId = table.Column<Guid>(type: "uuid", nullable: false),
                    FareClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatNumber = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Row = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Column = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExtraFee = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seats_FareClasses_FareClassId",
                        column: x => x.FareClassId,
                        principalTable: "FareClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Seats_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 20, 30, 8, 107, DateTimeKind.Utc).AddTicks(6525));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 20, 30, 8, 107, DateTimeKind.Utc).AddTicks(6531));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 1, 20, 30, 8, 107, DateTimeKind.Utc).AddTicks(6534));

            migrationBuilder.CreateIndex(
                name: "IX_Airports_City_Country",
                table: "Airports",
                columns: new[] { "City", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Airports_CountryCode",
                table: "Airports",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Airports_IataCode",
                table: "Airports",
                column: "IataCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airports_IcaoCode",
                table: "Airports",
                column: "IcaoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Airports_IsActive",
                table: "Airports",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_Category",
                table: "Amenities",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_IsActive",
                table: "Amenities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_Name",
                table: "Amenities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Amenities_SortOrder",
                table: "Amenities",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_FareClassAmenities_AmenityId",
                table: "FareClassAmenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_FareClassAmenities_FareClass_Amenity",
                table: "FareClassAmenities",
                columns: new[] { "FareClassId", "AmenityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FareClassAmenities_FareClassId",
                table: "FareClassAmenities",
                column: "FareClassId");

            migrationBuilder.CreateIndex(
                name: "IX_FareClassAmenities_IsIncluded",
                table: "FareClassAmenities",
                column: "IsIncluded");

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_ClassCode",
                table: "FareClasses",
                column: "ClassCode");

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_Flight_ClassName",
                table: "FareClasses",
                columns: new[] { "FlightId", "ClassName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_Flight_SortOrder",
                table: "FareClasses",
                columns: new[] { "FlightId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_FlightId",
                table: "FareClasses",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_IsActive",
                table: "FareClasses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FareClasses_SortOrder",
                table: "FareClasses",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_AirlineCode",
                table: "Flights",
                column: "AirlineCode");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DepartureDate",
                table: "Flights",
                column: "DepartureDate");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_FlightNumber",
                table: "Flights",
                column: "FlightNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_IsActive",
                table: "Flights",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Route_Date",
                table: "Flights",
                columns: new[] { "RouteId", "DepartureDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_RouteId",
                table: "Flights",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Status",
                table: "Flights",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Unique_Flight",
                table: "Flights",
                columns: new[] { "AirlineCode", "FlightNumber", "DepartureDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_ArrivalAirportId",
                table: "Routes",
                column: "ArrivalAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DepartureAirportId",
                table: "Routes",
                column: "DepartureAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DepartureArrival",
                table: "Routes",
                columns: new[] { "DepartureAirportId", "ArrivalAirportId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_IsActive",
                table: "Routes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_IsInternational",
                table: "Routes",
                column: "IsInternational");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_RouteCode",
                table: "Routes",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seats_FareClass_Row_Column",
                table: "Seats",
                columns: new[] { "FareClassId", "Row", "Column" });

            migrationBuilder.CreateIndex(
                name: "IX_Seats_FareClassId",
                table: "Seats",
                column: "FareClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Flight_SeatNumber",
                table: "Seats",
                columns: new[] { "FlightId", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seats_FlightId",
                table: "Seats",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_IsActive",
                table: "Seats",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Status",
                table: "Seats",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Type",
                table: "Seats",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FareClassAmenities");

            migrationBuilder.DropTable(
                name: "Seats");

            migrationBuilder.DropTable(
                name: "Amenities");

            migrationBuilder.DropTable(
                name: "FareClasses");

            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Airports");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 31, 16, 39, 57, 859, DateTimeKind.Utc).AddTicks(8184));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 31, 16, 39, 57, 859, DateTimeKind.Utc).AddTicks(8187));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 8, 31, 16, 39, 57, 859, DateTimeKind.Utc).AddTicks(8189));
        }
    }
}
