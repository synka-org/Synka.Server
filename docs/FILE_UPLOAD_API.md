# File Upload API

## Overview

The File Upload API provides endpoints for uploading files with comprehensive metadata tracking. Files are tracked using platform-specific identifiers, allowing the system to identify files even if they're moved on disk.

## Features

- **Platform-Specific File Tracking**: Uses Windows file IDs (volume serial + file index) and Unix file IDs (device + inode) to track files
- **Content Deduplication**: Computes SHA-256 hash of file content for identifying duplicates
- **Metadata Persistence**: Stores file metadata in database with user association
- **Size Limits**: Configurable file size limits (default: 100MB)
- **Secure Upload**: Authenticated endpoints with user-scoped access
- **File Management**: Full CRUD operations (upload, get, list, delete)

## API Endpoints

All file endpoints are under `/api/v1/files` and require authentication.

### Upload File

```http
POST /api/v1/files
Content-Type: multipart/form-data
```

**Request**: Multipart form with `file` field containing the file to upload.

**Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "document.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 1048576,
  "contentHash": "A1B2C3D4...",
  "uploadedAt": "2025-10-12T10:30:00Z"
}
```

**Example**:
```bash
curl -X POST https://api.synka.local/api/v1/files \
  -H "Authorization: Bearer <token>" \
  -F "file=@/path/to/document.pdf"
```

### Get File Metadata

```http
GET /api/v1/files/{fileId}
```

**Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fileName": "document.pdf",
  "contentType": "application/pdf",
  "sizeBytes": 1048576,
  "storagePath": "/app/uploads/3fa85f64-5717-4562-b3fc-2c963f66afa6.pdf",
  "windowsFileId": "A1B2C3D4:E5F6A7B8:C9D0E1F2",
  "unixFileId": "FE01:123456789ABCDEF",
  "contentHash": "A1B2C3D4...",
  "uploadedAt": "2025-10-12T10:30:00Z",
  "updatedAt": null
}
```

### List User Files

```http
GET /api/v1/files
```

**Response** (200 OK):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fileName": "document.pdf",
    "contentType": "application/pdf",
    "sizeBytes": 1048576,
    "storagePath": "/app/uploads/3fa85f64-5717-4562-b3fc-2c963f66afa6.pdf",
    "windowsFileId": "A1B2C3D4:E5F6A7B8:C9D0E1F2",
    "unixFileId": null,
    "contentHash": "A1B2C3D4...",
    "uploadedAt": "2025-10-12T10:30:00Z",
    "updatedAt": null
  }
]
```

### Delete File

```http
DELETE /api/v1/files/{fileId}
```

**Response**: 
- `204 No Content` - File deleted successfully
- `404 Not Found` - File not found or not owned by user

## Configuration

Configure upload directory in `appsettings.json`:

```json
{
  "FileUpload": {
    "Directory": "/app/uploads"
  }
}
```

Or via environment variables:

```bash
export FileUpload__Directory=/app/uploads
```

## File Tracking Details

### Windows File Identifiers

On Windows systems, files are tracked using:
- **Volume Serial Number**: Unique identifier for the disk volume
- **File Index**: NTFS file ID (high + low 32-bit values)

Format: `{VolumeSerial:X8}:{FileIndexHigh:X8}:{FileIndexLow:X8}`

Example: `A1B2C3D4:E5F6A7B8:C9D0E1F2`

### Unix File Identifiers

On Unix/Linux systems, files are tracked using:
- **Device ID**: Identifier for the storage device
- **Inode Number**: Unique file identifier on the device

Format: `{Device:X}:{Inode:X}`

Example: `FE01:123456789ABCDEF`

### Content Hash

All files have their SHA-256 hash computed during upload for:
- Content-based deduplication detection
- File integrity verification
- Identifying identical files across users

## Storage

Files are stored with a UUID-based filename to avoid conflicts:

```
{UploadDirectory}/{FileId}{OriginalExtension}
```

Example: `/app/uploads/3fa85f64-5717-4562-b3fc-2c963f66afa6.pdf`

## Database Schema

The `FileMetadata` table stores:

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| UserId | Guid | Owner user ID (FK to AspNetUsers) |
| FileName | string(512) | Original filename |
| ContentType | string(256) | MIME type |
| SizeBytes | long | File size in bytes |
| StoragePath | string(1024) | Full path on disk |
| WindowsFileId | string(100) | Windows file identifier |
| UnixFileId | string(100) | Unix file identifier |
| ContentHash | string(64) | SHA-256 hash (hex) |
| UploadedAt | DateTimeOffset | Upload timestamp |
| UpdatedAt | DateTimeOffset? | Last update timestamp |

### Indexes

- `IX_FileMetadata_UserId` - For fast user file lookups
- `IX_FileMetadata_ContentHash` - For deduplication queries
- `IX_FileMetadata_WindowsFileId_UnixFileId` - For file tracking queries

## Security

- All endpoints require authentication
- Users can only access their own files
- File size limits prevent DoS attacks
- Antiforgery disabled for upload endpoint (API usage)

## Use Cases

1. **File Synchronization**: Track files across devices using platform-specific IDs
2. **Deduplication**: Identify duplicate content using SHA-256 hashes
3. **File Management**: CRUD operations with metadata persistence
4. **Audit Trail**: Track when files were uploaded and by whom
5. **Cross-Platform Tracking**: Identify same file on different operating systems
