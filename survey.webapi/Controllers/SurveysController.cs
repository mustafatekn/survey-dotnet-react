using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using survey.business.Abstract;
using survey.entity;
using survey.webapi.DTO;

namespace survey.webapi.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("[Controller]")]
    public class SurveysController : ControllerBase
    {
        private ISurveyService _surveyService;
        private IChoiceService _choiceService;
        private ICategoryService _categoryService;
        private IAuthService _authService;
        public SurveysController(ISurveyService surveyService, IChoiceService choiceService, ICategoryService categoryService, IAuthService authService)
        {
            _surveyService = surveyService;
            _choiceService = choiceService;
            _categoryService = categoryService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSurveys()
        {
            var surveys = await _surveyService.GetSurveysWithAllData();
            if (surveys == null) return NotFound();
            return Ok(surveys);

        }

        [HttpGet]
        [Route("discover/category")]
        public async Task<IActionResult> GetDiscoverSurveysByCategory(int categoryId)
        {
            var surveys = await _surveyService.GetDiscoverSurveysByCategory(categoryId);
            if (surveys == null) return NotFound(categoryId);
            return StatusCode(200, surveys);
        }

        [HttpGet]
        [Route("discover")]
        public async Task<IActionResult> GetDiscoverSurveys()
        {
            var surveys = await _surveyService.GetDiscoverSurveys();
            if (surveys == null) return NotFound();
            return StatusCode(200, surveys);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyDto createSurveyDto)
        {
            var category = new Category();
            var user = new User();
            if (createSurveyDto.CategoryId < 1 && createSurveyDto.Description == null
            && createSurveyDto.Question == null && createSurveyDto.ChoiceNames == null) return BadRequest();

            category = await _categoryService.GetById(createSurveyDto.CategoryId);
            if (category == null) return NotFound();

            user = await _authService.GetById(createSurveyDto.UserId);
            if (user == null) return NotFound();

            var survey = new Survey
            {
                Category = category,
                Question = createSurveyDto.Question,
                CreatedAt = createSurveyDto.CreatedAt,
                Description = createSurveyDto.Description,
                ImageUrl = createSurveyDto.ImageUrl,
                Url = CreateUrl(createSurveyDto.Question),
                User = user
            };

            var createdSurvey = await _surveyService.Create(survey);
            foreach (var choiceName in createSurveyDto.ChoiceNames)
            {
                var choice = new Choice
                {
                    Name = choiceName,
                    SurveyId = createdSurvey.Id
                };
                await _choiceService.Create(choice);
            };
            return StatusCode(201, SurveyToDto(survey));
        }
        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            if (id < 1) return BadRequest();
            var survey = await _surveyService.GetById(id);
            if (survey == null) return NotFound();
            var deletedSurvey = await _surveyService.Delete(survey);
            return StatusCode(200, deletedSurvey);
        }

        private static string CreateUrl(string question)
        {
            string[] words = question.Split(' ');
            var url = "";
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i] != words[words.Length - 1]) url += words[i] + "-";
                url += words[i];
            }
            return url;
        }
        private static SurveyToReturnDto SurveyToDto(Survey survey)
        {
            return new SurveyToReturnDto
            {
                Id = survey.Id,
                Choices = survey.Choices,
                Votes = survey.Votes,
                Category = survey.Category,
                CreatedAt = survey.CreatedAt,
                Question = survey.Question,
                Url = survey.Url
            };
        }
    }
}