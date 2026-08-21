using System;
using System.Collections.Generic;
using System.IO;
using ClipboardHistoryManager.Models;
using Microsoft.Data.Sqlite;

namespace ClipboardHistoryManager.Services;

/// <summary>
/// Persists clipboard history to a local SQLite database under %AppData%.
/// </summary>
public class HistoryStore
{
    private readonly string _connectionString;
    private const int MaxUnpinnedEntries = 200;

    public HistoryStore()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipboardHistoryManager");
        Directory.CreateDirectory(appDataDir);
        var dbPath = Path.Combine(appDataDir, "history.db");
        _connectionString = $"Data Source={dbPath}";

        Initialize();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void Initialize()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ClipboardEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Type INTEGER NOT NULL,
                TextContent TEXT NULL,
                ImageData BLOB NULL,
                CreatedAt TEXT NOT NULL,
                IsPinned INTEGER NOT NULL DEFAULT 0
            );
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Returns the most recent entry's preview text, used to skip consecutive duplicates.</summary>
    public string? GetMostRecentPreview()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Type, TextContent, ImageData
            FROM ClipboardEntries
            ORDER BY Id DESC
            LIMIT 1;
            """;
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var type = (ClipboardEntryType)reader.GetInt32(0);
        if (type == ClipboardEntryType.Text)
            return "T:" + reader.GetString(1);

        var bytes = (byte[])reader["ImageData"];
        return "I:" + bytes.Length + ":" + Convert.ToBase64String(bytes[..Math.Min(64, bytes.Length)]);
    }

    public long AddEntry(ClipboardEntry entry)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClipboardEntries (Type, TextContent, ImageData, CreatedAt, IsPinned)
            VALUES ($type, $text, $image, $createdAt, 0);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$type", (int)entry.Type);
        cmd.Parameters.AddWithValue("$text", (object?)entry.TextContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$image", (object?)entry.ImageData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("O"));
        var id = (long)cmd.ExecuteScalar()!;

        TrimOldEntries(connection);
        return id;
    }

    private static void TrimOldEntries(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM ClipboardEntries
            WHERE IsPinned = 0 AND Id NOT IN (
                SELECT Id FROM ClipboardEntries
                WHERE IsPinned = 0
                ORDER BY Id DESC
                LIMIT $max
            );
            """;
        cmd.Parameters.AddWithValue("$max", MaxUnpinnedEntries);
        cmd.ExecuteNonQuery();
    }

    public List<ClipboardEntry> GetAll()
    {
        var result = new List<ClipboardEntry>();
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Type, TextContent, ImageData, CreatedAt, IsPinned
            FROM ClipboardEntries
            ORDER BY IsPinned DESC, Id DESC;
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new ClipboardEntry
            {
                Id = reader.GetInt64(0),
                Type = (ClipboardEntryType)reader.GetInt32(1),
                TextContent = reader.IsDBNull(2) ? null : reader.GetString(2),
                ImageData = reader.IsDBNull(3) ? null : (byte[])reader["ImageData"],
                CreatedAt = DateTime.Parse(reader.GetString(4)),
                IsPinned = reader.GetInt32(5) != 0
            });
        }
        return result;
    }

    public void TogglePin(long id)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE ClipboardEntries SET IsPinned = 1 - IsPinned WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ClipboardEntries WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Clears all non-pinned entries.</summary>
    public void ClearUnpinned()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ClipboardEntries WHERE IsPinned = 0;";
        cmd.ExecuteNonQuery();
    }
}
