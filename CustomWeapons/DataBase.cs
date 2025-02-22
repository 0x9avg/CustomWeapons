using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace CustomWeapons
{
    public class Database(string dbConnectionString)
    {
        public async Task<MySqlConnection> GetConnectionAsync()
        {
            try
            {
                var connection = new MySqlConnection(dbConnectionString);
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                CustomWeapons.Instance.Logger.LogError($"Unable to connect to database: {ex.Message}");
                throw;
            }
        }
    }
}