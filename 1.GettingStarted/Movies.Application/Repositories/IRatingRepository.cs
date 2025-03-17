using Movies.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Application.Repositories
{
    public interface IRatingRepository
    {
        Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken token = default);
        Task<float?> GetRatingAsync(Guid movieId, CancellationToken token = default);
        Task<(float? Rating, int? UserRating)> GetRatingsAsync(Guid movieId, Guid? userId, CancellationToken token = default);
        Task<bool> DeletingRatingAsync(Guid movieId, Guid userId, CancellationToken token = default);
        Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token = default);
    }
}
