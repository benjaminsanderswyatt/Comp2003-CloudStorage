﻿using Microsoft.AspNetCore.Mvc;
using StorageController.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using StorageController.Data.Models;
using static StorageController.Controllers.v2.FileRenameController;

namespace StorageController.Controllers.v2
{

    [ApiController]
    public class FileMoveController : Controller
    {

        public struct MoveData
        {

            public int Location { get; set; }

        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Route("/v2/file/move/{id}")]
        public async Task<string> MoveFileController([FromBody] MoveData data, [FromRoute] int id)
        {

            string? auth = Request.Headers.Authorization.FirstOrDefault();

            if (auth == null || !auth.StartsWith("Bearer "))
                return await new Response<string>(false, "Authorization Header missing or in wrong format.").Serialize();

            string token = auth.Substring("Bearer ".Length).Trim();

            Response<string> authResponse = await AuthManager.AuthorizeUser(token);

            if (!authResponse.Success)
                return await authResponse.Serialize();

            int userID = int.Parse(authResponse.Message);

            DataHandler db = new();

            UserFile? fileLink = db.UserFiles.Where(fileLink => fileLink.FileID == id && fileLink.UserID == userID).FirstOrDefault();

            if (fileLink == null)
                return await new Response<string>(false, "File link not found").Serialize();

            FileData? fileData = db.Files.Where(file => file.FileID == id).FirstOrDefault();

            if (fileData == null)
                return await new Response<string>(false, "File not found").Serialize();

            fileData.FolderID = data.Location;
            await db.SaveChangesAsync();

            return await new Response<string>(true, "File location changed successfully").Serialize();

        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Route("/v2/folder/move/{id}")]
        public async Task<string> RenameFolderController([FromBody] MoveData data, [FromRoute] int id)
        {

            string? auth = Request.Headers.Authorization.FirstOrDefault();

            if (auth == null || !auth.StartsWith("Bearer "))
                return await new Response<string>(false, "Authorization Header missing or in wrong format.").Serialize();

            string token = auth.Substring("Bearer ".Length).Trim();

            Response<string> authResponse = await AuthManager.AuthorizeUser(token);

            if (!authResponse.Success)
                return await authResponse.Serialize();

            int userID = int.Parse(authResponse.Message);

            DataHandler db = new();

            UserFolder? folderLink = db.UserFolders.Where(folderLink => folderLink.FolderID == id && folderLink.UserID == userID).FirstOrDefault();

            if (folderLink == null)
                return await new Response<string>(false, "Folder link not found").Serialize();

            Folder? folderData = db.Folders.Where(folder => folder.FolderID == id).FirstOrDefault();

            if (folderData == null)
                return await new Response<string>(false, "Folder not found").Serialize();

            folderData.ParentID = data.Location;
            await db.SaveChangesAsync();

            return await new Response<string>(true, "Folder location changed successfully").Serialize();

        }
    }
}
