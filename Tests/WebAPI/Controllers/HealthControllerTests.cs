using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Tests.WebAPI.Controllers;

public class HealthControllerTests
{
    [Fact]
    public void GetHealth_ReturnsHealthyResponse()
    {
        var controller = new HealthController();

        var result = controller.GetHealth();

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.Value.Should().BeEquivalentTo(new
        {
            status = "healthy",
            version = "1.0"
        }, options => options.ExcludingMissingMembers());
    }
}
