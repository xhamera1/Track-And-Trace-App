-- 1. Tabela Adresów
CREATE TABLE IF NOT EXISTS `Addresses` (
    `AddressId` INT AUTO_INCREMENT PRIMARY KEY,
    `Street` VARCHAR(255) NOT NULL,
    `City` VARCHAR(100) NOT NULL,
    `ZipCode` VARCHAR(10) NOT NULL,
    `Country` VARCHAR(100) NOT NULL,
    UNIQUE KEY `uq_address` (`Street`, `City`, `ZipCode`, `Country`)
);

-- 2. Tabela Użytkowników
CREATE TABLE IF NOT EXISTS `Users` (
    `UserId` INT AUTO_INCREMENT PRIMARY KEY,
    `Username` VARCHAR(32) UNIQUE NOT NULL,
    `Email` VARCHAR(128) UNIQUE NOT NULL,
    `Password` VARCHAR(255) NOT NULL,
    `ApiKey` CHAR(44) NULL,
    `FirstName` VARCHAR(100) NULL,
    `LastName` VARCHAR(100) NULL,
    `Role` ENUM('user', 'admin', 'courier') NOT NULL,
    `Birthday` DATE NULL,
    `AddressId` INT NULL REFERENCES `Addresses`(`AddressId`),
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. Tabela Definicji Statusów (niewielka zmiana)
CREATE TABLE IF NOT EXISTS `StatusDefinitions` (
    `StatusId` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(32) UNIQUE NOT NULL,
    `Description` VARCHAR(255) NOT NULL
);

-- 4. Tabela Paczek
CREATE TABLE IF NOT EXISTS `Packages` (
    `PackageId` INT AUTO_INCREMENT PRIMARY KEY,
    `TrackingNumber` VARCHAR(255) UNIQUE NOT NULL,
    `SenderUserId` INT NOT NULL REFERENCES `Users`(`UserId`),
    `RecipientUserId` INT NOT NULL REFERENCES `Users`(`UserId`),
    `AssignedCourierId` INT NULL REFERENCES `Users`(`UserId`),
    `PackageSize` VARCHAR(50) NOT NULL,
    `WeightInKg` DECIMAL(8, 2) NULL,
    `Notes` TEXT NULL,
    `OriginAddressId` INT NOT NULL REFERENCES `Addresses`(`AddressId`),
    `DestinationAddressId` INT NOT NULL REFERENCES `Addresses`(`AddressId`),
    `SubmissionDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `DeliveryDate` DATETIME NULL,
    `StatusId` INT NOT NULL REFERENCES `StatusDefinitions`(`StatusId`),
    `Longitude` DECIMAL(10, 7) NULL,
    `Latitude` DECIMAL(10, 7) NULL
);

-- 5. Tabela Historii Paczek
CREATE TABLE IF NOT EXISTS `PackageHistory` (
    `PackageHistoryId` INT AUTO_INCREMENT PRIMARY KEY,
    `PackageId` INT NOT NULL REFERENCES `Packages`(`PackageId`),
    `StatusId` INT NOT NULL REFERENCES `StatusDefinitions`(`StatusId`),
    `Timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Longitude` DECIMAL(10, 7) NULL,
    `Latitude` DECIMAL(10, 7) NULL
);

-- Indeksy dla tabeli Users
-- UserId (PRIMARY KEY) jest automatycznie indeksowany.
-- Username (UNIQUE) jest automatycznie indeksowany.
-- Email (UNIQUE) jest automatycznie indeksowany.
-- AddressId (FOREIGN KEY) powinien być indeksowany, ponieważ często będzie używany w JOINach z tabelą Addresses.
CREATE INDEX idx_users_addressid ON Users (AddressId);

-- Indeksy dla tabeli Packages
-- PackageId (PRIMARY KEY) jest automatycznie indeksowany.
-- TrackingNumber (UNIQUE) jest automatycznie indeksowany.
-- Klucze obce SenderUserId, RecipientUserId, AssignedCourierId
CREATE INDEX idx_packages_senderuserid ON Packages (SenderUserId);
CREATE INDEX idx_packages_recipientuserid ON Packages (RecipientUserId);
CREATE INDEX idx_packages_assignedcourierid ON Packages (AssignedCourierId);

-- Indeksy dla tabeli PackageHistory
-- PackageHistoryId (PRIMARY KEY) jest automatycznie indeksowany.
-- PackageId (FOREIGN KEY) jest kluczowy, ponieważ historia jest często wyszukiwana po ID paczki.
CREATE INDEX idx_packagehistory_packageid ON PackageHistory (PackageId);

-- Insert Sample Addresses
INSERT IGNORE INTO `Addresses` (`Street`, `City`, `ZipCode`, `Country`) VALUES
('Rynek Główny 1', 'Kraków', '31-042', 'Polska'),
('ul. Piękna 5A', 'Warszawa', '00-500', 'Polska'),
('Aleje Jerozolimskie 100', 'Warszawa', '00-800', 'Polska'),
('ul. Sezamkowa 12', 'Gdańsk', '80-100', 'Polska');

-- Insert Sample Users
INSERT IGNORE INTO `Users` (`Username`, `Email`, `Password`, `ApiKey`, `FirstName`, `LastName`, `Role`, `Birthday`, `AddressId`) VALUES
('admin', 'admin@example.com', 'AQAAAAIAAYagAAAAENXNElv7OpwB8US60TqyEx5s6ZH+M2FWjH8W6MzJfs8k4pF1NfzACrMhXTSggOhWEg==', 'ABL+sYvvtiKRcMa4kDn/JhPyG4Q+Mme3BvgW7Y3HsGs=', 'Admin', 'Admin', 'admin', '1985-01-15', 1),
('jan_kowalski', 'jan.kowalski@example.com', 'AQAAAAIAAYagAAAAEMIK/OUZ6D47rr3EFZZhLeq9HgVuMb523ZypwdJjVbGu00GlUKznE/byUCZeq9Ejdw==', 'Mwc4lORruM40rwGU6hjY27G2wmWjk3rBbyOWg/1M1is=', 'Jan', 'Kowalski', 'user', '1990-03-20', 2),
('anna_limanowska', 'anna.limanowska@example.com', 'AQAAAAIAAYagAAAAEI1r7cAZaMnLTH3yCUTk1yGh6O5+NXaRys6HgV601qLP71BrlGxxLAsW3ZfICFd2Nw==', 'RsnlzH8zYnR3uYyJlnGO60d7aHGYkeau62ggBytIgDA=', 'Anna', 'Limanowska', 'user', '1992-07-10', 3),
('kurier', 'kurier@example.com', 'AQAAAAIAAYagAAAAEA6YpkaV8cGpvYSiXHcZrji1/cfHDq18fLefHPD6evHgaL+S1bDS9JL73RjzMhgT4g==', '1jum3j8D8wsk0y0qLSPo3CMqrI0AP4lkZIBPIq3Kds4=', 'kurier', 'kurier', 'courier', '1998-12-12', 3);

-- Insert Status Definitions
INSERT IGNORE INTO `StatusDefinitions` (`Name`, `Description`) VALUES
('New Order', 'Paczka została zgłoszona do wysyłki.'),
('In Preparation', 'Nadawca przygotowuje paczkę do nadania.'),
('Sent', 'Paczka została nadana i jest w drodze do odbiorcy.'),
('In Delivery', 'Kurier lub dostawca jest w trakcie doręczania paczki.'),
('Delivered', 'Paczka została pomyślnie dostarczona odbiorcy.'),
('Canceled', 'Zamówienie na wysyłkę paczki zostało anulowane.'),
('Return', 'Paczka jest zwracana do nadawcy (np. z powodu nieodebrania).');

-- Insert a Sample Package
INSERT IGNORE INTO `Packages` (`TrackingNumber`, `SenderUserId`, `RecipientUserId`, `AssignedCourierId`, `PackageSize`, `WeightInKg`, `Notes`, `OriginAddressId`, `DestinationAddressId`, `DeliveryDate`, `StatusId`, `Longitude`, `Latitude`) VALUES
(
    'TRK123456789PL',
    (SELECT UserId FROM Users WHERE Username = 'jan_kowalski'),
    (SELECT UserId FROM Users WHERE Username = 'anna_limanowska'),
    (SELECT UserId FROM Users WHERE Username = 'admin_user'),
    'Medium',
    5.25,
    'Książki i drobne upominki',
    (SELECT AddressId FROM Addresses WHERE Street = 'ul. Piękna 5A'),
    (SELECT AddressId FROM Addresses WHERE Street = 'Aleje Jerozolimskie 100'),
    NULL,
    (SELECT StatusId FROM StatusDefinitions WHERE Name = 'New Order'),
    52.2297,
    21.0122
);

-- Insert Package History for the sample package
INSERT IGNORE INTO `PackageHistory` (`PackageId`, `StatusId`, `Longitude`, `Latitude`) VALUES
(
    (SELECT PackageId FROM Packages WHERE TrackingNumber = 'TRK123456789PL'),
    (SELECT StatusId FROM StatusDefinitions WHERE Name = 'New Order'),
    52.2297,
    21.0122
),
(
    (SELECT PackageId FROM Packages WHERE TrackingNumber = 'TRK123456789PL'),
    (SELECT StatusId FROM StatusDefinitions WHERE Name = 'In Preparation'),
    52.2297,
    21.0122
),
(
    (SELECT PackageId FROM Packages WHERE TrackingNumber = 'TRK123456789PL'),
    (SELECT StatusId FROM StatusDefinitions WHERE Name = 'Sent'),
    51.1079,
    17.0385
);
