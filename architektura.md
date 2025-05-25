# Architektura Systemu Track and Trace

## 🏛️ Architektura Ogólna

Projekt zostanie zrealizowany w technologii **ASP.NET Core**, wykorzystując wzorzec **MVC** (Model-View-Controller) dla interfejsu użytkownika oraz **REST API** do operacji na danych i komunikacji z potencjalnymi systemami zewnętrznymi. Nacisk zostanie położony na separację odpowiedzialności (SoC) poprzez zastosowanie warstwy serwisowej, wykorzystanie wbudowanego mechanizmu Dependency Injection (DI) oraz promowanie operacji asynchronicznych (`async/await`) dla zwiększenia wydajności.

---

## 💾 Schemat Bazy Danych (SQLite) - Karol

Poniższy schemat bazuje na Twoich wstępnych założeniach oraz najlepszych praktykach.

### 1. `Users` (Obsługa logowania i użytkowników systemu)
* `UserId` (INTEGER, PK, Autoinkrementacja) - Klucz główny użytkownika.
* `Username` (TEXT, UNIQUE, NOT NULL) - Unikalna nazwa użytkownika. [na podstawie `username` z `users` w pliku `database_scheme.txt`]
* `Email` (TEXT, UNIQUE, NOT NULL) - Unikalny adres email użytkownika.
* `PasswordHash` (TEXT, NOT NULL) - Zahaszowane hasło użytkownika. [na podstawie `password (hashed)` z `users` w pliku `database_scheme.txt`]
* `ApiKey` (TEXT, UNIQUE, NULL) - Klucz API dla użytkowników korzystających z REST API (generowany, gdy potrzebny). [na podstawie `token` z `users` w pliku `database_scheme.txt`]
* `FirstName` (TEXT, NULL) - Imię użytkownika. [na podstawie `first_name` z `users` w pliku `database_scheme.txt`]
* `LastName` (TEXT, NULL) - Nazwisko użytkownika. [na podstawie `last_name` z `users` w pliku `database_scheme.txt`]
* `Role` (TEXT, NOT NULL) - Rola użytkownika w systemie (np. "Admin", "Courier", "Client"). [na podstawie `role (user, admin, courier)` z `users` w pliku `database_scheme.txt`]
* `Country` (TEXT, NULL) - Kraj. [na podstawie `country` z `users` w pliku `database_scheme.txt`]
* `City` (TEXT, NULL) - Miasto. [na podstawie `city` z `users` w pliku `database_scheme.txt`]
* `Street` (TEXT, NULL) - Ulica. [na podstawie `street` z `users` w pliku `database_scheme.txt`]
* `Birthday` (TEXT, NULL) - Data urodzenia (format ISO8601). [na podstawie `birthday` z `users` w pliku `database_scheme.txt`]
* `CreatedAt` (TEXT, NOT NULL) - Data utworzenia rekordu (format ISO8601).

