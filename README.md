# Track & Trace System - Advanced Package Tracking Application

## Project Overview

Track & Trace System is a comprehensive full-stack web application designed for managing and monitoring courier shipments. It enables users to send packages, track their status in real-time, and allows couriers to efficiently manage their assigned deliveries. 

This project was developed as part of an Advanced Programming (Programowanie Zaawansowane 2) university course.

**The currently deployed version of the application and its database are hosted on Microsoft Azure under a student license.**
* **Application Link:** [https://track-and-trace-app-gsbndfgegvhgfbhz.polandcentral-01.azurewebsites.net/](https://track-and-trace-app-gsbndfgegvhgfbhz.polandcentral-01.azurewebsites.net/)

## Authors

* **Patryk Chamera** - [GitHub Profile](https://github.com/xhamera1)
* **Karol Bystrek** - [GitHub Profile](https://github.com/karolbystrek)



## System Features

The system offers a wide range of functionalities tailored to different user roles:

### Registered Users (Role: User)
* **Registration and Login:** Secure account creation and login system utilizing session management and password hashing.
* **User Dashboard:** Personalized view with quick statistics of sent and received packages, and shortcuts to key actions.
* **Sending Packages:** Intuitive form for dispatching new shipments, including recipient details, package specifics, and origin/destination addresses. The system automatically assigns a courier.
* **Package Tracking:** Ability to view detailed information about a package, including its current status, full location history, and involved parties.
* **Package Route Map:** Visualization of the package's location history and current position on an interactive map (Leaflet.js).
* **Package Pick-Up:** Functionality to mark a package as "picked up," which updates its status to "Delivered."
* **Account Management:** Editing profile information, including address details.

### Couriers (Role: Courier)
* **Courier Dashboard:** Access to a list of packages assigned to the courier, filterable by status (active, delivered, all).
* **Package Details for Courier:** Access to complete information for packages handled by the courier, including the route map.
* **Package Status Updates:** Ability to change the shipment status (e.g., "In Delivery," "Delivered").
* **Package Location Updates:**
    * Option to input an address, which is then geocoded to coordinates.
    * Optionally, direct input of geographic coordinates.
    * Automatic coordinate retrieval based on the destination address when status changes to "Delivered."
* **Adding Notes:** Ability to add remarks to a package during status updates.

### Administrators (Role: Admin)
* **Full Access to User and Courier Functionalities.**
* **User Management:** Ability to create, view, edit, and delete user accounts within the system.
* **Status Definition Management:** Ability to add, edit, and delete package status definitions.
* **API Access:** Administrators have privileged access to API endpoints for system management and data retrieval.

### General and Technical Features
* **REST API:** Core data operations (packages, users, statuses) exposed via a REST API secured with API keys.
* **Geocoding:** Integration with Nominatim (OpenStreetMap) service for converting addresses to geographic coordinates and vice-versa.
* **Dynamic Courier Assignment:** Random assignment of an available courier to newly created packages.
* **Shared Addresses:** Optimized address storage by sharing identical address entries among different users and packages.
* **Package History:** Detailed logging of every status change and location update for a package.
* **Security:** Password hashing, CSRF protection (`AntiForgeryToken`), role-based and session-based authorization for MVC, and API key authentication for the REST API.
* **Responsive Web Design (RWD):** User interface adapts to various screen sizes.

## Technologies and Libraries

* **Backend:**
    * C#
    * ASP.NET Core MVC & REST API (.NET Framework)
    * Entity Framework Core (ORM for database interaction)
    * ASP.NET Core Session Management & Identity (for MVC login and role handling via Claims)
    * Authorization Attributes (`[Authorize]`, `[SessionAuthorize]`)
* **Database:**
    * MySQL (hosted on Azure)
* **Frontend:**
    * HTML5
    * CSS3 (utilizing CSS variables for theming)
    * Bootstrap 5 (CSS framework)
    * JavaScript
    * jQuery (for some scripts and validation)
    * Font Awesome (icons)
    * Leaflet.js (interactive maps)
    * Razor Pages (MVC view engine)
* **External Services:**
    * Nominatim (OpenStreetMap) - for geocoding addresses.
* **Architecture:**
    * Model-View-Controller (MVC) pattern
    * Repository/Service Layer pattern (for business logic separation)
    * Dependency Injection (DI)
    * Asynchronous operations (`async`/`await`)

## Demo Accounts

To test the application, you can log in with one of the following demonstration accounts:

1.  **Standard User:**
    * **Login:** `demo_user`
    * **Password:** `demo_user`
    * **Description:** A standard system user who can send packages, track them, mark them as received, and view their history on a map.

2.  **Courier User:**
    * **Login:** `demo_kurier`
    * **Password:** `demo_kurier`
    * **Description:** A courier who can manage assigned packages, update their status and location (via address or coordinates), and view package history on a map.

## Application Screenshots

**Key Interfaces Screenshots:**

**1. Login Page:**
The main entry point for users to access the system. It features fields for username and password, along with a link to the registration page.
![Login Page](/photos/login.png)

**2. User Dashboard:**
The personalized landing page for logged-in users. It displays quick package statistics (sent/received) and provides easy access to common actions like sending a new package or viewing packages for pick-up.
![User Dashboard](/photos/user_dashboard.png)

**3. Package Details with Map:**
A comprehensive view showing all details of a specific package, including sender/recipient information, package characteristics, current status, and a full location history visualized on an interactive map.
![Package Details with Map](/photos/package_details.png)

**4. Courier Package List:**
The courier's main interface for managing assigned packages. It lists packages, typically filterable by status (e.g., active, delivered), and provides actions to view details or update status.
![Courier Package List](/photos/kurier_packages.png)

**5. Courier Update Package Status/Details:**
The form used by couriers to update the status of a package and its current location. Couriers can input a new address (which is then geocoded) or directly provide coordinates.
![Courier Update Package Details](/photos/kurier_update_status.png)

**6. Admin User Management:**
The administrator's view for managing user accounts within the system. It typically allows admins to view, create, edit, and delete users.
![Admin User Management](/photos/admin_manage_users.png)



## Admin API Endpoints Overview

The following API endpoints are available for administrative purposes.

All requests to these endpoints should include the `ApiKey` in the HTTP headers:

### User Management (`/api/users`)
* **`GET /users`**: Retrieves a list of all users in the system.
* **`GET /users/{id}`**: Retrieves detailed information for a specific user by their ID.
* **`POST /users`**: Creates a new user. Requires a JSON body with user details (username, email, password, role, etc.).
* **`PUT /users/{id}`**: Updates an existing user's information. Requires a JSON body with fields to be updated.
* **`DELETE /users/{id}`**: Deletes a user by their ID.

### Address Management (`/api/addresses`)
* **`GET /addresses`**: Retrieves a list of all addresses in the system.
* **`GET /addresses/{id}`**: Retrieves detailed information for a specific address by its ID.
* **`POST /addresses`**: Creates a new address. Requires a JSON body with address details (street, city, zipCode, country).
* **`PUT /addresses/{id}`**: Updates an existing address. Requires a JSON body with fields to be updated.
* **`DELETE /addresses/{id}`**: Deletes an address by its ID (if not currently in use by users or packages).

### Package Management (`/api/package`)
* **`GET /package`**: Retrieves a list of all packages.
* **`GET /package/{trackingNumber}`**: Retrieves detailed information for a specific package by its tracking number.
* **`POST /package`**: Creates a new package. Requires a JSON body with package details (senderId, recipientId, addresses, size, etc.).
* **`PUT /package/{packageId}`**: Updates an existing package's information.
* **`DELETE /package/{packageId}`**: Deletes a package by its ID.
* **`POST /package/{trackingNumber}/status`**: Adds a new status entry to a package's history. Requires a JSON body with new status ID, optional location, and notes.

### Package History (`/api/packagehistory`)
* **`GET /api/packagehistory`**: Retrieves a list of all package history entries across all packages. (Admin only)
* **`GET /api/packagehistory/{id}`**: Retrieves the full history for a specific package by its **Package ID**.
* **`POST /api/packagehistory`**: Creates a new package history entry. Requires a JSON body with details such as `PackageId` and `StatusId`.
* **`PUT /api/packagehistory/{id}`**: Updates an existing package history entry by its **History Entry ID**. Requires a JSON body with fields to be updated.
* **`DELETE /api/packagehistory/{id}`**: Deletes a package history entry by its **History Entry ID**.

### Status Definition Management (`/api/status-definition`)
* **`GET /status-definition`**: Retrieves a list of all package status definitions.
* **`GET /status-definition/{id}`**: Retrieves details for a specific status definition by its ID.
* **`POST /status-definition`**: Creates a new status definition. Requires a JSON body with `name` and `description`.
* **`PUT /status-definition/{id}`**: Updates an existing status definition.
* **`DELETE /status-definition/{id}`**: Deletes a status definition (if not in use by packages).


## License

This project is licensed under the MIT License.
For detailed information, please refer to the LICENSE file.

