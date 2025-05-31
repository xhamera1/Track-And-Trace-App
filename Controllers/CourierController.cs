// File: Controllers/CourierController.cs
using _10.Attributes; // For PackageAccessAuthorizeAttribute
using _10.Models; // For Package, User, StatusDefinition, CourierUpdatePackageStatusViewModel
using _10.Services; // For authorization service
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace _10.Controllers
{
    /// <summary>
    /// Controller for courier-specific operations with comprehensive package access protection.
    ///
    /// SECURITY FEATURES:
    /// - All actions require Courier or Admin role via [Authorize(Roles = "Courier,Admin")]
    /// - Package-specific actions (PackageDetails, UpdateStatus) use PackageAccessAuthorizeAttribute
    ///   to ensure only assigned couriers and admins can access specific packages
    /// - The PackageAccessAuthorizeAttribute performs authorization at the action level and
    ///   pre-loads authorized packages for optimal performance
    /// - Unauthorized access attempts are logged and redirected appropriately
    ///
    /// AUTHORIZATION FLOW:
    /// 1. User must be authenticated and have Courier or Admin role
    /// 2. For package-specific actions, PackageAccessAuthorizeAttribute verifies:
    ///    - Admin users: Full access to all packages
    ///    - Courier users: Access only to packages assigned to them (AssignedCourierId matches user ID)
    /// 3. Authorized packages are pre-loaded and cached in HttpContext.Items["AuthorizedPackage"]
    /// 4. Action methods use the pre-authorized package for optimal performance and security
    /// </summary>
    [Authorize(Roles = "Courier,Admin")]
    public class CourierController : Controller
    {
        private readonly ILogger<CourierController> _logger;
        private readonly ICourierBusinessService _courierBusinessService;

        public CourierController(
            ILogger<CourierController> logger,
            ICourierBusinessService courierBusinessService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _courierBusinessService = courierBusinessService ?? throw new ArgumentNullException(nameof(courierBusinessService));
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            _logger.LogWarning("Could not determine the ID of the logged-in user. User.Identity.IsAuthenticated: {isAuthenticated}, Claims: {claims}",
                User.Identity?.IsAuthenticated,
                string.Join(",", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            throw new InvalidOperationException("Could not determine the ID of the logged-in user. Ensure the authentication mechanism correctly sets NameIdentifier.");
        }

        private UserRole GetCurrentUserRole()
        {
            var roleString = User.FindFirstValue(ClaimTypes.Role);
            if (Enum.TryParse<UserRole>(roleString, out var userRole))
            {
                return userRole;
            }
            _logger.LogWarning("Could not determine the role of the logged-in user. Role claim: {role}", roleString);
            throw new InvalidOperationException("Could not determine the role of the logged-in user. Ensure the authentication mechanism correctly sets the role claim.");
        }

        // GET: /Courier/ActivePackages (or /Courier, /Courier/Dashboard)
        [HttpGet]
        [Route("Courier")]
        [Route("Courier/Dashboard")]
        [Route("Courier/ActivePackages")]
        public async Task<IActionResult> ActivePackages()
        {
            try
            {
                var courierId = GetCurrentUserId();
                var result = await _courierBusinessService.GetActivePackagesAsync(courierId);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Error in ActivePackages: {ErrorMessage}", result.ErrorMessage);
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                ViewData["Title"] = "Active Packages for Delivery";
                return View("PackageList", result.Data);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in ActivePackages while fetching courier ID.");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ActivePackages.");
                TempData["ErrorMessage"] = "An unexpected error occurred while fetching active packages.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Courier/DeliveredPackages
        [HttpGet]
        public async Task<IActionResult> DeliveredPackages()
        {
            try
            {
                var courierId = GetCurrentUserId();
                var result = await _courierBusinessService.GetDeliveredPackagesAsync(courierId);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Error in DeliveredPackages: {ErrorMessage}", result.ErrorMessage);
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                ViewData["Title"] = "Delivered Packages";
                return View("PackageList", result.Data);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in DeliveredPackages while fetching courier ID.");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeliveredPackages.");
                TempData["ErrorMessage"] = "An unexpected error occurred while fetching delivered packages.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Courier/AllMyPackages
        [HttpGet]
        public async Task<IActionResult> AllMyPackages()
        {
            try
            {
                var courierId = GetCurrentUserId();
                var result = await _courierBusinessService.GetAllAssignedPackagesAsync(courierId);

                if (!result.IsSuccess)
                {
                    _logger.LogError("Error in AllMyPackages: {ErrorMessage}", result.ErrorMessage);
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                ViewData["Title"] = "All My Assigned Packages";
                return View("PackageList", result.Data);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in AllMyPackages while fetching courier ID.");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AllMyPackages.");
                TempData["ErrorMessage"] = "An unexpected error occurred while fetching all assigned packages.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Displays detailed information about a specific package.
        ///
        /// AUTHORIZATION: Only assigned couriers and admins can access package details.
        /// The PackageAccessAuthorizeAttribute ensures proper authorization and pre-loads the package.
        /// </summary>
        /// <param name="id">Package ID to display details for</param>
        /// <returns>Package details view or appropriate error response</returns>
        // GET: /Courier/PackageDetails/5
        [HttpGet]
        [PackageAccessAuthorize(PackageAccessType.AssignedCourier)]
        public async Task<IActionResult> PackageDetails(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("PackageDetails called without an ID.");
                return NotFound("Package ID not provided.");
            }

            try
            {
                // The package has already been authorized and loaded by the PackageAccessAuthorizeAttribute
                var authorizedPackage = HttpContext.Items["AuthorizedPackage"] as Package;

                if (authorizedPackage != null)
                {
                    _logger.LogInformation("Displaying package details for package {PackageId} to user {UserId}",
                        authorizedPackage.PackageId, GetCurrentUserId());
                    return View(authorizedPackage);
                }

                // Fallback to the original logic if the attribute didn't set the package
                var courierId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();
                var result = await _courierBusinessService.GetPackageDetailsAsync(id.Value, courierId, userRole);

                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        return NotFound();
                    }

                    if (result.ErrorCode == "UNAUTHORIZED")
                    {
                        TempData["ErrorMessage"] = "You are not authorized to access this package.";
                        return RedirectToAction(nameof(ActivePackages));
                    }

                    _logger.LogError("Error in PackageDetails: {ErrorMessage}", result.ErrorMessage);
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                return View(result.Data);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in PackageDetails while fetching courier ID for package {PackageId}.", id.Value);
                TempData["ErrorMessage"] = "Authentication error occurred. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PackageDetails for package {PackageId}.", id.Value);
                TempData["ErrorMessage"] = "An unexpected error occurred while fetching package details.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Courier/UpdateStatus/5
        [HttpGet]
        [PackageAccessAuthorize(PackageAccessType.AssignedCourier)]
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("UpdateStatus (GET) called without an ID.");
                return NotFound("Package ID not provided.");
            }

            try
            {
                // The package has already been authorized and loaded by the PackageAccessAuthorizeAttribute
                var authorizedPackage = HttpContext.Items["AuthorizedPackage"] as Package;

                if (authorizedPackage != null)
                {
                    // Use the authorized package to prepare the view model
                    var courierId = GetCurrentUserId();
                    var userRole = GetCurrentUserRole();
                    var result = await _courierBusinessService.PrepareUpdateStatusViewModelAsync(authorizedPackage.PackageId, courierId, userRole);

                    if (!result.IsSuccess)
                    {
                        if (result.ErrorCode == "CONFLICT")
                        {
                            TempData["InfoMessage"] = result.ErrorMessage;
                            return RedirectToAction(nameof(PackageDetails), new { id = authorizedPackage.PackageId });
                        }

                        _logger.LogError("Error in UpdateStatus (GET): {ErrorMessage}", result.ErrorMessage);
                        TempData["ErrorMessage"] = result.ErrorMessage;
                        return RedirectToAction("Index", "Home");
                    }

                    _logger.LogInformation("Displaying update status form for package {PackageId} to user {UserId}",
                        authorizedPackage.PackageId, courierId);
                    return View(result.Data);
                }

                // Fallback to the original logic if the attribute didn't set the package
                var fallbackCourierId = GetCurrentUserId();
                var fallbackUserRole = GetCurrentUserRole();
                var fallbackResult = await _courierBusinessService.PrepareUpdateStatusViewModelAsync(id.Value, fallbackCourierId, fallbackUserRole);

                if (!fallbackResult.IsSuccess)
                {
                    if (fallbackResult.ErrorCode == "NOT_FOUND")
                    {
                        return NotFound();
                    }

                    if (fallbackResult.ErrorCode == "UNAUTHORIZED")
                    {
                        TempData["ErrorMessage"] = fallbackResult.ErrorMessage;
                        return RedirectToAction(nameof(ActivePackages));
                    }

                    if (fallbackResult.ErrorCode == "CONFLICT")
                    {
                        TempData["InfoMessage"] = fallbackResult.ErrorMessage;
                        return RedirectToAction(nameof(PackageDetails), new { id = id.Value });
                    }

                    _logger.LogError("Error in UpdateStatus (GET): {ErrorMessage}", fallbackResult.ErrorMessage);
                    TempData["ErrorMessage"] = fallbackResult.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                return View(fallbackResult.Data);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in UpdateStatus (GET) while fetching courier ID for package {PackageId}.", id.Value);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateStatus (GET) for package {PackageId}.", id.Value);
                TempData["ErrorMessage"] = "An unexpected error occurred while preparing the status update form.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Courier/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PackageAccessAuthorize(PackageAccessType.AssignedCourier, packageIdParameterName: "id")]
        public async Task<IActionResult> UpdateStatus(int id, CourierUpdatePackageStatusViewModel viewModel)
        {
            if (id != viewModel.PackageId)
            {
                return BadRequest("Package ID mismatch.");
            }

            try
            {
                // The package has already been authorized by the PackageAccessAuthorizeAttribute
                var authorizedPackage = HttpContext.Items["AuthorizedPackage"] as Package;
                var courierId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (ModelState.IsValid)
                {
                    var result = await _courierBusinessService.UpdatePackageStatusAsync(viewModel, courierId, userRole);

                    if (result.IsSuccess)
                    {
                        var package = result.Data!;
                        TempData["SuccessMessage"] = $"Package {package.TrackingNumber} status successfully updated.";
                        _logger.LogInformation("User {UserId} successfully updated status for package {PackageId}",
                            courierId, package.PackageId);
                        return RedirectToAction(nameof(PackageDetails), new { id = package.PackageId });
                    }

                    // Handle different types of failures
                    if (result.ErrorCode == "NOT_FOUND")
                    {
                        ModelState.AddModelError(string.Empty, "Package not found.");
                    }
                    else if (result.ErrorCode == "UNAUTHORIZED")
                    {
                        // This should not happen since we already authorized via attribute, but handle gracefully
                        _logger.LogWarning("Unexpected authorization error in UpdateStatus POST for package {PackageId} and user {UserId}: {ErrorMessage}",
                            viewModel.PackageId, courierId, result.ErrorMessage);
                        TempData["ErrorMessage"] = result.ErrorMessage;
                        return RedirectToAction(nameof(ActivePackages));
                    }
                    else if (result.ErrorCode == "VALIDATION_ERROR")
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                    }
                    else if (result.ErrorCode == "CONFLICT")
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An unexpected error occurred.");
                    }
                }
                else
                {
                    _logger.LogWarning("UpdateStatus (POST) for package ID {PackageId} failed due to invalid model state. Errors: {Errors}",
                        viewModel.PackageId,
                        string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                }

                // Repopulate view model for error display
                await _courierBusinessService.PopulateViewModelForErrorAsync(viewModel);
                return View(viewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in UpdateStatus (POST) while fetching courier ID for package {PackageId}.", viewModel.PackageId);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateStatus (POST) for package {PackageId}.", viewModel.PackageId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the package status.");

                await _courierBusinessService.PopulateViewModelForErrorAsync(viewModel);
                return View(viewModel);
            }
        }
    }
}
