// File: Controllers/CourierController.cs
using _10.Data;
using _10.Models; // For Package, User, StatusDefinition, CourierUpdatePackageStatusViewModel
using _10.Services; // For authorization service
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
        private readonly IPackageAuthorizationService _authorizationService;
        private readonly IPackageLocationService _packageLocationService;

        public CourierController(
            ApplicationDbContext context,
            ILogger<CourierController> logger,
            IPackageAuthorizationService authorizationService,
            IPackageLocationService packageLocationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _packageLocationService = packageLocationService ?? throw new ArgumentNullException(nameof(packageLocationService));
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
                var userRole = GetCurrentUserRole();

                var package = await _context.Packages
                    .Include(p => p.SenderUser)
                    .Include(p => p.RecipientUser)
                    .Include(p => p.OriginAddress)
                    .Include(p => p.DestinationAddress)
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.History).ThenInclude(h => h.Status)
                    .Include(p => p.AssignedCourier) // Include assigned courier for authorization
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id.Value);

                if (package == null)
                {
                    return NotFound();
                }

                var authResult = _authorizationService.GetAuthorizationResult(package, courierId, userRole);
                if (!authResult.IsAuthorized)
                {
                    _logger.LogWarning("Courier {CourierId} with role {UserRole} denied access to package {PackageId}: {Reason}",
                        courierId, userRole, package.PackageId, authResult.Reason);

                    TempData["ErrorMessage"] = $"Access denied: {authResult.Reason}";
                    return RedirectToAction(nameof(ActivePackages));
                }

                _logger.LogInformation("Courier {CourierId} ({AccessType}) accessing package {PackageId} details",
                    courierId, authResult.AccessType, package.PackageId);

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
                var userRole = GetCurrentUserRole();

                var package = await _context.Packages
                    .Include(p => p.CurrentStatus)
                    .Include(p => p.AssignedCourier) 
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PackageId == id.Value);

                if (package == null)
                {
                    return NotFound();
                }

                if (!_authorizationService.IsAuthorizedToModifyPackage(package, courierId, userRole))
                {
                    _logger.LogWarning("Courier {CourierId} denied access to modify package {PackageId}", courierId, package.PackageId);
                    TempData["ErrorMessage"] = "You are not authorized to update this package status.";
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
                    vm.TrackingNumber = currentPackageData.TrackingNumber;
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
                    var userRole = GetCurrentUserRole();

                    packageToUpdate = await _context.Packages
                        .Include(p => p.CurrentStatus)
                        .Include(p => p.OriginAddress)  
                        .Include(p => p.DestinationAddress) 
                        .Include(p => p.AssignedCourier)  
                        .FirstOrDefaultAsync(p => p.PackageId == viewModel.PackageId);

                    if (packageToUpdate == null)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(string.Empty, "Package not found.");
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }

                    if (!_authorizationService.IsAuthorizedToModifyPackage(packageToUpdate, courierId, userRole))
                    {
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "You are not authorized to update this package.";
                        _logger.LogWarning("Courier {CourierId} (Role: {UserRole}) attempt to modify package {PackageId} denied.", courierId, userRole, packageToUpdate.PackageId);
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }

                    var newStatus = await _context.StatusDefinitions.FindAsync(viewModel.NewStatusId);
                    if (newStatus == null)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(nameof(viewModel.NewStatusId), "The selected new status is invalid.");
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }

                    if (packageToUpdate.CurrentStatus?.Name == "Delivered" && newStatus.Name != "Delivered")
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(nameof(viewModel.NewStatusId), "Cannot change the status of a package that has already been delivered, unless re-affirming 'Delivered' status with new notes/location.");
                        await PopulateViewModelForError(viewModel);
                        return View(viewModel);
                    }

                    bool statusChanged = packageToUpdate.StatusId != viewModel.NewStatusId;
                    bool locationChangedByUser = false;

                    decimal? finalLatitude = packageToUpdate.Latitude; 
                    decimal? finalLongitude = packageToUpdate.Longitude; 

                    if (viewModel.NewLatitude.HasValue && viewModel.NewLongitude.HasValue)
                    {
                        finalLatitude = viewModel.NewLatitude.Value;
                        finalLongitude = viewModel.NewLongitude.Value;
                        locationChangedByUser = true;
                        _logger.LogInformation("Package {PackageId}: Using directly provided coordinates Lat={Lat}, Lon={Lon}", packageToUpdate.PackageId, finalLatitude, finalLongitude);
                    }
                    else if (viewModel.HasNewLocationAddress())
                    {
                        _logger.LogInformation("Package {PackageId}: Attempting to geocode provided address: {Street}, {City}, {Zip}, {Country}",
                                               packageToUpdate.PackageId, viewModel.NewLocationStreet, viewModel.NewLocationCity, viewModel.NewLocationZipCode, viewModel.NewLocationCountry);
                        var addressToGeocode = new Address
                        {
                            Street = viewModel.NewLocationStreet!,
                            City = viewModel.NewLocationCity!,
                            ZipCode = viewModel.NewLocationZipCode!,
                            Country = viewModel.NewLocationCountry!
                        };
                        var geocodingResult = await _packageLocationService.GeocodeAddressAsync(addressToGeocode);

                        if (geocodingResult.IsSuccess && geocodingResult.Latitude.HasValue && geocodingResult.Longitude.HasValue)
                        {
                            finalLatitude = geocodingResult.Latitude.Value;
                            finalLongitude = geocodingResult.Longitude.Value;
                            locationChangedByUser = true;
                             _logger.LogInformation("Package {PackageId}: Successfully geocoded provided address to Lat={Lat}, Lon={Lon}", packageToUpdate.PackageId, finalLatitude, finalLongitude);
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            var geocodeError = $"Could not geocode the provided address: {geocodingResult.ErrorMessage ?? "Unknown error."}";
                            ModelState.AddModelError(nameof(viewModel.NewLocationStreet), geocodeError);
                            _logger.LogWarning("Package {PackageId}: Geocoding failed for provided address. Error: {Error}", packageToUpdate.PackageId, geocodeError);
                            await PopulateViewModelForError(viewModel);
                            return View(viewModel);
                        }
                    }
                    else if (newStatus.Name == "Delivered" && packageToUpdate.DestinationAddress != null)
                    {
                         _logger.LogInformation("Package {PackageId}: Status changed to 'Delivered'. Attempting to geocode destination address.", packageToUpdate.PackageId);
                        var geocodingResult = await _packageLocationService.GeocodeAddressAsync(packageToUpdate.DestinationAddress);
                        if (geocodingResult.IsSuccess && geocodingResult.Latitude.HasValue && geocodingResult.Longitude.HasValue)
                        {
                            finalLatitude = geocodingResult.Latitude.Value;
                            finalLongitude = geocodingResult.Longitude.Value;
                            locationChangedByUser = true;
                            _logger.LogInformation("Package {PackageId}: Geocoded destination for 'Delivered' status to Lat={Lat}, Lon={Lon}", packageToUpdate.PackageId, finalLatitude, finalLongitude);
                        }
                         else
                        {
                             _logger.LogWarning("Package {PackageId}: Failed to geocode destination address for 'Delivered' status. Error: {Error}", packageToUpdate.PackageId, geocodingResult.ErrorMessage);
                        }
                    }


                    bool notesChanged = packageToUpdate.Notes != viewModel.Notes;

                    bool finalLocationIsDifferent = packageToUpdate.Latitude != finalLatitude || packageToUpdate.Longitude != finalLongitude;

                    packageToUpdate.StatusId = newStatus.StatusId; 
                    packageToUpdate.Longitude = finalLongitude;
                    packageToUpdate.Latitude = finalLatitude;
                    packageToUpdate.Notes = viewModel.Notes;

                    if (newStatus.Name == "Delivered" && packageToUpdate.DeliveryDate == null)
                    {
                        packageToUpdate.DeliveryDate = DateTime.UtcNow;
                    }

                    if (statusChanged || finalLocationIsDifferent || notesChanged ||
                        (newStatus.Name == "In Delivery" && packageToUpdate.CurrentStatus?.Name == "In Delivery"))
                    {
                        var packageHistoryEntry = new PackageHistory
                        {
                            PackageId = packageToUpdate.PackageId,
                            StatusId = newStatus.StatusId,
                            Timestamp = DateTime.UtcNow,
                            Longitude = finalLongitude,
                            Latitude = finalLatitude
                        };
                        _context.PackageHistories.Add(packageHistoryEntry);
                        _logger.LogInformation("Package {PackageId}: PackageHistory entry created for status {StatusName}.", packageToUpdate.PackageId, newStatus.Name);
                    }

                    _context.Packages.Update(packageToUpdate);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Courier {CourierId} successfully updated package {PackageId} (Tracking: {TrackingNumber}) to status {NewStatusName}. Location: Lat={Lat}, Lon={Lon}.",
                                           courierId, packageToUpdate.PackageId, packageToUpdate.TrackingNumber, newStatus.Name, finalLatitude, finalLongitude);
                    TempData["SuccessMessage"] = $"Package {packageToUpdate.TrackingNumber} status successfully updated to '{newStatus.Description}'.";
                    return RedirectToAction(nameof(PackageDetails), new { id = packageToUpdate.PackageId });
                }
                catch (InvalidOperationException ex) 
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Authorization error in UpdateStatus (POST) for package {PackageId}.", viewModel.PackageId);
                    ModelState.AddModelError(string.Empty, "An authorization error occurred: " + ex.Message);
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
                    ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the package status.");
                }
            }
            else
            {
                 _logger.LogWarning("UpdateStatus (POST) for package ID {PackageId} failed due to invalid model state. Errors: {Errors}",
                                   viewModel.PackageId,
                                   string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            await PopulateViewModelForError(viewModel); 
            return View(viewModel);
        }

    }
}
