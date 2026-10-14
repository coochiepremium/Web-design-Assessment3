namespace AICreativeArts.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

/// <summary>Friendly description of an HTTP error status (404, 403, 400 ...).</summary>
public class StatusCodeViewModel
{
    public int StatusCode { get; init; }

    public string Title { get; init; } = "";

    public string Message { get; init; } = "";

    public string? OriginalPath { get; init; }

    public static StatusCodeViewModel For(int statusCode, string? originalPath) => statusCode switch
    {
        400 => Create(statusCode, "Bad Request",
            "Your browser sent a request we could not understand. If you were submitting a form, please reload the page and try again.", originalPath),
        403 => Create(statusCode, "Access Denied",
            "You do not have permission to view this page.", originalPath),
        404 => Create(statusCode, "Page Not Found",
            "We searched every gallery wall but could not find that page. It may have been moved or deleted.", originalPath),
        405 => Create(statusCode, "Method Not Allowed",
            "That page cannot be used in this way. Please use the links and buttons on the site.", originalPath),
        413 => Create(statusCode, "Upload Too Large",
            "The file you tried to upload is too large. Please choose an image smaller than 5 MB.", originalPath),
        _ => Create(statusCode, "Something Went Wrong",
            "The request could not be completed. Please try again in a moment.", originalPath),
    };

    private static StatusCodeViewModel Create(int code, string title, string message, string? path) =>
        new() { StatusCode = code, Title = title, Message = message, OriginalPath = path };
}
