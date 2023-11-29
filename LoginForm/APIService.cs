﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using LoginForm;
using LoginForm.Constants;
using LoginForm.Model;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace GoogleDriveAPIExample
{
    public class APIService
    {
        static string[] Scopes = { DriveService.Scope.Drive };
        static string ApplicationName = "demo 01";
        UserCredential credential;
        List<Container> listOfFiles = new List<Container>();
        public string location;
        string credPath;
        public string userName;

        public UserCredential GetCredential()
        {
            UserCredential credential;
            credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            credPath = Path.Combine(credPath, ".credential/drive-dotnet-quickstart.json");

            var fileDataStore = new FileDataStore(credPath, true);

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    fileDataStore
                ).Result;
            }

            return credential;
        }
        public void saveToken(string sourcePath, string destinationPath)
        {

            string fileContent = File.ReadAllText(sourcePath + "\\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user");
            JObject jsonObject = JObject.Parse(fileContent);

            string accessToken = jsonObject["access_token"].ToString();
            string refreshToken = jsonObject["refresh_token"].ToString();
            string saveContent = accessToken + "," + refreshToken;
            File.WriteAllText(destinationPath, saveContent);
            Console.WriteLine("Done writing");
            try
            {
                // Kiểm tra xem tệp tin có tồn tại không
                if (File.Exists((sourcePath + "\\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user")))
                {
                    // Xóa tệp tin
                    File.Delete((sourcePath + "\\Google.Apis.Auth.OAuth2.Responses.TokenResponse-user"));
                    Console.WriteLine("Đã xóa tệp tin thành công.");
                }
                else
                {
                    Console.WriteLine("Tệp tin không tồn tại.");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Đã xảy ra lỗi: " + ex.Message);
            }

        }
        public DriveService startService()
        {
            string fileName = "history.txt";
            credential = GetCredential();
            //start service

            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            var aboutRequest = service.About.Get();
            aboutRequest.Fields = "user";
            var aboutResponse = aboutRequest.Execute();

            // Lấy tên của người dùng
            userName = aboutResponse.User.EmailAddress;
            userName = userName.Replace("@gmail.com", "");
            // Đường dẫn thư mục mới
            string folderPath = Path.Combine("C:\\Users\\dante\\Downloads\\", userName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            this.location = folderPath;
            saveToken(credPath, this.location + "\\Token.txt");



            if (File.Exists(Path.Combine(Environment.CurrentDirectory, fileName)))
            {
                // Đọc nội dung của tệp tin
                string[] lines = File.ReadAllLines(Path.Combine(Environment.CurrentDirectory, fileName));

                // Kiểm tra xem tên người dùng đã tồn tại trong tệp tin chưa
                if (Array.IndexOf(lines, userName) != -1)
                {
                    Console.WriteLine("Người dùng đã tồn tại trong tệp tin.");
                }
                else
                {
                    // Ghi thêm tên người dùng vào tệp tin
                    File.AppendAllText(Path.Combine(Environment.CurrentDirectory, fileName), userName + Environment.NewLine);
                    Console.WriteLine("Đã ghi tên người dùng vào tệp tin.");
                }
            }
            else
            {
                // Tạo tệp tin và ghi tên người dùng vào đó
                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, fileName), userName + Environment.NewLine);
                Console.WriteLine("Đã tạo và ghi tên người dùng vào tệp tin.");
            }

            dowloadAllFilesAndFolders(service, this.location);
            return service;
        }
        public DriveService automatic(string userName)
        {
            this.location = $"C:\\Users\\dante\\Downloads\\{userName}";
            string tokenFilePath = $"C:\\Users\\dante\\Downloads\\{userName}\\Token.txt";
            string json = File.ReadAllText("client_secret.json");

            // Chuyển đổi chuỗi JSON thành đối tượng JObject
            JObject jsonObject = JObject.Parse(json);

            // Truy cập vào client_id và client_secret
            string clientId = jsonObject["installed"]["client_id"].ToString(); ;
            string clientSecret = jsonObject["installed"]["client_secret"].ToString();
            try
            {
                // Đọc dữ liệu từ tệp tin
                string tokenData = File.ReadAllText(tokenFilePath);

                // Tách chuỗi thành accessToken và refreshToken
                string[] tokens = tokenData.Split(',');

                // Lấy giá trị accessToken và refreshToken
                string accessToken = tokens[0];
                string refreshToken = tokens[1];

                // Tạo đối tượng UserCredential từ accessToken và refreshToken
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        // Thông tin client secrets của bạn
                        ClientId = clientId,
                        ClientSecret = clientSecret
                    },
                    Scopes = new[] { DriveService.ScopeConstants.Drive },
                    DataStore = new FileDataStore("StoredCredential")
                });

                // Tạo UserCredential từ AccessToken và RefreshToken
                var credential = new UserCredential(flow, $"{userName}", new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });

                // Tạo đối tượng DriveService từ UserCredential
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Drive API Sample"
                });

                // Bây giờ bạn có thể sử dụng service để gọi các phương thức API Google Drive
                // Ví dụ: service.Files.List() để liệt kê các tệp trên Google Drive

                Console.WriteLine("Authentication successful!");
                return service;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Authentication failed: " + ex.Message);
            }
            return null;
        }

        public void createFolder(string folderName, DriveService service, string folderPath, string ParentId)
        {
            if (!isNameExist(Path.GetFileName(folderPath)))
            {
                if (ParentId == null)
                {
                    string Directory = Path.GetDirectoryName(folderPath);
                    string fName = Path.GetFileName(Directory);
                    ParentId = getIdFromFile(fName);
                }
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = Path.GetFileName(folderPath),
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<string>() { ParentId }
                };

                // Tạo request để tạo thư mục mới
                var request = service.Files.Create(fileMetadata);
                request.Fields = "id"; // Chỉ lấy trường 'id' của thư mục được tạo

                // Thực hiện request để tạo thư mục mới
                var folder = request.Execute();
                AddDataTofile(Path.GetFileName(folderPath), folder.Id);
                // In thông tin về thư mục mới được tạo
                Console.WriteLine("Thư mục đã được tạo:");
                Console.WriteLine("ID: " + folder.Id);
                ////////
                if (Directory.Exists(folderPath))
                {
                    // Lấy danh sách tất cả các tệp tin và thư mục con trong thư mục đã cho
                    string[] files = Directory.GetFiles(folderPath);
                    string[] directories = Directory.GetDirectories(folderPath);
                    if (directories.Length > 0)
                    {
                        Console.WriteLine("Các thư mục con:");
                        foreach (string directory in directories)
                        {
                            createFolder(Path.GetFileName(directory), service, directory, folder.Id);
                        }
                    }
                    if (files.Length > 0)
                    {
                        Console.WriteLine("Các tệp tin trong thư mục:");
                        foreach (string file in files)
                        {
                            uploadFile(service, file, folder.Id);
                        }
                    }



                    if (files.Length == 0 && directories.Length == 0)
                    {
                        Console.WriteLine("Thư mục không chứa tệp tin hoặc thư mục con.");
                    }
                }
                else
                {
                    Console.WriteLine("Thư mục " +
                        "không tồn tại.");
                }
                Console.WriteLine("-----Thêm hoàn tất-----");
            }

        }
        ///
        public void DeleteFolderAndContents(DriveService service, string folderId)
        {
            

            // Lấy danh sách tất cả các tệp tin và thư mục con trong thư mục cha
            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents";
            var result = request.Execute();
            var files = result.Files;

            // Xóa tất cả các tệp tin con
            foreach (var file in files)
            {
                if (file.MimeType != "application/vnd.google-apps.folder")
                {
                    service.Files.Delete(file.Id).Execute();
                    Console.WriteLine($"Đã xóa tệp tin: {file.Name}");
                }
                else
                {
                    // Gọi đệ quy để xóa thư mục con
                    DeleteFolderAndContents(service, file.Id);
                }
            }

            // Xóa thư mục cha
            service.Files.Delete(folderId).Execute();
            Console.WriteLine("Đã xóa thư mục: {0}", folderId);
        }
        ///
        public void deleteFile(DriveService service, string fileName, string filePath)
        {
            string fileId = getIdFromFile(fileName);
            if (DeleteData(fileId))
            {
                Console.WriteLine("Cập nhật file thành công");
            }
            // Tạo yêu cầu xóa tệp
            try
            {
                var file = service.Files.Get(fileId).Execute();
                // Tệp tin tồn tại trên Google Drive
                Console.WriteLine("Tệp tin tồn tại trên Google Drive.");

                // Thực hiện xóa tệp tin
                var request = service.Files.Delete(fileId);
                request.Execute();
                Console.WriteLine("Tệp tin đã được xóa thành công.");
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Tệp tin không tồn tại trên Google Drive
                Console.WriteLine("Tệp tin không tồn tại trên Google Drive.");
            }
            listOfFiles.Remove(listOfFiles.Where(x => x.id == fileId).FirstOrDefault());
            Console.WriteLine("Tệp đã được xóa thành công.");
            //UpdateDataFile();

        }
        ///
        public void renameFile(DriveService service, string currentfileName, string newfileName)
        {

            string fileId = getIdFromFile(currentfileName);
            UpdateData(currentfileName, newfileName);
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = newfileName
            };

            // Tạo yêu cầu cập nhật tên
            var request = service.Files.Update(fileMetadata, fileId);
            request.Fields = "id";
            var updatedFile = request.Execute();

            Console.WriteLine("Tên tệp hoặc thư mục đã được cập nhật thành công. ID của tệp hoặc thư mục là: " + updatedFile.Id);
        }

        public void dowloadAllFilesAndFolders(DriveService service, string downloadFolderPath)
        {
            listOfFiles = new List<Container>();
            string rootFolderId = "root";
            // Lấy danh sách tất cả các tệp tin và thư mục trên Google Drive
            var fileListRequest = service.Files.List();
            fileListRequest.Q = $"'{rootFolderId}' in parents and trashed = false";

            var fileList = fileListRequest.Execute();

            // Tải xuống từng mục
            foreach (var file in fileList.Files)
            {
                Container contain = new Container(file.Name, file.Id, file.MimeType, this.userName, this.location);
                listOfFiles.Add(contain);
                if (file.MimeType == "application/vnd.google-apps.folder")
                {

                    // Nếu là thư mục, tải xuống thư mục bằng đệ quy
                    string folderPath = Path.Combine(downloadFolderPath, file.Name);
                    DownloadFolder(service, file.Id, folderPath);
                    Console.WriteLine("Thư mục : " + file.Name + " ID: " + file.Id + " Đã được tải");

                }
                else
                {
                    // Nếu là tệp tin, tải xuống tệp tin
                    string filePath = Path.Combine(downloadFolderPath, file.Name);
                    //DownloadFile(service, file.Id, filePath);
                    File.WriteAllText(filePath, (contain.id + "," + contain.name + "," + contain.owner + "," + contain.root));
                }
            }
            string datapath = downloadFolderPath + "\\data.txt";
            using (StreamWriter writer = new StreamWriter(datapath))
            {
                // Ghi dữ liệu từ danh sách vào tệp tin
                foreach (Container item in listOfFiles)
                {
                    string line = $"{item.id},{item.name}";
                    writer.WriteLine(line);
                }
            }
            Console.WriteLine("Done Dowloading...");

        }
        public void UpdateDataFile()
        {
            string datapath = this.location + "\\data.txt";
            using (StreamWriter writer = new StreamWriter(datapath))
            {
                // Ghi dữ liệu từ danh sách vào tệp tin
                foreach (Container item in listOfFiles)
                {
                    string line = $"{item.id},{item.name}";
                    writer.WriteLine(line);
                }
            }
            Console.WriteLine("Done Updating..");
        }
        public void DownloadFolder(DriveService service, string folderId, string folderPath)
        {
            Directory.CreateDirectory(folderPath);
            var fileListRequest = service.Files.List();
            fileListRequest.Q = $"'{folderId}' in parents";
            fileListRequest.Fields = "files(id, name, mimeType)";
            var fileList = fileListRequest.Execute();

            // Tải xuống từng mục
            foreach (var file in fileList.Files)
            {
                Container contain = new Container(file.Name, file.Id, file.MimeType, this.userName, this.location);
                listOfFiles.Add(contain);
                if (file.MimeType == "application/vnd.google-apps.folder")
                {


                    // Nếu là thư mục, tải xuống thư mục con bằng đệ quy
                    string subfolderPath = Path.Combine(folderPath, file.Name);
                    DownloadFolder(service, file.Id, subfolderPath);
                    Console.WriteLine("Thư mục : " + file.Name + " ID: " + file.Id + " Đã được tải");

                }
                else
                {
                    // Nếu là tệp tin, tải xuống tệp tin
                    string filePath = Path.Combine(folderPath, file.Name);
                    File.WriteAllText(filePath, file.Id);
                    //DownloadFile(service, file.Id, filePath);
                    Console.WriteLine("Tệp " + file.Name + " ID: " + file.Id + " Đã được tải");
                }
            }
        }
        public void DownloadFile(DriveService service, string fileId, string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                service.Files.Get(fileId).Download(stream);
            }

        }
        public void uploadFile(DriveService service, string filePath, string parentFolderId)
        {
            string uploadedFileId = null;
            if (!isNameExist(Path.GetFileName(filePath)))
            {
                if (parentFolderId == null)
                {
                    string Directory = Path.GetDirectoryName(filePath);
                    string folderName = Path.GetFileName(Directory);
                    parentFolderId = getIdFromFile(folderName);
                }

                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                    Parents = new List<string>() { parentFolderId }
                };
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        // Lấy thông tin về tệp tin mà quy trình đang sử dụng
                        string processFilePath = process.MainModule.FileName;

                        // Kiểm tra xem tệp tin có trùng khớp với filePath không
                        if (string.Equals(processFilePath, filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Dừng quy trình đang sử dụng tệp tin
                            process.Kill();
                            Console.WriteLine("Quy trình đã được dừng.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Xử lý ngoại lệ nếu không thể lấy thông tin về quy trình
                        Console.WriteLine($"Lỗi: {ex.Message}");
                    }
                }
                FilesResource.CreateMediaUpload request;
                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open))
                {
                    request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                    request.Fields = "id";
                    request.Upload();
                    var response = request.ResponseBody;

                    // Lấy ID của tệp đã tải lên
                    uploadedFileId = response?.Id;
                    AddDataTofile(fileMetadata.Name, uploadedFileId);


                    //AccessFileContent(service, GetFileById(service,uploadedFileId));
                }
                File.WriteAllText(filePath, (uploadedFileId + "," + Path.GetFileName(filePath) + "," + this.userName + "," + this.location));
                Console.WriteLine("Done Uploading");
            }
        }
        public void readAllFileInfo(string folderPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
            if (directoryInfo.Exists)
            {
                FileInfo[] files = directoryInfo.GetFiles();
                foreach (var file in files)
                {
                    Console.WriteLine(file.Name + " id: " + File.ReadAllText(file.FullName));
                }
            }
        }
        public string readOneFile(string filepath)
        {
            int count = 1;
            foreach (var line in File.ReadLines(filepath))
            {
                return line;
            }
            return null;

        }
        public string getIdFromFile(string Name)
        {
            string filePath = $"{this.location}\\data.txt";
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    Console.WriteLine(parts[0]);
                    if (parts.Length == 2 && parts[0] == Name)
                    {

                        return parts[1];
                    }
                }
            }

            return null;
        }
        public bool isNameExist(string Name)
        {
            string filePath = $"{this.location}\\data.txt";
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && parts[0] == Name)
                    {

                        return true;
                    }
                }
            }
            return false;
        }
        public void AddDataTofile(string name, string id)
        {
            string filePath = $"{this.location}\\data.txt";
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine($"{name},{id}");
            }
        }
        public bool DeleteData(string idToDelete)
        {
            string filePath = $"{this.location}\\data.txt";
            string tempFilePath = Path.GetTempFileName();

            using (StreamReader reader = new StreamReader(filePath))
            using (StreamWriter writer = new StreamWriter(tempFilePath))
            {
                string line;
                bool lineDeleted = false;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && parts[1] == idToDelete)
                    {
                        lineDeleted = true;
                        continue;
                    }

                    writer.WriteLine(line);
                }

                if (!lineDeleted)
                {
                    return false;
                }
            }

            File.Delete(filePath);
            File.Move(tempFilePath, filePath);

            return true;
        }
        public bool UpdateData(string name, string newName)
        {
            string filePath = $"{this.location}\\data.txt";
            string tempFilePath = Path.GetTempFileName();

            using (StreamReader reader = new StreamReader(filePath))
            using (StreamWriter writer = new StreamWriter(tempFilePath))
            {
                string line;
                bool nameUpdated = false;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        line = $"{newName},{parts[1]}";
                        nameUpdated = true;
                    }

                    writer.WriteLine(line);
                }

                if (!nameUpdated)
                {
                    return false;
                }
            }

            File.Delete(filePath);
            File.Move(tempFilePath, filePath);

            return true;
        }
        public async Task<Stream> GetContentStreamAsync(DriveService service, Google.Apis.Drive.v3.Data.File fileEntry)
        {
            //Not every file resources in Google Drive has file content.
            //For example, a folder
            if (fileEntry.Size == null)
            {
                Console.WriteLine(fileEntry.Name + " is not a file. Skipped.");
                Console.WriteLine();
                return (null);
            }

            //Generate URI to file resource
            //"alt=media" indicates downloading file content instead of JSON metadata
            Uri fileUri = new Uri("https://www.googleapis.com/drive/v3/files/" + fileEntry.Id + "?alt=media");

            try
            {
                //Use HTTP client in DriveService to obtain response
                Task<Stream> fileContentStream = service.HttpClient.GetStreamAsync(fileUri);
                Console.WriteLine("Downloading file {0}...", fileEntry.Name);

                return (await fileContentStream);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while downloading file: " + e.Message);
                return (null);
            }
        }
        public Google.Apis.Drive.v3.Data.File GetFileById(DriveService service, string fileId)
        {
            try
            {
                // Gửi yêu cầu lấy thông tin tệp với ID cụ thể
                var request = service.Files.Get(fileId);
                request.Fields = "*"; // Lấy tất cả các trường của tệp

                // Thực hiện yêu cầu và trả về kết quả
                return request.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("Lỗi khi lấy thông tin tệp: " + e.Message);
                return null;
            }
        }
        public async Task AccessFileContent(DriveService service, Google.Apis.Drive.v3.Data.File fileEntry)
        {
            // Gọi phương thức GetContentStreamAsync để tải nội dung của tệp
            Stream fileContentStream = await GetContentStreamAsync(service, fileEntry);

            if (fileContentStream != null)
            {
                try
                {
                    // Sử dụng đối tượng Stream để truy cập vào nội dung của tệp
                    // Ví dụ: Đọc dữ liệu từ Stream
                    using (StreamReader reader = new StreamReader(fileContentStream))
                    {
                        string fileContent = reader.ReadToEnd();
                        Console.WriteLine("Content of {0}:", fileEntry.Name);
                        Console.WriteLine(fileContent);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error while accessing file content: " + e.Message);
                }
                finally
                {
                    // Đảm bảo đóng Stream sau khi sử dụng xong
                    fileContentStream.Close();
                    fileContentStream.Dispose();
                }
            }
        }
        public void LogoutAsync()
        {
            // Xóa thông tin xác thực đã lưu trữ
            UserCredential credential = GetCredential();// Lấy đối tượng UserCredential đã được lưu trữ
            credential.RevokeTokenAsync(CancellationToken.None).Wait();
            credential = null;

            // Xóa các tệp cookie hoặc thông tin xác thực khác liên quan đến đăng nhập
            // Ví dụ: Xóa cookie trong trình duyệt
            // ...
        }
        public void switchFileBetweenDrive(string filePath)
        {
            Console.WriteLine(File.ReadAllText(filePath));
        }
        public void DownloadFile(string fileId, DriveService driveService, string fileType)
        {
            var request = driveService.Files.Get(fileId);
            var stream = new MemoryStream();

            // Tạo đường dẫn cho thư mục "temp"
            string savePath = Path.Combine(Environment.CurrentDirectory, "temp");

            // Tải file từ Google Drive
            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine($"Đang tải... {progress.BytesDownloaded} bytes");
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Tải thành công!");

                            // Kiểm tra và tạo thư mục nếu không tồn tại
                            if (!Directory.Exists(savePath))
                            {
                                Directory.CreateDirectory(savePath);
                            }

                            // Lấy extension từ tên file trên Google Drive
                            // Lấy extension từ URL
                            string extension = fileType;
                            // Tạo tên file tải về
                            string fileName = fileId + "." + extension;

                            // Lưu file vào thư mục cần
                            string filePath = Path.Combine(savePath, fileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            {
                                stream.Seek(0, SeekOrigin.Begin);
                                stream.CopyTo(fileStream);
                            }
                            // Mở file sau khi tải xong
                            OpenFile(filePath);

                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Tải thất bại.");
                            break;
                        }
                }
            };

            request.Download(stream);
        }
        private void OpenFile(string filePath)
        {
            try
            {
                Process.Start(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không thể mở file: " + ex.Message);
            }
        }
        public void UploadFolder(DriveService service, string folderPath, string folderId)
        {
            // Lấy tên thư mục gốc
            string folderName = new DirectoryInfo(folderPath).Name;

            // Tạo metadata cho thư mục
            var folderMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { folderId }
            };

            // Tạo thư mục trên Google Drive
            var request = service.Files.Create(folderMetadata);
            request.Fields = "id";
            var folder = request.Execute();
            var folderIdOnDrive = folder.Id;

            Console.WriteLine($"Đã tạo thư mục: {folderName} - ID: {folderIdOnDrive}");

            // Lấy danh sách tất cả các tệp tin và thư mục con trong thư mục nguồn
            var files = Directory.GetFiles(folderPath);
            var directories = Directory.GetDirectories(folderPath);

            // Upload các tệp tin trong thư mục nguồn
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new List<string> { folderIdOnDrive }
                };

                // Tạo tệp tin trên Google Drive
                using (var stream = new FileStream(file, FileMode.Open))
                {
                    var uploadRequest = service.Files.Create(fileMetadata, stream, GetMimeType(file));
                    uploadRequest.Upload();
                }

                Console.WriteLine($"Đã upload tệp tin: {fileName}");
            }

            // Đệ quy để upload các thư mục con
            foreach (var directory in directories)
            {
                UploadFolder(service, directory, folderIdOnDrive);
            }
        }

        // Hàm lấy kiểu MIME của tệp tin
        public string GetMimeType(string fileName)
        {
            var mimeType = "application/unknown";
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

            using (var registryKey = Registry.ClassesRoot.OpenSubKey(extension))
            {
                if (registryKey != null && registryKey.GetValue("Content Type") != null)
                    mimeType = registryKey.GetValue("Content Type").ToString();
            }

            return mimeType;
        }
        public string GetFileOrFolderId(DriveService service, string fileName)
        {
            // Tìm kiếm tệp tin hoặc thư mục dựa trên tên
            var request = service.Files.List();
            request.Q = $"name = '{fileName}'";
            request.Fields = "files(id)";
            var results = request.Execute();
            var files = results.Files;

            // Trả về ID của tệp tin hoặc thư mục nếu tìm thấy
            if (files != null && files.Count > 0)
            {
                return files[0].Id;
            }

            return null;
        }
        /// <summary>
        /// Tim kiem va sap xep tren Drive
        /// </summary>
        /// <param name="service">Service dung call Drive API</param>
        /// <param name="searchParams">search va sort params</param>
        /// <returns></returns>
        public IList<Google.Apis.Drive.v3.Data.File> SearchFile(DriveService service, SearchFileParams searchParams)
        {
            var request = service.Files.List();
            string queryBuilder;
            //Kieu file can search
            switch (searchParams.FileType)
            {
                case "File":
                    queryBuilder = DriveSearchFileParams.IsNotFolder;
                    break;
                case "Folder":
                    queryBuilder = DriveSearchFileParams.IsFolder;
                    break;
                default:
                    queryBuilder = DriveSearchFileParams.Root;
                    break;
            }
            //search theo filename
            if (!string.IsNullOrEmpty(searchParams.FileName.Trim()))
            {
                queryBuilder = $"{queryBuilder} and {DriveSearchFileParams.NameContains} '{searchParams.FileName}'";
            }
            request.Q = queryBuilder;
            request.Fields = "files(id, name, mimeType, iconLink)";
            //sort
            if(!string.IsNullOrEmpty(searchParams.SortBy))
            {
                if (!string.IsNullOrEmpty(searchParams.SortType))
                    searchParams.SortBy += (" " + searchParams.SortType);
                request.OrderBy = searchParams.SortBy.Trim();
                
            }
            var results = request.Execute();
            var files = results.Files;
            return files;
        }
        public IList<Google.Apis.Drive.v3.Data.File> LoadFilesFromRootFolder(DriveService service)
        {
            var request = service.Files.List();
            request.Q = "'root' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType, iconLink)";
            var results = request.Execute();
            var files = results.Files;
            return files;
        }
        public IList<Google.Apis.Drive.v3.Data.File> GetFilesFromTrash(DriveService driveService)
        {
            // Tạo yêu cầu để lấy danh sách các tệp tin và thư mục trong thùng rác
            var request = driveService.Files.List();
            request.Q = "trashed = true"; // Lọc chỉ lấy các tệp tin và thư mục trong thùng rác
            request.Fields = "files(id, name, mimeType, iconLink)";
            // Thực hiện yêu cầu và lấy danh sách các tệp tin và thư mục
            var results = request.Execute();
            var files = results.Files;
            return files;
        }
        public void MoveFileToTrash(string fileId, DriveService driveService)
        {
            // Tạo đối tượng File để cập nhật trạng thái thùng rác
            var file = new Google.Apis.Drive.v3.Data.File
            {
                Trashed = true
            };

            // Tạo yêu cầu để cập nhật tệp tin và di chuyển vào thùng rác
            var updateRequest = driveService.Files.Update(file, fileId);

            // Thực hiện yêu cầu để di chuyển tệp tin vào thùng rác
            updateRequest.Execute();
        }
        public void RestoreFileFromTrash(string fileId, DriveService driveService)
        {
            // Tạo yêu cầu để khôi phục lại tệp tin hoặc thư mục
            var updateRequest = driveService.Files.Update(new Google.Apis.Drive.v3.Data.File { Trashed = false }, fileId);
            updateRequest.SupportsAllDrives = true;

            // Cập nhật trạng thái trashed thành false để khôi phục lại tệp tin hoặc thư mục
            updateRequest.Execute();
        }

    }
}
