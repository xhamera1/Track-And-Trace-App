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

## TODO
## Application Screenshots

**Example Screenshots:**
* `![Login Page](URL_to_Login_Page_Screenshot.png)`
* `![User Dashboard](URL_to_User_Dashboard_Screenshot.png)`
* `![Package Details with Map](URL_to_Package_Details_Map_Screenshot.png)`
* `![Courier Package List](URL_to_Courier_Package_List_Screenshot.png)`
* `![Admin User Management](URL_to_Admin_User_Management_Screenshot.png)`


## License

This project is licensed under the MIT License.
For detailed information, please refer to the LICENSE file.

