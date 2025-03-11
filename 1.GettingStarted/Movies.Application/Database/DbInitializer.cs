using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Database
{
    public class DbInitializer
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DbInitializer(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task InitializeAsync()
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS movies2 (
                    id UUID PRIMARY KEY,
                    slug TEXT NOT NULL,
                    title TEXT NOT NULL,
                    yearofrelease INTEGER NOT NULL
                );
            ");
            
            await connection.ExecuteAsync("""
                create unique index concurrently if not exists movies2_slug_idx on movies2 using btree(slug);
                """);

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS genres  (
                    movieId UUID references movies2 (Id),
                    genre TEXT NOT NULL
                );
            ");
        }
    }
}
