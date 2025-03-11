using Dapper;
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

        public async Task<bool> CreateAsync(Movie movie)
        {
            using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            var result = await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO movies2 (id, title, slug, yearofrelease) VALUES (@Id, @Title, @Slug, @YearOfRelease)",
                movie
            ));

            if (result > 0)
            {
                foreach (var genre in movie.Genres)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        "INSERT INTO genres (movieId, genre) VALUES (@MovieId, @Genre)",
                        new { MovieId = movie.Id, Genre = genre }
                    ));
                }
            }
            transaction.Commit();
            return result > 0;
        }

        public Task<Movie?> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Movie?> GetBySlugAsync(string slug)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Movie>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(Movie movie)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        Task<bool> IMovieRepository.ExistsByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
