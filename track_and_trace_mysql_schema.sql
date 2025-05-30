-- Usunięcie bazy danych, jeśli istnieje, aby zapewnić czysty start
DROP DATABASE IF EXISTS `trackandtrace`;

-- Utworzenie nowej bazy danych
CREATE DATABASE IF NOT EXISTS `trackandtrace`;

-- Wybór bazy danych do dalszych operacji
USE `trackandtrace`;

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
    `AddressId` INT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`AddressId`) REFERENCES `Addresses`(`AddressId`)
);

-- 3. Tabela Definicji Statusów
CREATE TABLE IF NOT EXISTS `StatusDefinitions` (
    `StatusId` INT AUTO_INCREMENT PRIMARY KEY,
    `Name` VARCHAR(32) UNIQUE NOT NULL,
    `Description` VARCHAR(255) NOT NULL
);

-- 4. Tabela Paczek
CREATE TABLE IF NOT EXISTS `Packages` (
    `PackageId` INT AUTO_INCREMENT PRIMARY KEY,
    `TrackingNumber` VARCHAR(255) UNIQUE NOT NULL,
    `SenderUserId` INT NOT NULL,
    `RecipientUserId` INT NOT NULL,
    `AssignedCourierId` INT NULL,
    `PackageSize` VARCHAR(50) NOT NULL,
    `WeightInKg` DECIMAL(8, 2) NULL,
    `Notes` TEXT NULL,
    `OriginAddressId` INT NOT NULL,
    `DestinationAddressId` INT NOT NULL,
    `SubmissionDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `DeliveryDate` DATETIME NULL,
    `StatusId` INT NOT NULL,
    `Longitude` DECIMAL(10, 7) NULL,
    `Latitude` DECIMAL(10, 7) NULL,
    FOREIGN KEY (`SenderUserId`) REFERENCES `Users`(`UserId`),
    FOREIGN KEY (`RecipientUserId`) REFERENCES `Users`(`UserId`),
    FOREIGN KEY (`AssignedCourierId`) REFERENCES `Users`(`UserId`),
    FOREIGN KEY (`OriginAddressId`) REFERENCES `Addresses`(`AddressId`),
    FOREIGN KEY (`DestinationAddressId`) REFERENCES `Addresses`(`AddressId`),
    FOREIGN KEY (`StatusId`) REFERENCES `StatusDefinitions`(`StatusId`)
);

-- 5. Tabela Historii Paczek
CREATE TABLE IF NOT EXISTS `PackageHistory` (
    `PackageHistoryId` INT AUTO_INCREMENT PRIMARY KEY,
    `PackageId` INT NOT NULL,
    `StatusId` INT NOT NULL,
    `Timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Longitude` DECIMAL(10, 7) NULL,
    `Latitude` DECIMAL(10, 7) NULL,
    FOREIGN KEY (`PackageId`) REFERENCES `Packages`(`PackageId`),
    FOREIGN KEY (`StatusId`) REFERENCES `StatusDefinitions`(`StatusId`)
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
CREATE INDEX idx_packages_originaddressid ON Packages (OriginAddressId);
CREATE INDEX idx_packages_destinationaddressid ON Packages (DestinationAddressId);
CREATE INDEX idx_packages_statusid ON Packages (StatusId);


-- Indeksy dla tabeli PackageHistory
-- PackageHistoryId (PRIMARY KEY) jest automatycznie indeksowany.
-- PackageId (FOREIGN KEY) jest kluczowy, ponieważ historia jest często wyszukiwana po ID paczki.
CREATE INDEX idx_packagehistory_packageid ON PackageHistory (PackageId);
CREATE INDEX idx_packagehistory_statusid ON PackageHistory (StatusId);

-- Insert Sample Addresses
INSERT IGNORE INTO `Addresses` (`Street`, `City`, `ZipCode`, `Country`) VALUES
('Rynek Główny 1', 'Kraków', '31-042', 'Polska'),
('ul. Piękna 5A', 'Warszawa', '00-500', 'Polska'),
('Aleje Jerozolimskie 100', 'Warszawa', '00-800', 'Polska'),
('ul. Sezamkowa 12', 'Gdańsk', '80-100', 'Polska');

-- Insert Sample Users
INSERT IGNORE INTO `Users` (`Username`, `Email`, `Password`, `ApiKey`, `FirstName`, `LastName`, `Role`, `Birthday`, `AddressId`) VALUES
('admin', 'admin@example.com', 'AQAAAAIAAYagAAAAENXNElv7OpwB8US60TqyEx5s6ZH+M2FWjH8W6MzJfs8k4pF1NfzACrMhXTSggOhWEg==', 'ABL+sYvvtiKRcMa4kDn/JhPyG4Q+Mme3BvgW7Y3HsGs=', 'Admin', 'Admin', 'admin', '1985-01-15', (SELECT AddressId FROM Addresses WHERE Street = 'Rynek Główny 1' AND City = 'Kraków')),
('jan_kowalski', 'jan.kowalski@example.com', 'AQAAAAIAAYagAAAAEMIK/OUZ6D47rr3EFZZhLeq9HgVuMb523ZypwdJjVbGu00GlUKznE/byUCZeq9Ejdw==', 'Mwc4lORruM40rwGU6hjY27G2wmWjk3rBbyOWg/1M1is=', 'Jan', 'Kowalski', 'user', '1990-03-20', (SELECT AddressId FROM Addresses WHERE Street = 'ul. Piękna 5A' AND City = 'Warszawa')),
('anna_limanowska', 'anna.limanowska@example.com', 'AQAAAAIAAYagAAAAEI1r7cAZaMnLTH3yCUTk1yGh6O5+NXaRys6HgV601qLP71BrlGxxLAsW3ZfICFd2Nw==', 'RsnlzH8zYnR3uYyJlnGO60d7aHGYkeau62ggBytIgDA=', 'Anna', 'Limanowska', 'user', '1992-07-10', (SELECT AddressId FROM Addresses WHERE Street = 'Aleje Jerozolimskie 100' AND City = 'Warszawa')),
('kurier_waw', 'kurier@example.com', 'AQAAAAIAAYagAAAAEA6YpkaV8cGpvYSiXHcZrji1/cfHDq18fLefHPD6evHgaL+S1bDS9JL73RjzMhgT4g==', '1jum3j8D8wsk0y0qLSPo3CMqrI0AP4lkZIBPIq3Kds4=', 'Karol', 'Kurierski', 'courier', '1998-12-12', (SELECT AddressId FROM Addresses WHERE Street = 'Aleje Jerozolimskie 100' AND City = 'Warszawa'));

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
-- Uwaga: W
