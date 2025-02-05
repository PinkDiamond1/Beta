﻿using Auctus.DataAccessInterfaces.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Auctus.DataAccessMock.Storage
{
    public class AzureStorageResource : IAzureStorageResource
    {
        public Task<bool> DeleteFileAsync(string containerName, string fileName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UploadFileFromBytesAsync(string containerName, string fileName, byte[] file, string contentType = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UploadFileFromUrlAsync(string containerName, string fileName, string url)
        {
            throw new NotImplementedException();
        }
    }
}
