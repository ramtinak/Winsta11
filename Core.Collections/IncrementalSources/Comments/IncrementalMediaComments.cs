﻿using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using Microsoft.Toolkit.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinstaCore;

namespace Core.Collections.IncrementalSources.Comments
{
    public class IncrementalMediaComments : IIncrementalSource<InstaComment>
    {
        PaginationParameters Pagination { get; set; }

        public string MediaId { get; }
        public string TargetCommentId { get; }

        public IncrementalMediaComments(string mediaId, string targetCommentId = "")
        {
            MediaId = mediaId;
            TargetCommentId = targetCommentId;
            Pagination = PaginationParameters.MaxPagesToLoad(1);
        }

        bool HasMoreAvailable = true;
        public async Task<IEnumerable<InstaComment>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (!HasMoreAvailable) return null;
            using (IInstaApi Api = AppCore.Container.GetService<IInstaApi>())
            {
                var result = await Api.CommentProcessor.GetMediaCommentsAsync(MediaId, Pagination,
                                   cancellationToken: cancellationToken,
                                   targetCommentId: TargetCommentId);

                if (!result.Succeeded && result.Info.Exception is not TaskCanceledException)
                    throw result.Info.Exception;

                HasMoreAvailable = result.Value.MoreHeadLoadAvailable;
                
                return result.Value.Comments;
            }
        }
    }
}
