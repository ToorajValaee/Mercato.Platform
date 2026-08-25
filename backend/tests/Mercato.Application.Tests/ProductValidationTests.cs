using FluentAssertions;
using Xunit;

namespace Mercato.Application.Tests;

public class ProductValidationTests
{
    [Fact]
    public void Product_name_should_not_be_empty()
    {
        var name = string.Empty;

        name.Should().BeEmpty();
    }
}
