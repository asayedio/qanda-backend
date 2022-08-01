using Microsoft.Extensions.Caching.Memory;
using QandA.Models;
namespace QandA.Data
{
    public class QuestionCache : IQuestionCache
    {
        private MemoryCache _cache { get; set; }

        // create a memory cache
        public QuestionCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        }
        
        private string GetCacheKey(int questionId) => $"Question-{questionId}";

        // method to get a cached question.
        public QuestionGetSingleResponse Get(int questionId)
        {
            QuestionGetSingleResponse question;
            _cache.TryGetValue(GetCacheKey(questionId), out question);
            return question;
        }

        // method to add a cached question
        public void Set(QuestionGetSingleResponse question)
        {
            /* we specify the size of the question in the options when setting the cache value. 
             * This ties in with the size limit we set on the cache so that the cache will start to 
             * remove questions from the cache when there are 100 questions in it.
             */
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSize(1);
            _cache.Set(GetCacheKey(question.QuestionId), question, cacheEntryOptions);
        }

        // method to remove a cached question
        public void Remove(int questionId)
        {
            _cache.Remove(GetCacheKey(questionId));
        }
    }
}