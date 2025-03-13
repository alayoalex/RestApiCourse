﻿using Movies.Application.Model;

namespace Movies.Application.Services
{
    public interface IMovieService
    {
        Task<bool> CreateAsync(Movie movie);
        Task<Movie?> GetByIdAsync(Guid id);
        Task<Movie?> GetBySlugAsync(string id);
        Task<IEnumerable<Movie>> GetAllAsync();
        Task<Movie?> UpdateAsync(Movie movie);
        Task<bool> DeleteByIdAsync(Guid id);
    }
}