### 2. `Packages` (Główna tabela dla przesyłek)
* `PackageId` (INTEGER, PK, Autoinkrementacja) - Klucz główny przesyłki. [zastępuje `order_id` z `orders` w pliku `database_scheme.txt`]
* `TrackingNumber` (TEXT, UNIQUE, NOT NULL) - Unikalny numer do śledzenia przesyłki.
* `SenderUserId` (INTEGER, FK do `Users.UserId`, NOT NULL) - ID nadawcy. [na podstawie `user_id_from` z `orders` w pliku `database_scheme.txt`]
* `RecipientUserId` (INTEGER, FK do `Users.UserId`, NOT NULL) - ID odbiorcy. [na podstawie `user_id_to` z `orders` w pliku `database_scheme.txt`]
* `AssignedCourierId` (INTEGER, FK do `Users.UserId`, NULL) - ID przypisanego kuriera. [na podstawie `courier_id` z `orders` w pliku `database_scheme.txt`]
* `PackageSize` (TEXT, NOT NULL) - Rozmiar paczki (np. "Small", "Medium", "Large"). [na podstawie `package_size (small, medium, large)` z `order_details` w pliku `database_scheme.txt`]
* `WeightInKg` (REAL, NULL) - Waga przesyłki w kilogramach.
* `Notes` (TEXT, NULL) - Dodatkowe uwagi dotyczące przesyłki.
* `OriginAddress` (TEXT, NOT NULL) - Adres nadawcy (tekstowo).
* `DestinationAddress` (TEXT, NOT NULL) - Adres odbiorcy (tekstowo).
* `SubmissionDate` (TEXT, NOT NULL) - Data nadania przesyłki (format ISO8601). [na podstawie `date_złożenia` z `order_details` w pliku `database_scheme.txt`]
* `DeliveryDate` (TEXT, NULL) - Rzeczywista data dostarczenia (format ISO8601). [na podstawie `data_dostarczenia` z `order_details` w pliku `database_scheme.txt`]
* `StatusId` (INTEGER, FK do `StatusDefinitions.StatusId`, NOT NULL) - ID aktualnego statusu przesyłki. [na podstawie `status_id` z `order_details` w pliku `database_scheme.txt`]
* `Longitude` (REAL, NULL) - Aktualna długość geograficzna przesyłki. [na podstawie `longitude` z `order_details` w pliku `database_scheme.txt`]
* `Latitude` (REAL, NULL) - Aktualna szerokość geograficzna przesyłki. [na podstawie `latitude` z `order_details` w pliku `database_scheme.txt`]

### 3. `PackageHistory` (Historia zmian statusów i lokalizacji przesyłki)
* `PackageHistoryId` (INTEGER, PK, Autoinkrementacja) - Klucz główny wpisu historii.
* `PackageId` (INTEGER, FK do `Packages.PackageId`, NOT NULL) - ID powiązanej przesyłki. [na podstawie `order_id` z `order_history` w pliku `database_scheme.txt`]
* `StatusId` (INTEGER, FK do `StatusDefinitions.StatusId`, NOT NULL) - ID statusu w tym punkcie historii. [na podstawie `status_id` z `order_history` w pliku `database_scheme.txt`]
* `Timestamp` (TEXT, NOT NULL) - Data i czas zdarzenia (format ISO8601). [na podstawie `timestamp` z `order_history` w pliku `database_scheme.txt`]
* `Longitude` (REAL, NULL) - Długość geograficzna w momencie zdarzenia. [na podstawie `longitude` z `order_history` w pliku `database_scheme.txt`]
* `Latitude` (REAL, NULL) - Szerokość geograficzna w momencie zdarzenia. [na podstawie `latitude` z `order_history` w pliku `database_scheme.txt`]

### 4. `StatusDefinitions` (Definicje możliwych statusów przesyłek)
* `StatusId` (INTEGER, PK, Autoinkrementacja) - Klucz główny definicji statusu. [na podstawie `status_id` z `status` w pliku `database_scheme.txt`]
* `StatusCode` (TEXT, UNIQUE, NOT NULL) - Krótki, unikalny kod statusu (np. "NEW", "IN_TRANSIT", "DELIVERED").
* `Description` (TEXT, NOT NULL) - Pełny, czytelny opis statusu. [na podstawie `description (przyjęta w magazynie, w trakcie przygotowania, wysłana, dostarczona, odebrana)` z `status` w pliku `database_scheme.txt`]

---

## 🧩 Modele (C#) - Patryk

