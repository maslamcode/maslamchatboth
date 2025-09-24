using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiChatBot.Services
{
    public interface IGoogleDriveService
    {
        Task InitializeAsync();
        Task<IList<DriveFileResult>> ReadAllFilesAsync(string folderId, string downloadFolder);

    }
}
