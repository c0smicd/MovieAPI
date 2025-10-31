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
        _fakeContext.Movies.Add(new Movie()
        {
            Id = 2,
            Title = "The Dark Knight",
            Genre = "Action",
            Rating = 9.0,
            PosterUrl = "poster2"
        });
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetMovies();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMovies = Assert.IsAssignableFrom<IEnumerable<MovieDToResponse>>(okResult.Value).ToList();


        Assert.Equal(2, returnedMovies.Count);
        Assert.Equal("Interstellar", returnedMovies[0].Title);
        Assert.Equal("The Dark Knight", returnedMovies[1].Title);
    }


    [Fact]
    public async Task GetMovies_ReturnsOk_OnCorrectPage()
    {
        // Arrange
        _fakeContext.Movies.AddRange(new[]
        {
            new Movie { Id = 1, Title = "Movie 1", Genre = "Genre 1", Rating = 7.0, PosterUrl = "poster1" },
            new Movie { Id = 2, Title = "Movie 2", Genre = "Genre 2", Rating = 8.0, PosterUrl = "poster2" },
            new Movie { Id = 3, Title = "Movie 3", Genre = "Genre 3", Rating = 9.0, PosterUrl = "poster3" },
            new Movie { Id = 4, Title = "Movie 4", Genre = "Genre 4", Rating = 6.0, PosterUrl = "poster4" }
        });
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetMovies(page: 2, pageSize: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMovies = Assert.IsAssignableFrom<IEnumerable<MovieDToResponse>>(okResult.Value).ToList();

        Assert.Equal(2, returnedMovies.Count);
        Assert.Equal("Movie 3", returnedMovies[0].Title);
        Assert.Equal("Movie 4", returnedMovies[1].Title);
    }

    [Fact]
    public async Task GetMovies_ReturnsOk_MoviesByAuditoriumId()
    {
        // Arrange

        // Seed context with seating plan and auditorium

        var seatingPlan1 = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        var seatingPlan2 = new SeatingPlan
        {
            Id = 2,
            PlanName = "Premium Plan",
            LayoutJson = "{}",
            Description = "A premium seating plan.",
            AuditoriumId = 2
        };

        _fakeContext.SeatingPlans.AddRange(seatingPlan1, seatingPlan2);
        await _fakeContext.SaveChangesAsync();

        var auditorium1 = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan1
        };

        var auditorium2 = new Auditorium
        {
            Id = 2,
            AuditoriumName = "VIP Auditorium",
            SeatingPlan = seatingPlan2
        };

        _fakeContext.Auditoriums.AddRange(auditorium1, auditorium2);
        await _fakeContext.SaveChangesAsync();

        // Seed movies and associate with auditoriums
        var movie1 = new Movie { Id = 1, Title = "Movie A", Genre = "Genre A", Rating = 7.5, PosterUrl = "posterA" };
        var movie2 = new Movie { Id = 2, Title = "Movie B", Genre = "Genre B", Rating = 8.5, PosterUrl = "posterB" };
        var movie3 = new Movie { Id = 3, Title = "Movie C", Genre = "Genre C", Rating = 9.0, PosterUrl = "posterC" };

        movie1.Auditoriums.Add(auditorium1);
        movie2.Auditoriums.Add(auditorium1);
        movie3.Auditoriums.Add(auditorium2);

        _fakeContext.Movies.AddRange(movie1, movie2, movie3);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetMoviesByAuditorium(auditoriumId: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMovies = Assert.IsAssignableFrom<IEnumerable<MovieDToResponse>>(okResult.Value).ToList();
        Assert.Equal(2, returnedMovies.Count);
        Assert.Contains(returnedMovies, m => m.Title == "Movie A");
        Assert.Contains(returnedMovies, m => m.Title == "Movie B");
    }
}