### 1. Encje Domenowe (Entities)
Bezpośrednie odwzorowanie tabel bazy danych na klasy C#, używane przez Entity Framework Core.
* **`User.cs`**: Zawiera właściwości odpowiadające kolumnom tabeli `Users`. Rola użytkownika (`Role`) może być reprezentowana przez `enum UserRole { Admin, Courier, Client }`. [na podstawie `user` i `enum role` z pliku `mcv_model_scheme.txt`]
* **`Package.cs`**: Właściwości z tabeli `Packages`. Rozmiar paczki (`PackageSize`) jako `enum PackageSize { Small, Medium, Large }`. Właściwości nawigacyjne do powiązanych encji: `User Sender`, `User Recipient`, `User AssignedCourier`, `StatusDefinition CurrentStatus`, `ICollection<PackageHistory> History`. [na podstawie `order` i `enum package_size` z pliku `mcv_model_scheme.txt`]
* **`PackageHistory.cs`**: Właściwości z tabeli `PackageHistory`. Właściwości nawigacyjne: `Package Package`, `StatusDefinition Status`. [na podstawie `order_history` z pliku `mcv_model_scheme.txt`]
* **`StatusDefinition.cs`**: Właściwości z tabeli `StatusDefinitions`. [zastępuje `enum status` z pliku `mcv_model_scheme.txt` bardziej rozbudowaną encją]

### 2. Modele Widoków (ViewModels) - dla MVC
Klasy C# używane do przekazywania danych między kontrolerami a widokami Razor, często zawierające logikę walidacji (DataAnnotations).
* `LoginViewModel { string UsernameOrEmail, string Password, bool RememberMe }`
* `RegisterUserViewModel { string Username, string Email, string Password, string ConfirmPassword, string FirstName, string LastName, UserRole Role, string Country, string City, string Street, DateTime? Birthday }`
* `UserProfileViewModel { /* właściwości do wyświetlania i edycji profilu */ }`
* `PackageSummaryViewModel { string TrackingNumber, string OriginAddress, string DestinationAddress, string CurrentStatusDescription, DateTime SubmissionDate }`
* `PackageDetailsViewModel { /* szczegółowe dane paczki */, List<PackageHistoryViewModel> History }`
* `CreatePackageViewModel { /* pola formularza tworzenia paczki, np. ID nadawcy, ID odbiorcy, adresy, rozmiar */ }`
* `UpdatePackageStatusViewModel { int PackageId, int NewStatusId, string LocationDescription, string Notes, double? Longitude, double? Latitude }`
* `UserManagementViewModel { List<UserSummaryViewModel> Users }` (dla panelu admina)
* `StatusDefinitionViewModel { int StatusId, string StatusCode, string Description }`

### 3. Obiekty Transferu Danych (DTOs) - dla REST API
Klasy C# używane do definiowania struktury danych przesyłanych w żądaniach i odpowiedziach API. Pomagają oddzielić model API od wewnętrznych modeli domenowych.
* `UserDto { int UserId, string Username, string Email, string FirstName, string LastName, string Role, string ApiKey }`
* `CreateUserDto { string Username, string Email, string Password, string FirstName, string LastName, string Role }`
* `PackageDto { string TrackingNumber, ..., StatusDefinitionDto CurrentStatus, UserDto Sender, UserDto Recipient, UserDto AssignedCourier, List<PackageHistoryDto> History }`
* `CreatePackageDto { int SenderUserId, int RecipientUserId, string OriginAddress, string DestinationAddress, string PackageSize, double? WeightInKg, string Notes, DateTime? EstimatedDeliveryDate }`
* `UpdatePackageDto { /* pola, które można zaktualizować */ }`
* `PackageHistoryDto { DateTime Timestamp, string StatusDescription, string LocationDescription, double? Longitude, double? Latitude, string Notes }`
* `AddPackageStatusDto { int NewStatusId, string LocationDescription, double? Longitude, double? Latitude, string Notes }`
* `StatusDefinitionDto { int StatusId, string StatusCode, string Description }`

---

## ⚙️ Serwisy (Logika Biznesowa)

