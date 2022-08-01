using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using QandA.Controllers;
using QandA.Data;
using QandA.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace QandA.Tests
{
    public class QuestionsControllerShould
    {
        [Fact]
        public void ReturnAllQuestionsWhenNoParameters()
        {
            var mockQuestions = new List<QuestionGetManyResponse>();
            for (int i = 1; i <= 10; i++)
            {
                mockQuestions.Add(new QuestionGetManyResponse
                {
                    QuestionId = 1,
                    Title = $"Test title {i}",
                    Content = $"Test content {i}",
                    UserName = "User1",
                    Answers = new List<AnswerGetResponse>()
                });
            }

            var mockDataRepository = new Mock<IDataRepository>();
            mockDataRepository.Setup(repo => repo.GetQuestions())
                .Returns(() => mockQuestions.AsEnumerable());

            var mockConfigurationRoot = new Mock<IConfigurationRoot>();
            mockConfigurationRoot.SetupGet(config => config[It.IsAny<string>()]).Returns("somesetting");
            var questionsController = new QuestionsController(mockDataRepository.Object, null, null, mockConfigurationRoot.Object);
            var result = questionsController.GetQuestions(null, false);

            Assert.Equal(10, result.Count());
            mockDataRepository.Verify(mock => mock.GetQuestions(), Times.Once());
        }

        [Fact]
        public void ReturnTheRightQuestionsWhenHaveSearchParameter()
        {
            var mockQuestions = new List<QuestionGetManyResponse>();
            mockQuestions.Add(new QuestionGetManyResponse
            {
                QuestionId = 1,
                Title = "Test",
                Content = "Test content",
                UserName = "User1",
                Answers = new List<AnswerGetResponse>()
            });
            var mockDataRepository = new Mock<IDataRepository>();

            mockDataRepository.Setup(repo => repo.GetQuestionsBySearchWithPaging("Test", 1, 20))
                .Returns(() => mockQuestions.AsEnumerable());

            var mockConfigurationRoot = new Mock<IConfigurationRoot>();
            mockConfigurationRoot.SetupGet(config => config[It.IsAny<string>()]).Returns("somesetting");

            var questionsController = new QuestionsController(mockDataRepository.Object, null, null, mockConfigurationRoot.Object);

            var result = questionsController.GetQuestions("Test", false);

            Assert.Single(result);
            mockDataRepository.Verify(mock => mock.GetQuestionsBySearchWithPaging("Test", 1, 20), Times.Once());
        }

        [Fact]
        public void Return404WhenQuestionNotFound()
        {
            var mockDataRepository = new Mock<IDataRepository>();

            mockDataRepository.Setup(repo => repo.GetQuestion(1)).Returns(() => default(QuestionGetSingleResponse));

            var mockQuestionCache = new Mock<IQuestionCache>();

            mockQuestionCache.Setup(cache => cache.Get(1))
                .Returns(() => null);

            var mockConfigurationRoot = new Mock<IConfigurationRoot>();

            mockConfigurationRoot.SetupGet(config => config[It.IsAny<string>()]).Returns("somesetting");

            var questionsController = new QuestionsController(mockDataRepository.Object, mockQuestionCache.Object, null, mockConfigurationRoot.Object);

            var result = questionsController.GetQuestion(1);
            var actionResult = Assert.IsType<ActionResult<QuestionGetSingleResponse>>(result);

            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public void ReturnQuestionWhenQuestionIsFound()
        {
            var mockQuestion = new QuestionGetSingleResponse
            {
                QuestionId = 1,
                Title = "test"
            };
            var mockDataRepository = new Mock<IDataRepository>();

            mockDataRepository.Setup(repo => repo.GetQuestion(1)).Returns(() => mockQuestion);

            var mockQuestionCache = new Mock<IQuestionCache>();

            mockQuestionCache.Setup(cache => cache.Get(1)).Returns(() => mockQuestion);

            var mockConfigurationRoot = new Mock<IConfigurationRoot>();

            mockConfigurationRoot.SetupGet(config => config[It.IsAny<string>()]).Returns("somesetting ");
        
            var questionsController = new QuestionsController(mockDataRepository.Object, mockQuestionCache.Object, null, mockConfigurationRoot.Object);
            
            var result = questionsController.GetQuestion(1);

            var actionResult = Assert.IsType<ActionResult<QuestionGetSingleResponse>>(result);
            var questionResult = Assert.IsType<QuestionGetSingleResponse>(actionResult.Value); Assert.Equal(1, questionResult.QuestionId);
        }
    }
}