﻿using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Model;
using Movies.Application.Repositories;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;
using System.Reflection;

namespace Movies.Api.Controllers
{
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    public class MoviesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly IOutputCacheStore _outputCacheStore;

        public MoviesController(IMovieService movieService, IOutputCacheStore outputCacheSotre)
        {
            _movieService = movieService;
            _outputCacheStore = outputCacheSotre;
        }

        [AllowAnonymous]
        [HttpGet(ApiEndpoints.Movies.GetAll)]
        [OutputCache(PolicyName = "MovieCache")]
        [ProducesResponseType(typeof(MoviesResponse), StatusCodes.Status201Created)]
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

        [AllowAnonymous]
        [HttpGet(ApiEndpoints.Movies.Get)]
        [OutputCache(PolicyName = "MovieCache")]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [ServiceFilter(typeof(ApiKeyAuthFilter))]
        [HttpPost(ApiEndpoints.Movies.Create)]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody]CreateMovieRequest request, CancellationToken token)
        {
            var movie = request.MapToMovie();
            var result = await _movieService.CreateAsync(movie, token);
            await _outputCacheStore.EvictByTagAsync("movies", token);
            return CreatedAtAction(nameof(GetV1), new { idOrSlug = movie.Id }, movie);
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpPut(ApiEndpoints.Movies.Update)]
        [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
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
            await _outputCacheStore.EvictByTagAsync("movies", token);
            return Ok(response);
        }

        [Authorize(AuthConstants.TrustedMemberPolicyName)]
        [HttpDelete(ApiEndpoints.Movies.Delete)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
        {
            var deleted = await _movieService.DeleteByIdAsync(id, token);
            if (!deleted)
            {
                return NotFound();
            }
            await _outputCacheStore.EvictByTagAsync("movies", token);
            return Ok();
        }
    }
}