Interfejsy definiujące kontrakty oraz klasy implementujące logikę biznesową, wstrzykiwane do kontrolerów.
* **`IUserService` / `UserService.cs`**: Zarządzanie użytkownikami (CRUD), procesy autentykacji (logowanie MVC), obsługa kluczy API, zarządzanie rolami. [na podstawie `user_service` z pliku `mcv_model_scheme.txt`]
* **`IPackageService` / `PackageService.cs`**: Główna logika biznesowa operacji na przesyłkach: tworzenie, wyszukiwanie, aktualizacja danych, dodawanie wpisów do historii statusów, aktualizacja bieżącego statusu paczki. [na podstawie `order_service` z pliku `mcv_model_scheme.txt`]
* **`ICourierService` / `CourierService.cs`**: Logika specyficzna dla kurierów, np. pobieranie przypisanych przesyłek, aktualizacja statusów przesyłek przez kuriera. [na podstawie `courier_service` z pliku `mcv_model_scheme.txt`]
* **`IStatusDefinitionService` / `StatusDefinitionService.cs`**: Zarządzanie (CRUD) definicjami statusów przez administratora.
* **`IDataSeederService` / `DataSeederService.cs`**: Serwis odpowiedzialny za inicjalizację danych przy pierwszym uruchomieniu aplikacji (np. tworzenie konta admina, dodawanie domyślnych definicji statusów). [na podstawie `init_db_service` z pliku `mcv_model_scheme.txt`]

---

## 🕹️ Kontrolery i Endpointy

### Kontrolery MVC (Interfejs Webowy)
Odpowiedzialne za obsługę żądań HTTP z przeglądarki, przygotowanie danych dla widoków i zwracanie stron HTML. Autentykacja oparta na cookies/sesji.

* **`HomeController`** [na podstawie `home_controller "/"` z pliku `mcv_model_scheme.txt`]
    * `GET /` lub `GET /Home/Index`: Wyświetla stronę główną.
    * `GET /Home/Privacy`: Wyświetla politykę prywatności.
* **`AccountController`** [na podstawie `login_controller "/login"`, `register_controller "/register"`, `account_controller "account/{user_id}"` z pliku `mcv_model_scheme.txt`]
    * `GET /Account/Login`: Wyświetla formularz logowania.
    * `POST /Account/Login`: Przetwarza dane logowania.
    * `POST /Account/Logout`: Wylogowuje użytkownika.
    * `GET /Account/AccessDenied`: Strona informująca o braku dostępu.
    * `GET /Account/Profile`: (Zalogowany użytkownik) Wyświetla profil użytkownika.
    * `POST /Account/Profile`: (Zalogowany użytkownik) Przetwarza aktualizację profilu.
* **`PackagesController` (MVC)** [na podstawie `order_controller "/order"` z pliku `mcv_model_scheme.txt`]
    * `GET /Packages`: (Wymaga autoryzacji) Wyświetla listę przesyłek (dostosowaną do roli użytkownika).
    * `GET /Packages/Track`: (Publiczne) Strona z formularzem do wpisania numeru śledzenia.
    * `GET /Packages/Details/{trackingNumber}`: Wyświetla szczegóły przesyłki i jej historię.
    * `GET /Packages/Create`: (Wymaga roli Admin/Uprawnionej) Formularz tworzenia nowej przesyłki.
    * `POST /Packages/Create`: (Wymaga roli Admin/Uprawnionej) Przetwarza tworzenie nowej przesyłki.
    * `GET /Packages/Edit/{packageId:int}`: (Wymaga roli Admin/Uprawnionej) Formularz edycji przesyłki.
    * `POST /Packages/Edit/{packageId:int}`: (Wymaga roli Admin/Uprawnionej) Przetwarza edycję przesyłki.
    * `GET /Packages/UpdateStatus/{packageId:int}`: (Wymaga roli Courier/Admin) Formularz aktualizacji statusu przesyłki.
    * `POST /Packages/UpdateStatus/{packageId:int}`: (Wymaga roli Courier/Admin) Przetwarza aktualizację statusu.
