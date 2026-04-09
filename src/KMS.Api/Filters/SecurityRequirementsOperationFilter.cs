using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KMS.Api.Filters;

public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var isAnonymous = endpointMetadata.OfType<AllowAnonymousAttribute>().Any()
            || endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (isAnonymous)
        {
            operation.Security?.Clear();
            return;
        }

        var requiresAuthorization = endpointMetadata.OfType<AuthorizeAttribute>().Any()
            || endpointMetadata.OfType<IAuthorizeData>().Any();

        if (!requiresAuthorization)
        {
            operation.Security?.Clear();
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        ];

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized"
        });

        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Forbidden"
        });
    }
}