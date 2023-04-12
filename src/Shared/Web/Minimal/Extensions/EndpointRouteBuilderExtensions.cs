using Humanizer;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Web.Contracts;
using Shared.Web.ProblemDetail.HttpResults;

namespace Shared.Web.Minimal.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapCommandEndpoint<TRequest, TCommand>(
        this IEndpointRouteBuilder builder,
        string pattern,
        Func<TRequest, TCommand>? mapRequestToCommand = null
    )
        where TRequest : class
        where TCommand : IRequest
    {
        return builder.MapPost(pattern, Handle).WithName(nameof(TCommand)).WithDisplayName(nameof(TCommand).Humanize());

        async Task<NoContent> Handle([AsParameters] HttpCommand<TRequest> requestParameters)
        {
            var (request, context, mediator, mapper, cancellationToken) = requestParameters;

            var command = mapRequestToCommand is not null
                ? mapRequestToCommand(request)
                : mapper.Map<TCommand>(request);
            await mediator.Send(command, cancellationToken);

            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-7.0#multiple-response-types
            return TypedResults.NoContent();
        }
    }

    public static RouteHandlerBuilder MapCommandEndpoint<TRequest, TResponse, TCommand, TCommandResult>(
        this IEndpointRouteBuilder builder,
        string pattern,
        int statusCode,
        Func<TRequest, TCommand>? mapRequestToCommand = null,
        Func<TCommandResult, TResponse>? mapCommandResultToResponse = null
    )
        where TRequest : class
        where TResponse : class
        where TCommandResult : class
        where TCommand : IRequest<TCommandResult>
    {
        return builder.MapPost(pattern, Handle).WithName(nameof(TCommand)).WithDisplayName(nameof(TCommand).Humanize());

        // https://github.com/dotnet/aspnetcore/issues/47630
        async Task<
            Results<
                Ok<TResponse>,
                CreatedAtRoute<TResponse>,
                Accepted<TResponse>,
                UnAuthorizedHttpProblemResult,
                InternalHttpProblemResult
            >
        > Handle([AsParameters] HttpCommand<TRequest> requestParameters)
        {
            var (request, context, mediator, mapper, cancellationToken) = requestParameters;
            var host = $"{context.Request.Scheme}://{context.Request.Host}";

            var command = mapRequestToCommand is not null
                ? mapRequestToCommand(request)
                : mapper.Map<TCommand>(request);
            var res = await mediator.Send(command, cancellationToken);

            var response = mapCommandResultToResponse is not null
                ? mapCommandResultToResponse(res)
                : mapper.Map<TResponse>(res);

            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-7.0#multiple-response-types
            return statusCode switch
            {
                StatusCodes.Status201Created => TypedResults.CreatedAtRoute(response, routeName: $"{host}{pattern}/id"),
                StatusCodes.Status401Unauthorized => TypedResultsExtensions.UnAuthorizedProblem(),
                StatusCodes.Status500InternalServerError => TypedResultsExtensions.InternalProblem(),
                StatusCodes.Status202Accepted => TypedResults.Accepted(new Uri($"{host}{pattern}"), response),
                _ => TypedResults.Ok(response)
            };
        }
    }

    public static RouteHandlerBuilder MapQueryEndpoint<TRequestParameters, TResponse, TQuery, TQueryResult>(
        this IEndpointRouteBuilder builder,
        string pattern,
        Func<TRequestParameters, TQuery>? mapRequestToQuery = null,
        Func<TQueryResult, TResponse>? mapQueryResultToResponse = null
    )
        where TRequestParameters : IHttpQuery
        where TResponse : class
        where TQueryResult : class
        where TQuery : IRequest<TQueryResult>
    {
        return builder.MapGet(pattern, Handle).WithName(nameof(TQuery)).WithDisplayName(nameof(TQuery).Humanize());

        async Task<Ok<TResponse>> Handle([AsParameters] TRequestParameters requestParameters)
        {
            var mediator = requestParameters.Mediator;
            var mapper = requestParameters.Mapper;
            var cancellationToken = requestParameters.CancellationToken;

            var query = mapRequestToQuery is not null
                ? mapRequestToQuery(requestParameters)
                : mapper.Map<TQuery>(requestParameters);

            var res = await mediator.Send(query, cancellationToken);

            var response = mapQueryResultToResponse is not null
                ? mapQueryResultToResponse(res)
                : mapper.Map<TResponse>(res);

            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/openapi?view=aspnetcore-7.0#multiple-response-types
            return TypedResults.Ok(response);
        }
    }
}