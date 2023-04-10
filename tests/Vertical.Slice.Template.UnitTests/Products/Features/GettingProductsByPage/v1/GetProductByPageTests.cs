using AutoBogus;
using FluentAssertions;
using NSubstitute;
using Tests.Shared.XunitCategories;
using Vertical.Slice.Template.Products.Features.GettingProductsByPage.v1;
using Vertical.Slice.Template.Products.ReadModel;
using Vertical.Slice.Template.UnitTests.Common;

namespace Vertical.Slice.Template.UnitTests.Products.Features.GettingProductsByPage.v1;

[Collection(UnitTestCollection.Name)]
public class GetProductByPageTests : CatalogsUnitTestBase
{
    [CategoryTrait(TestCategory.Unit)]
    [Fact]
    public async Task can_get_products_with_valid_inputs()
    {
        // Arrange
        var productList = new AutoFaker<ProductReadModel>().Generate(5);

        var executor = Substitute.For<DbExecutors.GetProductsExecutor>();
        executor(Arg.Any<CancellationToken>()).Returns(productList.AsQueryable());
        var query = new GetProductsByPage() { PageSize = 10, PageNumber = 1 };

        var handler = new GetProductByPageHandler(executor, SieveProcessor, Mapper);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        executor.Received(1).Invoke(Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
    }
}