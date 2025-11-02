using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MovieAPI.Controller;
using MovieAPI.Data;
using MovieAPI.DTOs.Requests.Auditorium;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

namespace MovieAPITests;

public class AuditoriumControllerTest : IDisposable
{
    private readonly AppDbContext _fakeContext;
    private readonly ILogger<MovieController> _fakeLogger;
    private readonly IMemoryCache _fakeCache;
    private readonly AuditoriumController _controller;

    public AuditoriumControllerTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _fakeContext = new AppDbContext(options);

        _fakeLogger = A.Fake<ILogger<MovieController>>();
        _fakeCache = A.Fake<IMemoryCache>();

        _controller = new AuditoriumController(_fakeContext, _fakeLogger, _fakeCache);
    }

    [Fact]
    public async Task UpdateAuditorium_UpdatesNameOnly_ReturnsNoContent()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Old Name",
            SeatingPlan = seatingPlan
        };

        _fakeContext.Auditoriums.Add(auditorium);
        await _fakeContext.SaveChangesAsync();

        var updateDto = new AuditoriumDToPatch
        {
            AuditoriumName = "New Name"
        };

        // Act
        var result = await _controller.UpdateAuditorium(1, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedAuditorium = await _fakeContext.Auditoriums.FindAsync(1);
        Assert.NotNull(updatedAuditorium);
        Assert.Equal("New Name", updatedAuditorium.AuditoriumName);
    }

    [Fact]
    public async Task AddMovieToAuditorium_AddsMovie_ReturnsNoContent()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan
        };

        var movie = new Movie
        {
            Id = 1,
            Title = "Inception",
            Genre = "Sci-Fi",
            Rating = 8.8,
            Year = 2010
        };

        _fakeContext.Auditoriums.Add(auditorium);
        _fakeContext.Movies.Add(movie);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.AddMovieToAuditorium(1, 1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedAuditorium = await _fakeContext.Auditoriums
            .Include(a => a.Movies)
            .FirstOrDefaultAsync(a => a.Id == 1);
        
        Assert.NotNull(updatedAuditorium);
        Assert.Single(updatedAuditorium.Movies);
        Assert.Equal(1, updatedAuditorium.Movies.First().Id);
    }

    [Fact]
    public async Task AddMovieToAuditorium_MovieNotFound_ReturnsBadRequest()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan
        };

        _fakeContext.Auditoriums.Add(auditorium);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.AddMovieToAuditorium(1, 999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMovieToAuditorium_AuditoriumNotFound_ReturnsNotFound()
    {
        // Arrange
        var movie = new Movie
        {
            Id = 1,
            Title = "Inception",
            Genre = "Sci-Fi",
            Rating = 8.8,
            Year = 2010
        };

        _fakeContext.Movies.Add(movie);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.AddMovieToAuditorium(999, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddMovieToAuditorium_MovieAlreadyAssociated_ReturnsBadRequest()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var movie = new Movie
        {
            Id = 1,
            Title = "Inception",
            Genre = "Sci-Fi",
            Rating = 8.8,
            Year = 2010
        };

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan
        };

        auditorium.Movies.Add(movie);

        _fakeContext.Auditoriums.Add(auditorium);
        _fakeContext.Movies.Add(movie);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.AddMovieToAuditorium(1, 1);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemoveMovieFromAuditorium_RemovesMovie_ReturnsNoContent()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var movie = new Movie
        {
            Id = 1,
            Title = "Inception",
            Genre = "Sci-Fi",
            Rating = 8.8,
            Year = 2010
        };

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan
        };

        auditorium.Movies.Add(movie);

        _fakeContext.Auditoriums.Add(auditorium);
        _fakeContext.Movies.Add(movie);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveMovieFromAuditorium(1, 1);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedAuditorium = await _fakeContext.Auditoriums
            .Include(a => a.Movies)
            .FirstOrDefaultAsync(a => a.Id == 1);
        
        Assert.NotNull(updatedAuditorium);
        Assert.Empty(updatedAuditorium.Movies);
    }

    [Fact]
    public async Task RemoveMovieFromAuditorium_AuditoriumNotFound_ReturnsNotFound()
    {
        // Act
        var result = await _controller.RemoveMovieFromAuditorium(999, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveMovieFromAuditorium_MovieNotAssociated_ReturnsNotFound()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan
        };

        _fakeContext.Auditoriums.Add(auditorium);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveMovieFromAuditorium(1, 999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateAuditoriumSeatingPlan_UpdatesSeatingPlan_ReturnsNoContent()
    {
        // Arrange
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
            LayoutJson = "{\"premium\":true}",
            Description = "A premium seating plan."
        };

        _fakeContext.SeatingPlans.AddRange(seatingPlan1, seatingPlan2);
        await _fakeContext.SaveChangesAsync();

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan1
        };

        _fakeContext.Auditoriums.Add(auditorium);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.UpdateAuditoriumSeatingPlan(1, 2);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        var updatedAuditorium = await _fakeContext.Auditoriums
            .Include(a => a.SeatingPlan)
            .FirstOrDefaultAsync(a => a.Id == 1);
        
        Assert.NotNull(updatedAuditorium);
        Assert.NotNull(updatedAuditorium.SeatingPlan);
        Assert.Equal(2, updatedAuditorium.SeatingPlan.Id);
    }

    [Fact]
    public async Task UpdateAuditoriumSeatingPlan_AuditoriumNotFound_ReturnsNotFound()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan."
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.UpdateAuditoriumSeatingPlan(999, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateAuditoriumSeatingPlan_SeatingPlanNotFound_ReturnsBadRequest()
    {
        // Arrange
        var seatingPlan = new SeatingPlan
        {
            Id = 1,
            PlanName = "Standard Plan",
            LayoutJson = "{}",
            Description = "A standard seating plan.",
            AuditoriumId = 1
        };

        _fakeContext.SeatingPlans.Add(seatingPlan);
        await _fakeContext.SaveChangesAsync();

        var auditorium = new Auditorium
        {
            Id = 1,
            AuditoriumName = "Main Auditorium",
            SeatingPlan = seatingPlan
        };

        _fakeContext.Auditoriums.Add(auditorium);
        await _fakeContext.SaveChangesAsync();

        // Act
        var result = await _controller.UpdateAuditoriumSeatingPlan(1, 999);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    public void Dispose()
    {
        // Clear cache
        if(_fakeCache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
        }
    }
}
