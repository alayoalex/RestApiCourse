﻿using Dapper;
using Movies.Application.Database;
using Movies.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Repositories
{
    public class MovieRepository : IMovieRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public MovieRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<bool> CreateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            using var transaction = connection.BeginTransaction();

            var result = await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO movies2 (id, title, slug, yearofrelease) VALUES (@Id, @Title, @Slug, @YearOfRelease)",
                movie, cancellationToken: token
            ));

            if (result > 0)
            {
                foreach (var genre in movie.Genres)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        "INSERT INTO genres (movieId, genre) VALUES (@MovieId, @Genre)",
                        new { MovieId = movie.Id, Genre = genre }, cancellationToken: token
                    ));
                }
            }
            transaction.Commit();
            return result > 0;
        }

        public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
                new CommandDefinition("""
                select 
                    m.*, 
                    round(avg(r.rating), 1) as rating, 
                    myr.rating as userrating
                from movies2 m 
                    left join ratings r on m.id = r.movieid
                    left join ratings myr on m.id = myr.movieid 
                        and myr.userid = @userId
                where id = @id
                group by id, userrating
                """, new { id, userId }, cancellationToken: token));

            if (movie is null)
            {
                return null;
            }

            var genres = await connection.QueryAsync<string>(
                new CommandDefinition("""
                    select genre from genres where movieid = @id
                    """, new { id }, cancellationToken: token));

            foreach (var genre in genres) 
            {
                movie.Genres.Add(genre);
            }

            return movie;
        }

        public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId = default, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
                new CommandDefinition("""
                select 
                    m.*,
                    round(avg(r.rating), 1) as rating, 
                    myr.rating as userrating
                from movies2 m 
                    left join ratings r on m.id = r.movieid
                    left join ratings myr on m.id = myr.movieid 
                        and myr.userid = @userId
                where slug = @slug
                group by id, userrating
                """, new { slug, userId }, cancellationToken: token));

            if (movie is null)
            {
                return null;
            }

            var genres = await connection.QueryAsync<string>(
                new CommandDefinition("""
                    select genre from genres where movieid = @id
                    """, new { id = movie.Id }, cancellationToken: token));

            foreach (var genre in genres)
            {
                movie.Genres.Add(genre);
            }

            return movie;
        }

        public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

            var orderClause = string.Empty;
            if (options.SortField is not null)
            {
                orderClause = $"""
                    , m.{options.SortField}
                    order by m.{options.SortField} {(options.SortOrder == SortOrder.Ascending ? "asc" : "desc")}
                    """;
            }

            var result = await connection.QueryAsync(new CommandDefinition($"""
                select m.*, 
                    string_agg(distinct g.genre, ',') as genres,
                    round(avg(r.rating), 1) as rating,
                    myr.rating as userrating
                from movies2 m 
                    left join genres g on m.id = g.movieid
                    left join ratings r on m.id = r.movieid
                    left join ratings myr on m.id = myr.movieid 
                        and myr.userid = @userId
                where (@title is null or m.title like ('%' || @title || '%'))
                    and (@yearofrelease is null or m.yearofrelease = @yearofrelease)
                group by id, userrating {orderClause}
                limit @pageSize
                offset @pageOffset
                """, new { 
                            userId = options.UserId, 
                            title = options.Title, 
                            yearofrelease = options.YearOfRelease ,
                            pageSize = options.PageSize,
                            pageOffset = (options.Page - 1) * options.PageSize,
                        }, cancellationToken: token));

            return result.Select(x => new Movie
            {
                Id = x.id,
                Title = x.title,
                YearOfRelease = x.yearofrelease,
                Rating = (float?)x.rating,
                UserRating = (int?)x.userrating,
                Genres = Enumerable.ToList(x.genres.Split(','))
            });
        }

        public async Task<bool> UpdateAsync(Movie movie, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition(
                """delete from genres where movieid = @id""",
                new { id = movie.Id }, cancellationToken: token));

            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO genres (movieId, genre) VALUES (@MovieId, @Genre)",
                    new { MovieId = movie.Id, Genre = genre }, cancellationToken: token
                ));
            }

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                update movies2 set title = @Title, slug = @Slug, yearofrelease = @YearOfRelease where id = @Id
                """, movie, cancellationToken: token));

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(new CommandDefinition(
                """delete from genres where movieid = @id""",
                new { id }, cancellationToken: token));

            var result = await connection.ExecuteAsync(new CommandDefinition("""
                delete from movies2 where id = @id
                """, new { id }, cancellationToken: token));

            transaction.Commit();
            return result > 0;
        }

        public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken token = default)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition("""
                    select count(1) 
                    from movies2 
                    where id = @id
                    """, new { id }, cancellationToken: token));
        }

        public async Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken token)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync(token);
            return await connection.QuerySingleAsync<int>(new CommandDefinition("""
                select count(id) from movies2
                where (@title is null or title like ('%' || @title || '%'))
                    and (@yearOfRelease is null or yearofrelease = @yearOfRelease)
                """, new
            {
                title,
                yearOfRelease
            }, cancellationToken: token));
        }
    }
}
