using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MorWalPiz.VideoImporter.Data;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace MorWalPiz.VideoImporter.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly ITenantContext _tenantContext;

        public DatabaseService(ITenantContext tenantContext)
        {
            _dbPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "VideoImporter.db"));
            _tenantContext = tenantContext;
        }

        public string DatabasePath => _dbPath;

        public void InitializeDatabase()
        {
            using var context = new AppDbContext(_tenantContext, _dbPath);
            var stage = DatabaseStartupStage.MigrationHistory;
            try
            {
                var knownMigrations = context.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
                var unknownMigrations = context.Database.GetAppliedMigrations()
                    .Where(migration => !knownMigrations.Contains(migration))
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                if (unknownMigrations.Length > 0)
                    throw new DatabaseStartupException(stage, unknownMigrations);

                stage = DatabaseStartupStage.Migration;
                context.Database.Migrate();

                stage = DatabaseStartupStage.MigrationVerification;
                var pendingMigrations = context.Database.GetPendingMigrations()
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                if (pendingMigrations.Length > 0)
                    throw new DatabaseStartupException(stage, pendingMigrations);

                stage = DatabaseStartupStage.SchemaVerification;
                VerifyPhysicalSchema(context);
            }
            catch (DatabaseStartupException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new DatabaseStartupException(stage, [], exception);
            }
        }

        /// <summary>
        /// Crea un nuovo contesto da utilizzare all'interno di un blocco using.
        /// Questo garantisce che ogni operazione abbia un proprio contesto isolato.
        /// </summary>
        public AppDbContext CreateContext()
        {
            return new AppDbContext(_tenantContext, _dbPath);
        }

        private static void VerifyPhysicalSchema(AppDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var closeConnection = connection.State != ConnectionState.Open;
            if (closeConnection)
                connection.Open();

            try
            {
                var relationalModel = context.Model.GetRelationalModel();
                foreach (var table in relationalModel.Tables.Where(table => !table.IsExcludedFromMigrations))
                    VerifyTable(connection, table);
            }
            finally
            {
                if (closeConnection)
                    connection.Close();
            }
        }

        private static void VerifyTable(System.Data.Common.DbConnection connection, ITable expectedTable)
        {
            var details = new List<string>();
            if (!IsPhysicalTable(connection, expectedTable.Name))
                throw new DatabaseStartupException(
                    DatabaseStartupStage.SchemaVerification,
                    [$"{expectedTable.Name} (table missing)"]);

            var columns = ReadColumns(connection, expectedTable.Name);

            foreach (var expectedColumn in expectedTable.Columns)
            {
                if (!columns.TryGetValue(expectedColumn.Name, out var actualColumn))
                {
                    details.Add($"{expectedTable.Name}.{expectedColumn.Name} (column missing)");
                    continue;
                }

                if (GetSqliteAffinity(expectedColumn.StoreType) != GetSqliteAffinity(actualColumn.StoreType))
                    details.Add($"{expectedTable.Name}.{expectedColumn.Name} (type mismatch)");

                var isPrimaryKeyColumn = expectedTable.PrimaryKey?.Columns.Contains(expectedColumn) == true;
                if (!isPrimaryKeyColumn && expectedColumn.IsNullable == actualColumn.IsNotNull)
                    details.Add($"{expectedTable.Name}.{expectedColumn.Name} (nullability mismatch)");
            }

            var expectedPrimaryKey = expectedTable.PrimaryKey?.Columns.Select(column => column.Name).ToArray() ?? [];
            var actualPrimaryKey = columns.Values
                .Where(column => column.PrimaryKeyOrdinal > 0)
                .OrderBy(column => column.PrimaryKeyOrdinal)
                .Select(column => column.Name)
                .ToArray();
            if (!expectedPrimaryKey.SequenceEqual(actualPrimaryKey, StringComparer.OrdinalIgnoreCase))
                details.Add($"{expectedTable.Name} (primary key mismatch)");

            VerifyIndexes(connection, expectedTable, details);
            VerifyForeignKeys(connection, expectedTable, details);

            if (details.Count > 0)
                throw new DatabaseStartupException(DatabaseStartupStage.SchemaVerification, details);
        }

        private static bool IsPhysicalTable(System.Data.Common.DbConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name COLLATE NOCASE";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);
            return command.ExecuteScalar() is not null;
        }

        private static Dictionary<string, PhysicalColumn> ReadColumns(
            System.Data.Common.DbConnection connection,
            string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)})";
            using var reader = command.ExecuteReader();
            var columns = new Dictionary<string, PhysicalColumn>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                var name = reader.GetString(1);
                columns[name] = new PhysicalColumn(
                    name,
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.GetInt64(3) != 0,
                    reader.GetInt64(5));
            }

            return columns;
        }

        private static void VerifyIndexes(
            System.Data.Common.DbConnection connection,
            ITable table,
            ICollection<string> details)
        {
            var actualIndexes = ReadIndexes(connection, table.Name);

            foreach (var expectedIndex in table.Indexes)
            {
                var actualIndex = actualIndexes.FirstOrDefault(index =>
                    string.Equals(index.Name, expectedIndex.Name, StringComparison.OrdinalIgnoreCase));
                if (actualIndex is null)
                {
                    details.Add($"{table.Name}.{expectedIndex.Name} (index missing)");
                    continue;
                }

                var expectedColumns = expectedIndex.Columns.Select(column => column.Name).ToArray();
                var expectedDescending = expectedIndex.IsDescending?.ToArray();
                if (expectedDescending is null || expectedDescending.Length == 0)
                    expectedDescending = expectedColumns.Select(_ => false).ToArray();
                if (expectedIndex.IsUnique != actualIndex.IsUnique ||
                    !expectedColumns.SequenceEqual(actualIndex.Columns, StringComparer.OrdinalIgnoreCase) ||
                    !expectedDescending.SequenceEqual(actualIndex.IsDescending))
                {
                    details.Add($"{table.Name}.{expectedIndex.Name} (index definition mismatch)");
                    continue;
                }

                var expectedFilter = NormalizeSqlExpression(expectedIndex.Filter);
                var actualFilter = NormalizeSqlExpression(actualIndex.Filter);
                if ((expectedIndex.Filter is not null) != actualIndex.IsPartial ||
                    !string.Equals(expectedFilter, actualFilter, StringComparison.Ordinal))
                    details.Add($"{table.Name}.{expectedIndex.Name} (index filter mismatch)");
            }

            foreach (var uniqueConstraint in table.UniqueConstraints.Where(constraint => constraint != table.PrimaryKey))
            {
                var expectedColumns = uniqueConstraint.Columns.Select(column => column.Name).ToArray();
                var exists = actualIndexes.Any(index =>
                    index.IsUnique &&
                    index.Filter is null &&
                    expectedColumns.SequenceEqual(index.Columns, StringComparer.OrdinalIgnoreCase));
                if (!exists)
                    details.Add($"{table.Name} (unique constraint missing or incompatible)");
            }
        }

        private static List<PhysicalIndex> ReadIndexes(
            System.Data.Common.DbConnection connection,
            string tableName)
        {
            var indexDefinitions = new List<(string Name, bool IsUnique, bool IsPartial)>();
            using var listCommand = connection.CreateCommand();
            listCommand.CommandText = $"PRAGMA index_list({QuoteIdentifier(tableName)})";
            using (var listReader = listCommand.ExecuteReader())
            {
                while (listReader.Read())
                    indexDefinitions.Add((
                        listReader.GetString(1),
                        listReader.GetInt64(2) != 0,
                        listReader.GetInt64(4) != 0));
            }

            var indexes = new List<PhysicalIndex>();
            foreach (var (name, unique, partial) in indexDefinitions)
            {
                var (columns, descending) = ReadIndexColumns(connection, name);
                indexes.Add(new PhysicalIndex(
                    name,
                    unique,
                    columns,
                    descending,
                    partial,
                    partial ? ReadIndexFilter(connection, name) : null));
            }

            return indexes;
        }

        private static (string[] Columns, bool[] Descending) ReadIndexColumns(
            System.Data.Common.DbConnection connection,
            string indexName)
        {
            var columns = new List<(int Order, string Name, bool Descending)>();
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA index_xinfo({QuoteIdentifier(indexName)})";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetInt64(5) == 0 || reader.IsDBNull(2))
                    continue;

                columns.Add((reader.GetInt32(0), reader.GetString(2), reader.GetInt64(3) != 0));
            }

            var ordered = columns.OrderBy(column => column.Order).ToArray();
            return (ordered.Select(column => column.Name).ToArray(), ordered.Select(column => column.Descending).ToArray());
        }

        private static string? ReadIndexFilter(System.Data.Common.DbConnection connection, string indexName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = $name";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$name";
            parameter.Value = indexName;
            command.Parameters.Add(parameter);
            var sql = command.ExecuteScalar() as string;
            if (sql is null)
                return null;

            var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
            return whereIndex < 0 ? null : sql[(whereIndex + "WHERE".Length)..];
        }

        private static void VerifyForeignKeys(
            System.Data.Common.DbConnection connection,
            ITable expectedTable,
            ICollection<string> details)
        {
            var expected = expectedTable.ForeignKeyConstraints
                .Select(constraint => new ForeignKeyShape(
                    constraint.PrincipalTable.Name,
                    constraint.Columns.Select(column => column.Name).ToArray(),
                    constraint.PrincipalColumns.Select(column => column.Name).ToArray(),
                    ToSqliteAction(constraint.OnDeleteAction.ToString())))
                .ToArray();
            var actual = ReadForeignKeys(connection, expectedTable.Name);

            foreach (var expectedKey in expected)
            {
                var matchingIndex = actual.FindIndex(actualKey => ForeignKeysEqual(expectedKey, actualKey));
                if (matchingIndex < 0)
                    details.Add($"{expectedTable.Name} (foreign key to {expectedKey.PrincipalTable} missing or incompatible)");
                else
                    actual.RemoveAt(matchingIndex);
            }

            if (actual.Count > 0)
                details.Add($"{expectedTable.Name} (unexpected foreign key)");
        }

        private static bool ForeignKeysEqual(ForeignKeyShape expected, ForeignKeyShape actual) =>
            string.Equals(expected.PrincipalTable, actual.PrincipalTable, StringComparison.OrdinalIgnoreCase) &&
            expected.Columns.SequenceEqual(actual.Columns, StringComparer.OrdinalIgnoreCase) &&
            expected.PrincipalColumns.SequenceEqual(actual.PrincipalColumns, StringComparer.OrdinalIgnoreCase) &&
            string.Equals(expected.OnDelete, actual.OnDelete, StringComparison.OrdinalIgnoreCase);

        private static List<ForeignKeyShape> ReadForeignKeys(
            System.Data.Common.DbConnection connection,
            string tableName)
        {
            var keys = new List<ForeignKeyShape>();
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA foreign_key_list({QuoteIdentifier(tableName)})";
            using var reader = command.ExecuteReader();
            var rows = new List<(long Id, long Sequence, string PrincipalTable, string Column, string PrincipalColumn, string OnDelete)>();
            while (reader.Read())
            {
                rows.Add((
                    reader.GetInt64(0),
                    reader.GetInt64(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(6)));
            }

            foreach (var group in rows.GroupBy(row => row.Id))
            {
                var ordered = group.OrderBy(row => row.Sequence).ToArray();
                keys.Add(new ForeignKeyShape(
                    ordered[0].PrincipalTable,
                    ordered.Select(row => row.Column).ToArray(),
                    ordered.Select(row => row.PrincipalColumn).ToArray(),
                    ordered[0].OnDelete));
            }

            return keys;
        }

        private static string ToSqliteAction(string action) => action switch
        {
            "NoAction" => "NO ACTION",
            "Restrict" => "RESTRICT",
            "Cascade" => "CASCADE",
            "SetNull" => "SET NULL",
            "SetDefault" => "SET DEFAULT",
            _ => action.ToUpperInvariant()
        };

        private static string GetSqliteAffinity(string storeType)
        {
            var type = storeType.ToUpperInvariant();
            if (type.Contains("INT", StringComparison.Ordinal))
                return "INTEGER";
            if (type.Contains("CHAR", StringComparison.Ordinal) ||
                type.Contains("CLOB", StringComparison.Ordinal) ||
                type.Contains("TEXT", StringComparison.Ordinal))
                return "TEXT";
            if (type.Length == 0 ||
                type.Contains("BLOB", StringComparison.Ordinal))
                return "BLOB";
            if (type.Contains("REAL", StringComparison.Ordinal) ||
                type.Contains("FLOA", StringComparison.Ordinal) ||
                type.Contains("DOUB", StringComparison.Ordinal))
                return "REAL";
            return "NUMERIC";
        }

        private static string NormalizeSqlExpression(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return string.Empty;

            return Regex.Replace(expression, "[\\s\\[\\]`\"()]", string.Empty)
                .ToUpperInvariant();
        }

        private static string QuoteIdentifier(string identifier) =>
            $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

        private sealed record PhysicalColumn(string Name, string StoreType, bool IsNotNull, long PrimaryKeyOrdinal);
        private sealed record PhysicalIndex(
            string Name,
            bool IsUnique,
            string[] Columns,
            bool[] IsDescending,
            bool IsPartial,
            string? Filter);
        private sealed record ForeignKeyShape(
            string PrincipalTable,
            string[] Columns,
            string[] PrincipalColumns,
            string OnDelete);
    }

    internal enum DatabaseStartupStage
    {
        MigrationHistory,
        Migration,
        MigrationVerification,
        SchemaVerification
    }

    internal sealed class DatabaseStartupException(
        DatabaseStartupStage stage,
        IReadOnlyList<string> details,
        Exception? innerException = null)
        : Exception($"Database startup failed during {stage}.", innerException)
    {
        public DatabaseStartupStage Stage { get; } = stage;
        public IReadOnlyList<string> Details { get; } = details;
    }
}
