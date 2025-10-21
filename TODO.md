# TODO

- Restrict `CreateFolderAsync` to non-root folders by limiting parameters to name, parent folder ID, and cancellation token.
- Replace generic exceptions such as `ArgumentException` with domain-specific variants (e.g., `FolderNotFoundException`).
- Validate configured root folders at startup or after configuration changes, creating any missing entries in the database.
- Ensure `CreateFolderAsync` also creates the directory on disk.
- Extend `RequiresConfigurationAsync` (or its consumer) to provide manifest details describing remaining setup tasks so clients can guide first-time administrators through steps like initial user creation or SMTP setup.
