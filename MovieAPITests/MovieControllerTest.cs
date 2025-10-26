using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MovieAPI.Controller;
using MovieAPI.Data;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

namespace MovieAPITests;

public class MovieControllerTest
{
    private readonly AppDbContext _fakeContext;
    private readonly ILogger<MovieController> _fakeLogger;
    private readonly IMemoryCache _fakeCache;
    private readonly MovieController _controller;

    public MovieControllerTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("MovieTestDb")
            .Options;

        _fakeContext = new AppDbContext(options);

        _fakeLogger = A.Fake<ILogger<MovieController>>();
        _fakeCache = A.Fake<IMemoryCache>();

        _controller = new MovieController(_fakeContext, _fakeLogger, _fakeCache);
    }


    [Fact]
    public async Task GetMovies_ReturnsOk_WhenDataIsCached()
    {
        // Arrange
        var cachedMovies = new[]
        {
            new MovieDToResponse { Id = 1, Title = "Inception", Rating = 8.8, Genre = "Sci-Fi", PosterUrl = "poster1" },
            new MovieDToResponse { Id = 2, Title = "The Matrix", Rating = 9.0, Genre = "Action", PosterUrl = "poster2" }
        };

        object? cacheEntry = cachedMovies;
        A.CallTo(() => _fakeCache.TryGetValue(A<object>._, out cacheEntry))
            .Returns(true);

        // Act
        var result = await _controller.GetMovies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMovies = Assert.IsAssignableFrom<IEnumerable<MovieDToResponse>>(okResult.Value);
        Assert.Equal(2, returnedMovies.Count());
    }


    [Fact]
    public async Task GetMovies_ReturnsOk_WhenDataIsNotCached()
    {
        // Arrange
        object? cacheEntry = null;
        A.CallTo(() => _fakeCache.TryGetValue(A<object>._, out cacheEntry))
            .Returns(false);

        // Seed data directly into real in-memory context
        _fakeContext.Movies.Add(new Movie
        {
            Id = 1,
            Title = "Interstellar",
            Genre = "Sci-Fi",
            Rating = 9.0,
            PosterUrl = "poster1"
        });
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetMovies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMovies = Assert.IsAssignableFrom<IEnumerable<MovieDToResponse>>(okResult.Value);
        var movie = returnedMovies.First();

        Assert.Equal("Interstellar", movie.Title);
        Assert.Equal("Sci-Fi", movie.Genre);
        Assert.Equal(9.0, movie.Rating);
    }
}