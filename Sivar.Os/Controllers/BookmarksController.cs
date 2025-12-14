using Microsoft.AspNetCore.Mvc;
using Sivar.Os.Shared.Services;

namespace Sivar.Os.Controllers;

/// <summary>
/// API controller for managing profile bookmarks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BookmarksController : ControllerBase
{
    private readonly IProfileBookmarkService _bookmarkService;
    private readonly ILogger<BookmarksController> _logger;

    public BookmarksController(
        IProfileBookmarkService bookmarkService,
        ILogger<BookmarksController> logger)
    {
        _bookmarkService = bookmarkService ?? throw new ArgumentNullException(nameof(bookmarkService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all bookmarked post IDs for the current user
    /// </summary>
    [HttpGet("post-ids")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetMyBookmarkedPostIds(CancellationToken cancellationToken)
    {
        try
        {
            var postIds = await _bookmarkService.GetMyBookmarkedPostIdsAsync(cancellationToken);
            return Ok(postIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookmarked post IDs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Checks if a post is bookmarked by the current user
    /// </summary>
    [HttpGet("check/{postId:guid}")]
    public async Task<ActionResult<bool>> IsBookmarked(Guid postId, CancellationToken cancellationToken)
    {
        try
        {
            var isBookmarked = await _bookmarkService.IsBookmarkedAsync(postId, cancellationToken);
            return Ok(isBookmarked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bookmark status for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Toggles bookmark for a post (add if not exists, remove if exists)
    /// Returns the new bookmark state (true = bookmarked, false = not bookmarked)
    /// </summary>
    [HttpPost("toggle/{postId:guid}")]
    public async Task<ActionResult<bool>> ToggleBookmark(Guid postId, [FromBody] ToggleBookmarkRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var newState = await _bookmarkService.ToggleBookmarkAsync(postId, request?.Note, cancellationToken);
            return Ok(newState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling bookmark for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Adds a bookmark for a post
    /// </summary>
    [HttpPost("{postId:guid}")]
    public async Task<ActionResult<BookmarkDto>> AddBookmark(Guid postId, [FromBody] AddBookmarkRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bookmark = await _bookmarkService.AddBookmarkAsync(postId, request?.Note, cancellationToken);
            if (bookmark == null)
            {
                return BadRequest("Unable to create bookmark. User may not have an active profile.");
            }

            return Ok(new BookmarkDto
            {
                Id = bookmark.Id,
                PostId = bookmark.PostId,
                Note = bookmark.Note,
                CreatedAt = bookmark.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding bookmark for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Removes a bookmark for a post
    /// </summary>
    [HttpDelete("{postId:guid}")]
    public async Task<ActionResult<bool>> RemoveBookmark(Guid postId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bookmarkService.RemoveBookmarkAsync(postId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bookmark for post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates the note for a bookmark
    /// </summary>
    [HttpPut("{postId:guid}/note")]
    public async Task<ActionResult<BookmarkDto?>> UpdateNote(Guid postId, [FromBody] UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var bookmark = await _bookmarkService.UpdateNoteAsync(postId, request.Note, cancellationToken);
            if (bookmark == null)
            {
                return NotFound("Bookmark not found");
            }

            return Ok(new BookmarkDto
            {
                Id = bookmark.Id,
                PostId = bookmark.PostId,
                Note = bookmark.Note,
                CreatedAt = bookmark.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note for bookmark on post {PostId}", postId);
            return StatusCode(500, "Internal server error");
        }
    }
}

// DTOs for the controller
public record ToggleBookmarkRequest
{
    public string? Note { get; init; }
}

public record AddBookmarkRequest
{
    public string? Note { get; init; }
}

public record UpdateNoteRequest
{
    public string? Note { get; init; }
}

public record BookmarkDto
{
    public Guid Id { get; init; }
    public Guid PostId { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
}
