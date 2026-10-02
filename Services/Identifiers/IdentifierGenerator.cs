using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusServiceMarketingSystem.Data;
using NexusServiceMarketingSystem.Models.Common;
using NexusServiceMarketingSystem.Models.Entities;
using NexusServiceMarketingSystem.Models.Enums;

namespace NexusServiceMarketingSystem.Services.Identifiers
{
    /// <summary>
    /// SQL Server implementation of <see cref="IIdentifierGenerator"/>.
    ///
    /// HOW IT STAYS SAFE UNDER CONCURRENCY
    /// One row per counter "scope" lives in the IdentifierCounters table. A single MERGE ... WITH (HOLDLOCK)
    /// statement either increments the existing row or inserts it with value 1, and returns the new value.
    /// SQL Server holds a lock on that row until the surrounding transaction ends, so simultaneous
    /// requests for the same scope are queued and each one receives a different number.
    ///
    /// SCOPES (one independent serial sequence each)
    ///   Order numbers : "ORDER:D", "ORDER:B", "ORDER:T"         (one sequence per connection type)
    ///   Account IDs   : "ACCOUNT:D001", "ACCOUNT:B004", ...      (one sequence per type + city code)
    /// To change the serial scope (for example one global account sequence), change only the two
    /// scope methods below.
    /// </summary>
    public class IdentifierGenerator : IIdentifierGenerator
    {
        // Atomic "increment or create" statement. Parameter: @scope. Returns the new counter value.
        private static readonly string NextValueSql =
            $"MERGE [dbo].[{IdentifierCounter.TableName}] WITH (HOLDLOCK) AS [target] " +
            "USING (SELECT @scope AS [Scope]) AS [source] ON [target].[Scope] = [source].[Scope] " +
            "WHEN MATCHED THEN UPDATE SET [LastValue] = [target].[LastValue] + 1 " +
            "WHEN NOT MATCHED THEN INSERT ([Scope], [LastValue]) VALUES ([source].[Scope], 1) " +
            "OUTPUT inserted.[LastValue];";

        private readonly AppDbContext _db;

        public IdentifierGenerator(AppDbContext db)
        {
            _db = db;
        }

        /// <inheritdoc />
        public async Task<string> GenerateOrderNumberAsync(ConnectionType type, CancellationToken cancellationToken = default)
        {
            long serial = await NextSerialAsync(OrderScope(type), cancellationToken);

            // FormatOrderNumber throws if the ten-digit serial is exhausted, which aborts the transaction.
            return IdentifierFormat.FormatOrderNumber(type, serial);
        }

        /// <inheritdoc />
        public async Task<string> GenerateAccountIdAsync(ConnectionType type, string cityCode, CancellationToken cancellationToken = default)
        {
            // Validate before touching the counter, so a bad city code never consumes a serial.
            if (!IdentifierFormat.IsValidCityCode(cityCode))
            {
                throw new ArgumentException($"City code must be exactly {IdentifierFormat.CityCodeLength} digits.", nameof(cityCode));
            }

            long serial = await NextSerialAsync(AccountScope(type, cityCode), cancellationToken);
            return IdentifierFormat.FormatAccountId(type, cityCode, serial);
        }

        /// <inheritdoc />
        public async Task AssignOrderNumberAsync(Order order, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(order);

            string orderNumber = await GenerateOrderNumberAsync(order.ConnectionType, cancellationToken);
            order.AssignOrderNumber(orderNumber);
        }

        /// <inheritdoc />
        public async Task AssignAccountIdAsync(Connection connection, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(connection);

            // The city code always comes from the database, never from user input.
            City? city = await _db.Cities.FindAsync(new object[] { connection.CityId }, cancellationToken);
            if (city is null)
            {
                throw new InvalidOperationException($"City {connection.CityId} does not exist, so an account ID cannot be created.");
            }

            string accountId = await GenerateAccountIdAsync(connection.ConnectionType, city.Code, cancellationToken);
            connection.AssignAccountId(accountId, city.Code);
        }

        // ---------------------------------------------------------------- scopes
        private static string OrderScope(ConnectionType type) => "ORDER:" + type.ToCode();

        private static string AccountScope(ConnectionType type, string cityCode) => "ACCOUNT:" + type.ToCode() + cityCode;

        // ---------------------------------------------------------------- counter access
        /// <summary>
        /// Runs the atomic MERGE for one scope and returns the new serial. Uses the DbContext's own
        /// connection, and joins its current transaction if one is open.
        /// </summary>
        private async Task<long> NextSerialAsync(string scope, CancellationToken cancellationToken)
        {
            var database = _db.Database;

            // EF opens the connection if it is closed and keeps a count, so this is safe whether or not
            // the caller already has a transaction / open connection.
            await database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using DbCommand command = database.GetDbConnection().CreateCommand();
                command.CommandText = NextValueSql;
                command.CommandType = CommandType.Text;

                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = "@scope";
                parameter.DbType = DbType.AnsiString;
                parameter.Size = 40;
                parameter.Value = scope;
                command.Parameters.Add(parameter);

                // Join the caller's transaction so the counter and the new row commit or roll back together.
                IDbContextTransaction? current = database.CurrentTransaction;
                if (current is not null)
                {
                    command.Transaction = current.GetDbTransaction();
                }

                object? result = await command.ExecuteScalarAsync(cancellationToken);
                if (result is null || result is DBNull)
                {
                    throw new InvalidOperationException($"The identifier counter '{scope}' did not return a value.");
                }

                return Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }
            finally
            {
                await database.CloseConnectionAsync();
            }
        }
    }
}
