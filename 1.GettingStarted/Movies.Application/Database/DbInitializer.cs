﻿using Dapper;
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

        public async Task InitializeAsync(CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
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
                """
            );

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS genres  (
                    movieId UUID references movies2 (Id),
                    genre TEXT NOT NULL
                );
            ");

            await connection.ExecuteAsync("""
                create table if not exists ratings (
                userid uuid,
                movieid uuid references movies2 (id),
                rating integer not null,
                primary key (userid, movieid))
                """);
        }
    }
}
