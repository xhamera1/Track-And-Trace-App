using _10.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace _10.Services
{
    public class DataSeederService : IDataSeederService
    {
        private readonly MySqlSettings _mySqlSettings;
        private readonly ILogger<DataSeederService> _logger;
        private readonly string _schemaFilePath;

        public DataSeederService(IOptions<MySqlSettings> mySqlSettings, ILogger<DataSeederService> logger)
        {
            _mySqlSettings = mySqlSettings.Value;
            _logger = logger;
            _schemaFilePath = Path.Combine(AppContext.BaseDirectory, "../../..", "track_and_trace_mysql_schema.sql");
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Attempting to initialize database by executing SQL script...");

            try
            {
                if (!File.Exists(_schemaFilePath))
                {
                    _logger.LogError("SQL schema file not found at {Path}", _schemaFilePath);
                    return;
                }

                string sqlScript = await File.ReadAllTextAsync(_schemaFilePath);

                if (string.IsNullOrWhiteSpace(sqlScript))
                {
                    _logger.LogWarning("SQL schema file is empty.");
                    return;
                }

                await using var connection = new MySqlConnection(_mySqlSettings.ConnectionString);
                await connection.OpenAsync();
                _logger.LogInformation("Successfully connected to MySQL database: {Database}", _mySqlSettings.Database);

                var commands = sqlScript.Split([';'], StringSplitOptions.RemoveEmptyEntries);

                foreach (var commandText in commands)
                {
                    if (string.IsNullOrWhiteSpace(commandText))
                        continue;

                    var trimmedCommand = commandText.Trim();
                    if (trimmedCommand.StartsWith("USE", StringComparison.OrdinalIgnoreCase) ||
                        trimmedCommand.StartsWith("--") ||
                        trimmedCommand.StartsWith("/*"))
                    {
                        _logger.LogInformation("Skipping command: {Command}", trimmedCommand.Split('\n')[0]);
                        continue;
                    }

                    _logger.LogInformation("Executing SQL command: {Command}...", trimmedCommand.Split('\n')[0]);
                    try
                    {
                        await using var command = new MySqlCommand(trimmedCommand, connection);
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (MySqlException ex) when (ex.Number == 1061) // Error code for 'Duplicate key name'
                    {
                        _logger.LogWarning("Skipping duplicate index creation: {Command} - Error: {ErrorMessage}", trimmedCommand.Split('\n')[0], ex.Message);
                    }
                    catch (MySqlException ex) when (ex.Number == 1050) // Error code for 'Table already exists'
                    {
                        _logger.LogWarning("Skipping table creation as it already exists: {Command} - Error: {ErrorMessage}", trimmedCommand.Split('\n')[0], ex.Message);
                    }
                    // Możesz dodać więcej obsługi specyficznych błędów MySQL, jeśli są potrzebne
                }

                _logger.LogInformation("SQL script executed successfully.");
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "A MySQL error occurred while executing the SQL script.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "An error occurred while reading the SQL schema file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during database initialization from SQL script.");
            }
        }
    }
}
