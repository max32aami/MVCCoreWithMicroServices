using Azure;
using Mango.MessageBus;
using Mango.Services.AuthAPI.Models.DTO;
using Mango.Services.AuthAPI.Services.Iservices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Mango.Services.AuthAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthAPIController : ControllerBase
    {
        public readonly IAuthService _authService;
        protected ResponseDto _response;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        public AuthAPIController(IAuthService authService, IMessageBus messageBus, IConfiguration configuration)
        {

            _authService = authService;
            _configuration = configuration;
            _messageBus = messageBus;
            _response = new();
        }   

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationRequestsDto model)
        {
            var result = await _authService.Register(model);
            if(!string.IsNullOrEmpty(result))
            {
                _response.IsSucess = false;
                _response.Message = result;
                return BadRequest(_response);
               
            }
            else
            {
                await _messageBus.PublishMessage(model.Email, _configuration.GetValue<string>("TopicAndQueueNames:RegisterUserQueue"));
                return Ok(_response);
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            var loginResponse = await _authService.Login(model);
            if(loginResponse.User == null) 
            {
               _response.IsSucess = false;
                _response.Message = "Wrong Credntials";
                return BadRequest(_response);
            }
            else
            {
                _response.Result = loginResponse;
                _response.IsSucess = true;
                _response.Message = "Login Successful";
                return Ok(_response);
            }
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole([FromBody] RegistrationRequestsDto model)
        {
            var loginResponse = await _authService.AssignRole(model.Email,model.Role.ToUpper());
            if (!loginResponse)
            {
                _response.IsSucess = false;
                _response.Message = "Error Encountered, Wrong data";
                return BadRequest(_response);
            }
            else
            {
                _response.Result = loginResponse;
                _response.IsSucess = true;
                _response.Message = "Role Added Successfully";
                return Ok(_response);
            }
        }

    }
}
