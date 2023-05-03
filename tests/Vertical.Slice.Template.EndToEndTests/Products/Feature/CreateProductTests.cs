using FluentAssertions;
using Vertical.Slice.Template.Api;
using Vertical.Slice.Template.Products.Features.CreatingProduct.v1;
using Vertical.Slice.Template.Shared.Data;
using Vertical.Slice.Template.TestsShared.Fakes.Products;
using Vertical.Slice.Template.TestsShared.Fixtures;
using Xunit.Abstractions;

namespace Vertical.Slice.Template.EndToEndTests.Products.Feature;

public class CreateProductTests : ECommerceEndToEndTestBase
{
    public CreateProductTests(
        SharedFixtureWithEfCore<CatalogsApiMetadata, CatalogsDbContext> sharedFixture,
        ITestOutputHelper outputHelper
    )
        : base(sharedFixture, outputHelper)
    {
        AssertionOptions.AssertEquivalencyUsing(options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task should_returns_created_status_code_using_valid_dto()
    {
        // Arrange
        var fakeCategoryId = Guid.NewGuid();

        var createProductRequestFake = new CreateProductRequestFake(fakeCategoryId).Generate();

        var route = Constants.Routes.Products.Create;

        // Act
        var response = await SharedFixture.GuestClient.PostAsJsonAsync(route, createProductRequestFake);

        // Assert
        response.Should().Be201Created();
    }

    [Fact]
    public async Task should_returns_valid_response_using_valid_dto()
    {
        // Arrange
        var fakeCategoryId = Guid.NewGuid();
        var createProductRequestFake = new CreateProductRequestFake(fakeCategoryId).Generate();
        var route = Constants.Routes.Products.Create;

        // Act
        var response = await SharedFixture.GuestClient.PostAsJsonAsync(route, createProductRequestFake);

        // Assert
        response.Should().Satisfy<CreateProductResponse>(x => x.Id.Should().NotBeEmpty());
    }
}
