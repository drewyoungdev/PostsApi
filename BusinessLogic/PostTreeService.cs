using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostsApi.BusinessLogic.Interfaces;
using PostsApi.Models;
using PostsApi.Repositories.Interfaces;

namespace PostsApi.BusinessLogic
{
    public class PostTreeService : IPostTreeService
    {
        private readonly IPostsRepository postsRepository;

        public PostTreeService(IPostsRepository postsRepository)
        {
            this.postsRepository = postsRepository;
        }

        public async Task<List<Post>> LoadMainFeed()
        {
            // return top x posts (filter further by date range? e.g. last two days)
            var mainFeed = await this.postsRepository.GetMainFeed(2);

            // TODO: start task to append main post data (same functionality as LoadMainPost but for many ids)

            return mainFeed;
        }

        public async Task<List<Post>> LoadRootPostWithReplies(int rootPostId)
        {
            // return top 20 comments for each depth
            var flatPostTree = await this.postsRepository.GetRootPostWithReplies(rootPostId);

            // extract main post (no parent id)
            var rootPost = flatPostTree.Where(x => x.ParentId == 0).FirstOrDefault();

            if (rootPost == null) return null;

            // remove main post from post tree building logic (ensure order is not messed up)
            var repliesToRootPost = flatPostTree.Where(x => x.Id != rootPost.Id).ToList();

            // TODO: start task to append main post data (link_url (images), subreddit, subreddit info, up/down percentage)

            // start task to build tree. main post was depth 0 and replies start at 1.
            var repliesTree = BuildTree(repliesToRootPost);

            // combine data sets
            var postTree = new List<Post>(repliesTree.Count + 1);
            postTree.Add(rootPost);
            postTree.AddRange(repliesTree);

            return postTree;
        }

        public async Task<List<Post>> LoadReplies(int parentId)
        {
            // return next 5 replies, then one level in with one additional reply
            var flatPostTree = await this.postsRepository.GetReplies(parentId, 5, 1, 1);

            return BuildTree(flatPostTree);
        }

        // Depends on posts being ordered. This ensures lookup always contains parent before child searches for parent.
        // Foreach loop guarentees items are re-added to new tree structure in order they come from db
        private List<Post> BuildTree(List<Post> posts)
        {
            var lookup = new Dictionary<int, Post>();
            var rootPosts = new List<Post>();

            foreach (var reply in posts)
            {
                lookup.Add(reply.Id, reply);

                if (reply.Depth == 0)
                {
                    rootPosts.Add(reply);
                }
                else
                {
                    var parentPost = lookup[reply.ParentId];
                    parentPost.Replies.Add(reply);
                }
            }

            return rootPosts;
        }
    }
}
