using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace Chatbot.Service.Services.GoogleDrive
{
    public class GoogleDriveService: IGoogleDriveService
    {
        private readonly string[] _scopes = { DriveService.Scope.DriveReadonly };
        private const string ApplicationName = "Chatbot";
        private readonly string _credentialsPath = "credentials.json";
        private DriveService? _driveService;

        public GoogleDriveService()
        {
          
        }

        public async Task InitializeAsync()
        {
            GoogleCredential credential;
            using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
            }

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            await Task.CompletedTask;
        }


        public async Task<IList<DriveFileResult>> ReadAllFilesAsync(string folderId, string downloadFolder)
        {
            if (_driveService == null)
                throw new InvalidOperationException("Call InitializeAsync first.");

            var results = new List<DriveFileResult>();
            var listRequest = _driveService.Files.List();
            listRequest.Q = $"'{folderId}' in parents and trashed = false";
            listRequest.Fields = "files(id,name,mimeType,modifiedTime,size)";

            var files = await listRequest.ExecuteAsync();

            if (files.Files == null || files.Files.Count == 0)
                return results;

            foreach (var file in files.Files)
            {
                var meta = new DriveFileResult
                {
                    Id = file.Id,
                    Name = file.Name,
                    MimeType = file.MimeType,
                    ModifiedTime = file.ModifiedTime,
                    Size = file.Size
                };

                var filePath = Path.Combine(downloadFolder, file.Name);
                Directory.CreateDirectory(downloadFolder);

                if (file.MimeType == "application/vnd.google-apps.document")
                {
                    // Export Google Docs to plain text
                    var exportRequest = _driveService.Files.Export(file.Id, "text/plain");
                    using var stream = new MemoryStream();
                    await exportRequest.DownloadAsync(stream);
                    var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());

                    meta.Content = text;
                    await File.WriteAllTextAsync(filePath + ".txt", text);
                }
                else
                {
                    // Normal file download
                    using var stream = new MemoryStream();
                    var request = _driveService.Files.Get(file.Id);
                    await request.DownloadAsync(stream);
                    await File.WriteAllBytesAsync(filePath, stream.ToArray());
                }

                results.Add(meta);
            }

            return results;
        }
    
    }

    public class DriveFileResult
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? MimeType { get; set; }
        public DateTimeOffset? ModifiedTime { get; set; }
        public long? Size { get; set; }
        public string? Content { get; set; }
    }
}
