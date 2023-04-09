using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Catalogs.Products.Dtos;
using Catalogs.Products.Models;
using Catalogs.Products.ReadModel;
using Catalogs.Shared.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core;
using Shared.Core.Exceptions;
using Shared.EF.Extensions;
using Shared.Validation;
using Shared.Validation.Extensions;

namespace Catalogs.Products.Features.GettingProductById.v1;

internal record GetProductById(Guid Id) : IRequest<GetProductByIdResult>
{
    /// <summary>
    /// GetProductById query with validation
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static GetProductById Of(Guid id)
    {
        return new GetProductByIdValidator().HandleValidation(new GetProductById(id));
    }
}

internal class GetProductByIdValidator : AbstractValidator<GetProductById>
{
    public GetProductByIdValidator()
    {
        RuleFor(r => r.Id).NotEmpty();
    }
}

internal class GetProductByIdHandler : IRequestHandler<GetProductById, GetProductByIdResult>
{
    private readonly DbExecutors.GetProductByIdExecutor _getProductByIdExecutor;
    private readonly IMapper _mapper;

    public GetProductByIdHandler(DbExecutors.GetProductByIdExecutor getProductByIdExecutor, IMapper mapper)
    {
        _getProductByIdExecutor = getProductByIdExecutor;
        _mapper = mapper;
    }

    public async Task<GetProductByIdResult> Handle(GetProductById request, CancellationToken cancellationToken)
    {
        request.NotNull();

        var productReadModel = await _getProductByIdExecutor(request.Id, cancellationToken);

        if (productReadModel is null)
        {
            throw new NotFoundException($"product with id {request.Id} not found");
        }

        var productDto = _mapper.Map<ProductDto>(productReadModel);

        return new GetProductByIdResult(productDto);
    }
}

internal record GetProductByIdResult(ProductDto Product);

internal class DbExecutors : IDbExecutors
{
    public delegate Task<ProductReadModel?> GetProductByIdExecutor(Guid id, CancellationToken cancellationToken);

    public void Register(IServiceCollection services)
    {
        services.AddTransient<GetProductByIdExecutor>(sp =>
        {
            var context = sp.GetRequiredService<CatalogsDbContext>();
            var mapper = sp.GetRequiredService<IMapper>();

            Task<ProductReadModel?> Query(Guid id, CancellationToken cancellationToken) =>
                context
                    .ProjectEntity<Product, ProductReadModel>(mapper.ConfigurationProvider, cancellationToken)
                    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return Query;
        });
    }
}
