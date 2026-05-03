using Application.Controllers.Products;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Application.Swagger;

public class ProductsFilterQueryOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor cad)
            return;

        if (cad.ControllerTypeInfo.AsType() != typeof(ProductsController) || cad.ActionName != "GetProducts")
            return;

        operation.Description =
            (operation.Description ?? string.Empty) +
            "\n\nDynamic characteristic filters are supported via query string in format " +
            "`filter.{characteristicGuid}.min` and `filter.{characteristicGuid}.max`." +
            "\n\nExamples:" +
            "\n- `GET /api/products?categoryId=<categoryGuid>&filter.11111111-1111-1111-1111-111111111111.min=100`" +
            "\n- `GET /api/products?categoryId=<categoryGuid>&filter.11111111-1111-1111-1111-111111111111.max=500`" +
            "\n- `GET /api/products?categoryId=<categoryGuid>&filter.11111111-1111-1111-1111-111111111111.min=100&filter.22222222-2222-2222-2222-222222222222.max=32`";

        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "filter.{characteristicGuid}.min",
            In = ParameterLocation.Query,
            Required = false,
            Description = "Minimum allowed value for a characteristic GUID.",
            Schema = new OpenApiSchema
            {
                Type = "number",
                Format = "double"
            },
            Example = new OpenApiDouble(100)
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "filter.{characteristicGuid}.max",
            In = ParameterLocation.Query,
            Required = false,
            Description = "Maximum allowed value for a characteristic GUID.",
            Schema = new OpenApiSchema
            {
                Type = "number",
                Format = "double"
            },
            Example = new OpenApiDouble(500)
        });
    }
}