* **`AdminController`**
    * `GET /Admin/Dashboard`: Wyświetla panel administratora.
    * `GET /Admin/Users`: Wyświetla listę użytkowników systemu.
    * `GET /Admin/Users/Create`: Formularz tworzenia nowego użytkownika przez admina.
    * `POST /Admin/Users/Create`: Przetwarza tworzenie nowego użytkownika.
    * `GET /Admin/Users/Edit/{userId:int}`: Formularz edycji użytkownika.
    * `POST /Admin/Users/Edit/{userId:int}`: Przetwarza edycję użytkownika.
    * `POST /Admin/Users/Delete/{userId:int}`: Usuwa użytkownika.
    * `GET /Admin/StatusDefinitions`: Zarządzanie definicjami statusów (lista).
    * `GET /Admin/StatusDefinitions/Create`: Formularz tworzenia definicji statusu.
    * `POST /Admin/StatusDefinitions/Create`: Przetwarza tworzenie definicji statusu.
    * `GET /Admin/StatusDefinitions/Edit/{statusId:int}`: Formularz edycji definicji statusu.
    * `POST /Admin/StatusDefinitions/Edit/{statusId:int}`: Przetwarza edycję definicji statusu.
* **`CourierController`** [na podstawie `courier_controller "/courier"` z pliku `mcv_model_scheme.txt`]
    * `GET /Courier/Dashboard`: Panel kuriera (np. lista przypisanych przesyłek).
    * `GET /Courier/MyPackages`: Lista przesyłek przypisanych do zalogowanego kuriera.

### Kontrolery REST API (Prefiks `/api`)
Odpowiedzialne za obsługę żądań od klientów API (np. programów konsolowych, aplikacji mobilnych, frontendu SPA). Zwracają dane w formacie JSON. Autentykacja oparta o `ApiKey` przesyłany w nagłówku żądania.

* **`PackagesApiController` (`[Route("api/packages")]`)**
    * `GET /`: Pobiera listę przesyłek z opcjami filtrowania (np. `?status=DELIVERED&senderId=123`).
    * `GET /{trackingNumber}`: Pobiera szczegóły konkretnej przesyłki po numerze śledzenia.
    * `POST /`: Tworzy nową przesyłkę (oczekuje `CreatePackageDto` w ciele żądania).
    * `PUT /{packageId:int}`: Aktualizuje istniejącą przesyłkę (oczekuje `UpdatePackageDto`).
    * `DELETE /{packageId:int}`: (Tylko Admin) Usuwa przesyłkę.
    * `GET /{trackingNumber}/history`: Pobiera historię statusów dla danej przesyłki.
    * `POST /{trackingNumber}/status`: Dodaje nowy wpis do historii statusów przesyłki (oczekuje `AddPackageStatusDto`).
* **`UsersApiController` (`[Route("api/users")]`)**
    * `GET /`: (Tylko Admin) Pobiera listę wszystkich użytkowników.
    * `GET /{userId:int}`: (Tylko Admin) Pobiera szczegóły konkretnego użytkownika.
    * `POST /`: (Tylko Admin) Tworzy nowego użytkownika (oczekuje `CreateUserDto`).
    * `PUT /{userId:int}`: (Tylko Admin lub użytkownik edytujący własne dane) Aktualizuje użytkownika.
    * `DELETE /{userId:int}`: (Tylko Admin) Usuwa użytkownika.
    * `GET /me/apikey`: (Zalogowany przez API użytkownik) Pozwala użytkownikowi odświeżyć/pobrać swój klucz API.
* **`StatusDefinitionsApiController` (`[Route("api/statusdefinitions")]`)**
    * `GET /`: Pobiera wszystkie dostępne definicje statusów.
    * `POST /`: (Tylko Admin) Tworzy nową definicję statusu.
    * `PUT /{statusId:int}`: (Tylko Admin) Aktualizuje istniejącą definicję statusu.

---

## 📄 Strony HTML (Widoki .cshtml)

Pliki Razor renderujące interfejs użytkownika.

* **Shared (Udostępnione):**
    * `_Layout.cshtml`: Główny szablon strony (menu nawigacyjne, stopka).
    * `_LoginPartial.cshtml`: Fragment wyświetlający linki logowania/rejestracji lub informacje o zalogowanym użytkowniku.
    * `Error.cshtml`: Ogólna strona błędu.
