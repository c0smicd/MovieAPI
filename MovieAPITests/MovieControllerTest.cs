using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MovieAPI.Controller;
using MovieAPI.Data;

namespace MovieAPITests;

public class MovieControllerTest
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly Mock<ILogger<MovieController>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly MovieController _controller;

    public MovieControllerTest()
    {
        _mockContext = new Mock<AppDbContext>();
        _mockLogger = new Mock<ILogger<MovieController>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // You can pass mocks into the controller
        _controller = new MovieController(_mockContext.Object, _mockLogger.Object, _memoryCache);
    }
    
    
}