// File: Controllers/CourierController.cs
using _10.Data;
using _10.Models; // For Package, User, StatusDefinition, CourierUpdatePackageStatusViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace _10.Controllers
{
    [Authorize(Roles = "Courier,Admin")]
    public class CourierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourierController> _logger;

        public CourierController(ApplicationDbContext context, ILogger<CourierController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var activeStatusNames = new List<string> { "Sent", "In Delivery" };

                var activePackages = await _context.Packages
                    .Where(p => p.AssignedCourierId == courierId && p.CurrentStatus != null && activeStatusNames.Contains(p.CurrentStatus.Name))
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .OrderByDescending(p => p.SubmissionDate)
                    .AsNoTracking()
                    .ToListAsync();
                
                ViewData["Title"] = "Active Packages for Delivery";
                return View("PackageList", activePackages);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in ActivePackages while fetching courier ID or data.");
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
                var deliveredPackages = await _context.Packages
                    .Where(p => p.AssignedCourierId == courierId && p.CurrentStatus != null && p.CurrentStatus.Name == "Delivered")
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .OrderByDescending(p => p.DeliveryDate)
                    .AsNoTracking()
                    .ToListAsync();
                
                ViewData["Title"] = "Delivered Packages";
                return View("PackageList", deliveredPackages);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in DeliveredPackages while fetching courier ID or data.");
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
                var allAssignedPackages = await _context.Packages
                    .Where(p => p.AssignedCourierId == courierId)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .OrderByDescending(p => p.SubmissionDate)
                    .AsNoTracking()
                    .ToListAsync();
                
                ViewData["Title"] = "All My Assigned Packages";
                return View("PackageList", allAssignedPackages);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in AllMyPackages while fetching courier ID or data.");
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

        // GET: /Courier/PackageDetails/5
        [HttpGet]
        public async Task<IActionResult> PackageDetails(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("PackageDetails called without an ID.");
                return NotFound("Package ID not provided.");
            }

            try
            {
                var courierId = GetCurrentUserId();
                var package = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.History).ThenInclude(h => h.Status)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id.Value && p.AssignedCourierId == courierId);

                if (package == null)
                {
                    _logger.LogWarning("Package {PackageId} not found for courier {CourierId} or not assigned to them.", id.Value, courierId);
                    TempData["ErrorMessage"] = "Package not found or not assigned to you.";
                    return RedirectToAction(nameof(ActivePackages));
                }
                return View(package);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in PackageDetails while fetching courier ID or data for package {PackageId}.", id.Value);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Home");
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
        public async Task<IActionResult> UpdateStatus(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("UpdateStatus (GET) called without an ID.");
                return NotFound("Package ID not provided.");
            }
            try
            {
                var courierId = GetCurrentUserId();
                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id.Value && p.AssignedCourierId == courierId);

                if (package == null)
                {
                    _logger.LogWarning("Package {PackageId} not found for courier {CourierId} or not assigned (UpdateStatus GET).", id.Value, courierId);
                    TempData["ErrorMessage"] = "Package not found or not assigned to you.";
                    return RedirectToAction(nameof(ActivePackages));
                }
                
                if (package.CurrentStatus?.Name == "Delivered")
                {
                    TempData["InfoMessage"] = "This package has already been delivered and its status cannot be changed further through this form.";
                    return RedirectToAction(nameof(PackageDetails), new { id = package.PackageId });
                }

                List<string> allowedNewStatusNames = new List<string>();
                if (package.CurrentStatus?.Name == "Sent")
                {
                    allowedNewStatusNames.Add("In Delivery");
                    allowedNewStatusNames.Add("Delivered");
                }
                else if (package.CurrentStatus?.Name == "In Delivery")
                {
                    allowedNewStatusNames.Add("In Delivery");
                    allowedNewStatusNames.Add("Delivered");
                }
                
                var viewModel = new CourierUpdatePackageStatusViewModel
                {
                    PackageId = package.PackageId,
                    TrackingNumber = package.TrackingNumber,
                    CurrentStatusName = package.CurrentStatus?.Description, 
                    NewStatusId = package.StatusId,
                    CurrentLongitude = package.Longitude,
                    CurrentLatitude = package.Latitude,
                    NewLongitude = package.Longitude,
                    NewLatitude = package.Latitude,
                    Notes = package.Notes,
                    AvailableStatuses = await _context.StatusDefinitions
                                               .Where(s => allowedNewStatusNames.Contains(s.Name))
                                               .OrderBy(s => s.Description)
                                               .Select(s => new SelectListItem { Value = s.StatusId.ToString(), Text = s.Description })
                                               .ToListAsync()
                };
                
                return View(viewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error in UpdateStatus (GET) while fetching courier ID or data for package {PackageId}.", id.Value);
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
        public async Task<IActionResult> UpdateStatus(int id, CourierUpdatePackageStatusViewModel viewModel)
        {
            if (id != viewModel.PackageId)
            {
                return BadRequest("Package ID mismatch.");
            }

            var courierId = GetCurrentUserId(); 
            Package? packageToUpdate = null;

            async Task PopulateViewModelForError(CourierUpdatePackageStatusViewModel vm)
            {
                var currentPackageData = await _context.Packages
                                                   .Include(p => p.CurrentStatus)
                                                   .AsNoTracking()
                                                   .FirstOrDefaultAsync(p => p.PackageId == vm.PackageId);
                if (currentPackageData != null)
                {
                    vm.TrackingNumber = currentPackageData.TrackingNumber; // Ensure tracking number is set
                    vm.CurrentStatusName = currentPackageData.CurrentStatus?.Description;
                    vm.CurrentLongitude = currentPackageData.Longitude;
                    vm.CurrentLatitude = currentPackageData.Latitude;
                }

                List<string> allowedNewStatusNamesOnError = new List<string>();
                 if (currentPackageData?.CurrentStatus?.Name == "Sent")
                {
                    allowedNewStatusNamesOnError.Add("In Delivery");
                    allowedNewStatusNamesOnError.Add("Delivered");
                }
                else if (currentPackageData?.CurrentStatus?.Name == "In Delivery")
                {
                    allowedNewStatusNamesOnError.Add("In Delivery");
                    allowedNewStatusNamesOnError.Add("Delivered");
                }

                vm.AvailableStatuses = await _context.StatusDefinitions
                                           .Where(s => allowedNewStatusNamesOnError.Contains(s.Name))
                                           .OrderBy(s => s.Description)
                                           .Select(s => new SelectListItem { Value = s.StatusId.ToString(), Text = s.Description })
                                           .ToListAsync();
            }


            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    packageToUpdate = await _context.Packages
                        .Include(p => p.CurrentStatus) 
                        .FirstOrDefaultAsync(p => p.PackageId == viewModel.PackageId && p.AssignedCourierId == courierId);

                    if (packageToUpdate == null)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(string.Empty, "Package not found or you do not have permission to update it.");
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }

                    var newStatus = await _context.StatusDefinitions.FindAsync(viewModel.NewStatusId);
                    if (newStatus == null)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("NewStatusId", "The selected new status is invalid.");
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }
                    
                    if (packageToUpdate.CurrentStatus?.Name == "Delivered" && newStatus.Name != "Delivered") {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("NewStatusId", "Cannot change the status of a package that has already been delivered.");
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }

                    bool statusChanged = packageToUpdate.StatusId != viewModel.NewStatusId;
                    bool locationChanged = packageToUpdate.Longitude != viewModel.NewLongitude || packageToUpdate.Latitude != viewModel.NewLatitude;
                    bool notesChanged = packageToUpdate.Notes != viewModel.Notes;


                    packageToUpdate.StatusId = viewModel.NewStatusId;
                    packageToUpdate.Longitude = viewModel.NewLongitude;
                    packageToUpdate.Latitude = viewModel.NewLatitude;
                    packageToUpdate.Notes = viewModel.Notes;

                    if (newStatus.Name == "Delivered" && packageToUpdate.DeliveryDate == null)
                    {
                        packageToUpdate.DeliveryDate = DateTime.UtcNow;
                    }
                    
                    if (statusChanged || locationChanged || notesChanged || 
                        (newStatus.Name == "In Delivery" && packageToUpdate.CurrentStatus?.Name == "In Delivery"))
                    {
                         var packageHistoryEntry = new PackageHistory
                        {
                            PackageId = packageToUpdate.PackageId,
                            StatusId = viewModel.NewStatusId,
                            Timestamp = DateTime.UtcNow,
                            Longitude = viewModel.NewLongitude,
                            Latitude = viewModel.NewLatitude
                        };
                        _context.PackageHistories.Add(packageHistoryEntry);
                    }

                    _context.Packages.Update(packageToUpdate);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Courier {CourierId} updated package {PackageId} to status {NewStatusId}", courierId, packageToUpdate.PackageId, viewModel.NewStatusId);
                    TempData["SuccessMessage"] = $"Package {packageToUpdate.TrackingNumber} status successfully updated.";
                    return RedirectToAction(nameof(PackageDetails), new { id = packageToUpdate.PackageId });
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error in UpdateStatus (POST) while fetching courier ID for package {PackageId}.", viewModel.PackageId);
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning(ex, "Concurrency conflict while updating package {PackageId}.", viewModel.PackageId);
                    ModelState.AddModelError(string.Empty, "The package data was modified by another user. Please refresh and try again.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error in UpdateStatus (POST) for package {PackageId}.", viewModel.PackageId);
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating status.");
                }
            }

            await PopulateViewModelForError(viewModel);
            return View(viewModel);
        }
    }
}