# TODO

- Consider using `HashSet<Guid>` in the folder aggregation pipeline (currently concatenating owned, shared, and root folders) to avoid extra `Distinct()` work.
- Align `FolderService` with `FileService` by resolving the user identifier from `HttpContext`; extract the lookup logic into shared code.
- Enforce hard deletes for API-initiated folder deletions (including matching disk removal) while keeping watcher-triggered deletions soft with automatic restoration when content reappears.
- Convert `FolderService.ProjectToResponse` into an extension on `IQueryable<FolderEntity>` so callers can invoke `ProjectToResponse()` directly.
- Restrict `CreateFolderAsync` to non-root folders by limiting parameters to name, parent folder ID, and cancellation token.
- Replace generic exceptions such as `ArgumentException` with domain-specific variants (e.g., `FolderNotFoundException`).
- Validate configured root folders at startup or after configuration changes, creating any missing entries in the database.
- Ensure `CreateFolderAsync` also creates the directory on disk.
- Extend `RequiresConfigurationAsync` (or its consumer) to provide manifest details describing remaining setup tasks so clients can guide first-time administrators through steps like initial user creation or SMTP setup.
