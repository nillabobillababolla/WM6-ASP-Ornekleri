﻿using Admin.Models.IdentityModels;
using Admin.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using static Admin.BLL.Identity.MembershipTools;

namespace Admin.Web.UI.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            if (HttpContext.GetOwinContext().Authentication.User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }
            try
            {
                var userStore = NewUserStore();
                var userManager = NewUserManager();

                var rm = model.RegisterViewModel;

                var user = await userManager.FindByNameAsync(rm.UserName);
                if (user != null)
                {
                    ModelState.AddModelError("UserName", "Bu kullanıcı adı daha önceden alınmıştır");
                    return View("Index", model);
                }

                var newUser = new User()
                {
                    UserName = rm.UserName,
                    Email = rm.Email,
                    Name = rm.Name,
                    Surname = rm.Surname
                };
                var result = await userManager.CreateAsync(newUser, rm.Password);
                if (result.Succeeded)
                {
                    if (userStore.Users.Count() == 1)
                    {
                        await userManager.AddToRoleAsync(newUser.Id, "Admin");
                    }
                    else
                    {
                        await userManager.AddToRoleAsync(newUser.Id, "User");
                    }
                    //todo kullanıcıya mail gönderilsin
                }
                else
                {
                    var err = "";
                    foreach (var resultError in result.Errors)
                    {
                        err += resultError + " ";
                    }
                    ModelState.AddModelError("", err);
                    return View("Index", model);
                }

                TempData["Message"] = "Kaydınız alınlıştır. Lütfen giriş yapınız";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Model"] = new ErrorViewModel()
                {
                    Text = $"Bir hata oluştu {ex.Message}",
                    ActionName = "Index",
                    ControllerName = "Account",
                    ErrorCode = 500
                };
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(RegisterLoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View("Index", model);

                var userManager = NewUserManager();
                var user = await userManager.FindAsync(model.LoginViewModel.UserName, model.LoginViewModel.Password);
                if (user == null)
                {
                    ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı");
                    return View("Index", model);
                }
                var authManager = HttpContext.GetOwinContext().Authentication;
                var userIdentity =
                    await userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                authManager.SignIn(new AuthenticationProperties()
                {
                    IsPersistent = model.LoginViewModel.RememberMe
                }, userIdentity);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Model"] = new ErrorViewModel()
                {
                    Text = $"Bir hata oluştu {ex.Message}",
                    ActionName = "Index",
                    ControllerName = "Account",
                    ErrorCode = 500
                };
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public ActionResult Logout()
        {
            var authManager = HttpContext.GetOwinContext().Authentication;
            authManager.SignOut();
            return RedirectToAction("Index", "Account");
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> UserProfile()
        {
            var id = HttpContext.GetOwinContext().Authentication.User.Identity.GetUserId();

            try
            {
                var userManager = NewUserManager();
                var user = await userManager.FindByIdAsync(id);

                var data = new ProfilePasswordViewModel()
                {
                    UserProfileViewModel = new UserProfileViewModel()
                    {
                        Email = user.Email,
                        Id = user.Id,
                        Name = user.Name,
                        Surname = user.Surname,
                        PhoneNumber = user.PhoneNumber,
                        UserName = user.UserName
                    }
                };
                return View(data);
            }
            catch (Exception ex)
            {
                TempData["Model"] = new ErrorViewModel()
                {
                    Text = $"Bir hata oluştu {ex.Message}",
                    ActionName = "UserProfile",
                    ControllerName = "Account",
                    ErrorCode = 500
                };
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> UpdateProfile(ProfilePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            try
            {
                var userManager = NewUserManager();
                var user = await userManager.FindByIdAsync(model.UserProfileViewModel.Id);

                user.Name = model.UserProfileViewModel.Name;
                user.Surname = model.UserProfileViewModel.Surname;
                if (user.Email != model.UserProfileViewModel.Email)
                {
                    //todo tekrardan aktivasyon gönderilmeli ve kullanıcı rolü aktif olamayan bir kullanıcı rolüne atanmalı!
                }
                user.Email = model.UserProfileViewModel.Email;
                user.PhoneNumber = model.UserProfileViewModel.PhoneNumber;
                await userManager.UpdateAsync(user);
                TempData["Message"] = "Profiliniz güncellenmiştir";
                return RedirectToAction("UserProfile");
            }
            catch (Exception ex)
            {
                TempData["Model"] = new ErrorViewModel()
                {
                    Text = $"Bir hata oluştu {ex.Message}",
                    ActionName = "UserProfile",
                    ControllerName = "Account",
                    ErrorCode = 500
                };
                return RedirectToAction("Error", "Home");
            }
        }
    }
}