using Chatbot.Service.Model.Sholat;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text;

namespace Chatbot.Service.Services.Sholat
{
    public class SholatService : ISholatService
    {
        private readonly string _connectionString;
        private readonly IConfiguration _config;

        public SholatService(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("PostgreSqlConnection") ?? throw new ArgumentNullException("Connection string 'PostgreSqlConnection' not found.");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<string?> ExtractKotaNameDapper(string prompt)
        {
            using var conn = GetConnection();

            var sqlAll = @"SELECT nama FROM ref.kota WHERE is_aktif = true;";
            var allKota = (await conn.QueryAsync<string>(sqlAll)).ToList();

            if (!allKota.Any())
                return null;

            var matchedKota = allKota
                .Where(kota => prompt.Contains(kota, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedKota.Any())
            {
                return matchedKota.First();
            }

            var words = prompt
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var word in words)
            {
                var match = allKota.FirstOrDefault(k =>
                    k.Contains(word, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                    return match;
            }

            return null;
        }

        public async Task<IEnumerable<JadwalSholatModel>> GetJadwalSholatByKotaName(string kotaName, bool isCurrentMonth = true)
        {
            using var conn = GetConnection();

            var sql = @"
                SELECT
                    p.nama as propinsi,
                    k.nama as kota,
                    js.tanggal,
                    js.bulan,
                    js.imsak,
                    js.subuh,
                    js.terbit,
                    js.duha,
                    js.zuhur,
                    js.asar,
                    js.magrib,
                    js.isya
                FROM ref.jadwal_shalat js
                INNER JOIN ref.kota k ON k.kota_id = js.kota_id
                INNER JOIN ref.propinsi p ON p.propinsi_id = k.propinsi_id
                WHERE k.nama = @kotaName
            ";

            //TODO -- Get 30 Next 30 Prevs
            if (isCurrentMonth)
            {
                sql += " AND js.bulan = @currentMonth";
            }

            sql += " ORDER BY js.bulan, js.tanggal ASC;";


            return await conn.QueryAsync<JadwalSholatModel>(sql, new
            {
                kotaName,
                currentMonth = DateTime.Now.Month
            });
        }

        public async Task<string> GetJadwalSholatByKotaNameAsCsv(string kotaName, bool isCurrentMonth = true)
        {
            var jadwals = await GetJadwalSholatByKotaName(kotaName, isCurrentMonth);

            if (!jadwals.Any())
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("Propinsi,Kota,Tanggal,Bulan,Imsak,Subuh,Terbit,Duha,Zuhur,Asar,Magrib,Isya");

            foreach (var j in jadwals)
            {
                sb.AppendLine($"{j.propinsi},{j.kota},{j.tanggal},{j.bulan},{j.imsak},{j.subuh},{j.terbit},{j.duha},{j.zuhur},{j.asar},{j.magrib},{j.isya}");
            }

            return sb.ToString();
        }

    }
}
