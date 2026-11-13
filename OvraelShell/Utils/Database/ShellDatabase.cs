using Microsoft.Data.Sqlite;

namespace OvraelShell.Utils.Database;

/// <summary>
/// The SQLite database of the shell, in $XDG_DATA_HOME/ovraelshell/ovraelshell.db
/// (~/.local/share/ovraelshell/ovraelshell.db if XDG_DATA_HOME is not set).
/// The schema version is in PRAGMA user_version - see <see cref="Migrations"/>.
/// </summary>
public sealed class ShellDatabase : IDisposable
{
    /// <summary>
    /// The changes of the schema, in order. Item N changes the schema from version N to N + 1.
    /// Only add new items at the end - do not change an item that a database already has.
    /// </summary>
    private static readonly string[] Migrations =
    [
        // Example:
        // """
        // CREATE TABLE example (
        //     id   INTEGER PRIMARY KEY,
        //     name TEXT NOT NULL
        // );
        // """,
    ];

    private readonly string connectionString;

    public string FilePath { get; }

    /// <summary>The schema version after <see cref="Migrations"/>.</summary>
    public static int SchemaVersion => Migrations.Length;

    /// <summary>Creates the file if it does not exist and updates the schema.</summary>
    public ShellDatabase()
        : this(GetDefaultFilePath()) { }

    /// <summary>Creates the file if it does not exist and updates the schema.</summary>
    public ShellDatabase(string filePath)
    {
        FilePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
        }.ToString();

        Initialize();
        Console.WriteLine($"Database: {FilePath} (schema version {SchemaVersion})");
    }

    /// <summary>
    /// Opens a new connection with foreign keys on. Dispose it after use -
    /// the pool keeps the real connection open, so this is fast.
    /// </summary>
    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys = ON;");
        return connection;
    }

    /// <summary>Closes the pooled connections - SQLite then removes the -wal and -shm files.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
    }

    private void Initialize()
    {
        using var connection = Open();

        // WAL: a read does not wait for a write. The mode stays in the file.
        Execute(connection, "PRAGMA journal_mode = WAL;");

        var version = Convert.ToInt32(Scalar(connection, "PRAGMA user_version;"));
        if (version > SchemaVersion)
            throw new InvalidOperationException(
                $"Database {FilePath} has schema version {version}, this build knows only up to {SchemaVersion}."
            );

        if (version == SchemaVersion)
            return;

        // All migrations in one transaction - a failure leaves the database as it was
        using var transaction = connection.BeginTransaction();
        for (var i = version; i < Migrations.Length; i++)
            Execute(connection, Migrations[i], transaction);

        // PRAGMA does not accept parameters
        Execute(connection, $"PRAGMA user_version = {SchemaVersion};", transaction);
        transaction.Commit();

        Console.WriteLine($"Database: schema updated from version {version} to {SchemaVersion}");
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.ExecuteNonQuery();
    }

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    private static string GetDefaultFilePath()
    {
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(dataHome))
            dataHome = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local",
                "share"
            );

        return Path.Combine(dataHome, "ovraelshell", "ovraelshell.db");
    }
}