* **Home (Strona Główna):**
    * `Index.cshtml`: Główna strona aplikacji.
    * `Privacy.cshtml`: Polityka prywatności.
* **Account (Konto Użytkownika):**
    * `Login.cshtml`: Formularz logowania.
    * `Profile.cshtml`: Widok i edycja profilu zalogowanego użytkownika.
    * `AccessDenied.cshtml`: Strona błędu braku uprawnień.
* **Packages (Przesyłki - MVC):**
    * `Index.cshtml`: Lista przesyłek (z filtrami i paginacją).
    * `Track.cshtml`: Publiczny formularz do śledzenia przesyłki.
    * `Details.cshtml`: Szczegółowy widok przesyłki wraz z historią statusów.
    * `Create.cshtml`: Formularz tworzenia nowej przesyłki.
    * `Edit.cshtml`: Formularz edycji istniejącej przesyłki.
    * `UpdateStatus.cshtml`: Formularz do aktualizacji statusu przesyłki przez kuriera/admina.
* **Admin (Panel Administratora):**
    * `Dashboard.cshtml`: Główny panel administratora ze statystykami i skrótami.
    * `Users.cshtml`: Lista użytkowników systemu z opcjami zarządzania.
    * `CreateUser.cshtml`: Formularz tworzenia nowego użytkownika.
    * `EditUser.cshtml`: Formularz edycji danych użytkownika.
    * `StatusDefinitions.cshtml`: Lista definicji statusów z opcjami zarządzania.
    * `CreateEditStatusDefinition.cshtml`: Formularz do tworzenia/edycji definicji statusu.
* **Courier (Panel Kuriera):**
    * `Dashboard.cshtml`: Główny panel kuriera.
    * `MyPackages.cshtml`: Lista przesyłek przypisanych do zalogowanego kuriera.

---

## ✨ Dodatkowe Wskazówki Profesjonalne

* **Walidacja Danych:** Stosuj walidację po stronie klienta (JavaScript) i serwera (DataAnnotations w ViewModels/DTOs, dodatkowa walidacja w serwisach).
* **Obsługa Błędów i Logowanie:** Zaimplementuj globalny middleware do obsługi wyjątków. Używaj strukturalnego logowania (np. za pomocą Serilog) do zapisywania ważnych zdarzeń i błędów.
* **Bezpieczeństwo:** Zawsze używaj HTTPS w środowisku produkcyjnym. Implementuj zabezpieczenia przed atakami XSS, CSRF (ASP.NET Core oferuje wbudowane mechanizmy). Dbaj o poprawną konfigurację `[Authorize]` z rolami dla MVC oraz bezpieczną autentykację kluczem API dla REST.
* **Testowanie:** Pisz testy jednostkowe dla logiki w serwisach oraz testy integracyjne dla kontrolerów i interakcji z bazą danych.
* **Interfejs Użytkownika (UX/UI):** Zadbaj o przejrzystość, intuicyjność nawigacji oraz responsywność interfejsu (RWD - Responsive Web Design).
* **Dokumentacja API:** Dla REST API rozważ użycie narzędzi takich jak Swagger/OpenAPI (Swashbuckle w ASP.NET Core) do automatycznego generowania interaktywnej dokumentacji.
* **Asynchroniczność:** Wykorzystuj `async/await` dla wszystkich operacji I/O-bound (np. zapytania do bazy danych, wywołania zewnętrznych API), aby nie blokować wątków i zwiększyć skalowalność aplikacji.
* **Konfiguracja:** Przechowuj dane konfiguracyjne (np. connection string, ustawienia API) w `appsettings.json` i korzystaj z mechanizmu konfiguracji ASP.NET Core.
* **Komentarze i Dokumentacja Kodu:** Pisz czytelne komentarze, zwłaszcza dla publicznych metod w serwisach i API. XML documentation comments mogą być użyte do generowania dokumentacji.
