# TODO

Add a configuration for debounce time on file system scans

Change the file system watcher service to just watch the configured root paths

Question: Should we split the services, data services and file system services?

DateTimeOffset.UtcNow should not be used, use the mockable TimeProvider(?) in dotnet 10, update your instructions as well do only use it

Instead of casting `(FolderAccessPermissionLevel)row.Permission` we should have a method handling this

Work with HashSet instead of `List<Guid>` like this code part:
return ownedFolders
            .Concat(sharedFolders)
            .Concat(sharedRoots)
            .Distinct()
            .ToList();
It should give better performance, right?

FolderService should not accept a userId, it should like FileService find the userId in the HttpContext - extract the method to get it into shared code

We should always hard delete folders in the database if it's an api call, the folder on disk should also be deleted in that case. If it is the file system watcher that notices that a file or a folder is deleted on disk then it should be flagged as soft deleted in the database and restored if a folder with the same path is restored or if a file with the same file hash is detected on some other location on the disk

FolderService ProjectToResponse should be an extension method so instead of Select(ProjectToResponse()) it should be ProjectToResponse on `IQueryable<FolderEntity>`

CreateFolderAsync should not be able to create root folders, these are only created by configuration, so CreateFolderAsync should only have name, parentFolderId and cancellationToken as arguments

Throwed exceptions should be more application specific than i.e. `throw new ArgumentException($"Parent folder '{parentFolderId}' does not exist.", nameof(parentFolderId));` this should probably be a `FolderNotExistsException(folderId)` or something with a better name

On startup or application configuration change existing root folders should be validated that they still exist and new ones should be created in the database

CreateFolderAsync should also create the folder on disk

The RequiresConfigurationAsync or maybe the consumer should return something more to the manifest response so that the client knows what configuration steps it needs to show to the user, i.e. right now the initial admin user has to be created but we need to prepare for more required configurations. If a user starts Synka for the first time it might need to both create an initial user, but also maybe setup smtp if we decide that it is required in the future.

Should we should call it Admin and not Administrator
