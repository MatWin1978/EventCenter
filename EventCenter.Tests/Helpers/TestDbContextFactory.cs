using Microsoft.EntityFrameworkCore;
using EventCenter.Web.Domain;

namespace EventCenter.Tests.Helpers;

public static class TestDbContextFactory
{
    public static EventCenterDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<EventCenterDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new EventCenterDbContext(options);

        // Open connection for in-memory database (stays alive until context disposed)
        context.Database.OpenConnection();

        // Create schema
        context.Database.EnsureCreated();

        return context;
    }
}
