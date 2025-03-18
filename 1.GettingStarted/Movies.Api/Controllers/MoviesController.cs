using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Model;
using Movies.Application.Repositories;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using System.Reflection;

namespace Movies.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService)
        {
            _movieService = movieService;
        }

        [AllowAnonymous]
        [HttpGet(ApiEndpoints.Movies.GetAll)]
        public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var options = request.MapToOptions()
                .WithUser(userId);
            var movies = await _movieService.GetAllAsync(options, token);
            var movieCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
            var moviesResponse = movies.MapToMoviesResponse(request.Page, request.PageSize, movieCount);
            return Ok(moviesResponse);
        }

        [ApiVersion(1.0, Deprecated = true)]
        [AllowAnonymous]
        [HttpGet(ApiEndpoints.Movies.Get)]
        public async Task<IActionResult> GetV1([FromRoute]string idOrSlug, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();

            var movie = Guid.TryParse(idOrSlug, out var id) 
                ? await _movieService.GetByIdAsync(id, userId, token) 
                : await _movieService.GetBySlugAsync(idOrSlug, userId, token);
            if (movie is null)
            {
                return NotFound();
            }

            var response = movie.MapToMovieResponse();
            return Ok(response);
        }

        [ApiVersion(2.0)]
        [AllowAnonymous]
        [HttpGet(ApiEndpoints.Movies.Get)]
        public async Task<IActionResult> GetV2([FromRoute] string idOrSlug, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();

            var movie = Guid.TryParse(idOrSlug, out var id)
                ? await _movieService.GetByIdAsync(id, userId, token)
                : await _movieService.GetBySlugAsync(idOrSlug, userId, token);
            if (movie is null)
            {
                return NotFound();
            }

            var response = movie.MapToMovieResponse();
            return Ok(response);
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpPost(ApiEndpoints.Movies.Create)]
        public async Task<IActionResult> Create([FromBody]CreateMovieRequest request, CancellationToken token)
        {
            var movie = request.MapToMovie();
            var result = await _movieService.CreateAsync(movie, token);
            return CreatedAtAction(nameof(Get), new { idOrSlug = movie.Id }, movie);
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpPut(ApiEndpoints.Movies.Update)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request, CancellationToken token)
        {
            var userId = HttpContext.GetUserId();
            var movie = request.MapToMovie(id);
            var updatedMovie = await _movieService.UpdateAsync(movie, userId, token);
            if (updatedMovie is null)
            {
                return NotFound();
            }
            var response = movie.MapToMovieResponse();
            return Ok(response);
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpDelete(ApiEndpoints.Movies.Delete)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
        {
            var deleted = await _movieService.DeleteByIdAsync(id, token);
            if (!deleted)
            {
                return NotFound();
            }
            return Ok();
        }
    }
}
