#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Alejof.Notes.Storage;
using AutoMapper;
using MediatR;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace Alejof.Notes.Handlers
{
    public class CreateMedia
    {
        public class Request : BaseRequest, IRequest<Response>, IAuditableRequest
        {
            public string Name { get; set; } = string.Empty;
            public Stream? Content { get; set; }

            public object AuditRecord => new
            {
                this.Name,
            };
        }

        public class Response : ActionResponse
        {
            public string Path { get; set; } = string.Empty;
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private readonly CloudTable _mediaTable;
            private readonly CloudBlobContainer _container;

            public Handler(
                CloudTableClient tableClient,
                CloudBlobClient blobClient)
            {
                this._mediaTable = tableClient.GetTableReference(MediaEntity.TableName);
                this._container = blobClient.GetContainerReference(Blobs.ContentContainerName);
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                var blobName = GetBlobName(request.TenantId, request.Name);
                var blob = _container.GetBlockBlobReference(blobName);
                var entity = new MediaEntity
                {
                    PartitionKey = request.TenantId,
                    RowKey = Guid.NewGuid().ToString(),
                };
                    
                entity.Name = request.Name;
                entity.BlobUri = blob.Uri.ToString();

                if (request.Content == null)
                    return new Response { Message = "Empty content" };

                var uploadTask = blob.UploadFromStreamAsync(request.Content);
                var resultTask = _mediaTable.InsertAsync(entity);

                await Task.WhenAll(uploadTask, resultTask);
                if (!resultTask.Result)
                    return new Response { Message = "InsertMedia failed" };

                return new Response
                {
                    Success = true,
                    Path = $"{Blobs.ContentContainerName}/{blobName}",
                };
            }

            private string GetBlobName(string tenantId, string name) => $"{tenantId}/{name}";
        }
    }
}
