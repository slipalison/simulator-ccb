using Onboarding.Application.Common;
using Shouldly;

namespace Onboarding.Domain.Tests.Application;

public class PaginatedResultTests
{
    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };

        // Act — 3 items, page 1, pageSize 2 => totalPages = ceil(3/2) = 2
        var result = new PaginatedResult<string>(items.AsReadOnly(), 3, 1, 2);

        // Assert
        result.TotalPages.ShouldBe(2);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(2);
        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
    }

    [Fact]
    public void TotalPages_ShouldBeZero_WhenPageSizeIsZero()
    {
        // Arrange + Act
        var result = new PaginatedResult<string>(Array.Empty<string>().AsReadOnly(), 0, 1, 0);

        // Assert
        result.TotalPages.ShouldBe(0);
    }

    [Fact]
    public void TotalPages_ShouldRoundUp()
    {
        // Arrange + Act — 10 items, pageSize 3 => ceil(10/3) = 4
        var result = new PaginatedResult<string>(Array.Empty<string>().AsReadOnly(), 10, 1, 3);

        // Assert
        result.TotalPages.ShouldBe(4);
    }

    [Fact]
    public void TotalPages_ShouldBeExact_WhenEvenlyDivisible()
    {
        // Arrange + Act — 10 items, pageSize 5 => 2
        var result = new PaginatedResult<string>(Array.Empty<string>().AsReadOnly(), 10, 1, 5);

        // Assert
        result.TotalPages.ShouldBe(2);
    }
}
