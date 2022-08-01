using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http;
using System.Text.Json;
using QandA.Data;
using QandA.Models;

namespace QandA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Route to api/questions
    public class QuestionsController : ControllerBase
    {
        // the readonly keyword to make sure the variable's reference doesn't change outside the constructor
        private readonly IDataRepository _dataRepository;
        private readonly IQuestionCache _cache;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _auth0UserInfo;
        public QuestionsController(IDataRepository dataRepository, IQuestionCache questionCache, IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _dataRepository = dataRepository;
            _cache = questionCache;        
            _clientFactory = clientFactory;
            _auth0UserInfo = $"{configuration["Auth0:Authority"]}userinfo";
        }

        private async Task<string> GetUserName()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, _auth0UserInfo);
            request.Headers.Add("Authorization", Request.Headers["Authorization"].First());
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
                return user.Name;
            }
            else return "";
        }

        #region GET

        [AllowAnonymous]
        [HttpGet]
        public IEnumerable<QuestionGetManyResponse> GetQuestions(string search, bool includeAnswers, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrEmpty(search))
            {
                if (includeAnswers)
                    return _dataRepository.GetQuestionsWithAnswers();
                else
                    return _dataRepository.GetQuestions();
            }
            else
                return _dataRepository.GetQuestionsBySearchWithPaging(search, page, pageSize);
        }

        //[HttpGet("unanswered")]
        //public IEnumerable<QuestionGetManyResponse> GetUnansweredQuestions()
        //{
        //    return _dataRepository.GetUnansweredQuestions();
        //}
        [HttpGet("unanswered")]
        public async Task<IEnumerable<QuestionGetManyResponse>> GetUnansweredQuestions()
        {
            return await _dataRepository.GetUnansweredQuestionsAsync();
        }

        [HttpGet("{questionId}")]
        public ActionResult<QuestionGetSingleResponse> GetQuestion(int questionId)
        {
            //var question = _dataRepository.GetQuestion(questionId);
            var question = _cache.Get(questionId);
            if (question == null)
            {
                question = _dataRepository.GetQuestion(questionId);
                if (question == null)
                    return NotFound();
                _cache.Set(question);
            }
            return question;
        }

        #endregion GET

        #region POST

        [HttpPost]
        public async Task<ActionResult<QuestionGetSingleResponse>> PostQuestionAsync([FromBody] QuestionPostRequest questionPostRequest)
        {
            var savedQuestion = _dataRepository.PostQuestion(new QuestionPostFullRequest
            {
                Title = questionPostRequest.Title,
                Content = questionPostRequest.Content,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserName = await GetUserName(),
                Created = DateTime.UtcNow
            });
            return CreatedAtAction(nameof(GetQuestion),
            new { questionId = savedQuestion.QuestionId },
            savedQuestion);
        }

        [HttpPost("answer")]
        public async Task<ActionResult<AnswerGetResponse>> PostAnswerAsync(AnswerPostRequest answerPostRequest)
        {
            var questionExists = _dataRepository.QuestionExists(answerPostRequest.QuestionId.Value);

            if (!questionExists)
                return NotFound();

            var savedAnswer = _dataRepository.PostAnswer(new AnswerPostFullRequest
            {
                QuestionId = answerPostRequest.QuestionId.Value,
                Content = answerPostRequest.Content,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier).Value,
                UserName = await GetUserName(),
                Created = DateTime.UtcNow
            });
            _cache.Remove(answerPostRequest.QuestionId.Value);
            return savedAnswer;
        }
        #endregion POST

        #region PUT

        [Authorize(Policy = "MustBeQuestionAuthor")]
        [HttpPut("{questionId}")]
        public ActionResult<QuestionGetSingleResponse> PutQuestion(int questionId, QuestionPutRequest questionPutRequest)
        {
            var question = _dataRepository.GetQuestion(questionId);

            if (question == null)
                return NotFound();

            questionPutRequest.Title = string.IsNullOrEmpty(questionPutRequest.Title) ? question.Title : questionPutRequest.Title;
            questionPutRequest.Content = string.IsNullOrEmpty(questionPutRequest.Content) ? question.Content : questionPutRequest.Content;

            var savedQuestion = _dataRepository.PutQuestion(questionId, questionPutRequest);
            _cache.Remove(savedQuestion.QuestionId);
            return savedQuestion;
        }

        #endregion PUT

        #region DELETE

        [Authorize(Policy = "MustBeQuestionAuthor")]
        [HttpDelete("{questionId}")]
        public ActionResult DeleteQuestion(int questionId)
        {
            var question = _dataRepository.GetQuestion(questionId);
            if (question == null)
                return NotFound();

            _dataRepository.DeleteQuestion(questionId);
            _cache.Remove(questionId);
            // HTTP status code 204 if the deletion is successful.
            return NoContent();
        }

        #endregion DELETE
    }
}
