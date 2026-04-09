using Microsoft.AspNetCore.Mvc;
using KMS.Api.Models;

namespace KMS.Api.Helpers;

public static class ApiControllerExtensions
{
    public static ActionResult<ApiResponse<T>> Ok<T>(this ControllerBase controller, T data, string? message = null)
    {
        return controller.Ok(new ApiResponse<T>(data, message));
    }

    public static ActionResult<ApiResponse> Ok(this ControllerBase controller, string? message = null)
    {
        return controller.Ok(new ApiResponse(true, message));
    }

    public static ActionResult<ApiResponse<T>> Created<T>(this ControllerBase controller, string actionName, object routeValues, T data, string? message = null)
    {
        var response = new ApiResponse<T>(data, message);
        return controller.CreatedAtAction(actionName, routeValues, response);
    }

    public static ActionResult<ApiResponse<T>> Created<T>(this ControllerBase controller, string uri, T data, string? message = null)
    {
        var response = new ApiResponse<T>(data, message);
        return controller.Created(uri, response);
    }

    public static ActionResult<ApiResponse> BadRequest(this ControllerBase controller, string message, List<string>? errors = null)
    {
        return controller.BadRequest(new ApiResponse(message, errors));
    }

    public static ActionResult<ApiResponse<T>> BadRequest<T>(this ControllerBase controller, string message, List<string>? errors = null)
    {
        return controller.BadRequest(new ApiResponse<T>(message, errors));
    }

    public static ActionResult<ApiResponse> NotFound(this ControllerBase controller, string message)
    {
        return controller.NotFound(new ApiResponse(message));
    }

    public static ActionResult<ApiResponse<T>> NotFound<T>(this ControllerBase controller, string message)
    {
        return controller.NotFound(new ApiResponse<T>(message));
    }

    public static ActionResult<ApiResponse> Unauthorized(this ControllerBase controller, string message = "Unauthorized")
    {
        return controller.Unauthorized(new ApiResponse(message));
    }

    public static ActionResult<ApiResponse<T>> Unauthorized<T>(this ControllerBase controller, string message = "Unauthorized")
    {
        return controller.Unauthorized(new ApiResponse<T>(message));
    }

    public static ActionResult<ApiResponse> Forbidden(this ControllerBase controller, string message = "Forbidden")
    {
        return controller.Forbid();
    }

    public static ActionResult<ApiResponse<T>> Forbidden<T>(this ControllerBase controller, string message = "Forbidden")
    {
        return controller.Forbid();
    }

    public static ActionResult<ApiResponse> InternalServerError(this ControllerBase controller, string message = "An error occurred while processing your request.")
    {
        return controller.StatusCode(500, new ApiResponse(message));
    }

    public static ActionResult<ApiResponse<T>> InternalServerError<T>(this ControllerBase controller, string message = "An error occurred while processing your request.")
    {
        return controller.StatusCode(500, new ApiResponse<T>(message));
    }
}