namespace Sivar.Os.Shared.Configuration
{

    /// <summary>
    /// Types of file upload errors
    /// </summary>
    public enum FileUploadErrorType
    {
        FileTooLarge,
        UnsupportedFileType,
        InvalidFileName,
        StorageError,
        ValidationError,
        UnknownError
    }
}
