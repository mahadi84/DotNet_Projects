using CBS.Application.DTO;
using CBS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CBS.Web.Controllers
{
    public class LoginController : Controller
    {

        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditService;

        
        
        public LoginController(IAuthService authService, IAuditLogService auditService)
        {
            _authService = authService;
            _auditService = auditService;
        }

        
        
        
        
        
        
        [HttpGet]
        public IActionResult Login() => View();









        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDTO dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var result = await _authService.LoginAsync(dto);

            if (result.IsSuccess)
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", result.Message);
            return View(dto);
        }






        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }
    }







}
