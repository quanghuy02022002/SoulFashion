using Microsoft.AspNetCore.Http;

public interface IS3Service
{
    Task<string> UploadFileAsync(IFormFile file);
}
