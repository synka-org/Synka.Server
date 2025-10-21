using System.Linq.Expressions;
using Synka.Server.Contracts;
using Synka.Server.Data.Entities;

namespace Synka.Server.Extensions;

/// <summary>
/// Extension methods for <see cref="FolderEntity"/> queries.
/// </summary>
internal static class FolderEntityExtensions
{
    /// <summary>
    /// Projects <see cref="FolderEntity"/> to <see cref="FolderResponse"/>.
    /// </summary>
    /// <param name="query">The queryable source of folder entities.</param>
    /// <returns>A queryable that projects folders to their response representation.</returns>
    public static IQueryable<FolderResponse> ProjectToResponse(this IQueryable<FolderEntity> query)
    {
        return query.Select(ProjectToResponseExpression());
    }

    /// <summary>
    /// Gets the expression for projecting <see cref="FolderEntity"/> to <see cref="FolderResponse"/>.
    /// </summary>
    /// <returns>The projection expression.</returns>
    public static Expression<Func<FolderEntity, FolderResponse>> ProjectToResponseExpression() => folder => new FolderResponse(
        folder.Id,
        folder.OwnerId,
        folder.ParentFolderId,
        folder.Name,
        folder.PhysicalPath,
        folder.IsSharedRoot,
        folder.ParentFolderId == null && folder.OwnerId != null,
        folder.IsDeleted,
        folder.Files.Count(file => !file.IsDeleted),
        folder.ChildFolders.Count(subFolder => !subFolder.IsDeleted),
        folder.CreatedAt,
        folder.UpdatedAt);
}
